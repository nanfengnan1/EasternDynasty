using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private sealed partial class ReviewState
        {
            public bool archiveOpen, archiveAllSources;
            public string archiveSearch, archiveSelected;
            public float archiveScrollY;
            public int archiveCount;
            public string[] archiveVisibleIds;
            public string archiveDesignStyle;
            public bool archiveDesignAssetsReady, archiveDesignTextFits;
            public int archiveDesignColumns;
            public float archiveDesignCardHeight;
            public ReviewArchiveDesignNode[] archiveDesignNodes;
        }

        [Serializable] private sealed class ReviewArchiveDesignNode
        {
            public string id, name;
            public float[] card, art, nameArea, footer, troop, cost, stars, skill, scope;
        }

        private void FillArchiveVisualReview(ReviewState review)
        {
            review.archiveOpen = tab == "图鉴" && cardDetailId == null && drawResult == null && confirmation == null;
            review.archiveAllSources = s1Archives; review.archiveSearch = s1Search;
            review.archiveSelected = s1Selected; review.archiveScrollY = s1Scroll.y;
            review.archiveCount = ArchiveDefinitions().Count;
            // Observe only the current drawn controls. No hidden/cropped card is
            // registered here to make the desktop driver believe it is visible.
            review.archiveVisibleIds = review.archiveOpen ? reviewTargets.Keys
                .Where(id => id.StartsWith("archive-card-", StringComparison.Ordinal))
                .Select(id => id.Substring(13)).OrderBy(id => id).ToArray() : new string[0];
            review.archiveDesignStyle = review.archiveOpen ? "inkscape-archive-compact-v1" : "";
            review.archiveDesignAssetsReady = review.archiveOpen && ArchiveDesignAssetsReady();
            review.archiveDesignTextFits = review.archiveOpen && archiveDesignTextFits;
            review.archiveDesignColumns = review.archiveOpen ? archiveDesignColumns : 0;
            review.archiveDesignCardHeight = review.archiveOpen ? archiveDesignCardHeight : 0;
            review.archiveDesignNodes = review.archiveOpen ? archiveDesignNodes.Select(n => new ReviewArchiveDesignNode {
                id = n.Id, name = n.Name, card = CityOverviewReviewBounds(n.Card), art = CityOverviewReviewBounds(n.Art),
                nameArea = CityOverviewReviewBounds(n.NameArea), footer = CityOverviewReviewBounds(n.Footer),
                troop = CityOverviewReviewBounds(ArchiveDesignRect(n.Card, ArchiveCardLayout.Troop(n.Card.width, n.Card.height))),
                cost = CityOverviewReviewBounds(ArchiveDesignRect(n.Card, ArchiveCardLayout.Cost(n.Card.width, n.Card.height))),
                stars = CityOverviewReviewBounds(ArchiveDesignRect(n.Card, ArchiveCardLayout.Stars(n.Card.width, n.Card.height))),
                skill = CityOverviewReviewBounds(ArchiveDesignRect(n.Card, ArchiveCardLayout.Skill(n.Card.width, n.Card.height))),
                scope = CityOverviewReviewBounds(ArchiveDesignRect(n.Card, ArchiveCardLayout.Scope(n.Card.width, n.Card.height)))
            }).ToArray() : new ReviewArchiveDesignNode[0];
        }

        private void AssertArchiveDesignGeometry(bool requireTwoRows)
        {
            if (!ArchiveDesignAssetsReady() || !archiveDesignTextFits)
                throw new Exception("Archive inspected Inkscape assets or actual Chinese font fit failed.");
            Rect viewport = ArchiveReviewRect(reviewTargets["archive-scroll"]);
            if (archiveDesignNodes.Count != reviewTargets.Keys.Count(id => id.StartsWith("archive-card-", StringComparison.Ordinal)))
                throw new Exception("Archive design geometry must describe exactly the fully visible cards.");
            if (requireTwoRows && archiveDesignNodes.Count < Math.Min(ArchiveDefinitions().Count, archiveDesignColumns * 2))
                throw new Exception("Compact archive must expose two complete rows in the actual viewport.");
            foreach (var node in archiveDesignNodes)
            {
                var definition = ArchiveDefinitions().FirstOrDefault(g => g.id == node.Id);
                if (definition == null || definition.name != node.Name || !CityOverviewContains(viewport, GuiRectToPixels(node.Card)))
                    throw new Exception("Archive card identity or visible viewport differs from its real catalog result.");
                if (!CityOverviewContains(node.Card, node.Art) || !CityOverviewContains(node.Card, node.NameArea)
                    || !CityOverviewContains(node.Card, node.Footer) || node.Art.Overlaps(node.NameArea)
                    || node.Art.Overlaps(node.Footer) || node.NameArea.Overlaps(node.Footer))
                    throw new Exception("Archive portrait, name flag and factual footer overlap or escape their card.");
                var art = ArchiveCardLayout.Art(node.Card.width, node.Card.height);
                var name = ArchiveCardLayout.Name(node.Card.width, node.Card.height, definition.name.Length);
                var footer = ArchiveCardLayout.Footer(node.Card.width, node.Card.height);
                if (!ArchiveDesignRectMatches(node.Art, node.Card, art) || !ArchiveDesignRectMatches(node.NameArea, node.Card, name)
                    || !ArchiveDesignRectMatches(node.Footer, node.Card, footer) || Math.Abs(node.Card.height - archiveDesignCardHeight) > .01f)
                    throw new Exception("Archive runtime differs from the inspected complete-portrait/short-name/footer design.");
                var troop = ArchiveDesignRect(node.Card, ArchiveCardLayout.Troop(node.Card.width, node.Card.height));
                var cost = ArchiveDesignRect(node.Card, ArchiveCardLayout.Cost(node.Card.width, node.Card.height));
                var stars = ArchiveDesignRect(node.Card, ArchiveCardLayout.Stars(node.Card.width, node.Card.height));
                var skill = ArchiveDesignRect(node.Card, ArchiveCardLayout.Skill(node.Card.width, node.Card.height));
                var scope = ArchiveDesignRect(node.Card, ArchiveCardLayout.Scope(node.Card.width, node.Card.height));
                if (node.NameArea.Overlaps(troop) || troop.Overlaps(node.Art) || troop.Overlaps(node.Footer)
                    || cost.Overlaps(stars) || cost.Overlaps(node.Art) || stars.Overlaps(node.Art)
                    || !CityOverviewContains(node.Footer, skill) || !CityOverviewContains(node.Footer, scope) || skill.Overlaps(scope))
                    throw new Exception("Archive actual name/troop, cost/stars or footer text budgets overlap.");
                Texture2D portrait;
                if (!portraits.TryGetValue(node.Id, out portrait) || portrait == null)
                    throw new Exception("Archive actual visible portrait asset is missing: " + node.Id);
            }
        }

        private static bool ArchiveDesignRectMatches(Rect actual, Rect card, ArchiveCardLayout.Box expected)
        { return Math.Abs(actual.x-card.x-expected.X)+Math.Abs(actual.y-card.y-expected.Y)
            +Math.Abs(actual.width-expected.W)+Math.Abs(actual.height-expected.H)<.1f; }

        private void AssertArchiveVisualTargets()
        {
            if (tab != "图鉴" || cardDetailId != null || !BlocksMapInput)
                throw new Exception("Archive review does not show the actual library page.");
            string[] controls = { "archive-scroll", "archive-search", "archive-search-clear", "archive-scope", "library-back" };
            foreach (string id in controls.Concat(Catalog.S1Factions.Select(f => "faction-" + f)))
            {
                ReviewTarget target;
                if (!reviewTargets.TryGetValue(id, out target)) throw new Exception("Archive control was not drawn: " + id);
                Rect r = ArchiveReviewRect(target);
                if (r.width <= 0 || r.height <= 0 || r.xMin < uiPixels.xMin - .1f || r.yMin < uiPixels.yMin - .1f
                    || r.xMax > uiPixels.xMax + .1f || r.yMax > uiPixels.yMax + .1f)
                    throw new Exception("Archive control is clipped by the current safe area: " + id);
                // Non-map pages disable MapView input globally; HUD panels use
                // blockedRects instead. Check the effective input gate, not only
                // the HUD's rectangle mechanism.
                if (map.inputEnabled && !map.IsBlocked(r.center)) throw new Exception("Archive control leaks input to terrain: " + id);
            }
            Rect viewport = ArchiveReviewRect(reviewTargets["archive-scroll"]);
            var definitions = ArchiveDefinitions();
            var cards = reviewTargets.Values.Where(t => t.id.StartsWith("archive-card-", StringComparison.Ordinal)).ToArray();
            // Between row boundaries the viewport can contain two clipped rows
            // and no complete card. That must expose zero targets, not fake ones.
            if (definitions.Count == 0 && cards.Length != 0)
                throw new Exception("Empty archive results expose a fabricated card target.");
            for (int i = 0; i < cards.Length; i++)
            {
                Rect r = ArchiveReviewRect(cards[i]);
                if (!definitions.Any(g => g.id == cards[i].id.Substring(13)))
                    throw new Exception("Archive target is outside its actual faction/search/scope result.");
                if (r.width <= 0 || r.height <= 0 || r.xMin < viewport.xMin - .1f || r.yMin < viewport.yMin - .1f
                    || r.xMax > viewport.xMax + .1f || r.yMax > viewport.yMax + .1f)
                    throw new Exception("Archive exposes a clipped or hidden card: " + cards[i].id);
                for (int j = 0; j < i; j++)
                    if (r.Overlaps(ArchiveReviewRect(cards[j]))) throw new Exception("Archive card targets overlap.");
            }
            ReviewTarget first;
            if (reviewTargets.TryGetValue("card-first", out first))
            {
                ReviewTarget actual;
                if (definitions.Count == 0 || !reviewTargets.TryGetValue("archive-card-" + definitions[0].id, out actual)
                    || ArchiveReviewRect(first) != ArchiveReviewRect(actual))
                    throw new Exception("Compatibility first-card target is not the fully visible first card.");
            }
        }

        private static Rect ArchiveReviewRect(ReviewTarget target)
        { return new Rect(target.x - target.w / 2, target.y - target.h / 2, target.w, target.h); }

        private IEnumerator ArchiveVisualSmoke()
        {
            bool previousReview = reviewMode, previousArchives = s1Archives;
            float previousSpeed = speed;
            string previousTab = tab, previousFaction = s1Faction, previousSearch = s1Search, previousSelected = s1Selected;
            string previousDetail = cardDetailId; int previousUid = cardDetailUid, previousSection = cardDetailSection;
            Vector2 previousScroll = s1Scroll, previousDetailsScroll = s1DetailsScroll, previousCardScroll = cardDetailScroll;
            reviewMode = true; speed = 0;
            string unchanged = JsonUtility.ToJson(world);
            try
            {
                tab = "图鉴"; cardDetailId = null; cardDetailUid = 0;
                s1Faction = "全部"; s1Search = ""; s1Archives = true; s1Selected = ""; s1Scroll = Vector2.zero;
                yield return CaptureState("smoke-s1-archive.png");
                AssertArchiveVisualTargets();
                AssertArchiveDesignGeometry(true);
                yield return CaptureState("smoke-s1-archive-design.png");
                if (ArchiveDefinitions().Count != world.S1Pool("全部", true).Count())
                    throw new Exception("Archive all-source count does not match the actual catalog.");

                s1Faction = "魏"; s1Selected = ArchiveDefinitions().First().id;
                yield return CaptureState("smoke-s1-wei.png");
                AssertArchiveVisualTargets();
                if (ArchiveDefinitions().Any(g => g.faction != "魏")) throw new Exception("Wei archive includes another faction.");

                s1Scroll = new Vector2(0, 10000);
                for (int i = 0; i < 5; i++) yield return null;
                AssertArchiveVisualTargets();
                if (s1Scroll.y <= 0 || reviewTargets.ContainsKey("card-first"))
                    throw new Exception("Long archive did not reach a real nonzero scroll with the first card culled.");
                float actualScroll = s1Scroll.y;
                string visibleId = reviewTargets.Keys.First(id => id.StartsWith("archive-card-", StringComparison.Ordinal)).Substring(13);
                s1Selected = visibleId; ShowCardDetails(visibleId);
                for (int i = 0; i < 5; i++) yield return null;
                if (cardDetailId != visibleId || cardDetailUid != 0) throw new Exception("Archive opened the wrong definition identity.");
                HandleMobileBack();
                yield return CaptureState("smoke-s1-archive-scrolled.png");
                AssertArchiveVisualTargets();
                if (cardDetailId != null || s1Selected != visibleId || s1Faction != "魏" || !s1Archives
                    || !string.IsNullOrEmpty(s1Search) || Math.Abs(s1Scroll.y - actualScroll) > .01f)
                    throw new Exception("Archive detail return lost its actual filter, selection or nonzero scroll.");

                s1Archives = false; s1Scroll = Vector2.zero; s1Selected = "";
                yield return CaptureState("smoke-s1-archive-recruitable.png");
                AssertArchiveVisualTargets();
                if (ArchiveDefinitions().Count == 0 || ArchiveDefinitions().Any(g => !g.recruitable || g.faction != "魏"))
                    throw new Exception("Recruitable archive includes a historical-only definition.");

                s1Search = "99999999"; s1Scroll = Vector2.zero; s1Selected = "";
                yield return CaptureState("smoke-s1-archive-empty.png");
                AssertArchiveVisualTargets();
                if (ArchiveDefinitions().Count != 0 || reviewTargets.ContainsKey("card-first"))
                    throw new Exception("Empty archive search still exposes a card target.");
                s1Search = "";
                for (int i = 0; i < 5; i++) yield return null;
                AssertArchiveVisualTargets();
                if (ArchiveDefinitions().Count == 0) throw new Exception("Cleared archive search did not recover the real card list.");
                s1Faction = "全部"; s1Archives = true; s1Scroll = Vector2.zero;
                var longestName = ArchiveDefinitions().OrderByDescending(g => g.name.Length).ThenBy(g => g.id).First();
                s1Search = longestName.id;
                yield return CaptureState("smoke-s1-archive-long-name.png");
                AssertArchiveVisualTargets(); AssertArchiveDesignGeometry(true);
                if (archiveDesignNodes.All(n => n.Id != longestName.id))
                    throw new Exception("Archive longest-name font fixture was not actually drawn.");
                s1Search = "";
                var longestSkill = ArchiveDefinitions().OrderByDescending(g =>
                    (string.IsNullOrWhiteSpace(g.nativeSkill) ? g.skill ?? "" : g.nativeSkill).Length).ThenBy(g => g.id).First();
                s1Search = longestSkill.id; s1Scroll = Vector2.zero;
                yield return CaptureState("smoke-s1-archive-long-skill.png");
                AssertArchiveVisualTargets(); AssertArchiveDesignGeometry(true);
                if (archiveDesignNodes.All(n => n.Id != longestSkill.id))
                    throw new Exception("Archive longest-skill font fixture was not actually drawn.");
                Debug.Log("ARCHIVE_TEXT_EXTREMES_RUNTIME_OK: longestName=" + longestName.id
                    + "; longestSkill=" + longestSkill.id + "; real catalog text, rendered GUI font fit and visible card identity verified.");
                Ask("图鉴遮挡验收", () => {});
                for (int i = 0; i < 3; i++) yield return null;
                yield return new WaitForEndOfFrame();
                if (archiveDesignNodes.Count != 0 || reviewTargets.Keys.Any(id => id.StartsWith("archive-", StringComparison.Ordinal)))
                    throw new Exception("Covered archive exposes stale image/name/footer input geometry.");
                HandleMobileBack();
                if (JsonUtility.ToJson(world) != unchanged) throw new Exception("Archive filtering/detail/return changed the paused campaign.");
                Debug.Log("ARCHIVE_VISUAL_RUNTIME_OK: real all-source/Wei/scrolled/recruitable/empty archive states; fully visible card targets inside viewport; actual identity and nonzero scroll survive detail/mobile back; complete World unchanged; real input checked separately.");
                Debug.Log("ARCHIVE_DESIGN_RUNTIME_OK: inspected Inkscape assets, actual Chinese font fit including longest name and skill, complete two-row card grid, separate image/name/footer geometry and modal isolation; input verification remains separate.");
            }
            finally
            {
                reviewMode = previousReview; speed = previousSpeed; tab = previousTab;
                s1Faction = previousFaction; s1Search = previousSearch; s1Archives = previousArchives; s1Selected = previousSelected;
                s1Scroll = previousScroll; s1DetailsScroll = previousDetailsScroll;
                cardDetailId = previousDetail; cardDetailUid = previousUid; cardDetailSection = previousSection; cardDetailScroll = previousCardScroll;
                if (!reviewMode) reviewTargets.Clear();
            }
        }
    }
}
