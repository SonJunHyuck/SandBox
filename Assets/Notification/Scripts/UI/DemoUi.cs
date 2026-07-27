using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sandbox.Notification.UI
{
    /// <summary>Small runtime uGUI builder used by the demo scene only.</summary>
    public static class DemoUi
    {
        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform Container(Transform parent, string name, Vector2 min, Vector2 max, bool vertical)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect, min, max);

            if (vertical)
            {
                var layout = go.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 10;
                layout.childAlignment = TextAnchor.UpperRight;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
            }

            return rect;
        }

        public static RectTransform Card(Transform parent, Color color)
        {
            var card = Panel(parent, "Notification Card", color);
            Stretch(card, Vector2.zero, Vector2.one);
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = 82;
            return card;
        }

        public static TextMeshProUGUI FillText(Transform parent, string value, int size, Color color, TextAlignmentOptions alignment)
        {
            var text = Text(parent, value, size, color, alignment);
            Stretch(text.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f));
            return text;
        }

        public static TextMeshProUGUI Text(Transform parent, string value, int size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            return text;
        }

        public static void Button(Transform parent, string label, Color color, UnityEngine.Events.UnityAction action)
        {
            var root = Panel(parent, label, color);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 62;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            button.onClick.AddListener(action);
            FillText(root, label, 20, Color.white, TextAlignmentOptions.Center);
        }

        public static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
