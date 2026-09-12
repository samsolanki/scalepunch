using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using ScalePunch.Abilities;
using ScalePunch.Combat;
using ScalePunch.Core;
using ScalePunch.Enemies;
using ScalePunch.Feedback;
using ScalePunch.Player;
using ScalePunch.Progression;
using ScalePunch.Stages;
using ScalePunch.Run;
using ScalePunch.Save;
using ScalePunch.UI;
using ScalePunch.Weapons;

using Object = UnityEngine.Object;

namespace ScalePunch.EditorTools
{
    public static partial class RunSceneBuilder
    {
        const float SpawnRadius = 16f;

        /// <summary>
        /// Populates a scene that Build() has already created. The scene is NOT
        /// created here: doing so would unload every asset loaded before this
        /// call, and they would all wire into the scene as null.
        /// </summary>
        static void BuildScene(Scene scene, DataSet data, PrefabSet prefabs, MaterialSet mats)
        {
            BuildEnvironment(mats);

            GameObject player = BuildPlayer(data, mats, out AutoShoot weapon, out PlayerStats stats,
                                            out Health health, out LevelSystem levels,
                                            out AbilitySystem abilities, out DraftController draft);

            Camera camera = BuildCamera(data);

            Canvas canvas = BuildCanvas(out RectTransform damageNumberRoot,
                                        out RectTransform healthBarRoot);
            BuildDraftUI(canvas, draft, stats, prefabs);

            RunController run = BuildSystems(data, prefabs, player.transform, stats, levels,
                                             health, camera, damageNumberRoot, healthBarRoot);

            BuildHUD(canvas, levels, health, weapon, run, abilities);
            BuildRunEndUI(canvas, run);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            RegisterInBuildSettings();
        }

        // ---------------------------------------------------------- environment

