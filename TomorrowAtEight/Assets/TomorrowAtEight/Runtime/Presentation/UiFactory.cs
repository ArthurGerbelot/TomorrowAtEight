// ARCH-002, ARCH-005 | 2026-11-11 | UI is assembled in code; no prefab or Inspector wiring.
// uGUI needs managed components, so it stays behind this small presentation adapter.
// Decision: .agents/ARCHITECTURE.md#arch-005---unity-entities-simulation-and-web-presentation
// Journal: .agents/days/2026-11-12.md
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TomorrowAtEight.Presentation
{
    /// <summary>Code-only helpers for the provisional development UI.</summary>
    public sealed class UiFactory
    {
        public static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.09f, 0.86f);
        public static readonly Color TextColor = new Color(0.93f, 0.94f, 0.96f, 1f);
        public static readonly Color MutedTextColor = new Color(0.62f, 0.65f, 0.70f, 1f);
        public static readonly Color ErrorTextColor = new Color(1f, 0.55f, 0.50f, 1f);
        public static readonly Color ButtonColor = new Color(0.20f, 0.24f, 0.31f, 1f);
        public static readonly Color ButtonDisabledColor = new Color(0.14f, 0.15f, 0.18f, 1f);

        readonly Font m_Font;

        public UiFactory(Font font)
        {
            m_Font = font;
        }

        public static RectTransform CreateCanvas(Transform parent, string name)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return (RectTransform)canvasObject.transform;
        }

        public static EventSystem CreateEventSystem(Transform parent)
        {
            var eventObject = new GameObject("EventSystem");
            eventObject.transform.SetParent(parent, false);
            EventSystem eventSystem = eventObject.AddComponent<EventSystem>();
            // Assigns the Input System's default UI actions when none are set.
            eventObject.AddComponent<InputSystemUIInputModule>();
            return eventSystem;
        }

        public RectTransform AddPanel(RectTransform parent, string name, Color color)
        {
            RectTransform rect = CreateRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        public Text AddLabel(RectTransform parent, string name, string content, int size, TextAnchor anchor, Color color)
        {
            RectTransform rect = CreateRect(parent, name);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = m_Font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = content ?? string.Empty;
            return text;
        }

        public Button AddButton(RectTransform parent, string name, string label, bool interactable, Action onClick)
        {
            RectTransform rect = CreateRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = new Color(0.28f, 0.34f, 0.44f, 1f);
            colors.pressedColor = new Color(0.15f, 0.18f, 0.24f, 1f);
            colors.selectedColor = ButtonColor;
            colors.disabledColor = ButtonDisabledColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.interactable = interactable;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            Text text = AddLabel(rect, "Label", label, 20, TextAnchor.MiddleCenter,
                interactable ? TextColor : MutedTextColor);
            Stretch(text.rectTransform, 6f);
            return button;
        }

        /// <summary>Positions a rect with one anchor point, a pivot, an offset, and a size.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        static RectTransform CreateRect(RectTransform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }
    }
}
