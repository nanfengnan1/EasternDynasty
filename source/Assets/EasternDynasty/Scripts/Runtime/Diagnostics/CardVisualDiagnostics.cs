using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private void FillCardVisualReview(ReviewState review)
        {
            var definition = Catalog.Generals.Find(g => g.id == cardDetailId);
            var owned = world.cards.Find(c => c.uid == cardDetailUid && c.definition == cardDetailId);
            review.cardDetailOwnedUid = cardDetailUid;
            review.cardDetailScrollY = cardDetailScroll.y;
            review.cardDetailName = definition == null ? "" : definition.name;
            review.cardDetailArchive = definition != null && definition.s1 && !definition.recruitable;
            review.cardDetailOwnedLevel = owned == null ? 0 : owned.level;
            review.cardDetailOwnedExperience = owned == null ? 0 : owned.experience;
            review.cardDetailOwnedLevelCap = owned == null ? 0 : owned.LevelCap;
            review.selectedCardUid = selectedCard;
            review.cardChromeStyle = definition == null ? "" : CardChromeLayout.Style;
            review.cardChromeAssetsReady = definition != null && CardChromeAssetsReady();
            review.cardChromeTextFits = definition != null && cardChromeTextFits;
            review.cardChromeRight = definition == null ? new float[0] : CityOverviewReviewBounds(cardChromeRight);
            bool attributes = definition != null && cardDetailSection == 0;
            review.cardDossierIdentity = definition == null ? new float[0] : CityOverviewReviewBounds(cardDossierIdentity);
            review.cardDossierAttributes = attributes ? CityOverviewReviewBounds(cardDossierAttributes) : new float[0];
            review.cardDossierNative = attributes ? CityOverviewReviewBounds(cardDossierNative) : new float[0];
            review.cardDossierLearned = attributes ? CityOverviewReviewBounds(cardDossierLearned) : new float[0];
            review.cardDossierNativeName = definition == null ? "" : string.IsNullOrWhiteSpace(definition.nativeSkill) ? definition.skill : definition.nativeSkill;
            review.cardDossierLearnedName = definition == null ? "" : CardDossierLearnedDescription(owned);
            review.cardDossierDesignStyle = attributes ? CardDossierDesignLayout.Style : "";
            review.cardDossierDesignAssetsReady = attributes && CardDossierDesignAssetsReady();
            review.cardDossierDesignTextFits = attributes && cardDossierDesignTextFits;
            review.cardDossierDesignWidth = attributes ? cardDossierDesignWidth : 0;
            review.cardDossierAttributeIcons = attributes ? cardDossierAttributeIcons.SelectMany(CityOverviewReviewBounds).ToArray() : new float[0];
            review.cardDossierNativeImage = attributes ? CityOverviewReviewBounds(cardDossierNativeImage) : new float[0];
            review.cardDossierNativeNameArea = attributes ? CityOverviewReviewBounds(cardDossierNativeNameArea) : new float[0];
            review.cardDossierLearnedImage = attributes ? CityOverviewReviewBounds(cardDossierLearnedImage) : new float[0];
            review.cardDossierLearnedNameArea = attributes ? CityOverviewReviewBounds(cardDossierLearnedNameArea) : new float[0];
            review.cardPortraitStyle = definition == null ? "" : CardPortraitDesignLayout.Style;
            review.cardPortraitDesignAssetsReady = definition != null && CardPortraitDesignAssetsReady();
            review.cardPortraitFrame = definition == null ? new float[0] : CityOverviewReviewBounds(cardPortraitFrame);
            review.cardPortraitImage = definition == null ? new float[0] : CityOverviewReviewBounds(cardPortraitImage);
            review.cardPortraitBanner = definition == null ? new float[0] : CityOverviewReviewBounds(cardPortraitBanner);
            review.cardPortraitCrown = definition == null ? new float[0] : CityOverviewReviewBounds(cardPortraitCrown);
            review.cardPortraitBase = definition == null ? new float[0] : CityOverviewReviewBounds(cardPortraitBase);
            Texture2D texture = null;
            if (definition != null) portraits.TryGetValue(definition.id, out texture);
            review.cardPortraitSourceAspect = texture != null ? texture.width / (float)texture.height : 0;
            review.cardPortraitStars = definition == null ? 0 : definition.stars;
            review.cardPortraitRank = owned == null ? 0 : owned.rank;
            review.cardPortraitNameFontSize = cardDetailName == null ? 0 : cardDetailName.fontSize;
            review.cardPortraitNameLineHeight = cardDetailName == null ? 0 : cardDetailName.lineHeight;
            review.cardPortraitNameStep = definition == null ? 0 : cardPortraitNameStep;
            review.cardPortraitNameCellHeight = review.cardPortraitNameStep;
            review.cardPortraitNameGlyphWidth = 0;
            review.cardPortraitNameGlyphHeight = 0;
            if (definition != null && cardDetailName != null)
                foreach (char glyph in definition.name)
                {
                    Vector2 measured = cardDetailName.CalcSize(new GUIContent(glyph.ToString()));
                    review.cardPortraitNameGlyphWidth = Mathf.Max(review.cardPortraitNameGlyphWidth, measured.x);
                    review.cardPortraitNameGlyphHeight = Mathf.Max(review.cardPortraitNameGlyphHeight, measured.y);
                }
            review.cardPortraitRankFontSize = cardPortraitRank == null ? 0 : cardPortraitRank.fontSize;
            review.cardPortraitRankLineHeight = cardPortraitRank == null ? 0 : cardPortraitRank.lineHeight;
            Vector2 starSize = cardPortraitRank == null ? Vector2.zero : cardPortraitRank.CalcSize(new GUIContent("★"));
            review.cardPortraitRankGlyphWidth = starSize.x;
            review.cardPortraitRankGlyphHeight = starSize.y;
            bool skill = definition != null && cardDetailSection == 1 && cardDossierSkillViewport.height > 0;
            review.cardSkillViewport = skill ? CityOverviewReviewBounds(cardDossierSkillViewport) : new float[0];
            review.cardSkillContentHeight = skill ? cardDossierSkillLayout.ContentHeight : 0;
            review.cardSkillScrollLimit = skill ? cardDossierSkillLayout.ScrollLimit(cardDossierSkillViewport.height) : 0;
            review.cardSkillStyle = skill ? CardDossierSkillLayout.Style : "";
            review.cardSkillAssetsReady = skill && CardSkillSheetAssetsReady();
            review.cardSkillTextFits = skill && cardSkillSheetTextFits;
            review.cardSkillSheet = skill ? CityOverviewReviewBounds(cardSkillSheetArea) : new float[0];
            review.cardSkillImage = skill ? CityOverviewReviewBounds(cardSkillSheetImage) : new float[0];
            review.cardSkillTitle = skill ? CityOverviewReviewBounds(cardSkillSheetTitle) : new float[0];
            review.cardSkillDescription = skill ? CityOverviewReviewBounds(cardDossierSkillDescription) : new float[0];
        }

        private sealed partial class ReviewState
        {
            public string cardDetailName;
            public float cardDetailScrollY;
            public bool cardDetailArchive;
            public int cardDetailOwnedUid, cardDetailOwnedLevel, cardDetailOwnedExperience, cardDetailOwnedLevelCap, selectedCardUid;
            public string cardChromeStyle;
            public bool cardChromeAssetsReady, cardChromeTextFits;
            public float[] cardChromeRight;
            public float[] cardDossierIdentity, cardDossierAttributes, cardDossierNative, cardDossierLearned;
            public string cardDossierNativeName, cardDossierLearnedName;
            public string cardDossierDesignStyle;
            public bool cardDossierDesignAssetsReady, cardDossierDesignTextFits;
            public float cardDossierDesignWidth;
            public float[] cardDossierAttributeIcons, cardDossierNativeImage, cardDossierNativeNameArea, cardDossierLearnedImage, cardDossierLearnedNameArea;
            public string cardPortraitStyle;
            public bool cardPortraitDesignAssetsReady;
            public float[] cardPortraitFrame, cardPortraitImage, cardPortraitBanner, cardPortraitCrown, cardPortraitBase;
            public float cardPortraitSourceAspect;
            public int cardPortraitStars, cardPortraitRank;
            public int cardPortraitNameFontSize;
            public float cardPortraitNameLineHeight, cardPortraitNameCellHeight;
            public float cardPortraitNameStep, cardPortraitNameGlyphWidth, cardPortraitNameGlyphHeight;
            public int cardPortraitRankFontSize;
            public float cardPortraitRankLineHeight, cardPortraitRankGlyphWidth, cardPortraitRankGlyphHeight;
            public float[] cardSkillViewport;
            public float cardSkillContentHeight, cardSkillScrollLimit;
            public string cardSkillStyle;
            public bool cardSkillAssetsReady, cardSkillTextFits;
            public float[] cardSkillSheet, cardSkillImage, cardSkillTitle, cardSkillDescription;
        }

        private IEnumerator CardVisualSmoke()
        {
            // Smoke already owns a read-only campaign. Pause its simulation and
            // compare the entire actual World through open, tabs, scroll and back.
            float previousSpeed = speed;
            bool previousReview = reviewMode;
            string previousTab = tab, previousDetail = cardDetailId;
            int previousSelected = selectedCard, previousUid = cardDetailUid, previousSection = cardDetailSection;
            var previousScroll = cardDetailScroll;
            speed = 0; reviewMode = true;
            string unchanged = JsonUtility.ToJson(world);
            try
            {
                var preview = Catalog.Generals.First(g => g.s1 && g.recruitable);
                tab = "图鉴"; ShowCardDetails(preview.id);
                yield return CaptureState("smoke-card-full.png");
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "recruitable preview");
                yield return CaptureState("smoke-card-design.png");
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "exported portrait column design");
                yield return CaptureState("smoke-card-dossier-design.png");
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "exported inline attributes and horizontal skill design");
                yield return CaptureState("smoke-card-chrome.png");
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "exported compact navigation, identity band and preview footer");
                var longestNativeName = Catalog.Generals.Where(g => g.s1)
                    .OrderByDescending(g => (string.IsNullOrWhiteSpace(g.nativeSkill) ? g.skill : g.nativeSkill).Length)
                    .ThenBy(g => g.id).First();
                ShowCardDetails(longestNativeName.id);
                yield return CaptureState("smoke-card-dossier-long-skill.png");
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "longest actual native skill name in horizontal attribute slot");
                Debug.Log("CARD_DOSSIER_DESIGN_LONG_NAME: id=" + longestNativeName.id + "; native="
                    + (string.IsNullOrWhiteSpace(longestNativeName.nativeSkill) ? longestNativeName.skill : longestNativeName.nativeSkill)
                    + "; actual font fits=" + cardDossierDesignTextFits + ".");
                ShowCardDetails(preview.id);
                if (!BlocksMapInput || cardDetailUid != 0 || cardDetailId != preview.id || cardDetailSection != 0)
                    throw new Exception("Card preview lost its definition identity or map blocking.");

                cardDetailSection = 1; cardDetailScroll = Vector2.zero;
                yield return CaptureState("smoke-card-skill.png");
                AssertCardSkillContent(false);
                yield return CaptureState("smoke-card-skill-sheet.png");
                AssertCardSkillContent(false);
                Debug.Log("CARD_SKILL_SHEET_RUNTIME_OK: inspected Inkscape sheet, complete text, distinct image/effect columns and real title font fit.");
                AssertCardVisualUnchanged(unchanged, "native skill section");
                AssertCardDossierGeometry();
                CloseCardDetails();
                AssertCardVisualClosed(unchanged, "图鉴");

                var archive = Catalog.Generals.Where(g => g.s1 && !g.recruitable)
                    .OrderByDescending(g => CardDetailsText(g, null, 2).Length).First();
                ShowCardDetails(archive.id); cardDetailSection = 2;
                yield return CaptureState("smoke-card-archive-source.png");
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "historical source section");
                if (cardDetailUid != 0 || archive.recruitable)
                    throw new Exception("Historical card preview was treated as an owned or recruitable instance.");
                cardDetailScroll = new Vector2(0, 10000);
                yield return CaptureState("smoke-card-archive-scrolled.png");
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "source scroll");
                if (cardDetailScroll.y <= 0)
                    throw new Exception("Long historical card source did not expose its real scroll content.");
                HandleMobileBack();
                AssertCardVisualClosed(unchanged, "图鉴");

                var longest = Catalog.Generals.Where(g => g.s1).OrderByDescending(g => g.name.Length).ThenBy(g => g.id).First();
                ShowCardDetails(longest.id);
                yield return CaptureState("smoke-card-long-name.png");
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "longest source name");
                CloseCardDetails();
                AssertCardVisualClosed(unchanged, "图鉴");

                var owned = world.cards.First(c => c.Definition != null && c.Definition.s1);
                tab = "武将"; ShowCardDetails(owned.definition, owned.uid);
                yield return CaptureState("smoke-card-owned.png");
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "owned card attributes and actual experience");
                yield return CaptureState("smoke-card-chrome-owned.png");
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "owned footer keeps actual manage and return actions separate");
                if (selectedCard != owned.uid || cardDetailUid != owned.uid || cardDetailId != owned.definition)
                    throw new Exception("Owned card preview did not retain its actual owned UID.");
                cardDetailSection = 1; cardDetailScroll = Vector2.zero;
                yield return CaptureState("smoke-card-skill-sheet-owned.png");
                AssertCardSkillContent(false);
                AssertCardVisualUnchanged(unchanged, "owned skill sheet retains actual learned status");
                HandleMobileBack();
                AssertCardVisualClosed(unchanged, "武将");
                var longestSkill = Catalog.Generals.Where(g => g.s1)
                    .OrderByDescending(g => CardDossierSkillHeight(cardDossierSkillWidth, g, null)).ThenBy(g => g.id).First();
                tab = "图鉴"; ShowCardDetails(longestSkill.id); cardDetailSection = 1;
                yield return CaptureState("smoke-card-skill-long.png");
                AssertCardDossierGeometry();
                AssertCardSkillContent(false);
                AssertCardVisualUnchanged(unchanged, "longest skill description");
                cardDetailScroll = new Vector2(0, 10000);
                yield return CaptureState("smoke-card-skill-scrolled.png");
                AssertCardSkillContent(true);
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "longest skill content end");
                HandleMobileBack();
                AssertCardVisualClosed(unchanged, "图鉴");
                var lowRarity = Catalog.Generals.Where(g => g.s1).OrderBy(g => g.stars).ThenBy(g => g.id).First();
                ShowCardDetails(lowRarity.id);
                yield return CaptureState("smoke-card-low-rarity.png");
                AssertCardDossierGeometry();
                AssertCardVisualUnchanged(unchanged, "low rarity portrait with its own source aspect");
                HandleMobileBack();
                AssertCardVisualClosed(unchanged, "图鉴");
                Debug.Log("CARD_PORTRAIT_RUNTIME_OK: complete source aspect, separate name pennant, rarity crown and real owned-or-preview base remain within safe area without overlap; low rarity, long name and owned states preserve the whole World. Visual quality and desktop input require publisher review.");
                Debug.Log("CARD_PORTRAIT_DESIGN_RUNTIME_OK: inspected Inkscape portrait column dimensions, complete uncropped source at no smaller scale, measured 44-wide name cells and individually fitted 48x30 vertical stars preserve the safe area and complete World. Actual clicks and new screenshot comparison remain publisher checks.");
                Debug.Log("CARD_DOSSIER_RUNTIME_OK: identity band, grouped attributes and both visible skill slots contained in actual viewport; font-measured longest skill, actual overflow-or-fit scroll limit and visible final text preserve whole World through return. New screenshots require visual inspection for pictogram alignment.");
                Debug.Log("CARD_DOSSIER_DESIGN_RUNTIME_OK: five controlled Inkscape symbols, measured Chinese attribute text including longest actual native name, inline value/growth columns and both horizontal image/name skill regions match the inspected design inside the real viewport; full campaign unchanged. New screenshots and four actual image/name clicks remain publisher checks.");
                Debug.Log("CARD_VISUAL_RUNTIME_OK: actual recruitable, historical source, scrolled source, longest-name and owned card views; whole World unchanged through open, tabs, scroll and mobile back; desktop clicks are validated separately.");
                Debug.Log("CARD_CHROME_RUNTIME_OK: controlled Inkscape navigation, identity and footer assets, actual Chinese font fit and real visible targets match the inspected design; all three sections and owned/preview states keep disjoint safe-area controls and 56 more logical pixels of content. Whole World unchanged; actual clicks and new screenshot comparison remain publisher checks.");
            }
            finally
            {
                speed = previousSpeed; reviewMode = previousReview; tab = previousTab; selectedCard = previousSelected;
                cardDetailId = previousDetail; cardDetailUid = previousUid; cardDetailSection = previousSection; cardDetailScroll = previousScroll;
            }
        }

        private void AssertCardVisualUnchanged(string unchanged, string operation)
        {
            if (JsonUtility.ToJson(world) != unchanged)
                throw new Exception("Card browsing changed the campaign during " + operation + ".");
        }

        private void AssertCardSkillContent(bool atEnd)
        {
            var definition = Catalog.Generals.Find(g => g.id == cardDetailId);
            var owned = world.cards.Find(c => c.uid == cardDetailUid && c.definition == cardDetailId);
            if (definition == null || cardDetailSection != 1 || cardDossierSkillViewport.height <= 0)
                throw new Exception("Skill content checked outside the visible skill page.");
            Rect viewport = cardDossierSkillViewport;
            if (!CardSkillSheetAssetsReady() || !cardSkillSheetTextFits)
                throw new Exception("Skill sheet controlled textures or actual title font fit are missing.");
            if (cardSkillSheetImage.xMax + 30 > cardDossierSkillDescription.xMin
                || cardSkillSheetTitle.yMax + 30 > cardDossierSkillDescription.yMin
                || cardSkillSheetImage.width != 96 || cardSkillSheetImage.height != 96
                || cardDossierSkillDescription.yMax + 8 > cardSkillSheetArea.yMax)
                throw new Exception("Skill sheet image, title and complete description lost their inspected separation.");
            if (cardDetailScroll.y == 0 && (!CityOverviewContains(viewport, cardSkillSheetImage) || !CityOverviewContains(viewport, cardSkillSheetTitle)))
                throw new Exception("Skill sheet image or title is not visibly inside the initial viewport.");
            Rect[] textRegions = { cardDossierSkillDescription, cardDossierSkillLearnedText, cardDossierSkillFooter };
            string[] text = { CardDossierNativeDescription(definition), CardDossierLearnedDescription(owned), CardDossierScopeText };
            int count = definition.s1 ? 3 : 2;
            float previousBottom = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                Rect r = textRegions[i];
                GUIStyle style = i == 2 ? cardDetailCaption : cardDetailBody;
                float measuredHeight = style.CalcHeight(new GUIContent(text[i]), r.width);
                float contentBottom = r.yMax - viewport.y + cardDetailScroll.y;
                if (r.width <= 0 || measuredHeight > r.height + .05f || r.xMin < viewport.xMin || r.xMax > viewport.xMax
                    || r.yMin < previousBottom || contentBottom > cardDossierSkillLayout.ContentHeight + .05f)
                    throw new Exception("Actual skill text is clipped, overlapping or outside its scroll content: " + i);
                previousBottom = r.yMax;
            }
            float limit = cardDossierSkillLayout.ScrollLimit(viewport.height);
            if (atEnd)
            {
                // A longest paragraph can fit the whole desktop viewport. Its
                // content must then stay at zero; only actual overflow must scroll.
                // Keep the positive-scroll assertion for real overflowing text.
                if (limit > .05f && cardDetailScroll.y <= 0)
                    throw new Exception("Longest actual skill text did not expose its scroll content.");
                if (Mathf.Abs(cardDetailScroll.y - limit) > .05f)
                    throw new Exception("Skill scrolling did not reach its measured content end: actual="
                        + cardDetailScroll.y + "; expected=" + limit + ".");
                Rect last = textRegions[count - 1];
                if (last.yMin < viewport.yMin - .05f || last.yMax > viewport.yMax + .05f)
                    throw new Exception("Final actual skill text is not fully visible at the scroll end.");
                Debug.Log("CARD_SKILL_SCROLL_BOUNDS: id=" + definition.id + "; viewport=" + viewport.height
                    + "; content=" + cardDossierSkillLayout.ContentHeight + "; limit=" + limit + "; actual=" + cardDetailScroll.y + ".");
            }
            if (limit <= .05f)
            {
                if (Mathf.Abs(cardDetailScroll.y) > .05f) throw new Exception("Fitting skill content acquired a spurious scroll offset.");
                for (int i = 0; i < count; i++)
                    if (!CityOverviewContains(viewport, textRegions[i]))
                        throw new Exception("Non-overflowing skill text is not fully visible: " + i);
            }
        }

        private void AssertCardDossierGeometry()
        {
            if (cardDetailId == null || !BlocksMapInput) throw new Exception("Dossier geometry checked outside its modal.");
            AssertCardPortraitGeometry();
            AssertCardChromeGeometry();
            string[] controls = { "card-close", "card-return", "card-attributes", "card-skill", "card-source", "card-scroll" };
            foreach (string id in controls)
            {
                ReviewTarget target;
                if (!reviewTargets.TryGetValue(id, out target)) throw new Exception("Missing visible dossier control: " + id);
                Rect r = ArchiveReviewRect(target);
                if (!CityOverviewContains(uiPixels, r)) throw new Exception("Dossier control exceeds safe area: " + id);
                if (id != "card-scroll" && (r.height < 44 * scale || r.width < 44 * scale)) throw new Exception("Dossier touch target too small: " + id);
            }
            Rect viewport = ArchiveReviewRect(reviewTargets["card-scroll"]);
            Rect identity = GuiRectToPixels(cardDossierIdentity);
            if (!CityOverviewContains(uiPixels, identity) || identity.Overlaps(viewport)) throw new Exception("Dossier identity band overlaps content or safe area.");
            if (cardDetailSection != 0) return;
            AssertCardDossierDesignGeometry(viewport);
            Rect attributes = GuiRectToPixels(cardDossierAttributes);
            Rect native = GuiRectToPixels(cardDossierNative), learned = GuiRectToPixels(cardDossierLearned);
            if (attributes.Overlaps(native) || attributes.Overlaps(learned) || native.Overlaps(learned))
                throw new Exception("Attribute groups and skill rail overlap.");
            foreach (string id in new[] { "card-native-skill", "card-learned-skill" })
            {
                ReviewTarget target;
                if (!reviewTargets.TryGetValue(id, out target) || !CityOverviewContains(viewport, ArchiveReviewRect(target)))
                    throw new Exception("Initial dossier skill slot is not fully visible: " + id);
                Rect expected = id == "card-native-skill" ? native : learned;
                Rect actual = ArchiveReviewRect(target);
                if (Mathf.Abs(actual.x - expected.x) > .05f || Mathf.Abs(actual.y - expected.y) > .05f
                    || Mathf.Abs(actual.width - expected.width) > .05f || Mathf.Abs(actual.height - expected.height) > .05f)
                    throw new Exception("Dossier slot target does not match the actual drawn card: " + id);
            }
        }

        private void AssertCardChromeGeometry()
        {
            if (!CardChromeAssetsReady() || !cardChromeTextFits)
                throw new Exception("Card chrome export failed to load or its actual Chinese text exceeds the inspected design.");
            Rect right = cardChromeRight;
            if (Mathf.Abs(right.x - cardPortraitFrame.xMax - 28) > .02f
                || Mathf.Abs(right.y - cardPortraitFrame.y + 14) > .02f
                || Mathf.Abs(right.height - cardPortraitFrame.height - 28) > .02f
                || Mathf.Abs(right.width - (cardPortraitFrame.width / .43f - cardPortraitFrame.width - 72)) > .02f)
                throw new Exception("Card chrome changed the surrounding modal or complete portrait allocation.");
            var design = new CardPortraitDesignLayout.Box(right.x, right.y, right.width, right.height);
            string[] ids = { "card-attributes", "card-skill", "card-source", "card-close", "card-scroll", "card-return" };
            var expected = new[] { CardChromeLayout.Tab(design, 0), CardChromeLayout.Tab(design, 1),
                CardChromeLayout.Tab(design, 2), CardChromeLayout.Close(design), CardChromeLayout.Viewport(design), CardChromeLayout.Return(design) };
            var bounds = new System.Collections.Generic.List<Rect>();
            for (int i = 0; i < ids.Length; i++)
                bounds.Add(AssertCardChromeTarget(ids[i], expected[i], i != 4));
            AssertCardPortraitDesignBox(cardDossierIdentity, CardChromeLayout.Identity(design), "chrome identity band");
            Rect identity = GuiRectToPixels(cardDossierIdentity);
            if (!CityOverviewContains(uiPixels, identity)) throw new Exception("Chrome identity band leaves the safe area.");
            bounds.Add(identity);
            var owned = world.cards.Find(c => c.uid == cardDetailUid && c.definition == cardDetailId);
            if (owned != null) bounds.Add(AssertCardChromeTarget("owned-card-manage", CardChromeLayout.Manage(design), true));
            else if (reviewTargets.ContainsKey("owned-card-manage"))
                throw new Exception("Preview chrome exposes a stale owned management target.");
            Rect portrait = GuiRectToPixels(cardPortraitFrame);
            for (int i = 0; i < bounds.Count; i++)
            {
                if (portrait.Overlaps(bounds[i])) throw new Exception("Card chrome overlaps the complete portrait: " + i);
                for (int j = 0; j < i; j++)
                {
                    // Floating-point division of three adjacent tabs can differ
                    // at the subpixel boundary, but never by a visible pixel.
                    Rect a = bounds[i], b = bounds[j];
                    if (a.xMin < b.xMax - .05f && a.xMax > b.xMin + .05f
                        && a.yMin < b.yMax - .05f && a.yMax > b.yMin + .05f)
                        throw new Exception("Card chrome visible controls overlap: " + i + "/" + j);
                }
            }
            Rect viewport = bounds[4];
            if (Mathf.Abs(viewport.yMin - GuiRectToPixels(new Rect(right.x, right.y + 142, 1, 1)).yMin) > .05f
                || Mathf.Abs(viewport.height - (right.height - 226) * scale) > .05f
                || viewport.height / scale < CardDossierDesignLayout.ContentHeight)
                throw new Exception("Card chrome lost the inspected content gain or clips the first attribute page.");
        }

        private Rect AssertCardChromeTarget(string id, CardPortraitDesignLayout.Box expected, bool touch)
        {
            ReviewTarget target;
            if (!reviewTargets.TryGetValue(id, out target)) throw new Exception("Missing actually drawn chrome target: " + id);
            Rect actual = ArchiveReviewRect(target);
            Rect designed = GuiRectToPixels(new Rect(expected.X, expected.Y, expected.W, expected.H));
            if (Mathf.Abs(actual.x - designed.x) + Mathf.Abs(actual.y - designed.y)
                + Mathf.Abs(actual.width - designed.width) + Mathf.Abs(actual.height - designed.height) > .1f)
                throw new Exception("Actual chrome target differs from its inspected drawing: " + id);
            if (!CityOverviewContains(uiPixels, actual) || (touch && (actual.width < 44 * scale || actual.height < 44 * scale)))
                throw new Exception("Actual chrome target is clipped or too small: " + id);
            return actual;
        }

        private void AssertCardDossierDesignGeometry(Rect viewportPixels)
        {
            if (!CardDossierDesignAssetsReady() || !cardDossierDesignTextFits)
                throw new Exception("Dossier exported assets failed to load or actual Chinese text exceeds its design region.");
            Rect viewport = new Rect(cardDossierAttributes.x - 8, cardDossierAttributes.y + cardDetailScroll.y,
                cardDossierDesignWidth + 22, viewportPixels.height / scale);
            if (Mathf.Abs(viewport.width * scale - viewportPixels.width) > .05f
                || Mathf.Abs(GuiRectToPixels(viewport).x - viewportPixels.x) > .05f
                || Mathf.Abs(GuiRectToPixels(viewport).y - viewportPixels.y) > .05f)
                throw new Exception("Dossier content origin or width differs from its actual scroll viewport.");
            AssertCardDossierDesignBox(cardDossierAttributes, CardDossierDesignLayout.Attributes(cardDossierDesignWidth), viewport, "attributes");
            for (int i = 0; i < cardDossierAttributeIcons.Length; i++)
            {
                AssertCardDossierDesignBox(cardDossierAttributeIcons[i], CardDossierDesignLayout.Icon(cardDossierDesignWidth, i), viewport, "attribute icon " + i);
                if (!CityOverviewContains(cardDossierAttributes, cardDossierAttributeIcons[i]))
                    throw new Exception("Actual attribute icon exceeds its visible attribute group: " + i);
            }
            Rect[] slots = { cardDossierNative, cardDossierLearned };
            Rect[] images = { cardDossierNativeImage, cardDossierLearnedImage };
            Rect[] names = { cardDossierNativeNameArea, cardDossierLearnedNameArea };
            for (int i = 0; i < slots.Length; i++)
            {
                var designed = CardDossierDesignLayout.Skill(cardDossierDesignWidth, i);
                AssertCardDossierDesignBox(slots[i], designed, viewport, "skill " + i);
                AssertCardDossierDesignBox(images[i], CardDossierDesignLayout.SkillImage(designed), viewport, "skill image " + i);
                AssertCardDossierDesignBox(names[i], CardDossierDesignLayout.SkillName(designed), viewport, "skill name " + i);
                if (!CityOverviewContains(slots[i], images[i]) || !CityOverviewContains(slots[i], names[i])
                    || images[i].Overlaps(names[i]) || !CityOverviewContains(viewportPixels, GuiRectToPixels(slots[i]))
                    || slots[i].height < 44 || slots[i].width < 44)
                    throw new Exception("Designed skill image/name is clipped, overlapping or not a complete visible touch action: " + i);
            }
            if (Mathf.Abs(slots[0].y - slots[1].y) > .01f || slots[0].yMin < cardDossierAttributes.yMax + 19.99f)
                throw new Exception("Skill cards do not form the separated lower row in the inspected Inkscape design.");
        }

        private void AssertCardDossierDesignBox(Rect actual, CardDossierDesignLayout.Box expected, Rect viewport, string name)
        {
            Rect global = CardDossierViewportRect(new Rect(expected.X, expected.Y, expected.W, expected.H), viewport);
            if (Mathf.Abs(actual.x - global.x) + Mathf.Abs(actual.y - global.y)
                + Mathf.Abs(actual.width - global.width) + Mathf.Abs(actual.height - global.height) > .02f)
                throw new Exception("Dossier region differs from the inspected design: " + name + ".");
        }

        private void AssertCardPortraitGeometry()
        {
            if (!CardPortraitDesignAssetsReady())
                throw new Exception("Portrait Inkscape ribbon, seal or base export failed to load at its controlled dimensions.");
            Rect frame = GuiRectToPixels(cardPortraitFrame);
            if (!CityOverviewContains(uiPixels, frame)) throw new Exception("Portrait frame exceeds the actual safe area.");
            Rect[] regions = { cardPortraitImage, cardPortraitBanner, cardPortraitCrown, cardPortraitBase };
            for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i].width <= 0 || regions[i].height <= 0 || !CityOverviewContains(frame, GuiRectToPixels(regions[i])))
                    throw new Exception("Portrait identity or artwork region is clipped: " + i);
                for (int j = 0; j < i; j++)
                    if (regions[i].Overlaps(regions[j])) throw new Exception("Portrait artwork and identity regions overlap: " + i + "/" + j);
            }
            Texture2D texture;
            if (!portraits.TryGetValue(cardDetailId, out texture) || texture == null)
                throw new Exception("S1 portrait source failed to load for " + cardDetailId);
            if (Mathf.Abs(cardPortraitImage.width / cardPortraitImage.height - texture.width / (float)texture.height) > .001f)
                throw new Exception("Portrait source aspect was changed by its frame.");
            var definition = Catalog.Generals.Find(g => g.id == cardDetailId);
            var art = new CardPortraitDesignLayout.Box(cardPortraitFrame.x, cardPortraitFrame.y,
                cardPortraitFrame.width, cardPortraitFrame.height);
            var imageArea = CardPortraitDesignLayout.ImageArea(art);
            AssertCardPortraitDesignBox(cardPortraitImage, CardPortraitDesignLayout.FitImage(imageArea, texture.width, texture.height), "image");
            AssertCardPortraitDesignBox(cardPortraitBanner, CardPortraitDesignLayout.Banner(art, definition.name.Length), "banner");
            AssertCardPortraitDesignBox(cardPortraitCrown, CardPortraitDesignLayout.Stars(art, definition.name.Length, definition.stars), "vertical stars");
            AssertCardPortraitDesignBox(cardPortraitBase, CardPortraitDesignLayout.Base(art), "base");
            float oldScale = Mathf.Min((art.W - 82) / texture.width, (art.H - 137) / texture.height);
            if (cardPortraitImage.width + .01f < texture.width * oldScale || cardPortraitImage.height + .01f < texture.height * oldScale)
                throw new Exception("Portrait design shrank the complete source image.");
            float nameStep = cardPortraitNameStep;
            if (Mathf.Abs(nameStep - CardPortraitDesignLayout.NameStep(art.H, definition.name.Length)) > .01f)
                throw new Exception("Portrait actual name cells drifted from the inspected design.");
            foreach (char glyph in definition.name)
            {
                Vector2 measured = cardDetailName.CalcSize(new GUIContent(glyph.ToString()));
                if (measured.x > 44 || measured.y > nameStep || cardDetailName.lineHeight > nameStep)
                    throw new Exception("Portrait name glyph exceeds its actual pennant cell: id=" + definition.id
                        + "; glyph=" + glyph + "; fontSize=" + cardDetailName.fontSize + "; measured=" + measured
                        + "; lineHeight=" + cardDetailName.lineHeight + "; cell=44x" + nameStep + ".");
            }
            Vector2 star = cardPortraitRank.CalcSize(new GUIContent("★"));
            if (star.x > 48 || star.y > 30 || cardPortraitRank.lineHeight > 30)
                throw new Exception("Portrait individual rarity glyph exceeds its actual 48x30 cell: fontSize="
                    + cardPortraitRank.fontSize + "; measured=" + star + "; lineHeight=" + cardPortraitRank.lineHeight + ".");
            if (Mathf.Abs(cardPortraitCrown.x - cardPortraitBanner.x) > .01f
                || Mathf.Abs(cardPortraitCrown.y - cardPortraitBanner.yMax - 8) > .01f
                || Mathf.Abs(cardPortraitCrown.width - 48) > .01f
                || Mathf.Abs(cardPortraitCrown.height - definition.stars * 30) > .01f)
                throw new Exception("Portrait stars do not form the designed individual vertical cells below the name.");
            Rect dossier = GuiRectToPixels(cardDossierIdentity);
            if (frame.Overlaps(dossier)) throw new Exception("Portrait frame covers the dossier controls.");
        }

        private static void AssertCardPortraitDesignBox(Rect actual, CardPortraitDesignLayout.Box expected, string name)
        {
            if (Mathf.Abs(actual.x - expected.X) + Mathf.Abs(actual.y - expected.Y)
                + Mathf.Abs(actual.width - expected.W) + Mathf.Abs(actual.height - expected.H) > .02f)
                throw new Exception("Portrait region differs from the inspected design: " + name + ".");
        }

        private bool CardPortraitDesignAssetsReady()
        {
            return cardPortraitRibbonSkin != null && cardPortraitRibbonSkin.width == 96 && cardPortraitRibbonSkin.height == 480
                && cardPortraitSealSkin != null && cardPortraitSealSkin.width == 96 && cardPortraitSealSkin.height == 96
                && cardPortraitBaseSkin != null && cardPortraitBaseSkin.width == 1040 && cardPortraitBaseSkin.height == 136;
        }

        private void AssertCardVisualClosed(string unchanged, string expectedTab)
        {
            AssertCardVisualUnchanged(unchanged, "return to " + expectedTab);
            if (cardDetailId != null || cardDetailUid != 0 || cardDetailScroll != Vector2.zero || tab != expectedTab)
                throw new Exception("Card return lost its actual parent or retained a stale owned instance.");
        }
    }
}
