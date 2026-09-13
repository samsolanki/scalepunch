using TMPro;
using UnityEngine;
using UnityEngine.UI;

using ScalePunch.Run;
using ScalePunch.UI;

namespace ScalePunch.EditorTools
{
    public static partial class RunSceneBuilder
    {
        /// <summary>
        /// Victory / defeat summary.
        ///
        /// RunEndScreen lives on an always-active root and toggles a child panel,
        /// for the same reason DraftScreen does: it subscribes to RunController
        /// in OnEnable, and OnEnable never runs on an object that starts
        /// disabled. Put the component on the panel it hides and the run would
        /// end in silence.
        /// </summary>
        static void BuildRunEndUI(Canvas canvas, RunController run)
        {
            RectTransform root = UIChild("RunEndRoot", canvas.transform);
            Stretch(root);

            RectTransform panel = UIChild("Panel", root);
            Stretch(panel);

            var dim = panel.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.05f, 0.88f);

            var group = panel.gameObject.AddComponent<CanvasGroup>();

            var centre = new Vector2(0.5f, 0.5f);

            TextMeshProUGUI title = HudText("Title", panel, centre, new Vector2(0f, 330f),
                new Vector2(900f, 120f), 78f, TextAlignmentOptions.Center, "STAGE CLEAR");
            title.fontStyle = FontStyles.Bold;

            TextMeshProUGUI subtitle = HudText("Subtitle", panel, centre, new Vector2(0f, 232f),
                new Vector2(900f, 60f), 32f, TextAlignmentOptions.Center, "Wave 8 of 8");
            subtitle.color = new Color(0.70f, 0.74f, 0.82f);

            TextMeshProUGUI stats = HudText("Stats", panel, centre, new Vector2(0f, 90f),
                new Vector2(860f, 220f), 34f, TextAlignmentOptions.Center, "");

            TextMeshProUGUI coins = HudText("Coins", panel, centre, new Vector2(0f, -60f),
                new Vector2(860f, 70f), 40f, TextAlignmentOptions.Center, "");
            coins.fontStyle = FontStyles.Bold;
            coins.color = new Color(1f, 0.84f, 0.35f);

            TextMeshProUGUI balance = HudText("Balance", panel, centre, new Vector2(0f, -130f),
                new Vector2(860f, 56f), 28f, TextAlignmentOptions.Center, "");
            balance.color = new Color(0.66f, 0.70f, 0.78f);

            Button retry = BuildRetryButton(panel);

            var screen = root.gameObject.AddComponent<RunEndScreen>();
            Set(screen, "run", run);
            Set(screen, "panel", panel.gameObject);
            Set(screen, "canvasGroup", group);
            Set(screen, "titleLabel", title);
            Set(screen, "subtitleLabel", subtitle);
            Set(screen, "statsLabel", stats);
            Set(screen, "coinsLabel", coins);
            Set(screen, "balanceLabel", balance);
            Set(screen, "retryButton", retry);

            // Hidden at build time too, not just at runtime, so the Scene view is
            // not covered by a full-screen dim panel while editing.
            panel.gameObject.SetActive(false);
        }

        static Button BuildRetryButton(RectTransform parent)
        {
            RectTransform rect = UIChild("RetryButton", parent);
            Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(420f, 120f));

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.20f, 0.46f, 0.86f, 0.96f);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI label = HudText("Label", rect, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(400f, 90f), 42f, TextAlignmentOptions.Center, "RETRY");
            label.fontStyle = FontStyles.Bold;

            return button;
        }

        /// <summary>
        /// "BOSS INCOMING" strip under the level track.
        ///
        /// Returned active. RunHUD disables it in its own Awake and drives it
        /// from RunController.BossSpawned, so hiding it here as well would just
        /// duplicate ownership of the same flag.
        /// </summary>
        static GameObject BuildBossBanner(Transform hud)
        {
            RectTransform rect = UIChild("BossBanner", hud);
            Place(rect, new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(720f, 92f));

            var background = rect.gameObject.AddComponent<Image>();
            background.sprite = UiSprite();
            background.type = Image.Type.Sliced;
            background.color = new Color(0.62f, 0.10f, 0.12f, 0.92f);
            background.raycastTarget = false;

            TextMeshProUGUI label = HudText("Label", rect, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(700f, 80f), 46f, TextAlignmentOptions.Center, "BOSS INCOMING");
            label.fontStyle = FontStyles.Bold;

            return rect.gameObject;
        }
    }
}
