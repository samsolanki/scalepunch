using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using ScalePunch.Combat;
using ScalePunch.Enemies;
using ScalePunch.Feedback;
using ScalePunch.Progression;
using ScalePunch.UI;
using ScalePunch.Weapons;

using Object = UnityEngine.Object;

namespace ScalePunch.EditorTools
{
    public static partial class RunSceneBuilder
    {
        class PrefabSet
        {
            public Projectile bullet;
            public XPGem gem;
            public Enemy zombie;
            public Enemy boss;
            public DamageNumber damageNumber;
            public DraftCard draftCard;
            public HealthBarWidget healthBar;
        }

        /// <summary>
        /// UI child. A plain `new GameObject` gets a Transform, and Transform does
        /// not cast to RectTransform - the RectTransform has to be requested at
        /// construction, before anything tries to lay the object out.
        /// </summary>
        static RectTransform UIChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>
        /// Saves and destroys the temporary instance. Every field is wired on the
        /// temp object BEFORE this is called - editing a saved prefab asset
        /// afterwards needs a separate save round-trip and is easy to get wrong.
        /// </summary>
        static T SavePrefab<T>(GameObject temp, string file) where T : Component
        {
            string path = $"{PrefabDir}/{file}.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return saved.GetComponent<T>();
        }

        /// <summary>
        /// Primitives ship with a collider. Everything here is registry-driven,
        /// not physics-driven, so those colliders are pure overhead - and on the
        /// player they would make it shove itself around.
        /// </summary>
        static GameObject Primitive(PrimitiveType type, string name, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static TextMeshProUGUI Label(GameObject go, float size, TextAlignmentOptions align, Color colour)
        {
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = align;
            text.color = colour;
            text.raycastTarget = false;
            return text;
        }

        static Sprite UiSprite() =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        static T LoadPrefab<T>(string file) where T : Component
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{file}.prefab");
            return go != null ? go.GetComponent<T>() : null;
        }

        /// <summary>See Reacquire(DataSet) - same reason, same fix.</summary>
        static void Reacquire(PrefabSet set)
        {
            set.bullet = Keep(LoadPrefab<Projectile>("Projectile_Bullet"), set.bullet);
            set.gem = Keep(LoadPrefab<XPGem>("XPGem"), set.gem);
            set.zombie = Keep(LoadPrefab<Enemy>("Zombie"), set.zombie);
            set.boss = Keep(LoadPrefab<Enemy>("Zombie_Boss"), set.boss);
            set.damageNumber = Keep(LoadPrefab<DamageNumber>("DamageNumber"), set.damageNumber);
            set.draftCard = Keep(LoadPrefab<DraftCard>("DraftCard"), set.draftCard);
            set.healthBar = Keep(LoadPrefab<HealthBarWidget>("HealthBar"), set.healthBar);
        }

        static PrefabSet BuildPrefabs(DataSet data, MaterialSet mats)
        {
            var set = new PrefabSet
            {
                bullet = BuildBullet(mats),
                gem = BuildGem(mats),
                zombie = BuildZombiePrefab(data, mats),
                boss = BuildBossPrefab(data, mats),
                damageNumber = BuildDamageNumber(),
                draftCard = BuildDraftCard(),
                healthBar = BuildHealthBar()
            };

            // All three zombie definitions share one prefab - they differ by stats
            // and scale only, which is genuinely all Shambler/Runner/Brute need.
            foreach (EnemyDefinition def in data.zombies)
            {
                def.prefab = set.zombie;
                EditorUtility.SetDirty(def);
            }

            // The boss has its own prefab because it carries BossController; the
            // three ordinary tiers still share one.
            if (data.boss != null)
            {
                data.boss.prefab = set.boss;
                EditorUtility.SetDirty(data.boss);
            }

            data.pistol.projectilePrefab = set.bullet;
            EditorUtility.SetDirty(data.pistol);

            return set;
        }

        static Projectile BuildBullet(MaterialSet mats)
        {
            GameObject go = Primitive(PrimitiveType.Sphere, "Projectile_Bullet", mats.bullet);

            // Stretched along Z, and Projectile re-aims the transform down its
            // travel direction every frame, so the round reads as a streak
            // pointing where it is going rather than as a floating ball.
            go.transform.localScale = new Vector3(0.09f, 0.09f, 0.44f);

            // A tracer is not decoration. At 30 m/s a round crosses the radius in
            // a third of a second; with no trail the player sees no shot at all,
            // only zombies falling over.
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.055f;
            trail.startWidth = 0.10f;
            trail.endWidth = 0f;
            trail.sharedMaterial = mats.line;
            trail.numCapVertices = 2;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            var projectile = go.AddComponent<Projectile>();
            Set(projectile, "trail", trail);

            return SavePrefab<Projectile>(go, "Projectile_Bullet");
        }

