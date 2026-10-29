using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    // Unity composition root and screen coordination; rules and persistence live below this layer.
    public sealed partial class EasternDynastyGame : MonoBehaviour
    {
        private CampaignSession session;
        private World world { get { return session == null ? null : session.World; } }
        private MapView map;
        private readonly System.Random rng = new System.Random();
        private Homeland origin;
        private bool useYouzhou = true;
        private int youzhouStart;
        private int region = 4, selectedCard, selectedSlot, selectedReport;
        private string ruler = "新任太守", tab = "领地", status = "";
        private float speed = 1, autosave;
        private Vector2 rosterScroll, reportScroll, helpScroll;
        private GUIStyle body, small, heading, title, button, muted, cardName;
        private Font font;
        private bool stylesReady;
        private float scale, width, height;
        private Rect main;
        private Action confirmation;
        private string confirmText;
        private bool smokeFailed, reviewMode, transientRun;
        private static readonly Color Ink = C("111d2c"), Panel = C("1b2d40"), Border = C("3b5268"), Gold = C("e7bc70"), Text = C("f3eee4"), Muted = C("a9b5c4"), Green = C("4fb59d");
        private bool HasSave { get { return session != null && session.HasSave(useYouzhou); } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<EasternDynastyGame>() == null)
            { var root = new GameObject("Eastern Dynasty"); DontDestroyOnLoad(root); root.AddComponent<EasternDynastyGame>(); }
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            var args = Environment.GetCommandLineArgs();
            ConfigureMobilePlatform(args);
            reviewMode = args.Contains("-edReview");
            bool smoke = args.Contains("-edSmoke");
            transientRun = smoke || reviewMode;
            if(transientRun)StartCoroutine(ConfigureValidationWindow(args));
            useYouzhou = !args.Contains("-edLegacy");
            session = new CampaignSession(new JsonCampaignRepository(Application.persistentDataPath), () => DateTime.UtcNow, transientRun);
            if (smoke) Application.logMessageReceived += WatchSmokeErrors;
            map = gameObject.AddComponent<MapView>();
            map.mobileInputMode = mobileMode;
            map.LandClicked = OnMapLandClicked;
            map.Setup(useYouzhou ? World.CreateYouzhou(0,"天下") : World.Create(Homeland.Han,4,"天下"));
            if (!useYouzhou) { map.focus = new Vector3(17,0,13); map.zoom = 17; }
            if (smoke) StartCoroutine(Smoke());
            else if (reviewMode)
            {
                StartWorld(false);
                if (args.Contains("-edReviewReportArchive")) InitializeReportArchiveReview();
                if (args.Contains("-edOwnedRosterReview")) ConfigureOwnedRosterReview();
            }
        }

        private void Update()
        {
            UpdateReviewCapture();
            UpdateMobileInterface();
            UpdateBattleReplay();
            if (world != null && !mobileClock.IsPaused && tab != "战斗回放")
            {
                float frameSeconds = skipMobileTick ? 0 : Time.unscaledDeltaTime;
                skipMobileTick = false;
                string previousMessage = world.messages.FirstOrDefault();
                ObserveBattleMarch();
                session.Tick(frameSeconds * speed);
                if (world.messages.FirstOrDefault() != previousMessage) status = world.messages[0];
                autosave += frameSeconds;
                if (autosave >= 30) { autosave = 0; Save(false); }
            }
            UpdateBattleWatch();
            map.inputEnabled = world != null && !BlocksMapInput && !mobileClock.IsPaused;
        }

        private void OnApplicationQuit() { if (world != null) Save(false); }
        private void OnDestroy()
        {
            Application.logMessageReceived -= WatchSmokeErrors;
            if(cityOverviewTexture!=null)Destroy(cityOverviewTexture);
            if(cityFacilityMedallion!=null)Destroy(cityFacilityMedallion);
            if(replayFieldTexture!=null)Destroy(replayFieldTexture);
            if(minimapTexture!=null)Destroy(minimapTexture);
            foreach(var texture in cityFacilityArt.Values)if(texture!=null)Destroy(texture);
        }
        private static Color C(string hex) { Color c; ColorUtility.TryParseHtmlString("#" + hex, out c); return c; }
        private void Act(string message) { status = message; if (world != null) world.Note(message); }
        private void Ask(string text, Action action) { attackConfirmation = false; cityCancellation = null; confirmText = text; confirmation = action; }

        private void StartWorld(bool save)
        {
            if (useYouzhou) session.StartYouzhou(youzhouStart, ruler);
            else session.StartLegacy(origin, region, ruler);
            ActivateCampaign();
            status = useYouzhou ? "进入主城，开始城建任务。" : "点选主城周边的一级粮田，开启征程。";
            if (save) Save(false);
        }

        private void ActivateCampaign()
        {
            selectedCard = world.cards[0].uid; selectedReport = 0;
            tab = "领地"; speed = 1; autosave = 0; minimapWorld = null;
            trainingEditingFormation = false;
            trainingHistoryOpen = false; trainingHistorySelection = null; trainingHistoryScroll = Vector2.zero;
            cityEditingFormation = false;cityPage = "overview";
            cityQuestReceipt = null;
            replayRecord = null; replayPlayer = null; replayPlaying = false;
            replayOutcome = null; replayShowOutcome = false; battleOutcomeScroll = Vector2.zero;
            watchingCurrentBattle = false; attackConfirmation = false;
            currentBattleId = pendingBattleWatchId = dismissedBattleWatchId = "";
            ObserveBattleMarch();
            map.Setup(world);
        }

        private void Save(bool notify)
        { TrySave(notify); }

        private bool TrySave(bool notify)
        {
            if (world == null || transientRun) return true;
            try { session.Save(); if (notify) status = "征程已保存。"; return true; }
            catch (Exception e) { status = "保存失败：" + e.Message; Debug.LogError(status); return false; }
        }

        private void Load()
        {
            try
            {
                bool restoredBackup;
                session.Load(useYouzhou, out restoredBackup);
                ActivateCampaign();
                status = restoredBackup ? "已从备份恢复征程。" : "已读取存档并结算离线资源与行军。";
            }
            catch (Exception e) { status = "无法读取存档：" + e.Message; }
        }
    }
}
