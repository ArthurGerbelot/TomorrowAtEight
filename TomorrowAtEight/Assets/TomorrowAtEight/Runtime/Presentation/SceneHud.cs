// ARCH-002 | 2026-11-10 | Common scene HUD: every launched scene gets Restart and Back to Menu.
// Decision: .agents/ARCHITECTURE.md#arch-002---code-driven-scene-bootstrap
// Journal: .agents/days/2026-11-12.md
using System;
using UnityEngine;
using UnityEngine.UI;

namespace TomorrowAtEight.Presentation
{
    /// <summary>Title, description, live diagnostics, and navigation for a running scene.</summary>
    public sealed class SceneHud
    {
        readonly Text m_Stats;

        public SceneHud(
            UiFactory ui,
            RectTransform canvas,
            string title,
            string description,
            Action onRestart,
            Action onBackToMenu)
        {
            RectTransform info = ui.AddPanel(canvas, "SceneInfo", UiFactory.PanelColor);
            UiFactory.Place(info, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(470f, 214f));

            Text titleText = ui.AddLabel(info, "Title", title, 22, TextAnchor.UpperLeft, UiFactory.TextColor);
            UiFactory.Place(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(442f, 30f));

            Text descriptionText = ui.AddLabel(info, "Description", description, 15, TextAnchor.UpperLeft, UiFactory.MutedTextColor);
            UiFactory.Place(descriptionText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -44f), new Vector2(442f, 84f));

            m_Stats = ui.AddLabel(info, "Stats", string.Empty, 14, TextAnchor.UpperLeft, UiFactory.TextColor);
            UiFactory.Place(m_Stats.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -132f), new Vector2(442f, 74f));

            Button restart = ui.AddButton(canvas, "Restart", "Restart", true, onRestart);
            UiFactory.Place((RectTransform)restart.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-202f, -16f), new Vector2(150f, 46f));

            Button back = ui.AddButton(canvas, "BackToMenu", "Back to Menu", true, onBackToMenu);
            UiFactory.Place((RectTransform)back.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(174f, 46f));
        }

        public void SetStats(string stats)
        {
            if (m_Stats != null)
                m_Stats.text = stats;
        }
    }
}
