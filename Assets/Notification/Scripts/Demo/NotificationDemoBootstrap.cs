using Sandbox.Notification.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sandbox.Notification.Demo
{
    /// <summary>Scene-only composition root. Notification features live in their own folders.</summary>
    public sealed class NotificationDemoBootstrap : MonoBehaviour
    {
        private const float ToastDuration = 3f;

        [SerializeField] private ItemToastView itemToastPrefab;
        [SerializeField] private WarningView warningPrefab;
        [SerializeField] private QuestBannerView questBannerPrefab;
        [SerializeField] private AchievementToastView achievementToastPrefab;
        [SerializeField] private ConfirmModalView confirmModalPrefab;
        [SerializeField, Min(1)] private int itemToastMaximumVisible = 3;

        private NotificationService notifications;
        private ModalService modals;
        private int appleTotal = 7;

        private void Start()
        {
            BuildDemoUi(out var itemArea, out var achievementArea, out var warningArea, out var bannerArea, out var modalArea);

            notifications = new NotificationService(
                new ItemToastPresenter(this, itemArea, ToastDuration, itemToastPrefab, itemToastMaximumVisible),
                new WarningPresenter(this, warningArea, warningPrefab),
                new QuestBannerPresenter(this, bannerArea, questBannerPrefab),
                new AchievementToastPresenter(this, achievementArea, achievementToastPrefab));
            modals = new ModalService(new ModalPresenter(modalArea, confirmModalPrefab));
        }

        private void BuildDemoUi(out RectTransform itemArea, out RectTransform achievementArea,
            out RectTransform warningArea, out RectTransform bannerArea, out RectTransform modalArea)
        {
            var canvasObject = new GameObject("Notification Demo Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            CreatePanel(canvasRect, "Backdrop", new Color(0.035f, 0.075f, 0.12f, 1f), Vector2.zero, Vector2.one);

            var title = DemoUi.Text(canvasRect, "Independent Notification Demo", 42, Color.white, TextAlignmentOptions.Left);
            DemoUi.Stretch(title.rectTransform, new Vector2(0.055f, 0.90f), new Vector2(0.70f, 0.97f));
            var subtitle = DemoUi.Text(canvasRect,
                "Each button publishes to its own presenter. No notification pauses another layer.",
                20, new Color(0.66f, 0.76f, 0.86f), TextAlignmentOptions.Left);
            DemoUi.Stretch(subtitle.rectTransform, new Vector2(0.055f, 0.855f), new Vector2(0.78f, 0.90f));

            var controls = DemoUi.Panel(canvasRect, "Controls", new Color(0.07f, 0.14f, 0.21f, 0.96f));
            DemoUi.Stretch(controls, new Vector2(0.055f, 0.08f), new Vector2(0.32f, 0.80f));
            var controlsLayout = controls.gameObject.AddComponent<VerticalLayoutGroup>();
            controlsLayout.padding = new RectOffset(26, 26, 26, 26);
            controlsLayout.spacing = 14;
            controlsLayout.childControlHeight = false;
            controlsLayout.childControlWidth = true;
            controlsLayout.childForceExpandHeight = false;

            AddControlLabel(controls, "Publish notifications");
            DemoUi.Button(controls, "Get Apple (click several times)", new Color(0.18f, 0.42f, 0.30f), PublishItem);
            DemoUi.Button(controls, "Inventory Full Warning", new Color(0.56f, 0.32f, 0.11f), PublishWarning);
            DemoUi.Button(controls, "Quest Updated Banner", new Color(0.18f, 0.28f, 0.51f), PublishBanner);
            DemoUi.Button(controls, "Achievement Unlocked", new Color(0.45f, 0.30f, 0.08f), PublishAchievement);
            DemoUi.Button(controls, "Open Confirm Modal", new Color(0.35f, 0.20f, 0.48f), OpenModal);

            itemArea = DemoUi.Container(canvasRect, "Item Toast Area", new Vector2(0.70f, 0.64f), new Vector2(0.97f, 0.94f), true);
            achievementArea = DemoUi.Container(canvasRect, "Achievement Area", new Vector2(0.055f, 0.08f), new Vector2(0.58f, 0.17f), false);
            warningArea = DemoUi.Container(canvasRect, "Warning Area", new Vector2(0.34f, 0.76f), new Vector2(0.66f, 0.83f), false);
            bannerArea = DemoUi.Container(canvasRect, "Banner Area", new Vector2(0.32f, 0.44f), new Vector2(0.68f, 0.62f), false);
            modalArea = DemoUi.Container(canvasRect, "Modal Area", Vector2.zero, Vector2.one, false);
        }

        private static void CreatePanel(Transform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            var panel = DemoUi.Panel(parent, name, color);
            DemoUi.Stretch(panel, min, max);
        }

        private static void AddControlLabel(Transform parent, string value)
        {
            var text = DemoUi.Text(parent, value, 24, new Color(0.92f, 0.76f, 0.36f), TextAlignmentOptions.Left);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;
        }

        private void PublishItem()
        {
            appleTotal += 2;
            notifications.Publish(new ItemToastRequest("apple", "Apple", 2, appleTotal));
        }

        private void PublishWarning() => notifications.Publish(new WarningRequest("Inventory Full", 2.5f));

        private void PublishBanner() => notifications.Publish(
            new QuestBannerRequest("ancient-shrine", "Find the Ancient Shrine", "A distant light is calling."));

        private void PublishAchievement() => notifications.Publish(
            new AchievementToastRequest("first-sunrise", "Achievement Unlocked", "First Sunrise", "Watch the dawn from a high place."));

        private void OpenModal()
        {
            modals.Show(new ConfirmModalRequest("Use Ancient Key?", "This action demonstrates a modal with a result."),
                result => Debug.Log($"Modal result: {result}"));
        }
    }
}
