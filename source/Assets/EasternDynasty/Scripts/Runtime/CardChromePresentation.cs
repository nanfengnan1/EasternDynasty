using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D cardChromeTabsSkin, cardChromeTabActiveSkin, cardChromeIdentitySkin;
        private Texture2D cardChromeCloseSkin, cardChromeActionSkin, cardChromeActionHoverSkin;
        private Rect cardChromeRight;
        private bool cardChromeTextFits;

        private void InitCardChrome()
        {
            if (cardChromeTabsSkin != null) return;
            cardChromeTabsSkin = GameAssets.LoadTexture("ui.youzhou.card_tabs");
            cardChromeTabActiveSkin = GameAssets.LoadTexture("ui.youzhou.card_tab_active");
            cardChromeIdentitySkin = GameAssets.LoadTexture("ui.youzhou.card_identity");
            cardChromeCloseSkin = GameAssets.LoadTexture("ui.youzhou.card_close");
            cardChromeActionSkin = GameAssets.LoadTexture("ui.youzhou.card_action");
            cardChromeActionHoverSkin = GameAssets.LoadTexture("ui.youzhou.card_action_hover");
        }

        private bool CardChromeAssetsReady()
        {
            return CardChromeSize(cardChromeTabsSkin, 600, 54) && CardChromeSize(cardChromeTabActiveSkin, 200, 54)
                && CardChromeSize(cardChromeIdentitySkin, 600, 40) && CardChromeSize(cardChromeCloseSkin, 54, 54)
                && CardChromeSize(cardChromeActionSkin, 240, 54) && CardChromeSize(cardChromeActionHoverSkin, 240, 54);
        }

        private static bool CardChromeSize(Texture2D texture, int width, int height)
        { return texture != null && texture.width == width && texture.height == height; }

        private void CardChromeText(Rect rect, string value, int preferred, int minimum, TextAnchor alignment, Color color)
        {
            var style = new GUIStyle(body) { fontSize = preferred, alignment = alignment, wordWrap = false, clipping = TextClipping.Clip };
            style.normal.textColor = color;
            var content = new GUIContent(value);
            while (style.fontSize > minimum && !HeaderTextFits(rect, content, style, false)) style.fontSize--;
            cardChromeTextFits &= HeaderTextFits(rect, content, style, false);
            GUI.Label(rect, content, style);
        }

        private bool DrawCardChromeClose(Rect rect)
        {
            GUI.DrawTexture(rect, cardChromeCloseSkin, ScaleMode.StretchToFill, true);
            return ContentTileClicked(rect, "card-close", 0);
        }

        private bool DrawCardChromeAction(Rect rect, string label, string id)
        {
            bool hover = GUI.enabled && !mobileMode && rect.Contains(Event.current.mousePosition);
            GUI.DrawTexture(rect, hover ? cardChromeActionHoverSkin : cardChromeActionSkin, ScaleMode.StretchToFill, true);
            CardChromeText(rect, label, 18, 16, TextAnchor.MiddleCenter, Paper);
            return ContentTileClicked(rect, id, 0);
        }
    }
}