        static XPGem BuildGem(MaterialSet mats)
        {
            GameObject go = Primitive(PrimitiveType.Sphere, "XPGem", mats.gem);
            go.transform.localScale = Vector3.one * 0.3f;
            go.AddComponent<XPGem>();

            return SavePrefab<XPGem>(go, "XPGem");
        }

        static Enemy BuildZombiePrefab(DataSet data, MaterialSet mats)
        {
            // Root at ground level with the body as a raised child, exactly like
            // the player's turret. A bare capsule as the root sits centred on its
            // own origin, so spawning it at ground level buried half the zombie
            // below the floor - and put its transform a metre away from where the
            // bullets actually fly.
            var go = new GameObject("Zombie");

            GameObject body = Primitive(PrimitiveType.Capsule, "Body", mats.shambler);
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);

            // Order matters: Enemy declares [RequireComponent] for Health and
            // EnemyMovement, so adding it first would auto-add them unwired.
            var health = go.AddComponent<Health>();
            var movement = go.AddComponent<EnemyMovement>();
            var enemy = go.AddComponent<Enemy>();
            var flash = go.AddComponent<HitFlash>();
            var feedback = go.AddComponent<CombatFeedback>();
            var flinch = go.AddComponent<HitFlinch>();
            var bar = go.AddComponent<HealthBarTarget>();

            // The body, never the root: the root is what aims and walks, and
            // leaning it would steer the zombie off course.
            Set(flinch, "health", health);
            Set(flinch, "body", body.transform);
            Set(flinch, "tiltDegrees", 24f);
            Set(flinch, "critTiltDegrees", 38f);
            Set(flinch, "recoverSpeed", 7f);

            Set(bar, "health", health);
            Set(bar, "heightOffset", 2.1f);
            Set(bar, "width", 90f);
            Set(bar, "hideWhenFull", true);
            Set(bar, "priority", 0);

            Set(enemy, "health", health);
            Set(enemy, "movement", movement);

            Set(flash, "tuning", data.tuning);
            SetArray(flash, "renderers", new Object[] { body.GetComponent<Renderer>() });

            Set(feedback, "tuning", data.tuning);
            Set(feedback, "health", health);
            Set(feedback, "flash", flash);
            Set(feedback, "movement", movement);
            Set(feedback, "numberHeight", 1.7f);

            return SavePrefab<Enemy>(go, "Zombie");
        }

        /// <summary>
        /// The zombie prefab plus a BossController and its own material, saved
        /// separately so the pool can hand out a boss without every shambler
        /// carrying a boss brain it never uses.
        /// </summary>
        static Enemy BuildBossPrefab(DataSet data, MaterialSet mats)
        {
            var go = new GameObject("Zombie_Boss");

            GameObject body = Primitive(PrimitiveType.Capsule, "Body", mats.brute);
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);

            var health = go.AddComponent<Health>();
            var movement = go.AddComponent<EnemyMovement>();
            var enemy = go.AddComponent<Enemy>();
            var flash = go.AddComponent<HitFlash>();
            var feedback = go.AddComponent<CombatFeedback>();
            var flinch = go.AddComponent<HitFlinch>();
            var bar = go.AddComponent<HealthBarTarget>();
            var boss = go.AddComponent<BossController>();

            Set(flinch, "health", health);
            Set(flinch, "body", body.transform);
            // Half a normal zombie's lean: a boss that rocks as hard as a
            // shambler reads as light, however much HP it has.
            Set(flinch, "tiltDegrees", 10f);
            Set(flinch, "critTiltDegrees", 16f);
            Set(flinch, "recoverSpeed", 6f);

            Set(bar, "health", health);
            Set(bar, "heightOffset", 3.4f);
            Set(bar, "width", 200f);
            Set(bar, "hideWhenFull", false);
            // Above every other bar: at the boss wave the screen is full, and the
            // one bar that matters must survive the budget cut.
            Set(bar, "priority", 100);

            Set(enemy, "health", health);
            Set(enemy, "movement", movement);

            Set(flash, "tuning", data.tuning);
            SetArray(flash, "renderers", new Object[] { body.GetComponent<Renderer>() });

            Set(feedback, "tuning", data.tuning);
            Set(feedback, "health", health);
            Set(feedback, "flash", flash);
            Set(feedback, "movement", movement);
            Set(feedback, "numberHeight", 3.0f);

            Set(boss, "enemy", enemy);
            Set(boss, "movement", movement);
            Set(boss, "health", health);
            Set(boss, "body", body.transform);