        static void BuildEnvironment(MaterialSet mats)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = Vector3.one * 10f;   // 100 x 100 m
            ground.GetComponent<Renderer>().sharedMaterial = mats.ground;

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.30f, 0.36f);
        }

        // --------------------------------------------------------------- player

        static GameObject BuildPlayer(DataSet data, MaterialSet mats,
                                      out AutoShoot weapon, out PlayerStats stats,
                                      out Health health, out LevelSystem levels,
                                      out AbilitySystem abilities, out DraftController draft)
        {
            var player = new GameObject("Player");

            health = player.AddComponent<Health>();
            stats = player.AddComponent<PlayerStats>();
            weapon = player.AddComponent<AutoShoot>();
            levels = player.AddComponent<LevelSystem>();
            abilities = player.AddComponent<AbilitySystem>();
            draft = player.AddComponent<DraftController>();

            // Turret is a child so AutoShoot can slew it to face a target without
            // spinning the range ring with it.
            GameObject turret = Primitive(PrimitiveType.Capsule, "Turret", mats.turret);
            turret.transform.SetParent(player.transform, false);
            turret.transform.localPosition = new Vector3(0f, 1f, 0f);

            // A capsule is rotationally symmetric, so AutoShoot slewing it to face
            // a target was completely invisible - the turret was already aiming
            // correctly and looked identical from every angle. The barrel is what
            // makes the aim readable.
            GameObject barrel = Primitive(PrimitiveType.Cube, "Barrel", mats.barrel);
            barrel.transform.SetParent(turret.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.15f, 0.62f);
            barrel.transform.localScale = new Vector3(0.16f, 0.16f, 1.05f);

            GameObject muzzle = Child("Muzzle", turret.transform);
            muzzle.transform.localPosition = new Vector3(0f, 0.15f, 1.16f);

            // Flare sits at the barrel tip and is switched on for a few frames per
            // shot by MuzzleFlash.
            GameObject flare = Primitive(PrimitiveType.Sphere, "MuzzleFlash", mats.flash);
            flare.transform.SetParent(muzzle.transform, false);
            flare.transform.localScale = Vector3.one * 0.22f;

            var flareLight = flare.AddComponent<Light>();
            flareLight.type = LightType.Point;
            flareLight.range = 5f;
            flareLight.color = new Color(1f, 0.86f, 0.55f);
            flareLight.intensity = 0f;
            flareLight.shadows = LightShadows.None;

            var muzzleFlash = flare.AddComponent<MuzzleFlash>();

            GameObject ring = Child("RangeRing", player.transform);
            var line = ring.AddComponent<LineRenderer>();
            line.sharedMaterial = mats.line;
            line.widthMultiplier = 0.09f;
            line.loop = true;
            line.useWorldSpace = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startColor = new Color(0.25f, 1f, 0.55f, 0.55f);
            line.endColor = line.startColor;

            var indicator = ring.AddComponent<RangeIndicator>();

            // The same component zombies use. The HUD bar is for glancing at;
            // this one is where the player's eyes already are when they get hit.
            var bar = player.AddComponent<HealthBarTarget>();
            Set(bar, "health", health);
            Set(bar, "heightOffset", 2.7f);
            Set(bar, "width", 130f);
            Set(bar, "hideWhenFull", true);
            Set(bar, "priority", 10);

            Set(health, "maxHP", 100f);

            Set(stats, "health", health);

            Set(weapon, "stats", stats);
            Set(weapon, "weapon", data.pistol);
            Set(weapon, "tuning", data.tuning);
            Set(weapon, "turret", turret.transform);
            Set(weapon, "muzzle", muzzle.transform);
            Set(weapon, "priority", AutoShoot.TargetPriority.Closest);
            Set(weapon, "targetStickiness", 1f);
            Set(weapon, "minFiringArc", 1.5f);
            Set(weapon, "maxFiringArc", 25f);
            Set(weapon, "maxLeadSeconds", 1f);
            Set(weapon, "avoidOverkill", true);

            Set(levels, "curve", data.curve);

            Set(abilities, "stats", stats);
            Set(abilities, "weapon", weapon);

            Set(draft, "library", data.library);
            Set(draft, "abilities", abilities);
            Set(draft, "levels", levels);
            Set(draft, "stats", stats);

            Set(indicator, "weapon", weapon);
            Set(indicator, "line", line);

            Set(muzzleFlash, "weapon", weapon);
            Set(muzzleFlash, "flare", flare.GetComponent<Renderer>());
            Set(muzzleFlash, "burst", flareLight);

            return player;
        }

        // --------------------------------------------------------------- camera

        static Camera BuildCamera(DataSet data)
        {
            // CameraShake writes localPosition and zeroes it when idle, so the
            // camera MUST hang off a positioned rig. A root camera would be
            // snapped to the world origin on the first idle LateUpdate.
            var rig = new GameObject("CameraRig");
            rig.transform.SetPositionAndRotation(new Vector3(0f, 19f, -13f),
                                                 Quaternion.Euler(55f, 0f, 0f));

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(rig.transform, false);
            cameraGo.transform.localPosition = Vector3.zero;
            cameraGo.transform.localRotation = Quaternion.identity;

            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);

            cameraGo.AddComponent<AudioListener>();

            var shake = cameraGo.AddComponent<CameraShake>();
            Set(shake, "tuning", data.tuning);

            return camera;
        }

        // ------------------------------------------------------------------ UI

        static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static Canvas BuildCanvas(out RectTransform damageNumberRoot,
                                  out RectTransform healthBarRoot)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Health bars are created first so they sit UNDER the damage
            // numbers in the draw order - a number hidden behind a bar is the
            // one piece of feedback the player is actually reading.
            var bars = new GameObject("HealthBars", typeof(RectTransform));
            bars.transform.SetParent(canvasGo.transform, false);
            healthBarRoot = Stretch((RectTransform)bars.transform);

            var numbers = new GameObject("DamageNumbers", typeof(RectTransform));
            numbers.transform.SetParent(canvasGo.transform, false);
            damageNumberRoot = Stretch((RectTransform)numbers.transform);

            // The project is set to the new Input System only (activeInputHandler
            // 1), so StandaloneInputModule would silently receive nothing.
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();

            return canvas;
        }

        static Image Bar(string name, Transform parent, Vector2 anchor, Vector2 position,
                         Vector2 size, Color background, Color fill, out Image fillImage)
        {
            var backGo = new GameObject(name, typeof(RectTransform));
            backGo.transform.SetParent(parent, false);
            Place((RectTransform)backGo.transform, anchor, position, size);

            var back = backGo.AddComponent<Image>();
            back.color = background;
            back.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(backGo.transform, false);
            Stretch((RectTransform)fillGo.transform);

            fillImage = fillGo.AddComponent<Image>();
            fillImage.color = fill;
            fillImage.raycastTarget = false;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;

            // Image.Type.Filled needs a sprite to fill; without one it renders as
            // a plain quad and fillAmount does nothing.
            Sprite unit = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            back.sprite = unit;
            fillImage.sprite = unit;

            return back;
        }

        static TextMeshProUGUI HudText(string name, Transform parent, Vector2 anchor,
                                       Vector2 position, Vector2 size, float fontSize,
                                       TextAlignmentOptions align, string initial)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, anchor, position, size);

            TextMeshProUGUI text = Label(go, fontSize, align, Color.white);
            text.text = initial;
            return text;
        }

        static void BuildHUD(Canvas canvas, LevelSystem levels, Health health, AutoShoot weapon,
                             RunController run, AbilitySystem abilities)
        {
            var hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)hudGo.transform);
            Transform hud = hudGo.transform;

            // ------------------------------------------------------------ top

            BuildLevelTrack(hud, out Image xpFill, out TextMeshProUGUI levelLabel,
                            out TextMeshProUGUI xpLabel, out TextMeshProUGUI killsLabel);

            TextMeshProUGUI timerLabel = HudText("TimerLabel", hud, new Vector2(0.5f, 1f),
                new Vector2(0f, -104f), new Vector2(300f, 44f), 28f, TextAlignmentOptions.Center, "0:00");
            timerLabel.color = new Color(0.62f, 0.65f, 0.74f);

            TextMeshProUGUI waveLabel = HudText("WaveLabel", hud, new Vector2(0.5f, 1f),
                new Vector2(0f, -146f), new Vector2(420f, 40f), 24f, TextAlignmentOptions.Center, "LEVEL 1/8");
            waveLabel.color = new Color(0.75f, 0.78f, 0.85f);

            GameObject bossBanner = BuildBossBanner(hud);

            // --------------------------------------------------------- bottom

            // One plate holding health, level and the booster row. Grouping them
            // is what makes the bottom edge read as a status bar rather than as
            // three unrelated widgets that happen to be near each other.
            RectTransform plate = UIChild("BottomBar", hud);
            plate.anchorMin = new Vector2(0f, 0f);
            plate.anchorMax = new Vector2(1f, 0f);
            plate.pivot = new Vector2(0.5f, 0f);
            plate.anchoredPosition = new Vector2(0f, 18f);
            plate.sizeDelta = new Vector2(-28f, 140f);

            var plateImage = plate.gameObject.AddComponent<Image>();
            plateImage.sprite = UiSprite();
            plateImage.type = Image.Type.Sliced;
            plateImage.color = new Color(0.16f, 0.14f, 0.24f, 0.95f);
            plateImage.raycastTarget = false;

            BuildHealthRow(plate, out Image healthFill, out Image healthDelayed, out TextMeshProUGUI healthLabel);
            BuildSlotRow(plate, abilities);

            var runHud = hudGo.AddComponent<RunHUD>();
            Set(runHud, "levels", levels);
            Set(runHud, "playerHealth", health);
            Set(runHud, "weapon", weapon);
            Set(runHud, "run", run);
            Set(runHud, "healthFill", healthFill);
            Set(runHud, "healthDelayedFill", healthDelayed);
            Set(runHud, "healthLabel", healthLabel);
            Set(runHud, "xpFill", xpFill);
            Set(runHud, "levelLabel", levelLabel);
            Set(runHud, "xpLabel", xpLabel);
            Set(runHud, "timerLabel", timerLabel);
            Set(runHud, "killsLabel", killsLabel);
            Set(runHud, "waveLabel", waveLabel);
            Set(runHud, "bossBanner", bossBanner);
            Set(runHud, "bossBannerSeconds", 2.5f);

            BuildPriorityButton(hud, weapon);
        }

        /// <summary>
        /// The run's whole shape in one bar: a level badge, kills toward the next
        /// level, and the running total.
        ///
        /// Top edge and full width because it is the only promise the run makes,
        /// and because one progression now drives both the draft and the spawn
        /// ramp — there is nothing else competing for the position.
        /// </summary>
        static void BuildLevelTrack(Transform hud, out Image fill, out TextMeshProUGUI levelLabel,
                                    out TextMeshProUGUI fractionLabel, out TextMeshProUGUI killsLabel)
        {
            RectTransform track = UIChild("LevelTrack", hud);
            track.anchorMin = new Vector2(0f, 1f);
            track.anchorMax = new Vector2(1f, 1f);
            track.pivot = new Vector2(0.5f, 1f);
            track.anchoredPosition = new Vector2(0f, -26f);
            track.sizeDelta = new Vector2(-160f, 52f);

            var back = track.gameObject.AddComponent<Image>();
            back.sprite = UiSprite();
            back.type = Image.Type.Sliced;
            back.color = new Color(0.07f, 0.06f, 0.11f, 0.95f);
            back.raycastTarget = false;

            fill = BarFill(track, "Fill", new Color(0.42f, 0.62f, 1f));

            RectTransform fractionRect = UIChild("Fraction", track);
            Stretch(fractionRect);
            fractionLabel = Label(fractionRect.gameObject, 26f, TextAlignmentOptions.Center, Color.white);
            fractionLabel.fontStyle = FontStyles.Bold;
            fractionLabel.text = "0 / 5";

            // Level badge overhangs the left end of the bar, so the number reads
            // as owning the track rather than floating beside it.
            RectTransform badge = UIChild("LevelBadge", track);
            Place(badge, new Vector2(0f, 0.5f), new Vector2(-14f, 0f), new Vector2(104f, 60f));
            var badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = UiSprite();
            badgeImage.type = Image.Type.Sliced;
            badgeImage.color = new Color(0.42f, 0.34f, 0.72f);
            badgeImage.raycastTarget = false;

            RectTransform levelRect = UIChild("LevelLabel", badge);
            Stretch(levelRect);
            levelLabel = Label(levelRect.gameObject, 30f, TextAlignmentOptions.Center, Color.white);
            levelLabel.fontStyle = FontStyles.Bold;
            levelLabel.text = "LV 1";

            killsLabel = KillBadge(hud);
        }

        /// <summary>Total kills, top-right, on its own pill so it reads at a glance
        /// against whatever is behind it.</summary>
        static TextMeshProUGUI KillBadge(Transform hud)
        {
            RectTransform badge = UIChild("KillBadge", hud);
            Place(badge, new Vector2(1f, 1f), new Vector2(-22f, -20f), new Vector2(146f, 60f));

            var back = badge.gameObject.AddComponent<Image>();
            back.sprite = UiSprite();
            back.type = Image.Type.Sliced;
            back.color = new Color(0.12f, 0.11f, 0.18f, 0.92f);
            back.raycastTarget = false;

            // Placeholder for a skull icon. A flat block still says "a count of
            // something lives here"; swap the sprite when art lands.
            RectTransform mark = UIChild("SkullMark", badge);
            Place(mark, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(34f, 34f));
            var markImage = mark.gameObject.AddComponent<Image>();
            markImage.sprite = UiSprite();
            markImage.type = Image.Type.Sliced;
            markImage.color = new Color(0.88f, 0.86f, 0.92f);
            markImage.raycastTarget = false;

            RectTransform textRect = UIChild("Count", badge);
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(52f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);

            TextMeshProUGUI text = Label(textRect.gameObject, 32f, TextAlignmentOptions.Left, Color.white);
            text.fontStyle = FontStyles.Bold;
            text.text = "0";
            return text;
        }

        /// <summary>
        /// Health: a heart, a track, a delayed drain layer, then the live fill on
        /// top. The delayed layer is what turns a big hit into a visible chunk
        /// instead of the bar simply being shorter than it was.
        /// </summary>
        static void BuildHealthRow(RectTransform plate, out Image fill, out Image delayed,
                                   out TextMeshProUGUI label)
        {
            RectTransform heart = UIChild("Heart", plate);
            Place(heart, new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(46f, 46f));
            var heartImage = heart.gameObject.AddComponent<Image>();
            heartImage.sprite = UiSprite();
            heartImage.type = Image.Type.Sliced;
            heartImage.color = new Color(0.92f, 0.24f, 0.28f);
            heartImage.raycastTarget = false;

            RectTransform track = UIChild("HealthTrack", plate);
            Place(track, new Vector2(0f, 1f), new Vector2(76f, -16f), new Vector2(470f, 46f));
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.sprite = UiSprite();
            trackImage.type = Image.Type.Sliced;
            trackImage.color = new Color(0.07f, 0.06f, 0.11f, 0.95f);
            trackImage.raycastTarget = false;

            delayed = BarFill(track, "Delayed", new Color(0.95f, 0.55f, 0.35f, 0.8f));
            fill = BarFill(track, "Fill", new Color(0.30f, 0.82f, 0.22f));

            RectTransform labelRect = UIChild("HealthLabel", track);
            Stretch(labelRect);
            label = Label(labelRect.gameObject, 28f, TextAlignmentOptions.Center, Color.white);
            label.fontStyle = FontStyles.Bold;
            label.text = "100 / 100";
        }

        /// <summary>Four booster slots along the right of the bottom bar.</summary>
        static void BuildSlotRow(RectTransform plate, AbilitySystem abilities)
        {
            const int SlotCount = 4;
            const float Size = 92f;
            const float Gap = 10f;

            var slots = new AbilitySlot[SlotCount];

            for (int i = 0; i < SlotCount; i++)
            {
                float x = -20f - (Size + Gap) * (SlotCount - 1 - i);
                slots[i] = BuildSlot(plate, $"Slot{i + 1}", new Vector2(x, -16f), Size);
            }

            var slotObjects = new Object[SlotCount];
            for (int i = 0; i < SlotCount; i++) slotObjects[i] = slots[i];

            var bar = plate.gameObject.AddComponent<AbilitySlotBar>();
            Set(bar, "abilities", abilities);
            Set(bar, "unlockedSlots", SlotCount);
            SetArray(bar, "slots", slotObjects);
        }

        static AbilitySlot BuildSlot(RectTransform parent, string name, Vector2 position, float size)
        {
            RectTransform root = UIChild(name, parent);
            Place(root, new Vector2(1f, 1f), position, new Vector2(size, size));

            var frame = root.gameObject.AddComponent<Image>();
            frame.sprite = UiSprite();
            frame.type = Image.Type.Sliced;
            frame.color = new Color(0.18f, 0.16f, 0.26f);
            frame.raycastTarget = false;

            RectTransform iconRect = UIChild("Icon", root);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(12f, 12f);
            iconRect.offsetMax = new Vector2(-12f, -12f);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = UiSprite();
            icon.type = Image.Type.Sliced;
            icon.raycastTarget = false;
            icon.enabled = false;

            // Radial sweep over the icon. Filled + Radial360 from the top is the
            // convention every player already reads as a cooldown.
            RectTransform sweepRect = UIChild("Cooldown", root);
            sweepRect.anchorMin = Vector2.zero;
            sweepRect.anchorMax = Vector2.one;
            sweepRect.offsetMin = new Vector2(6f, 6f);
            sweepRect.offsetMax = new Vector2(-6f, -6f);
            var sweep = sweepRect.gameObject.AddComponent<Image>();
            sweep.sprite = UiSprite();
            sweep.type = Image.Type.Filled;
            sweep.fillMethod = Image.FillMethod.Radial360;
            sweep.fillOrigin = (int)Image.Origin360.Top;
            sweep.fillClockwise = true;
            sweep.fillAmount = 0f;
            sweep.color = new Color(0.55f, 0.75f, 1f, 0.35f);
            sweep.raycastTarget = false;

            RectTransform levelRect = UIChild("Level", root);
            Place(levelRect, new Vector2(1f, 0f), new Vector2(-6f, 4f), new Vector2(48f, 28f));
            TextMeshProUGUI levelLabel = Label(levelRect.gameObject, 20f, TextAlignmentOptions.Right, Color.white);
            levelLabel.fontStyle = FontStyles.Bold;

            RectTransform lockRect = UIChild("Locked", root);
            Stretch(lockRect);
            var lockImage = lockRect.gameObject.AddComponent<Image>();
            lockImage.sprite = UiSprite();
            lockImage.type = Image.Type.Sliced;
            lockImage.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
            lockImage.raycastTarget = false;

            RectTransform lockTextRect = UIChild("LockedLabel", lockRect);
            Stretch(lockTextRect);
            TextMeshProUGUI lockLabel = Label(lockTextRect.gameObject, 16f,
                                              TextAlignmentOptions.Center, new Color(0.6f, 0.62f, 0.7f));
            lockLabel.text = "LOCKED";
            lockRect.gameObject.SetActive(false);

            var slot = root.gameObject.AddComponent<AbilitySlot>();
            Set(slot, "frame", frame);
            Set(slot, "icon", icon);
            Set(slot, "cooldownFill", sweep);
            Set(slot, "levelLabel", levelLabel);
            Set(slot, "lockedOverlay", lockRect.gameObject);
            Set(slot, "lockedLabel", lockLabel);

            return slot;
        }

        /// <summary>A stretched fill layer inside a bar track.</summary>
        static Image BarFill(RectTransform track, string name, Color colour)
        {
            RectTransform rect = UIChild(name, track);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSprite();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillAmount = 1f;
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        static void BuildPriorityButton(Transform hud, AutoShoot weapon)
        {
            var go = new GameObject("PriorityButton", typeof(RectTransform));
            go.transform.SetParent(hud, false);
            Place((RectTransform)go.transform, new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(300f, 110f));

            var image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.24f, 0.95f);
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var captionGo = new GameObject("Caption", typeof(RectTransform));
            captionGo.transform.SetParent(go.transform, false);
            Place((RectTransform)captionGo.transform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(280f, 34f));
            TextMeshProUGUI caption = Label(captionGo, 22f, TextAlignmentOptions.Center, new Color(0.6f, 0.66f, 0.75f));
            caption.text = "TARGET";

            var valueGo = new GameObject("Value", typeof(RectTransform));
            valueGo.transform.SetParent(go.transform, false);
            Place((RectTransform)valueGo.transform, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(280f, 52f));
            TextMeshProUGUI value = Label(valueGo, 32f, TextAlignmentOptions.Center, Color.white);
            value.text = "Closest";

            // On the HUD, not in a settings menu: with automatic fire from a fixed
            // position this is one of very few tactical decisions a run offers.
            var toggle = go.AddComponent<TargetPriorityToggle>();
            Set(toggle, "weapon", weapon);
            Set(toggle, "button", button);
            Set(toggle, "label", value);
        }

        static void BuildDraftUI(Canvas canvas, DraftController draft, PlayerStats stats,
                                 PrefabSet prefabs)
        {
            // DraftScreen lives on an always-active root and toggles a child panel.
            // Putting it on the panel itself would mean OnEnable never runs while
            // the panel starts hidden, so it would never subscribe to OfferReady
            // and the draft would silently never appear.
            var rootGo = new GameObject("DraftRoot", typeof(RectTransform));
            rootGo.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)rootGo.transform);

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(rootGo.transform, false);
            Stretch((RectTransform)panelGo.transform);

            var dim = panelGo.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.05f, 0.82f);

            var group = panelGo.AddComponent<CanvasGroup>();

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(panelGo.transform, false);
            Place((RectTransform)titleGo.transform, new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(900f, 90f));
            TextMeshProUGUI title = Label(titleGo, 62f, TextAlignmentOptions.Center, Color.white);
            title.text = "LEVEL UP";
            title.fontStyle = FontStyles.Bold;

            var cardsGo = new GameObject("Cards", typeof(RectTransform));
            cardsGo.transform.SetParent(panelGo.transform, false);
            Place((RectTransform)cardsGo.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 440f));

            var layout = cardsGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            if (prefabs.draftCard == null)
            {
                Debug.LogError("[ScalePunch] DraftCard prefab is missing; the draft UI was not built. " +
                               "Re-run ScalePunch ▸ Build Run Scene.");
                return;
            }

            var cards = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.draftCard.gameObject);
                instance.name = $"Card{i + 1}";
                instance.transform.SetParent(cardsGo.transform, false);
                cards[i] = instance.GetComponent<DraftCard>();
            }

            var screen = rootGo.AddComponent<DraftScreen>();
            Set(screen, "draft", draft);
            Set(screen, "stats", stats);
            Set(screen, "panel", panelGo);
            Set(screen, "canvasGroup", group);
            SetArray(screen, "cards", cards);

            panelGo.SetActive(false);
        }

        // -------------------------------------------------------------- systems

        static RunController BuildSystems(DataSet data, PrefabSet prefabs, Transform player,
                                          PlayerStats stats, LevelSystem levels, Health playerHealth,
                                          Camera camera, RectTransform damageNumberRoot,
                                          RectTransform healthBarRoot)
        {
            var systems = new GameObject("Systems");

            // First component on the object, and DefaultExecutionOrder -10000, so
            // the config is published before anything else's Awake reads a toggle.
            var bootstrap = systems.AddComponent<GameBootstrap>();
            Set(bootstrap, "config", data.prototype);

            var time = systems.AddComponent<TimeController>();
            Set(time, "tuning", data.tuning);

            // Loads the profile at Awake and writes it on pause, focus loss and
            // quit. OnApplicationPause is the one that matters: mobile can kill a
            // backgrounded app without ever calling OnApplicationQuit.
            var saveHooks = systems.AddComponent<SaveHooks>();
            Set(saveHooks, "autosaveSeconds", 60f);

            systems.AddComponent<ProjectileService>();

            // The spawner no longer owns pacing — the stage does. Waves, boss and
            // reward numbers all come off the StageDefinition asset.
            var spawner = systems.AddComponent<EnemySpawner>();
            Set(spawner, "stage", data.stage);
            Set(spawner, "ramp", data.ramp);
            Set(spawner, "levelSource", levels);
            Set(spawner, "secondsPerWave", 20f);
            Set(spawner, "target", player);
            Set(spawner, "spawnRadius", SpawnRadius);
            Set(spawner, "maxConcurrent", 150);
            Set(spawner, "prewarmPerType", 24);

            var gems = systems.AddComponent<XPGemService>();
            Set(gems, "prefab", prefabs.gem);
            Set(gems, "player", player);
            Set(gems, "playerStats", stats);
            Set(gems, "levelSystem", levels);

            // DamageNumberService projects world positions to screen space, so it
            // needs the camera it is projecting from.
            var numbers = camera.gameObject.AddComponent<DamageNumberService>();
            Set(numbers, "tuning", data.tuning);
            Set(numbers, "prefab", prefabs.damageNumber);
            Set(numbers, "canvasRoot", damageNumberRoot);
            Set(numbers, "worldCamera", camera);
            Set(numbers, "maxConcurrent", 40);

            var bars = camera.gameObject.AddComponent<HealthBarService>();
            Set(bars, "prefab", prefabs.healthBar);
            Set(bars, "canvasRoot", healthBarRoot);
            Set(bars, "worldCamera", camera);
            Set(bars, "maxConcurrent", 24);

            var run = systems.AddComponent<RunController>();
            Set(run, "stage", data.stage);
            Set(run, "playerHealth", playerHealth);
            Set(run, "spawner", spawner);
            Set(run, "levels", levels);
            Set(run, "endDelaySeconds", 0.9f);
            Set(run, "bankRewards", true);

            return run;
        }

        static void RegisterInBuildSettings()
        {
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
                if (existing.path == ScenePath) return;

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
