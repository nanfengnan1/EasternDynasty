using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    // Card identity stays separate from the owned instance. Browsing never recruits or changes formation.
    public sealed partial class EasternDynastyGame
    {
        private string cardDetailId;
        private int cardDetailUid;
        private int cardDetailSection;
        private Vector2 cardDetailScroll;
        private GUIStyle cardDetailBody;
        private GUIStyle cardDetailCaption, cardDetailValue, cardDetailGrowth, cardDetailName, cardDetailTab, cardDetailStars;
        private GUIStyle tileStars, compactTileStars, tileTitle, compactTileTitle, longTileTitle, tileBadge;
        private Rect cardPortraitFrame, cardPortraitImage, cardPortraitBanner, cardPortraitCrown, cardPortraitBase;
        private GUIStyle cardPortraitRank, cardPortraitLevel, cardPortraitSmall;
        private GUIStyle cardPortraitFaction;
        private Texture2D cardPortraitRibbonSkin, cardPortraitSealSkin, cardPortraitBaseSkin;
        private float cardPortraitNameStep;
        private string tilePointerDefinition;
        private int tilePointerOwnedUid;
        private Rect tilePointerRect;
        private Vector2 tilePointerStart;
        private bool tilePointerPressed, tilePointerDragged;
        private bool BlocksMapInput { get { return tab != "领地" || drawResult != null || confirmation != null || cardDetailId != null || cityQuestReceipt != null; } }

        private void OnMapLandClicked(int landId)
        {
            if (world == null || landId < 0 || landId >= world.lands.Count) return;
            if (world.IsYouzhou && world.IsCityDistrict(landId)) OpenCity(landId);
        }

        private void ShowCardDetails(string definitionId, int ownedUid = 0)
        {
            if (!Catalog.Generals.Any(g => g.id == definitionId)) return;
            cardDetailId = definitionId; cardDetailUid = ownedUid;
            cardDetailSection = 0; cardDetailScroll = Vector2.zero;
            if (ownedUid > 0) selectedCard = ownedUid;
        }

        private void CloseCardDetails() { cardDetailId = null; cardDetailUid = 0; cardDetailScroll = Vector2.zero; }

        private bool DefinitionTile(Rect r, GeneralDefinition g, bool selected, GeneralCard owned = null)
        {
            if(tileStars==null)
            {
                tileStars=new GUIStyle(tiny){alignment=TextAnchor.MiddleRight,fontSize=14};tileStars.normal.textColor=Gold;
                compactTileStars=new GUIStyle(tileStars){fontSize=11};
                tileTitle=new GUIStyle(cardName){fontSize=25};compactTileTitle=new GUIStyle(cardName){fontSize=18};longTileTitle=new GUIStyle(cardName){fontSize=21};
                tileBadge=new GUIStyle(tiny){alignment=TextAnchor.MiddleCenter};
            }
            Portrait(r, g.id);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 70, r.width, 70), shade);
            Color accent = FactionColor(g.faction);
            Outline(r, selected ? Gold : Color.Lerp(accent, Gold, .25f), selected ? 3 : 1);
            RectFill(new Rect(r.x, r.y, 3, r.height), accent);
            RectFill(new Rect(r.x + 7, r.y + 7, 28, 30), accent);
            GUI.Label(new Rect(r.x + 7, r.y + 7, 28, 30), g.s1 ? g.faction : "汉", centered);
            var stars = r.width < 140 ? compactTileStars : tileStars;
            RectFill(new Rect(r.x + 41, r.y + 9, r.width - 49, 22), new Color(.04f,.07f,.11f,.72f));
            GUI.Label(new Rect(r.x + 42, r.y + 9, r.width - 53, 22), new string('★', g.stars), stars);
            Label(r.x + 10, r.yMax - 55, r.width - 20, 30, g.name,
                r.width < 140 ? compactTileTitle : g.name.Length > 4 ? longTileTitle : tileTitle);
            string foot = owned == null ? g.troop + "兵  ·  COST " + g.cost.ToString("0.0") : "Lv." + owned.level + "   " + owned.rank + "阶";
            Label(r.x + 10, r.yMax - 26, r.width - 20, 22, foot, tiny);
            if (owned != null && world.formation.Contains(owned.uid))
            {
                var badge = new Rect(r.xMax - 48, r.y + 42, 40, 22);
                RectFill(badge, accent); GUI.Label(badge, "上阵", tileBadge);
            }
            return ContentTileClicked(r, g.id, owned == null ? 0 : owned.uid);
        }

        private bool ContentTileClicked(Rect rect, string definitionId, int ownedUid)
        {
            var pointer = Event.current;
            var rawType = pointer.rawType;
            if (GUI.enabled && pointer.type == EventType.MouseDown && pointer.button == 0 && rect.Contains(pointer.mousePosition))
            {
                tilePointerDefinition = definitionId; tilePointerOwnedUid = ownedUid;
                tilePointerRect = rect; tilePointerStart = pointer.mousePosition;
                tilePointerPressed = true; tilePointerDragged = false;
            }
            bool ownsPointer = tilePointerPressed && tilePointerDefinition == definitionId && tilePointerOwnedUid == ownedUid && tilePointerRect == rect;
            if (ownsPointer && (rawType == EventType.MouseDrag || rawType == EventType.MouseUp))
            {
                // GUI.Button otherwise accepts a drag whenever its release stays inside
                // the tile. Cards and city sites share mobile list scrolling's physical threshold.
                float threshold = Mathf.Clamp(Screen.dpi > 0 ? Screen.dpi / 160f * 8f : 12f, 10f, 30f) / Mathf.Max(.01f, scale);
                if ((pointer.mousePosition - tilePointerStart).sqrMagnitude >= threshold * threshold) tilePointerDragged = true;
            }
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            bool accepted = clicked && (!ownsPointer || !tilePointerDragged);
            if (ownsPointer && rawType == EventType.MouseUp) tilePointerPressed = false;
            return accepted;
        }

        private void DrawCardDetails()
        {
            var definition = Catalog.Generals.Find(g => g.id == cardDetailId);
            if (definition == null) { CloseCardDetails(); return; }
            var owned = world.cards.Find(g => g.uid == cardDetailUid && g.definition == cardDetailId);
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            { CloseCardDetails(); Event.current.Use(); return; }
            Block(new Rect(0, 0, width, height));
            RectFill(new Rect(0, 0, width, height), new Color(.02f, .04f, .07f, .94f));
            float modalWidth = Mathf.Min(1280, width - 64), modalHeight = Mathf.Min(820, height - 28);
            Rect panel = new Rect((width - modalWidth) / 2, (height - modalHeight) / 2, modalWidth, modalHeight);
            InitCardDetailStyles();
            RectFill(panel, C("202323"));
            Outline(panel, C("756A52"));
            Color accent = FactionColor(definition.faction);
            Rect art = new Rect(panel.x + 20, panel.y + 14, panel.width * .43f, panel.height - 28);
            DrawCardPortraitStage(art, definition, owned, accent);
            float x = art.xMax + 28, w = panel.xMax - x - 24;
            InitCardChrome();
            cardChromeTextFits = true;
            cardChromeRight = new Rect(x, panel.y, w, panel.height);
            var chrome = new CardPortraitDesignLayout.Box(x, panel.y, w, panel.height);
            GUI.DrawTexture(CardPortraitRect(CardChromeLayout.Nav(chrome)), cardChromeTabsSkin, ScaleMode.StretchToFill, true);
            Rect close = CardPortraitRect(CardChromeLayout.Close(chrome));
            RecordReviewTarget("card-close", close);
            if (DrawCardChromeClose(close)) { CloseCardDetails(); return; }
            DrawCardIdentityBand(CardPortraitRect(CardChromeLayout.Identity(chrome)), definition);
            string[] sections = { "属性", "战法", "资料" };
            string[] sectionTargets = { "card-attributes", "card-skill", "card-source" };
            for (int i = 0; i < sections.Length; i++)
            {
                Rect section = CardPortraitRect(CardChromeLayout.Tab(chrome, i));
                RecordReviewTarget(sectionTargets[i], section);
                if (CardDetailSectionButton(section, sections[i], cardDetailSection == i))
                { cardDetailSection = i; cardDetailScroll = Vector2.zero; }
            }
            Rect area = CardPortraitRect(CardChromeLayout.Viewport(chrome));
            RecordReviewTarget("card-scroll", area);
            float innerWidth = w - 22;
            string text = CardDetailsText(definition, owned, cardDetailSection);
            float textHeight = cardDetailBody.CalcHeight(new GUIContent(text), innerWidth - 28);
            float contentHeight = Mathf.Max(area.height, cardDetailSection == 0 ? CardDossierAttributeHeight : cardDetailSection == 1 ? CardDossierSkillHeight(innerWidth, definition, owned) : textHeight + 96);
            cardDetailScroll = BeginContentScroll(area, cardDetailScroll, new Rect(0, 0, innerWidth, contentHeight));
            if (cardDetailSection == 0)
                DrawCardAttributes(innerWidth, definition, owned, area);
            else if (cardDetailSection == 1)
                DrawCardSkillDossier(innerWidth, definition, owned, area);
            else
            {
                DrawCardSeal(new Rect(12, 12, 36, 36), cardDetailSection == 1 ? "战" : "传", Gold);
                Label(62, 14, innerWidth - 76, 36, cardDetailSection == 1 ? "战法详情" : "人物与取得资料", cardName);
                RectFill(new Rect(12, 62, innerWidth - 24, 1), C("665E4F"));
                Label(12, 80, innerWidth - 28, textHeight + 8, text, cardDetailBody);
            }
            GUI.EndScrollView(GUI.enabled);
            Rect back = CardPortraitRect(CardChromeLayout.Return(chrome));
            RecordReviewTarget("card-return", back);
            if (owned != null)
            {
                Rect manage = CardPortraitRect(CardChromeLayout.Manage(chrome));
                RecordReviewTarget("owned-card-manage", manage);
                if (DrawCardChromeAction(manage, "编队与培养", "owned-card-manage"))
                { selectedCard = owned.uid; if(tab=="练兵")EditTrainingFormation();else tab="武将"; CloseCardDetails(); }
            }
            else CardChromeText(CardPortraitRect(CardChromeLayout.FooterNote(chrome)), definition.s1 && !definition.recruitable ? "资料收藏 · 不进入试玩招募" : "图鉴预览 · 基础属性", 16, 14, TextAnchor.MiddleLeft, C("B9B3A5"));
            if (DrawCardChromeAction(back, "返回卡牌", "card-return")) CloseCardDetails();
        }

        private void InitCardDetailStyles()
        {
            if (cardDetailBody != null) return;
            cardDetailBody = new GUIStyle(body) { fontSize = 18, wordWrap = true };
            cardDetailCaption = new GUIStyle(small) { fontSize = 16 };
            cardDetailCaption.normal.textColor = C("B9B3A5");
            cardDetailValue = new GUIStyle(heading) { fontSize = 27, alignment = TextAnchor.MiddleLeft };
            cardDetailValue.normal.textColor = Paper;
            cardDetailGrowth = new GUIStyle(cardDetailCaption) { alignment = TextAnchor.MiddleRight };
            cardDetailGrowth.normal.textColor = C("99B59B");
            cardDetailName = new GUIStyle(cardName) { fontSize = 30, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            cardDetailTab = new GUIStyle(body) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            cardDetailStars = new GUIStyle(centered) { fontSize = 20 };
            cardDetailStars.normal.textColor = Gold;
        }

        private void DrawCardPortraitStage(Rect art, GeneralDefinition definition, GeneralCard owned, Color accent)
        {
            if (cardPortraitRank == null)
            {
                cardPortraitRank = new GUIStyle(cardDetailStars) { fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = false };
                cardPortraitLevel = new GUIStyle(cardDetailValue) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
                cardPortraitSmall = new GUIStyle(cardDetailCaption) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
                cardPortraitFaction = new GUIStyle(cardDetailName) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
                cardPortraitRibbonSkin = GameAssets.LoadTexture("ui.youzhou.portrait_ribbon");
                cardPortraitSealSkin = GameAssets.LoadTexture("ui.youzhou.portrait_seal");
                cardPortraitBaseSkin = GameAssets.LoadTexture("ui.youzhou.portrait_base");
            }
            cardPortraitFrame = art;
            // Inkscape composition: independent identity column and full artwork.
            // Dynamic labels remain text; the exported surfaces contain no screenshot pixels.
            var frame = new CardPortraitDesignLayout.Box(art.x, art.y, art.width, art.height);
            var imageBox = CardPortraitDesignLayout.ImageArea(frame);
            Rect imageArea = CardPortraitRect(imageBox);
            Texture2D texture; portraits.TryGetValue(definition.id, out texture);
            cardPortraitImage = imageArea;
            if (texture != null)
            {
                cardPortraitImage = CardPortraitRect(CardPortraitDesignLayout.FitImage(imageBox, texture.width, texture.height));
                GUI.DrawTexture(cardPortraitImage, texture, ScaleMode.StretchToFill, true);
            }
            else PortraitComplete(imageArea, definition.id);

            cardPortraitNameStep = CardPortraitDesignLayout.NameStep(art.height, definition.name.Length);
            cardPortraitBanner = CardPortraitRect(CardPortraitDesignLayout.Banner(frame, definition.name.Length));
            Rect rail = cardPortraitBanner;
            GUI.DrawTexture(rail, cardPortraitRibbonSkin, ScaleMode.StretchToFill, true);
            Rect faction = new Rect(rail.x, rail.y, 48, 48);
            GUI.DrawTexture(faction, cardPortraitSealSkin, ScaleMode.StretchToFill, true);
            HudFittedLabel(faction, definition.faction, cardPortraitFaction, 16);
            // Microsoft YaHei's 30-point line is taller than a 38-unit cell.
            // Fit the selected desktop/bundled font's real metrics rather than
            // treating fontSize as rendered height or weakening clip diagnostics.
            cardDetailName.fontSize = CardPortraitTextLayout.FitName(definition.name, 44, cardPortraitNameStep, 30,
                MeasureCardPortraitNameGlyph);
            for (int i = 0; i < definition.name.Length; i++)
                GUI.Label(new Rect(rail.x + 2, rail.y + 55 + i * cardPortraitNameStep, 44, cardPortraitNameStep), definition.name[i].ToString(), cardDetailName);

            cardPortraitCrown = CardPortraitRect(CardPortraitDesignLayout.Stars(frame, definition.name.Length, definition.stars));
            cardPortraitRank.fontSize = CardPortraitTextLayout.FitName("★", 48, 30, 24, MeasureCardPortraitStarGlyph);
            for (int i = 0; i < definition.stars; i++)
                GUI.Label(new Rect(cardPortraitCrown.x, cardPortraitCrown.y + i * 30, 48, 30), "★", cardPortraitRank);
            cardPortraitBase = CardPortraitRect(CardPortraitDesignLayout.Base(frame));
            DrawCardPortraitBase(cardPortraitBase, definition, owned);
        }

        private static Rect CardPortraitRect(CardPortraitDesignLayout.Box b) { return new Rect(b.X,b.Y,b.W,b.H); }

        private CardPortraitGlyphMetrics MeasureCardPortraitStarGlyph(char glyph, int size)
        {
            cardPortraitRank.fontSize = size;
            Vector2 measured = cardPortraitRank.CalcSize(new GUIContent(glyph.ToString()));
            return new CardPortraitGlyphMetrics { Width=measured.x, Height=Mathf.Max(measured.y,cardPortraitRank.lineHeight) };
        }

        private CardPortraitGlyphMetrics MeasureCardPortraitNameGlyph(char glyph, int size)
        {
            cardDetailName.fontSize = size;
            Vector2 measured = cardDetailName.CalcSize(new GUIContent(glyph.ToString()));
            return new CardPortraitGlyphMetrics
            {
                Width = measured.x,
                Height = Mathf.Max(measured.y, cardDetailName.lineHeight)
            };
        }

        private void DrawCardPortraitBase(Rect r, GeneralDefinition definition, GeneralCard owned)
        {
            GUI.DrawTexture(r, cardPortraitBaseSkin, ScaleMode.StretchToFill, true);
            Rect seal = new Rect(r.x + 8, r.y + 8, 64, 52);
            RectFill(seal, C("151C1B"));
            Label(seal.x, seal.y - 1, seal.width, 18, owned == null ? "图 鉴" : "等 级", cardPortraitSmall);
            GUI.Label(new Rect(seal.x, seal.y + 17, seal.width, 34), owned == null ? definition.faction : owned.level.ToString(), cardPortraitLevel);
            float x = r.x + 86, w = r.width - 98;
            if (owned == null)
            {
                HudFittedLabel(new Rect(x, r.y + 9, w, 26), !definition.s1 ? "旧版区域招募" : !definition.recruitable ? "历史资料 · 收藏" : "可从试玩招募取得", body, 16);
                Label(x, r.y + 37, w, 22, "COST " + definition.cost.ToString("0.0") + "    /    " + definition.troop + "兵", cardDetailCaption);
            }
            else
            {
                string state = world.formation.Contains(owned.uid) ? "已上阵" : "待编队";
                Label(x, r.y + 7, w * .55f, 24, state + "   ·   进阶 " + owned.rank, cardDetailCaption);
                float pipWidth = 14, pipX = r.xMax - 12 - definition.stars * (pipWidth + 4);
                for (int pip = 0; pip < definition.stars; pip++)
                {
                    Rect p = new Rect(pipX + pip * (pipWidth + 4), r.y + 15, pipWidth, 8);
                    RectFill(p, pip < owned.rank ? Gold : C("434B43"));
                }
                bool capped = owned.level >= owned.LevelCap;
                string experience = capped ? "等级已满 · " + owned.LevelCap : "经验 " + owned.experience + " / " + owned.level * 80;
                Label(x, r.y + 32, w, 22, experience, cardDetailCaption);
                Rect track = new Rect(x, r.yMax - 9, w, 3);
                RectFill(track, C("434B43"));
                float progress = capped ? 1 : Mathf.Clamp01(owned.experience / (owned.level * 80f));
                RectFill(new Rect(track.x, track.y, track.width * progress, track.height), Gold);
            }
        }

        private void DrawCardSeal(Rect rect, string glyph, Color accent)
        {
            // All four edges share the glyph's logical coordinates, including
            // safe-area scaling and scroll clipping. RotateAroundPivot unclips
            // the pivot a second time and displaces the frame on mobile.
            Color edge = Color.Lerp(accent, Gold, .55f);
            HudLine(rect, .5f, .02f, .98f, .5f, edge, 1);
            HudLine(rect, .98f, .5f, .5f, .98f, edge, 1);
            HudLine(rect, .5f, .98f, .02f, .5f, edge, 1);
            HudLine(rect, .02f, .5f, .5f, .02f, edge, 1);
            GUI.Label(rect, glyph, centered);
        }

        private bool CardDetailSectionButton(Rect rect, string label, bool selected)
        {
            bool hover = GUI.enabled && !mobileMode && rect.Contains(Event.current.mousePosition);
            if (selected || hover) GUI.DrawTexture(rect, cardChromeTabActiveSkin, ScaleMode.StretchToFill, true);
            CardChromeText(rect, label, 22, 18, TextAnchor.MiddleCenter, selected ? Paper : C("B9B3A5"));
            return ContentTileClicked(rect, "card-tab-" + label, 0);
        }

        private void DrawCardAttributes(float contentWidth, GeneralDefinition definition, GeneralCard owned, Rect viewport)
        {
            DrawCardAttributeDossier(contentWidth, definition, owned, viewport);
        }

        private static string CardDetailsText(GeneralDefinition g, GeneralCard owned, int section)
        {
            if (section == 1)
            {
                string native = string.IsNullOrWhiteSpace(g.nativeSkill) ? g.skill : g.nativeSkill;
                string description = string.IsNullOrWhiteSpace(g.nativeSkillText) ? (g.s1 ? "战法说明尚未收录。" : g.description) : g.nativeSkillText;
                return "自带战法  ·  " + native + "\n\n" + description + "\n\n学习战法  ·  " + (owned == null ? "请从已拥有卡查看" : string.IsNullOrEmpty(owned.learnedSkill) ? "未学习" : owned.learnedSkill)
                    + (g.s1 ? "\n\n当前展示资料效果；逐条战法结算与拆解尚未开放。" : "");
            }
            if (section == 2)
                return (g.s1 ? (g.recruitable ? "获取：当前试玩招募池\n" : "获取：资料收藏，当前不进入招募池\n") : "获取：旧版区域招募\n")
                    + "\n人物列传\n" + (string.IsNullOrWhiteSpace(g.biography) ? (string.IsNullOrWhiteSpace(g.description)?"暂无人物介绍。":g.description) : g.biography.Replace("\\n","\n"))
                    + (string.IsNullOrWhiteSpace(g.combination)?"":"\n\n武将组合\n"+g.combination)
                    + (string.IsNullOrWhiteSpace(g.policyName)?"":"\n\n内政资料 · "+g.policyName+"\n"+g.policyText+"\n（组合与内政效果当前仅供查阅）")
                    + "\n\n资料依据\n" + (string.IsNullOrWhiteSpace(g.evidence) ? "本地原型定义" : g.evidence)
                    + "\n\n卡片 ID：" + g.id + (string.IsNullOrWhiteSpace(g.sourceUrl) ? "" : "\n来源：" + g.sourceUrl)
                    + (g.s1 ? "\n\n资料库包含历史身份；不代表现行官方完整 S1 卡池。" : "");
            string result = owned == null ? "基础属性预览\n招募后可查看等级、经验与进阶。" : "等级  " + owned.level + " / " + owned.LevelCap + "     进阶  " + owned.rank + " / " + g.stars + "\n经验  " + owned.experience + " / " + (owned.level >= owned.LevelCap ? "已满级" : (owned.level * 80).ToString());
            result += g.s1?"\n\n可转兵种  "+(string.IsNullOrWhiteSpace(g.advancedTroops)?"资料未收录":g.advancedTroops)+"\n上方为基础属性，成长值按资料展示。":"\n\n旧版原型武将未收录完整官方属性。";
            return result;
        }
    }
}