            return SavePrefab<Enemy>(go, "Zombie_Boss");
        }

        static DamageNumber BuildDamageNumber()
        {
            var go = new GameObject("DamageNumber", typeof(RectTransform), typeof(CanvasGroup));
            ((RectTransform)go.transform).sizeDelta = new Vector2(220f, 70f);

            TextMeshProUGUI text = Label(go, 44f, TextAlignmentOptions.Center, Color.white);
            text.fontStyle = FontStyles.Bold;

            var number = go.AddComponent<DamageNumber>();
            Set(number, "label", text);

            return SavePrefab<DamageNumber>(go, "DamageNumber");
        }

        static HealthBarWidget BuildHealthBar()
        {
            var root = new GameObject("HealthBar", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(90f, 10f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            RectTransform backRect = UIChild("Background", root.transform);
            Stretch(backRect);
            var back = backRect.gameObject.AddComponent<Image>();
            back.sprite = UiSprite();
            back.type = Image.Type.Sliced;
            back.color = new Color(0.05f, 0.06f, 0.08f, 0.85f);
            back.raycastTarget = false;

            RectTransform fillRect = UIChild("Fill", root.transform);
            Stretch(fillRect);
            // A 2px inset so the dark background reads as an outline rather than
            // the fill sitting flush against the edge.
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);

            var fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = UiSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.color = new Color(0.35f, 0.85f, 0.4f);
            fill.raycastTarget = false;

            var widget = root.AddComponent<HealthBarWidget>();
            Set(widget, "root", rect);
            Set(widget, "background", back);
            Set(widget, "fill", fill);
            Set(widget, "group", root.GetComponent<CanvasGroup>());

            return SavePrefab<HealthBarWidget>(root, "HealthBar");
        }

        static DraftCard BuildDraftCard()
        {
            var root = new GameObject("DraftCard", typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta = new Vector2(300f, 420f);

            var background = root.AddComponent<Image>();
            background.color = new Color(0.13f, 0.14f, 0.18f, 0.98f);
            background.sprite = UiSprite();
            background.type = Image.Type.Sliced;

            var button = root.AddComponent<Button>();
            button.targetGraphic = background;

            var element = root.AddComponent<LayoutElement>();
            element.preferredWidth = 300f;
            element.preferredHeight = 420f;

            // A colour stripe along the top edge is the fastest read of active vs
            // passive, which is the first thing a player sorts three cards by.
            RectTransform stripeRect = UIChild("KindStripe", root.transform);
            stripeRect.anchorMin = new Vector2(0f, 1f);
            stripeRect.anchorMax = new Vector2(1f, 1f);
            stripeRect.pivot = new Vector2(0.5f, 1f);
            stripeRect.anchoredPosition = Vector2.zero;
            stripeRect.sizeDelta = new Vector2(0f, 12f);
            var stripe = stripeRect.gameObject.AddComponent<Image>();
            stripe.raycastTarget = false;

            RectTransform iconRect = UIChild("Icon", root.transform);
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -36f);
            iconRect.sizeDelta = new Vector2(110f, 110f);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.enabled = false;   // DraftCard re-enables it only when the ability has a sprite

            RectTransform nameRect = UIChild("NameLabel", root.transform);
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0f, -160f);
            nameRect.sizeDelta = new Vector2(-30f, 50f);
            TextMeshProUGUI nameLabel = Label(nameRect.gameObject, 30f, TextAlignmentOptions.Center, Color.white);
            nameLabel.fontStyle = FontStyles.Bold;

            RectTransform levelRect = UIChild("LevelLabel", root.transform);
            levelRect.anchorMin = new Vector2(0f, 1f);
            levelRect.anchorMax = new Vector2(1f, 1f);
            levelRect.pivot = new Vector2(0.5f, 1f);
            levelRect.anchoredPosition = new Vector2(0f, -212f);
            levelRect.sizeDelta = new Vector2(-30f, 36f);
            TextMeshProUGUI levelLabel = Label(levelRect.gameObject, 24f, TextAlignmentOptions.Center,
                                               new Color(0.65f, 0.72f, 0.80f));

            RectTransform descRect = UIChild("DescriptionLabel", root.transform);
            descRect.anchorMin = Vector2.zero;
            descRect.anchorMax = new Vector2(1f, 0f);
            descRect.pivot = new Vector2(0.5f, 0f);
            descRect.anchoredPosition = new Vector2(0f, 26f);
            descRect.sizeDelta = new Vector2(-40f, 140f);
            TextMeshProUGUI descLabel = Label(descRect.gameObject, 24f, TextAlignmentOptions.Top,
                                              new Color(0.85f, 0.88f, 0.92f));

            var card = root.AddComponent<DraftCard>();
            Set(card, "button", button);
            Set(card, "icon", icon);
            Set(card, "nameLabel", nameLabel);
            Set(card, "levelLabel", levelLabel);
            Set(card, "descriptionLabel", descLabel);
            Set(card, "kindStripe", stripe);

            return SavePrefab<DraftCard>(root, "DraftCard");
        }
    }
}
