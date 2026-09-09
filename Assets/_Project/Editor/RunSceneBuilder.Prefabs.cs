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
            public DamageNumber damageNumber;
            public DraftCard draftCard;
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

        static PrefabSet BuildPrefabs(DataSet data, MaterialSet mats)
        {
            var set = new PrefabSet
            {
                bullet = BuildBullet(mats),
                gem = BuildGem(mats),
                zombie = BuildZombiePrefab(data, mats),
                damageNumber = BuildDamageNumber(),
                draftCard = BuildDraftCard()
            };

            // All three zombie definitions share one prefab - they differ by stats
            // and scale only, which is genuinely all Shambler/Runner/Brute need.
            foreach (EnemyDefinition def in data.zombies)
            {
                def.prefab = set.zombie;
                EditorUtility.SetDirty(def);
            }

            data.pistol.projectilePrefab = set.bullet;
            EditorUtility.SetDirty(data.pistol);

            return set;
        }

        static Projectile BuildBullet(MaterialSet mats)
        {
            GameObject go = Primitive(PrimitiveType.Sphere, "Projectile_Bullet", mats.bullet);
            go.transform.localScale = Vector3.one * 0.18f;

            // A tracer is not decoration. At 30 m/s a round crosses the radius in
            // a third of a second; with no trail the player sees no shot at all,
            // only zombies falling over.
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.09f;
            trail.startWidth = 0.14f;
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
            GameObject go = Primitive(PrimitiveType.Capsule, "Zombie", mats.shambler);

            // Order matters: Enemy declares [RequireComponent] for Health and
            // EnemyMovement, so adding it first would auto-add them unwired.
            var health = go.AddComponent<Health>();
            var movement = go.AddComponent<EnemyMovement>();
            var enemy = go.AddComponent<Enemy>();
            var flash = go.AddComponent<HitFlash>();
            var feedback = go.AddComponent<CombatFeedback>();

            Set(enemy, "health", health);
            Set(enemy, "movement", movement);

            Set(flash, "tuning", data.tuning);
            SetArray(flash, "renderers", new Object[] { go.GetComponent<Renderer>() });

            Set(feedback, "tuning", data.tuning);
            Set(feedback, "health", health);
            Set(feedback, "flash", flash);
            Set(feedback, "movement", movement);
            Set(feedback, "numberHeight", 1.7f);

            return SavePrefab<Enemy>(go, "Zombie");
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

        static DraftCard BuildDraftCard()
        {
            var root = new GameObject("DraftCard", typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta = new Vector2(300f, 420f);

            var background = root.AddComponent<Image>();
            background.color = new Color(0.13f, 0.14f, 0.18f, 0.98f);
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            background.type = Image.Type.Sliced;

            var button = root.AddComponent<Button>();
            button.targetGraphic = background;

            var element = root.AddComponent<LayoutElement>();
            element.preferredWidth = 300f;
            element.preferredHeight = 420f;

            // A colour stripe along the top edge is the fastest read of active vs
            // passive, which is the first thing a player sorts three cards by.
            GameObject stripeGo = Child("KindStripe", root.transform);
            var stripeRect = (RectTransform)stripeGo.transform;
            stripeRect.anchorMin = new Vector2(0f, 1f);
            stripeRect.anchorMax = new Vector2(1f, 1f);
            stripeRect.pivot = new Vector2(0.5f, 1f);
            stripeRect.anchoredPosition = Vector2.zero;
            stripeRect.sizeDelta = new Vector2(0f, 12f);
            var stripe = stripeGo.AddComponent<Image>();
            stripe.raycastTarget = false;

            GameObject iconGo = Child("Icon", root.transform);
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -36f);
            iconRect.sizeDelta = new Vector2(110f, 110f);
            var icon = iconGo.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.enabled = false;   // DraftCard re-enables it only when the ability has a sprite

            GameObject nameGo = Child("NameLabel", root.transform);
            var nameRect = (RectTransform)nameGo.transform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0f, -160f);
            nameRect.sizeDelta = new Vector2(-30f, 50f);
            TextMeshProUGUI nameLabel = Label(nameGo, 30f, TextAlignmentOptions.Center, Color.white);
            nameLabel.fontStyle = FontStyles.Bold;

            GameObject levelGo = Child("LevelLabel", root.transform);
            var levelRect = (RectTransform)levelGo.transform;
            levelRect.anchorMin = new Vector2(0f, 1f);
            levelRect.anchorMax = new Vector2(1f, 1f);
            levelRect.pivot = new Vector2(0.5f, 1f);
            levelRect.anchoredPosition = new Vector2(0f, -212f);
            levelRect.sizeDelta = new Vector2(-30f, 36f);
            TextMeshProUGUI levelLabel = Label(levelGo, 24f, TextAlignmentOptions.Center,
                                               new Color(0.65f, 0.72f, 0.80f));

            GameObject descGo = Child("DescriptionLabel", root.transform);
            var descRect = (RectTransform)descGo.transform;
            descRect.anchorMin = Vector2.zero;
            descRect.anchorMax = new Vector2(1f, 0f);
            descRect.pivot = new Vector2(0.5f, 0f);
            descRect.anchoredPosition = new Vector2(0f, 26f);
            descRect.sizeDelta = new Vector2(-40f, 140f);
            TextMeshProUGUI descLabel = Label(descGo, 24f, TextAlignmentOptions.Top,
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
