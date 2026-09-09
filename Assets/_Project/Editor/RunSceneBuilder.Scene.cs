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
using ScalePunch.UI;
using ScalePunch.Weapons;

using Object = UnityEngine.Object;

namespace ScalePunch.EditorTools
{
    public static partial class RunSceneBuilder
    {
        const float SpawnRadius = 16f;

        static void BuildScene(DataSet data, PrefabSet prefabs, MaterialSet mats)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildEnvironment(mats);

            GameObject player = BuildPlayer(data, mats, out AutoShoot weapon, out PlayerStats stats,
                                            out Health health, out LevelSystem levels,
                                            out AbilitySystem abilities, out DraftController draft);

            Camera camera = BuildCamera(data);

            Canvas canvas = BuildCanvas(out RectTransform damageNumberRoot);
            BuildHUD(canvas, levels, health, weapon);
            BuildDraftUI(canvas, draft, abilities, prefabs);

            BuildSystems(data, prefabs, player.transform, stats, levels, camera, damageNumberRoot);

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

            GameObject muzzle = Child("Muzzle", turret.transform);
            muzzle.transform.localPosition = new Vector3(0f, 0.2f, 0.6f);

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

            Set(health, "maxHP", 100f);

            Set(stats, "health", health);

            Set(weapon, "stats", stats);
            Set(weapon, "weapon", data.pistol);
            Set(weapon, "tuning", data.tuning);
            Set(weapon, "turret", turret.transform);
            Set(weapon, "muzzle", muzzle.transform);
            Set(weapon, "priority", AutoShoot.TargetPriority.Closest);

            Set(levels, "curve", data.curve);

            Set(abilities, "stats", stats);
            Set(abilities, "weapon", weapon);

            Set(draft, "library", data.library);
            Set(draft, "abilities", abilities);
            Set(draft, "levels", levels);
            Set(draft, "stats", stats);

            Set(indicator, "weapon", weapon);
            Set(indicator, "line", line);

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

        static Canvas BuildCanvas(out RectTransform damageNumberRoot)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

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

        static void BuildHUD(Canvas canvas, LevelSystem levels, Health health, AutoShoot weapon)
        {
            var hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)hudGo.transform);
            Transform hud = hudGo.transform;

            // XP bar: top edge, full width. It is the only promise the run makes,
            // and a thumb must never cover it.
            Bar("XPBar", hud, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(1000f, 26f),
                new Color(0.08f, 0.09f, 0.12f, 0.9f), new Color(0.35f, 0.85f, 1f), out Image xpFill);

            TextMeshProUGUI levelLabel = HudText("LevelLabel", hud, new Vector2(0f, 1f),
                new Vector2(40f, -52f), new Vector2(200f, 50f), 34f, TextAlignmentOptions.Left, "1");

            TextMeshProUGUI timerLabel = HudText("TimerLabel", hud, new Vector2(0.5f, 1f),
                new Vector2(0f, -52f), new Vector2(300f, 50f), 32f, TextAlignmentOptions.Center, "0:00");

            TextMeshProUGUI killsLabel = HudText("KillsLabel", hud, new Vector2(1f, 1f),
                new Vector2(-40f, -52f), new Vector2(200f, 50f), 32f, TextAlignmentOptions.Right, "0");

            Bar("HealthBar", hud, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(440f, 34f),
                new Color(0.08f, 0.09f, 0.12f, 0.9f), new Color(0.9f, 0.3f, 0.32f), out Image healthFill);

            TextMeshProUGUI healthLabel = HudText("HealthLabel", hud, new Vector2(0f, 0f),
                new Vector2(40f, 80f), new Vector2(440f, 40f), 26f, TextAlignmentOptions.Left, "100/100");

            var runHud = hudGo.AddComponent<RunHUD>();
            Set(runHud, "levels", levels);
            Set(runHud, "playerHealth", health);
            Set(runHud, "weapon", weapon);
            Set(runHud, "xpFill", xpFill);
            Set(runHud, "levelLabel", levelLabel);
            Set(runHud, "healthFill", healthFill);
            Set(runHud, "healthLabel", healthLabel);
            Set(runHud, "timerLabel", timerLabel);
            Set(runHud, "killsLabel", killsLabel);

            BuildPriorityButton(hud, weapon);
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

        static void BuildDraftUI(Canvas canvas, DraftController draft, AbilitySystem abilities, PrefabSet prefabs)
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
            Set(screen, "abilities", abilities);
            Set(screen, "panel", panelGo);
            Set(screen, "canvasGroup", group);
            SetArray(screen, "cards", cards);

            panelGo.SetActive(false);
        }

        // -------------------------------------------------------------- systems

        static void BuildSystems(DataSet data, PrefabSet prefabs, Transform player,
                                 PlayerStats stats, LevelSystem levels, Camera camera,
                                 RectTransform damageNumberRoot)
        {
            var systems = new GameObject("Systems");

            var time = systems.AddComponent<TimeController>();
            Set(time, "tuning", data.tuning);

            systems.AddComponent<ProjectileService>();

            var spawner = systems.AddComponent<EnemySpawner>();
            Set(spawner, "target", player);
            Set(spawner, "spawnRadius", SpawnRadius);
            Set(spawner, "initialInterval", 1.4f);
            Set(spawner, "minimumInterval", 0.18f);
            Set(spawner, "secondsPerWave", 20f);
            Set(spawner, "stageMultiplier", 1f);
            Set(spawner, "maxConcurrent", 150);
            Set(spawner, "prewarmPerType", 24);

            var definitions = new Object[data.zombies.Length];
            for (int i = 0; i < data.zombies.Length; i++) definitions[i] = data.zombies[i];
            SetArray(spawner, "definitions", definitions);

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
