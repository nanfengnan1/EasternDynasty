using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private const float CardDossierAttributeHeight = CardDossierDesignLayout.ContentHeight;
        private GUIStyle dossierNumber, dossierGrowth, dossierSkillName, dossierEyebrow;
        private Rect cardDossierIdentity, cardDossierAttributes, cardDossierNative, cardDossierLearned;
        private const string CardDossierScopeText = "当前展示资料效果；逐条战法结算与拆解尚未开放。";
        private Rect cardDossierSkillViewport, cardDossierSkillDescription, cardDossierSkillLearnedText, cardDossierSkillFooter;
        private CardDossierSkillLayout cardDossierSkillLayout;
        private float cardDossierSkillWidth;
        private Texture2D[] cardDossierDesignIcons;
        private Texture2D cardDossierDesignRing, cardDossierDesignBook, cardDossierDesignBase;
        private Texture2D cardSkillSheetSkin, cardSkillLearningSkin;
        private Rect cardSkillSheetArea, cardSkillSheetImage, cardSkillSheetTitle;
        private bool cardSkillSheetTextFits;
        private float cardDossierDesignWidth;
        private bool cardDossierDesignTextFits;
        private Rect[] cardDossierAttributeIcons = new Rect[5];
        private Rect cardDossierNativeImage, cardDossierNativeNameArea, cardDossierLearnedImage, cardDossierLearnedNameArea;

        private void InitCardDossierStyles()
        {
            if (dossierNumber != null) return;
            dossierNumber = new GUIStyle(cardDetailValue) { fontSize = 29, wordWrap = false };
            dossierGrowth = new GUIStyle(cardDetailGrowth) { fontSize = 16, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            dossierSkillName = new GUIStyle(cardName) { fontSize = 19, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            dossierEyebrow = new GUIStyle(cardDetailCaption) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            string[] names = { "attack", "siege", "defense", "speed", "strategy" };
            cardDossierDesignIcons = new Texture2D[names.Length];
            for (int i=0;i<names.Length;i++) cardDossierDesignIcons[i]=GameAssets.LoadTexture("ui.youzhou.dossier_"+names[i]);
            cardDossierDesignRing=GameAssets.LoadTexture("ui.youzhou.dossier_skill_ring");
            cardDossierDesignBook=GameAssets.LoadTexture("ui.youzhou.nav_book");
            cardDossierDesignBase=GameAssets.LoadTexture("ui.youzhou.portrait_base");
            cardSkillSheetSkin=GameAssets.LoadTexture("ui.youzhou.skill_sheet");
            cardSkillLearningSkin=GameAssets.LoadTexture("ui.youzhou.skill_learning");
        }

        private void DrawCardIdentityBand(Rect r, GeneralDefinition g)
        {
            InitCardDossierStyles();
            cardDossierIdentity = r;
            GUI.DrawTexture(r, cardChromeIdentitySkin, ScaleMode.StretchToFill, true);
            float segment = r.width / 3;
            GUI.DrawTexture(new Rect(r.x + 8, r.y + 6, 28, 28), cardDossierDesignIcons[0], ScaleMode.StretchToFill, true);
            CardChromeText(new Rect(r.x + 44, r.y, segment - 48, r.height), g.faction + " · " + g.troop + "兵", 18, 16, TextAnchor.MiddleLeft, Paper);
            CardChromeText(new Rect(r.x + segment + 12, r.y, segment - 20, r.height), "攻击距离  " + (g.s1 ? g.attackDistance.ToString() : "—"), 18, 16, TextAnchor.MiddleLeft, Paper);
            CardChromeText(new Rect(r.x + segment * 2 + 18, r.y, segment - 24, r.height), "COST  " + g.cost.ToString("0.0"), 18, 16, TextAnchor.MiddleLeft, Paper);
        }

        private void DrawCardAttributeDossier(float width, GeneralDefinition g, GeneralCard owned, Rect viewport)
        {
            InitCardDossierStyles();
            cardDossierDesignWidth=width;
            cardDossierDesignTextFits=true;
            Rect main = DossierDesignRect(CardDossierDesignLayout.Attributes(width));
            cardDossierAttributes = CardDossierViewportRect(main, viewport);
            DrawDossierDesignText(new Rect(8,0,width-16,30),"基础属性",cardName,24,20);
            DrawDossierDesignText(new Rect(8,30,width-16,26),g.s1 ? "资料基础值  /  绿色数字为每级成长" : "旧版原型 · 未收录项保留空缺",cardDetailCaption,16,14);
            Rect status=DossierDesignRect(CardDossierDesignLayout.Status(width));
            GUI.DrawTexture(status,cardDossierDesignBase,ScaleMode.StretchToFill,true);
            Rect statusLeft=new Rect(status.x+10,status.y,status.width*.4f-10,status.height);
            Rect statusRight=new Rect(status.x+status.width*.4f,status.y,status.width*.6f-10,status.height);
            if (owned != null)
            {
                DrawDossierDesignText(statusLeft,"Lv."+owned.level+" / "+owned.LevelCap,body,18,16);
                string experience = owned.level >= owned.LevelCap ? "已满级" : owned.experience + " / " + owned.level * 80;
                DrawDossierDesignText(statusRight,"经验  "+experience,cardDetailCaption,16,14,TextAnchor.MiddleRight);
                Rect track = new Rect(status.x,status.yMax-1,status.width,3);
                RectFill(track, C("41443C"));
                float progress = owned.level >= owned.LevelCap ? 1 : Mathf.Clamp01(owned.experience / (owned.level * 80f));
                RectFill(new Rect(track.x, track.y, track.width * progress, track.height), Gold);
            }
            else
            {
                DrawDossierDesignText(statusLeft,"图鉴预览",body,18,16);
                DrawDossierDesignText(statusRight,"未关联持有卡等级",cardDetailCaption,16,14,TextAnchor.MiddleRight);
            }

            string[] labels = { "攻击", "攻城", "防御", "速度", "谋略" };
            double[] values = { g.attack, g.siege, g.defense, g.speed, g.strategy };
            double[] growth = { g.attackGrowth, g.siegeGrowth, g.defenseGrowth, g.speedGrowth, g.strategyGrowth };
            for (int i = 0; i < labels.Length; i++)
            {
                Rect icon=DossierDesignRect(CardDossierDesignLayout.Icon(width,i));
                GUI.DrawTexture(icon,cardDossierDesignIcons[i],ScaleMode.StretchToFill,true);
                cardDossierAttributeIcons[i]=CardDossierViewportRect(icon,viewport);
                DrawDossierDesignText(DossierDesignRect(CardDossierDesignLayout.Label(width,i)),labels[i],body,18,16);
                DrawDossierDesignText(DossierDesignRect(CardDossierDesignLayout.Value(width,i)),!g.s1 && i>0 ? "—" : values[i].ToString("0.##"),dossierNumber,23,18);
                if(g.s1) DrawDossierDesignText(DossierDesignRect(CardDossierDesignLayout.Growth(width,i)),"(+"+growth[i].ToString("0.00")+")",dossierGrowth,17,14);
            }
            string troops = g.s1 && !string.IsNullOrWhiteSpace(g.advancedTroops) ? g.advancedTroops : "资料未收录";
            Rect troopArea=DossierDesignRect(CardDossierDesignLayout.Troops(width));
            DrawDossierDesignText(new Rect(troopArea.x,troopArea.y,86,troopArea.height),"可转兵种",cardDetailCaption,16,14);
            DrawDossierDesignText(new Rect(troopArea.x+92,troopArea.y,troopArea.width-92,troopArea.height),troops,body,18,14);

            Rect native = DossierDesignRect(CardDossierDesignLayout.Skill(width,0));
            Rect learned = DossierDesignRect(CardDossierDesignLayout.Skill(width,1));
            cardDossierNative = CardDossierViewportRect(native, viewport);
            cardDossierLearned = CardDossierViewportRect(learned, viewport);
            string nativeName = string.IsNullOrWhiteSpace(g.nativeSkill) ? g.skill : g.nativeSkill;
            DrawCardDesignedSkillSlot(CardDossierDesignLayout.Skill(width,0),nativeName,"自带战法",viewport,0);
            string learnedName = owned == null ? "持有卡可查看" : string.IsNullOrEmpty(owned.learnedSkill) ? "尚未学习" : owned.learnedSkill;
            DrawCardDesignedSkillSlot(CardDossierDesignLayout.Skill(width,1),learnedName,"学习战法",viewport,1);
            CardDossierSkillTarget(native, viewport, "card-native-skill", g, owned);
            CardDossierSkillTarget(learned, viewport, "card-learned-skill", g, owned);
        }

        private Rect CardDossierViewportRect(Rect content, Rect viewport)
        { return new Rect(viewport.x + content.x, viewport.y + content.y - cardDetailScroll.y, content.width, content.height); }

        private void CardDossierSkillTarget(Rect content, Rect viewport, string id, GeneralDefinition g, GeneralCard owned)
        {
            Rect visible = CardDossierViewportRect(content, viewport);
            if (visible.xMin >= viewport.xMin && visible.xMax <= viewport.xMax && visible.yMin >= viewport.yMin && visible.yMax <= viewport.yMax)
                RecordReviewTarget(id, visible);
            if (ContentTileClicked(content, id + ":" + g.id, owned == null ? 0 : owned.uid))
            { cardDetailSection = 1; cardDetailScroll = Vector2.zero; }
        }

        private static Rect DossierDesignRect(CardDossierDesignLayout.Box r) { return new Rect(r.X,r.Y,r.W,r.H); }

        private bool CardDossierDesignAssetsReady()
        {
            if(cardDossierDesignIcons==null||cardDossierDesignIcons.Length!=5||cardDossierDesignRing==null||cardDossierDesignBook==null||cardDossierDesignBase==null)return false;
            foreach(var icon in cardDossierDesignIcons)if(icon==null||icon.width!=64||icon.height!=64)return false;
            return cardDossierDesignRing.width==144&&cardDossierDesignRing.height==144
                &&cardDossierDesignBook.width==66&&cardDossierDesignBook.height==36
                &&cardDossierDesignBase.width==1040&&cardDossierDesignBase.height==136;
        }

        private void DrawDossierDesignText(Rect r,string value,GUIStyle source,int preferred,int minimum,TextAnchor align=TextAnchor.MiddleLeft,bool wrap=false)
        {
            var style=new GUIStyle(source){fontSize=preferred,alignment=align,wordWrap=wrap};
            var content=new GUIContent(value);
            while(style.fontSize>minimum && (wrap?style.CalcHeight(content,r.width)>r.height:style.CalcSize(content).x>r.width||Mathf.Max(style.lineHeight,style.CalcSize(content).y)>r.height))style.fontSize--;
            bool fits=wrap?style.CalcHeight(content,r.width)<=r.height+.05f:style.CalcSize(content).x<=r.width+.05f&&Mathf.Max(style.lineHeight,style.CalcSize(content).y)<=r.height+.05f;
            cardDossierDesignTextFits &= fits;
            GUI.Label(r,content,style);
        }

        private void DrawCardDesignedSkillSlot(CardDossierDesignLayout.Box box,string name,string label,Rect viewport,int slot)
        {
            Rect r=DossierDesignRect(box),image=DossierDesignRect(CardDossierDesignLayout.SkillImage(box));
            Rect nameArea=DossierDesignRect(CardDossierDesignLayout.SkillName(box));
            GUI.DrawTexture(r,cardDossierDesignBase,ScaleMode.StretchToFill,true);
            GUI.DrawTexture(image,cardDossierDesignRing,ScaleMode.StretchToFill,true);
            GUI.DrawTexture(new Rect(r.x+20,r.y+37,48,26),cardDossierDesignBook,ScaleMode.StretchToFill,true);
            DrawDossierDesignText(DossierDesignRect(CardDossierDesignLayout.SkillTitle(box)),label,cardDetailCaption,16,14);
            DrawDossierDesignText(nameArea,name,dossierSkillName,19,16,TextAnchor.MiddleLeft,true);
            if(slot==0){cardDossierNativeImage=CardDossierViewportRect(image,viewport);cardDossierNativeNameArea=CardDossierViewportRect(nameArea,viewport);}
            else{cardDossierLearnedImage=CardDossierViewportRect(image,viewport);cardDossierLearnedNameArea=CardDossierViewportRect(nameArea,viewport);}
            if (GUI.enabled && r.Contains(Event.current.mousePosition)) Outline(r, Gold);
        }

        private float CardDossierSkillHeight(float width, GeneralDefinition g, GeneralCard owned)
        { return MeasureCardDossierSkillLayout(width, g, owned).ContentHeight; }

        private CardDossierSkillLayout MeasureCardDossierSkillLayout(float width, GeneralDefinition g, GeneralCard owned)
        {
            float textHeight = cardDetailBody.CalcHeight(new GUIContent(CardDossierNativeDescription(g)), CardDossierSkillLayout.TextWidth(width));
            string learned = CardDossierLearnedDescription(owned);
            float learnedHeight = cardDetailBody.CalcHeight(new GUIContent(learned), CardDossierSkillLayout.TextWidth(width));
            float footerHeight = g.s1 ? cardDetailCaption.CalcHeight(new GUIContent(CardDossierScopeText), width - 24) : 0;
            return CardDossierSkillLayout.Calculate(textHeight, learnedHeight, footerHeight, g.s1);
        }

        private static string CardDossierNativeDescription(GeneralDefinition g)
        { return string.IsNullOrWhiteSpace(g.nativeSkillText) ? (g.s1 ? "战法说明尚未收录。" : g.description) : g.nativeSkillText; }

        private static string CardDossierLearnedDescription(GeneralCard owned)
        { return owned == null ? "请从已拥有卡查看" : string.IsNullOrEmpty(owned.learnedSkill) ? "尚未学习战法" : owned.learnedSkill; }

        private void DrawCardSkillDossier(float width, GeneralDefinition g, GeneralCard owned, Rect viewport)
        {
            InitCardDossierStyles();
            cardDossierSkillWidth = width;
            cardDossierSkillViewport = viewport;
            cardDossierSkillLayout = MeasureCardDossierSkillLayout(width, g, owned);
            var layout = cardDossierSkillLayout;
            cardDossierDesignTextFits = true;
            Rect sheet = new Rect(8, 4, width - 16, layout.SheetHeight);
            DrawCardSkillSheetSurface(sheet);
            Rect title = new Rect(24, 8, width - 48, 32);
            string native = string.IsNullOrWhiteSpace(g.nativeSkill) ? g.skill : g.nativeSkill;
            var titleStyle = new GUIStyle(cardName); titleStyle.normal.textColor = C("D3BC83");
            var roleStyle = new GUIStyle(cardDetailCaption); roleStyle.normal.textColor = C("C2B597");
            DrawDossierDesignText(title, native, titleStyle, 24, 18, TextAnchor.MiddleCenter);
            Rect icon = new Rect(26, 78, 96, 96);
            GUI.DrawTexture(icon, cardDossierDesignRing, ScaleMode.ScaleToFit, true);
            GUI.DrawTexture(new Rect(41,108,66,36), cardDossierDesignBook, ScaleMode.ScaleToFit, true);
            DrawDossierDesignText(new Rect(14,184,120,26), "自带战法", roleStyle,16,14,TextAnchor.MiddleCenter);
            DrawDossierDesignText(new Rect(CardDossierSkillLayout.NativeX,56,CardDossierSkillLayout.TextWidth(width),28),"战法效果",titleStyle,16,14);
            cardSkillSheetArea = CardDossierViewportRect(sheet, viewport);
            cardSkillSheetImage = CardDossierViewportRect(icon, viewport);
            cardSkillSheetTitle = CardDossierViewportRect(title, viewport);

            string description = CardDossierNativeDescription(g);
            Rect nativeText = new Rect(CardDossierSkillLayout.NativeX, layout.NativeY, CardDossierSkillLayout.TextWidth(width), layout.NativeHeight);
            cardDossierSkillDescription = CardDossierViewportRect(nativeText, viewport);
            GUI.Label(nativeText, description, cardDetailBody);
            float y = layout.LearnedY;
            string learned = CardDossierLearnedDescription(owned);
            float learnedHeight = layout.LearnedHeight;
            Rect learnedArea = new Rect(8, y, width - 16, learnedHeight);
            GUI.DrawTexture(learnedArea, cardSkillLearningSkin, ScaleMode.StretchToFill, true);
            DrawDossierDesignText(new Rect(26,y+12,120,36),"学习战法",roleStyle,17,14);
            Rect learnedText = new Rect(CardDossierSkillLayout.NativeX, y + 18, CardDossierSkillLayout.TextWidth(width), learnedHeight - 36);
            cardDossierSkillLearnedText = CardDossierViewportRect(learnedText, viewport);
            GUI.Label(learnedText, learned, cardDetailBody);
            Rect footer = new Rect(12, layout.FooterY, width - 24, layout.FooterHeight);
            cardDossierSkillFooter = CardDossierViewportRect(footer, viewport);
            if (g.s1) GUI.Label(footer, CardDossierScopeText, cardDetailCaption);
            cardSkillSheetTextFits = cardDossierDesignTextFits;
        }

        // Three vertical slices preserve the inspected title-band and border
        // heights while long descriptions extend the center. No generated art.
        private void DrawCardSkillSheetSurface(Rect r)
        {
            const float top = 48, bottom = 4, sourceHeight = 240;
            GUI.DrawTextureWithTexCoords(new Rect(r.x,r.y,r.width,top),cardSkillSheetSkin,new Rect(0,1-top/sourceHeight,1,top/sourceHeight),true);
            GUI.DrawTextureWithTexCoords(new Rect(r.x,r.y+top,r.width,r.height-top-bottom),cardSkillSheetSkin,new Rect(0,bottom/sourceHeight,1,1-(top+bottom)/sourceHeight),true);
            GUI.DrawTextureWithTexCoords(new Rect(r.x,r.yMax-bottom,r.width,bottom),cardSkillSheetSkin,new Rect(0,0,1,bottom/sourceHeight),true);
        }

        private bool CardSkillSheetAssetsReady()
        {
            return cardSkillSheetSkin != null && cardSkillSheetSkin.width == 1240 && cardSkillSheetSkin.height == 480
                && cardSkillLearningSkin != null && cardSkillLearningSkin.width == 1240 && cardSkillLearningSkin.height == 168
                && cardDossierDesignRing != null && cardDossierDesignBook != null;
        }

        // Existing land-command pictograms; skill pages use imported design assets.
        private void DrawCardAttributeEmblem(Rect r, int kind, Color c)
        {
            if (kind == 0) // crossed blades
            {
                HudLine(r,.18f,.12f,.82f,.88f,c,3); HudLine(r,.82f,.12f,.18f,.88f,c,3);
                HudLine(r,.12f,.68f,.4f,.9f,c,2); HudLine(r,.6f,.9f,.88f,.68f,c,2);
                HudLine(r,.18f,.12f,.18f,.32f,c,2); HudLine(r,.82f,.12f,.82f,.32f,c,2);
            }
            else if (kind == 1) // siege gate
            {
                HudLine(r,.12f,.26f,.12f,.84f,c,3); HudLine(r,.88f,.26f,.88f,.84f,c,3);
                HudLine(r,.12f,.4f,.88f,.4f,c,3); HudLine(r,.12f,.84f,.88f,.84f,c,3);
                for(int i=0;i<3;i++) RectFill(new Rect(r.x+r.width*(.15f+i*.29f),r.y+r.height*.18f,r.width*.13f,r.height*.22f),c);
                HudLine(r,.4f,.84f,.4f,.59f,c,2); HudLine(r,.6f,.84f,.6f,.59f,c,2); HudLine(r,.4f,.59f,.6f,.59f,c,2);
            }
            else if (kind == 2) // shield
            {
                HudLine(r,.16f,.2f,.5f,.08f,c,2.5f); HudLine(r,.5f,.08f,.84f,.2f,c,2.5f);
                HudLine(r,.16f,.2f,.22f,.65f,c,2.5f); HudLine(r,.84f,.2f,.78f,.65f,c,2.5f);
                HudLine(r,.22f,.65f,.5f,.92f,c,2.5f); HudLine(r,.78f,.65f,.5f,.92f,c,2.5f); HudLine(r,.5f,.2f,.5f,.76f,c,2);
            }
            else if (kind == 3) // marching banner
            {
                HudLine(r,.36f,.12f,.36f,.91f,c,2.5f); HudLine(r,.36f,.15f,.87f,.27f,c,2.5f);
                HudLine(r,.87f,.27f,.36f,.5f,c,2.5f); HudLine(r,.12f,.65f,.28f,.65f,c,2); HudLine(r,.06f,.8f,.28f,.8f,c,2);
            }
            else // strategy scroll
            {
                HudLine(r,.18f,.18f,.18f,.85f,c,3); HudLine(r,.82f,.18f,.82f,.85f,c,3);
                HudLine(r,.18f,.24f,.82f,.24f,c,2); HudLine(r,.18f,.78f,.82f,.78f,c,2);
                HudLine(r,.34f,.41f,.68f,.41f,c,2); HudLine(r,.34f,.58f,.62f,.58f,c,2);
            }
        }
    }
}
