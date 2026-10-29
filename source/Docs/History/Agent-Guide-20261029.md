# 历史 AI 指南快照

此文保存整理前的逐轮记录，不是当前开发授权或当前发布状态。2026-10-29 用户已要求停止全部项目 agent；以 [当前指南](../../agent.md) 和 [交接记录](../Project-Status.md) 为准。下方历史表中的文件路径仍相对于项目源码目录。

---

# Eastern Dynasty AI 接手指南

本项目位于 F:\工作项目\率土之滨\source，是 Unity 6000.3.27f1 的单人策略原型，已有 Windows 客户端和已通过包体验证的 Android 试玩 APK。默认地图为幽州 40×40，旧版天下地图与存档独立保留。修改应沿用当前可运行版本，保留既有玩法、资产 GUID 与存档兼容。

当前任务范围更新为 **S1 幽州**，先读 [S1交付说明](../../Docs/S1-Delivery.md)。武将新增 `Core/S1Roster.Generated.cs`，由 `Tools/Import-S1Rag.ps1` 从固定RAG快照生成，不能手改；245张是资料身份总量，41张是本地证据筛选试玩池，不能宣传成现行官方全量。地图与卡图来源见 [S1-Map-Art](../../Docs/S1-Map-Art.md) 和 [S1-Cards](../../Docs/S1-Cards.md)。新增军队状态/政令规则在 `Core/World.Orders.cs`，图鉴和军队UI增量在 `Runtime/S1Presentation.cs`，回归入口含 `S1OrderChecks/S1RosterChecks` 及 `Check-UiClicks.ps1 -S1`。

## 专业 UI 工具与原作参考先行

2026-10-27 用户纠正开发方式：停止凭想象手写粗糙外观。先读 [专业UI流程](../../Docs/Professional-UI-Workflow.md)，建立可编辑分层稿，实际调用 Inkscape 导出并查看，再按稿改界面。首份参考测量模板与导出证据在 `Docs/DesignUI/reference-layout-v1`，它不是最终UI。

## 每轮 UI 改进为必做项

用户再次明确当前界面太老土、远未接近原作。之后每轮必须交付实质性可见 UI 改进，优先主界面、主城和武将卡面；具体要求和截图对比记录见 [每轮 UI 要求](../../Docs/UI-Iteration-Requirements.md)。仅新增玩法、文字、按钮、测试或换色不能替代本轮 UI 工作。

## 从当前任务定位代码

先读需求和本表所列入口，再按调用链展开。不要整包读取 Library、Builds、Logs、Validation/Visual 或所有研究截图来寻找源码；它们分别是缓存、构建产物和验证证据。

| 当前任务 | 优先读取 | 修改归属 |
| --- | --- | --- |
| 武将战法分栏详情与完整原文 | Runtime/CardDossierSkillLayout.cs、CardDossierPresentation.cs；Diagnostics/CardVisualDiagnostics.cs；Docs/DesignUI/s1-20261029-010252300 | Inkscape两正式底面、共享标题/96图像与右侧效果、下置学习横栏；真实测高/滚动保留，新sheet/owned实图和图文只读点击待发布；无规则/存档/卡池变化 |
| 地图任务独立书册与信息签；首次F8就绪 | Runtime/MapHudQuestLayout.cs、MapHudQuestPresentation.cs；Diagnostics/MapHudVisualDiagnostics.cs、EasternDynastyDiagnostics.cs；Tools/Check-UiClicks.ps1；Docs/DesignUI/s1-20261028-235752015 | 两个Inkscape组件、80×68书册与264×68信息签、118高区域；实际图文/悬停/拖动待发布。review同PID READY与双文件/序号诊断接入Validate；无规则/存档变化 |
| 地图独立招募菱章与选地避让 | Runtime/MapHudNavigationLayout.cs、MapHudPresentation.cs；Diagnostics/MapHudVisualDiagnostics.cs；Docs/DesignUI/s1-20261028-225251901 | 两张Inkscape组件、160×140独立入口/60×88完整已有画像/分离标题；选地与主城保留compact；新图、上凸真实点击/拖动取消待发布，无规则或存档变化 |
| 武将详情连续导航、身份带与固定底栏 | Runtime/CardChromeLayout.cs、CardChromePresentation.cs、CardDetailPresentation.cs；Diagnostics/CardVisualDiagnostics.cs；Docs/DesignUI/s1-20261028-214751426 | 六个Inkscape组件、首行三页签/下置40高身份带、正文增加56高、54关闭/返回/培养；12屏纯布局与外部负例接入Validate；新chrome两图/实际字体/点击待发布，无规则或存档变化 |
| 主城连续资源账栏与跨页残影 | Runtime/CityHeaderLayout.cs、CityHeaderDesignPresentation.cs；Diagnostics/CityOverviewDiagnostics.cs；Docs/DesignUI/s1-20261028-204251299 | 不透明完整底面、62城徽/174完整城名、六库存同轴账栏；992布局/75外部断言与3像素比较器检查；普通武将页直接返回主城像素门禁及3张新图待发布，无规则/存档变化 |
| 地图顶部身份图章与连续资源账栏 | Runtime/MapHudHeaderLayout.cs、MapHudHeaderPresentation.cs；Diagnostics/MapHudVisualDiagnostics.cs；Docs/DesignUI/s1-20261028-193752561 | 两个Inkscape正式底片、62图章/174宽名称、672生产账栏与紧邻货币/同轴库存；新ledger图和图文往返待发布。另修Validate-Visual固定45秒超时，120秒有界等待及退出复查；无规则/存档变化 |
| 地图完整舆图与侧置定位 | Runtime/MapHudAtlasLayout.cs、MinimapPresentation.cs；Diagnostics/MapHudVisualDiagnostics.cs；Docs/DesignUI/s1-20261028-183252245 | 三个Inkscape正式组件，204方图与72宽侧栏、60圆形归城图文和三行图例；282×204范围；3302布局/12460舆图/139输入与合同检查；sidebar新图、字体/图文/名牌避让待发布，无规则/存档变化 |
| 地图与主城上图下横题导航 | Runtime/MapHudNavigationLayout.cs、MapHudPresentation.cs；Diagnostics/MapHudVisualDiagnostics.cs、CityOverviewDiagnostics.cs；Docs/DesignUI/s1-20261028-172751912 | 两个Inkscape正式底面、100×42完整图/100×22横题，六入口与78高占位保留；3217布局/13底栏/40全景回归及来源链检查；captions两新图、实际字体/图文往返待发布，无规则/存档变化 |
| 地图任务书册与全宽目标；土地截图时序 | Runtime/MapHudQuestLayout.cs、MapHudQuestPresentation.cs；Diagnostics/MapHudVisualDiagnostics.cs、LandVisualDiagnostics.cs；Docs/DesignUI/s1-20261028-162252252 | Inkscape正式底面、书册/章节/334宽目标与独立进度和44高行动；单父级章节手势，整区132高；土地严格断言/截图同帧末，纯帧序与任务断言接入Validate；新docket/土地及上轮rows实盘待发布，无规则/存档变化 |
| 地图部队横排名签与完整肖像 | Runtime/MapHudArmyLayout.cs、MapHudArmyPresentation.cs；Diagnostics/MapHudVisualDiagnostics.cs；Docs/DesignUI/s1-20261028-151752217 | Inkscape两正式组件，三条354×64图文横签、独立姓名/身份与354×302防误触区；3036布局/33外部断言及引用编译通过，rows新图/真实图文间隙操作待发布；原命令/规则/存档保留 |
| 地图城市图章与分层名签 | Runtime/MapCityLabelLayout.cs、MapCityLabelPresentation.cs；Diagnostics/MapCityLabelDiagnostics.cs；Docs/DesignUI/s1-20261028-130751956 | 52圆章/完整城市图、名称与归属独立、长名两行；三Inkscape正式资源，405布局/23交互规则与引用编译通过；新图/真实点击待发布者，无规则或存档变化 |
| 土地紧凑信息页与横行事实 | Runtime/LandSelectionLayout.cs、LandSelectionPresentation.cs；Diagnostics/LandVisualDiagnostics.cs；Docs/DesignUI/s1-20261028-120252024 | Inkscape正式底面、280×228单页/两条248×28事实行/156×78完整图；177602布局/1322字体预算/50纯交互与引用编译通过，新facts图及实际字体/点击待发布；无规则/存档变化 |
| 土地独立圆章命令与紧凑状态区 | Runtime/LandCommandMenuLayout.cs、LandSelectionLayout.cs、LandSelectionPresentation.cs；Diagnostics/LandVisualDiagnostics.cs；Docs/DesignUI/s1-20261028-110251322 | 两正式Inkscape皮肤、五状态高度与156宽文字；新orders/city-orders图待发布。另修手机设施状态16→20高，保持完整图与门禁；无规则/存档变化 |
| 土地目标两侧浮签、边缘停靠与手机字体 | Runtime/LandSelectionLayout.cs、LandSelectionPresentation.cs、LandCommandTextLayout.cs；Diagnostics/LandVisualDiagnostics.cs；Docs/DesignUI/s1-20261028-095751175 | 280×266事实与240×292行动独立分置目标两侧；140宽实测字体标题、竖排208×44命令；纯布局/字体/交互检查，新增flanking/edge-dock图待发布；无规则/存档变化 |
| 地图土地浮签与44高横向行动 | Runtime/LandSelectionLayout.cs、LandSelectionPresentation.cs；Diagnostics/LandVisualDiagnostics.cs；Docs/DesignUI/s1-20261028-085252514 | 三正式导出底面，360×418分层事实/行动、完整地块与独立身份、图文横排命令；930布局/36纯交互断言与引用编译通过；新design截图、实际字体/实绘-label点击待发布，无规则/存档变化 |
| 设施一览横向完整建筑图谱 | Runtime/CityFacilityAtlasLayout.cs、CityFacilityAtlasPresentation.cs、CityFacilityCatalogPresentation.cs；Diagnostics/CityFacilityCatalogDiagnostics.cs；Docs/DesignUI/s1-20261028-022752190 | 四个Inkscape正式组件，216×162完整图与真实等级/收益/44高行动并排，节点206高、三列阈值1460；1435项纯布局/字体预算，满级兵营新图与图像/名称/命令真实点击待发布；无规则/存档变化 |
| 设施矩形建筑展台与完整缩略图 | Runtime/CityFacilityGalleryLayout.cs、CityFacilityGalleryPresentation.cs、CityFacilityPresentation.cs；Diagnostics/CityFacilityDossierDiagnostics.cs；Docs/DesignUI/s1-20261028-012751756 | 三个Inkscape正式组件，大图/34高身份等级底栏与58高完整缩略图；九格48高保留，3514布局/字体预算及引用编译通过，新图/六次图文点击待发布；无规则/存档变化 |
| 主城设施专业横向账页 | Runtime/CityFacilitySheetLayout.cs、CityFacilitySheetPresentation.cs、CityFacilityPresentation.cs；Diagnostics/CityFacilityDossierDiagnostics.cs；Docs/DesignUI/s1-20261028-002751316 | 五个Inkscape组件、说明前置/横向效果/共享费用与360×44行动；3776项布局检查及引用编译通过，新压力图、真实点击与观感待发布；无规则/存档变化 |
| 武将档案全宽属性与横向战法入口 | Runtime/CardDossierDesignLayout.cs、CardDossierPresentation.cs；Diagnostics/CardVisualDiagnostics.cs；Docs/DesignUI/s1-20261027-232251526 | Inkscape六正式组件，双列内联基础值/成长、独立等级条、下方两横战法；12布局/8PNG来源链检查与必需长名称图/图文点击入口，新运行观感待发布；无规则/存档/卡池变化 |
| 武将详情专业身份列与完整画像 | Runtime/CardPortraitDesignLayout.cs、CardDetailPresentation.cs；Diagnostics/CardVisualDiagnostics.cs；Docs/DesignUI/s1-20261027-222251138 | Inkscape三正式组件；姓名旗/阵营菱章/竖星级、释放画像顶部与独立底座；245画像×12布局纯检查和引用编译通过；新图/三处只读点击待发布，规则/卡池/存档不变 |
| 主城底栏共享图签与独立招募 | Runtime/MapHudPresentation.cs、MapHudNavigationLayout.cs；Diagnostics/CityOverviewDiagnostics.cs、MapHudVisualDiagnostics.cs；Docs/DesignUI/s1-20261027-211752367 | Inkscape桌面/手机/状态稿；复用六个正式nav组件，五个左侧图签/竖标题、右侧招募与集中保存/时间；实际字体/空编队对比度与八控件检查，38例纯断言及引用编译通过；新图与十二次图文点击待发布，无新资源/规则/存档 |
| 主城顶部分组资源与完整城名 | Runtime/CityHeaderLayout.cs、CityHeaderDesignPresentation.cs、CityPresentation.cs；Diagnostics/CityOverviewDiagnostics.cs；Docs/DesignUI/s1-20261027-201251989 | 三个Inkscape正式组件，独立身份牌/两行长名、四生产资源与右侧货币；纯几何/来源SHA及引用编译通过；新压力截图、城章及12次资源文字点击待发布，无规则/存档变化 |
| 主城独立圆章导航与分层返回 | Runtime/CityDockLayout.cs、CityDockDesignPresentation.cs、CityPresentation.cs；Diagnostics/CityOverviewDiagnostics.cs；Docs/DesignUI/s1-20261027-190751837 | 五个Inkscape正式组件，独立72×54图像与弧形题签/圆形返回；新纯几何及导出SHA检查接入统一入口；新增导航图、五入口/返回图文真实点击待发布，无规则/存档变化 |
| 主城城务圆章与施工/章节分层 | Runtime/CityAffairsLayout.cs、CityAffairsDesignPresentation.cs；Diagnostics/CityOverviewDiagnostics.cs；Docs/DesignUI/s1-20261027-180252216 | 五个Inkscape正式组件、独立建筑/标题/信息带和44高命令；4299项布局及统一纯逻辑通过；真实施工/字体/图文点击与运行观感待发布，无规则/存档变化 |
| 主城部队专业卡组与紧凑补给 | Runtime/CityArmyLayout.cs、CityArmyDesignPresentation.cs、CityOverviewPresentation.cs；Diagnostics/CityOverviewDiagnostics.cs；Docs/DesignUI/s1-20261027-170251762 | 五个Inkscape正式组件；完整画像、42高姓名与44高命令，55,159项几何及27例交互断言纯回归；164图、实际图像/姓名/命令与观感待发布，无规则/存档变化 |
| 地图浮动舆图与图像定位 | Runtime/MinimapPresentation.cs、MapHudAtlasLayout.cs；Diagnostics/MapHudVisualDiagnostics.cs；Docs/DesignUI/s1-20261027-155751815 | Inkscape分层稿及四正式组件；保留204方形真实地图，独立坐标/图例与44圆形归城，2388项布局检查；163图及定位图像/文字/拖动防误触待发布者，未改规则/存档 |
| 地图部队分离卡签与完整立绘 | Runtime/MapHudArmyPresentation.cs、MapHudArmyLayout.cs；Diagnostics/MapHudVisualDiagnostics.cs；Docs/DesignUI/s1-20261027-145251750 | Inkscape实际分层稿/状态稿与四正式组件；保留354×242范围，完整画像80×116、实际字体适配与171×44操作；2184项地图布局检查；162图及画像/姓名/底座真实点击与观感待发布者，无规则/存档变化 |
| 地图任务竖签与目标/进度/行动 | Runtime/MapHudQuestPresentation.cs、MapHudQuestLayout.cs；Diagnostics/MapHudVisualDiagnostics.cs；Docs/DesignUI/s1-20261027-134751635 | Inkscape分层稿与4个正式组件；354×120任务区、独立章节入口/44高行动、真实进度和字体度量；1512项地图布局检查；161图和真实点击/观感待发布，未改规则/存档 |
| 安装、打开、构建、运行 | README.md；Tools/Common.ps1；相应入口脚本 | Tools 与 Editor 工具 |
| Android 构建与侧载 | Docs/Android.md；Tools/Build-Android.ps1；Assets/EasternDynasty/Editor/AndroidBuildTools.cs | Android 构建工具；由集成者串行执行 Unity |
| 手机触控、安全区、返回键与后台保存 | Docs/Mobile-Interaction.md；Runtime/MobilePresentation.cs；MobileMapInput.cs；MobileMapGesture.cs；MobileScreenLayout.cs；MobileNavigation.cs；MobileSuspendClock.cs | Presentation 与 Validation/MobileInputChecks.cs |
| 新增或替换地图图块、人物肖像 | Docs/Asset-Pipeline.md；Resources/asset-manifest.json；目标 ID 的现有条目 | 资源清单、受控资源目录、导入工具 |
| 武将定义、数值或成长 | Core/Catalog.cs；Core/GeneralModels.cs；Core/World.Generals.cs | Core |
| 资源收入、离线推进 | Core/World.Economy.cs；Application/CampaignSession.cs | 规则归 Core；会话时钟归 Application |
| 地图布局、出生点、连通 | Core/YouzhouMapDefinition.cs；Core/World.Creation.cs；World.Validation.cs | Core |
| 行军、八方向邻接、驻守、屯田、战斗 | Core/World.Orders.cs；World.Combat.cs；World.Economy.cs；Validation/MapInteractionChecks.cs | Core |
| 冻结战报、回放与战斗观看 | Core/BattleReplayModels.cs、CampaignModels.cs、World.Combat.cs；Application/BattleReplayFrame.cs；Runtime/BattleReplayPresentation.cs、BattleFieldPresentation.cs、BattleWatchPresentation.cs；Diagnostics/BattleReplayDiagnostics.cs；Validation/BattleReplayChecks.cs、BattleReplayFrameChecks.cs | 结算记录归Core，播放与整队战场只读冻结事件；可点回合/事件定位，旧文字兼容；60态及真实点击待宿主，运行验收见小时Backlog |
| 战果总览与逐将实得经验 | Application/BattleOutcomeSummary.cs；Runtime/BattleOutcomePresentation.cs；Diagnostics/BattleOutcomeDiagnostics.cs；Validation/BattleOutcomeSummaryChecks.cs | 冻结记录的不可变投影；总览返回原事件/本场来源，播放/定位不结算，旧文字不推造数据；无新存档字段，JsonUtility和91态/真实点击待宿主 |
| 冻结参战阵容与战前卡牌 | Application/BattleFormationSummary.cs；Runtime/BattleFormationPresentation.cs；Diagnostics/BattleFormationDiagnostics.cs；Validation/BattleFormationSummaryChecks.cs | 从战报/过程/战果进入；等级、阶数、基础攻击及携带战法只读冻结记录，选将和返回保留原事件/来源；不补造逐将伤害或守军，无新存档字段；JsonUtility及95态/真实点击待宿主 |
| 战报分类、冻结摘要与历史记录定位 | Application/BattleReportArchive.cs；Runtime/BattleReportArchivePresentation.cs；Diagnostics/BattleReportArchiveDiagnostics.cs；Validation/BattleReportArchiveChecks.cs | 按已占领/未占领/旧文字分类，未占领不等同败战；返回保留实际记录与滚动，新结算不偷换选择；386项纯C#检查，无新增存档字段；114态及第三只读军报场景实际点击待发布者 |
| 主城全景、九格设施与独立章节任务 | Core/CityModels.cs；World.City.cs；Runtime/CityPresentation.cs、CityFacilityPresentation.cs、CityOverviewPresentation.cs、CityQuestPresentation.cs；Validation/CityProgressionChecks.cs | 默认全高城景与城务/竖卡部队侧栏，长城名/空编队及可见目标诊断在CityOverviewDiagnostics；125态与真实输入待发布者。规则归Core，设施效果共享Core配置，九格仍为城区管理 |
| 主城图像导航与城务建筑主体 | Runtime/CityPresentation.cs、CityOverviewPresentation.cs；Diagnostics/CityOverviewDiagnostics.cs | 五导航图文分区、大建筑圆章与实际府邸/施工摘要、文书任务入口；保留44高父操作，图像/姓名底座点击及模态取证待发布者；部队卡组改版续见下行，无规则/存档/正式资源变化 |
| 主城编队卡组与补给行动区 | Runtime/CityOverviewPresentation.cs；Diagnostics/CityOverviewDiagnostics.cs；Tools/Check-Experience.ps1 | 342–406自适应侧栏、完整立绘/席位冠带、卡下兵力与真实资源图标、44高图文三行动；空位任命与忙碌阶段保留原命令，155态/六图文点击/九设施可见性及观感待发布者，无新规则/存档/正式资源 |
| 主城身份页头、图像底栏与贯通城景 | Runtime/CityPresentation.cs、CityOverviewPresentation.cs、MapHudPresentation.cs、EasternDynastyPresentation.cs；Diagnostics/CityOverviewDiagnostics.cs | 建筑徽章/城名等级、共享库存时产、图像底栏、城景延伸至悬浮工具背后；相机和设施命中不变，155态与实际图像/标题点击待发布者；顶部导航高度、建筑尺寸未改变，无新规则/存档/正式图 |
| 主城边缘图像导航、更高城景与4:3覆盖 | Runtime/CityOverviewLayout.cs、CityPresentation.cs、CityOverviewPresentation.cs、CityPanoramaCamera.cs；Diagnostics/CityOverviewDiagnostics.cs；Tools/Check-CityPanorama.ps1 | 总览左侧五图文入口/底部返回，内容高60、桌面宽少104，完整城名两行；4:3统一镜头覆盖背景、复位九设施可见，2671项纯几何检查；155态及五图文真实输入/4:3观感待发布者，无规则/存档/正式图变化 |
| 全景选址与九类原创设施外形 | Runtime/CityOverviewPresentation.cs、CitySceneRaster.cs；Diagnostics/CityOverviewDiagnostics.cs；Tools/Check-CityArt.ps1 | 选址只进入真实城区，施工/已建位置不可二次选址；返回保留层级，跨页清旧来源；九类外形共用且裁切检查接入Validate；81态与真实点击待宿主，无新存档字段或图片资产 |
| 等比城景、展开缩放与拖动浏览 | Runtime/CityPanoramaCamera.cs、CityOverviewPresentation.cs；Diagnostics/CityOverviewDiagnostics.cs；Tools/Check-CityPanorama.ps1 | 绘制/点击共用镜头坐标，裁切目标不登记；标签优先、主体按前景优先；详情保留视角，选址→展开→主城逐层返回；无新持久化字段；2156项几何检查，100态及实际手势待宿主 |
| 主城屋面/立面结构、设施铭牌与生产全景栅格 | Runtime/CitySceneRaster.cs、CityOverviewPresentation.cs；Diagnostics/CityOverviewDiagnostics.cs；Validation/CityArtChecks.cs；Tools/Check-CityArt.ps1 | 四坡瓦屋、梁柱窗棂/石砖与独立等级菱章；44高铭牌只对应真实设施，景观无收入/热点；四态真实建设、确定性与World不变接入Validate；127态及铭牌/施工实际点击待发布者，纯栅格不是运行截图 |
| 主城街坊园林、城郊延展与组内铭牌 | Runtime/CitySceneRaster.cs、CityOverviewPresentation.cs；Diagnostics/CityOverviewDiagnostics.cs；Validation/CityArtChecks.cs | 错层院落、三类空地景观与1800×880环境填满视窗，原1000×440城体坐标保持；菱章组内扫描线避免游离边线；138态及实际展开/缩放/拖动后的覆盖/原点检查待发布者，四态纯栅格不代表运行观感 |
| 主城树群、庭园与河岸景观 | Runtime/CitySceneRaster.cs；Diagnostics/CityOverviewDiagnostics.cs；Tools/Check-Experience.ps1 | 不规则叶丛/枝干、疏密林群、柔化投影与连续浅深水/湿岸；原城体及九设施坐标保持，缓存随城建状态更新；157图及可见树冠/水面真实点击待发布者，纯栅格非运行截图 |
| 主城城防体积、门楼纵深与石板道路 | Runtime/CitySceneRaster.cs；Diagnostics/CityOverviewDiagnostics.cs；Validation/CityPanoramaCameraChecks.cs | 厚墙步道/墙墩/石台角楼、投影拱券门洞及多列错缝石板；规划夹具独立开启采集修复094252047失败，旧断言保留；2751纯几何检查，158图及门洞/墙石/铺路可见点击待发布者；无规则/存档/正式PNG变化 |
| 设施一览建筑图谱、分类与真实位置导航 | Application/CityFacilityNavigation.cs；Runtime/CityFacilityCatalogPresentation.cs；Diagnostics/CityFacilityCatalogDiagnostics.cs；Validation/CityFacilityNavigationChecks.cs | 大建筑圆章、城主府核心、真实等级/前置和44高行动；图像/操作指向前置、工地、原设施或空地，返回保留分类/滚动；完整可见才启用目标，模态隔离；无新存档字段；144态及图像/末排真实点击待发布者 |
| 设施建筑主体、图像选择与收益账页 | Runtime/CityFacilityDossierPresentation.cs、CityFacilityPresentation.cs、CityPresentation.cs；Diagnostics/CityFacilityDossierDiagnostics.cs | 大建筑圆章、紧凑九格、八项图像选择、首屏五费及固定44高行动；施工读实际队列，退款模态隐藏背景，原前置/滚动/返回保留；无新规则/存档/正式图片，143态和实际输入待发布者 |
| 建设进度、完工效果与离线交接 | Application/CampaignSession.cs、CityConstructionCompletion.cs；Runtime/CityConstructionPresentation.cs；Diagnostics/CityConstructionDiagnostics.cs；Validation/CityConstructionCompletionChecks.cs | 只观察Core实际施工完成，冻结等级/效果为会话收据；取消不造记录，清除/查看不结算，无新存档字段；JsonUtility和86态/真实点击待宿主 |
| 任务前往、当前目标定位、建设中断退款 | Application/CityQuestNavigation.cs；Runtime/CityQuestPresentation.cs、CityFacilityPresentation.cs；Validation/CityQuestNavigationChecks.cs；Editor/RepositoryChecks.cs | 导航只选择真实操作位置；退款读取Core的paid收据，100%为有公告依据的项目配置；53态及真实点击由宿主验收 |
| 任务领奖收据、章节完成与下一目标 | Application/CityQuestClaimReceipt.cs；Runtime/CityQuestRewardPresentation.cs；Validation/CityQuestRewardChecks.cs | 收据只来自实际首次领取、临时UI不入存档；返回/导航不发奖；当前56态及真实点击由宿主验收 |
| 任务目标详情、真实前置/成本与逐层返回 | Application/CityQuestObjectivePlan.cs；Runtime/CityQuestObjectivePresentation.cs；Diagnostics/CityQuestObjectiveDiagnostics.cs；Validation/CityQuestObjectivePlanChecks.cs | 当前/未来/已领只读投影；费用保留小数，行动走真实设施/部队/土地，土地返回只对同World同目标有效；无新存档字段，220专项及110移动检查，109态与真实点击待发布者 |
| 独立征兵、人数/费用预览及完成收据 | Core/World.Combat.cs；Application/ReinforcementPlan.cs；Runtime/ReinforcementPresentation.cs；Diagnostics/ReinforcementDiagnostics.cs；Validation/ReinforcementChecks.cs；Editor/RepositoryChecks.cs | 共用现有即时补兵配置，确认冻结人数/兵力/容量/编队；完成收据可定位真实待领奖任务，导航不发奖；无新存档字段；137人小数JSON回归、67态及真实点击待宿主 |
| 练兵结果、逐将实得与最近十次记录 | Runtime/TrainingPresentation.cs、TrainingHistoryPresentation.cs；Diagnostics/TrainingHistoryDiagnostics.cs；Core/CampaignModels.cs、World.Orders.cs | 只读既有TrainingReceipt的参训UID/实得经验，不借当前等级或编队重造历史；卡片/记录/原方案逐层返回；无新增存档字段；77态与真实点击待宿主 |
| 练兵土地比较、往返时间与逐将升级预览 | Application/TrainingPlanPreview.cs；Runtime/TrainingTargetPresentation.cs、TrainingPresentation.cs；Diagnostics/TrainingTargetDiagnostics.cs；Validation/TrainingPlanPreviewChecks.cs | 复制卡片预测封顶实得并对照真实结算；候选/采用/批准分步，选择不扣费，返回保留来源；无新存档字段，103态及不同土地真实点击待宿主 |
| 练兵执行阶段、到账与回城倒计时 | Application/TrainingExecutionStatus.cs；Runtime/TrainingExecutionPresentation.cs；Diagnostics/TrainingExecutionDiagnostics.cs；Validation/TrainingExecutionChecks.cs | 只读真实订单/匹配收据，行军时长共用Core；取消无奖励，游戏内时间随倍速推进；无新存档字段，原103态四张更新及点击待发布者 |
| 持有武将卡库与固定编队操作 | Runtime/OwnedRosterPresentation.cs、EasternDynastyPresentation.cs；Diagnostics/OwnedRosterDiagnostics.cs | S1竖签完整立绘、真实成长底座与固定任命/练兵；培养独立滚动，完整可见UID目标，模态隐藏背景；旧命令/返回保留，无新存档字段，127态及只读36卡实际输入待发布者 |
| 武将图鉴与完整卡牌详情 | Runtime/S1ArchivePresentation.cs、S1Presentation.cs、CardDetailPresentation.cs；Diagnostics/ArchiveVisualDiagnostics.cs；Core/GeneralModels.cs；Docs/S1-Cards.md | 图鉴使用竖签立绘卡、侧栏筛选与固定顶栏；只读定义，详情返回保留滚动，裁切/模态下不登记卡目标；定义与来源归Core，123态与实际输入待发布者 |
| 武将详情身份带、属性图形与战法徽章 | Runtime/CardDossierPresentation.cs、CardDetailPresentation.cs；Diagnostics/CardVisualDiagnostics.cs | 真实基础值/成长并组，自带/学习状态分区；共用菱章保留父矩阵，未知战法类型/等级不补造；137态、长战法滚动与实际点击待发布者，无新规则/存档/图片 |
| 武将卡面身份旗、成长底座与局部图标线绘 | Runtime/CardDetailPresentation.cs、UiLineRaster.cs、MapHudPresentation.cs；Diagnostics/CardVisualDiagnostics.cs；Tools/Check-UiLineRaster.ps1 | 完整来源图片、短姓名旗/星级冠带及真实持有经验/进阶；共用线绘不旋转GUI矩阵以保留滚动裁切，36883项几何检查接入Validate；149态、长名/低星/手机人物占比与实际输入待发布者，无规则/存档/正式图片变化 |
| 姓名旗实际字体适配与发布越界修复 | Runtime/CardPortraitTextLayout.cs、CardDetailPresentation.cs；Diagnostics/CardVisualDiagnostics.cs；Tools/Check-CardPortraitText.ps1 | 062751663失败为字号30的微软雅黑行高39.5947超过38单元，按GUI实际字形宽高/行高适配，原断言保留加强；23237项纯字体检查纳入Validate，Unity及新图待发布者 |
| 战法实际排版与滚动验收修复 | Runtime/CardDossierSkillLayout.cs、CardDossierPresentation.cs；Diagnostics/CardVisualDiagnostics.cs；Tools/Check-CardDossierSkill.ps1 | 083752315失败误将最长文本等同必溢出；共享测量/绘制高度，溢出正滚动与末行可见、容纳零滚动/全可见分别验，55229项纯检查纳入Validate，Unity实际字体及输入待发布者 |
| 存档、备份、读档恢复 | Application/ICampaignRepository.cs；CampaignSession.cs；Infrastructure/Persistence/JsonCampaignRepository.cs | 应用接口与持久化适配器 |
| HUD、卡牌、弹窗、小地图 | Runtime/EasternDynastyPresentation.cs；MapHudPresentation.cs；YouzhouPresentation.cs；EasternDynastyGame.cs | S1地图采用资源/时产顶栏、上图下名导航、完整部队立绘/姓名签/席位底座与44高卡下操作；Diagnostics/MapHudVisualDiagnostics.cs保留可见区域/World不变检查；139态及图像/名称实际点击待发布者，Presentation 程序集 |
| 原作参考分层稿与地图图签导航 | Docs/DesignUI/s1-20261027-114202570；Runtime/MapHudNavigationLayout.cs、MapHudPresentation.cs；Tools/Export-UIDesign.ps1、Check-MapHudNavigation.ps1；Validation/UiDesignChecks.mjs | 2021同页参考、Inkscape实际双稿/切片、六个ui-sprite受控资源；左侧图签/竖标题、独立招募、集中辅助操作；原地图验证保持，主城专业稿复用见211752367入口；实际图文点击/运行观感待发布者 |
| 地图顶部资源分组与透明叠层 | Docs/DesignUI/s1-20261027-124251578；Runtime/MapHudHeaderLayout.cs、MapHudHeaderPresentation.cs、MapView.cs；Diagnostics/MapHudVisualDiagnostics.cs | 2023同页测量/2021清晰辅助、Inkscape桌面/手机/长值稿及三正式组件；四资源两行/货币右锚、长名实际字体适配；相机与输入范围分开保留顶部滚动；1284几何与9组件SHA链通过，160图/六库存点击/顶部悬停及观感待发布者 |
| 山河舆图、真实视野轮廓与定位 | Runtime/MinimapPresentation.cs、MinimapAtlasRaster.cs、MinimapViewportGeometry.cs；Diagnostics/MapHudVisualDiagnostics.cs；Tools/Check-MinimapAtlas.ps1 | 实际1600格山河、独立领地/城池及相机地面投影；边界交点精确0/1，保留真实多边形。12460项纯栅格/视野检查，198个视野场景；两点实际定位/归城待发布重验，PNG预览不是游戏截图，无新资源或存档字段 |
| 地图任务纪事与图像入口 | Runtime/MapHudPresentation.cs；Diagnostics/MapHudVisualDiagnostics.cs、ExperienceDiagnostics.cs；Tools/Check-Experience.ps1 | 原创建筑/真实编队图章、章节/目标分层、44高行动与已完成记录；拖动防误开，真实任务进度，无新规则/存档；148态及地图领奖/遮罩/记录实际点击与观感待发布者 |
| 地图选中土地插画与集中指令 | Runtime/LandSelectionPresentation.cs；EasternDynastyPresentation.cs；Diagnostics/LandVisualDiagnostics.cs | 360×506地块图/等级归属分区、守军/时产双栏和72高并列驻守/屯田/练兵图章；原命令/返回及模态隔离保留；153态与真实图像/身份/图标/文字/关闭重选待发布者，无新规则/存档/正式图片 |
| 地块拾取、缩放、地图渲染 | Runtime/MapView.cs；YouzhouLandscape.cs；相关 Shader | Presentation 与资源 |
| 主地图资源景物、连续地表与留白拾取 | Runtime/YouzhouTerrainLayout.cs、YouzhouLandscape.cs；Resources/YouzhouResource.shader、YouzhouGround.shader；Tools/Check-MapTerrain.ps1 | 四资源真实身份、坐标稳定局部景物、连续草地/柔边；40302项纯几何与World不变检查接入Validate，135态及实际草地点选/Shader观感待发布者，不改PNG/meta或规则 |
| 自动截图、运行检查 | Runtime/Diagnostics/EasternDynastyDiagnostics.cs；ExperienceDiagnostics.cs；RouteDiagnostics.cs；Tools/Validate-Visual.ps1；Check-Experience.ps1；Check-Routes.ps1 | 诊断与验证工具 |
| 地图河岸、水深与实际渡桥结构 | Runtime/YouzhouRiverLayout.cs、YouzhouLandscape.cs；Resources/YouzhouGround.shader、YouzhouBridge.shader；Tools/Check-RiverLayout.ps1 | 真实水格岸距、浅深层次和跨格渡桥曲线；桥面/栏杆投影不进入邻水格，30919项纯检查纳入Validate；141态及真实浅岸/桥面/栏杆输入待发布者，无规则/存档/PNG变更 |

表中 Core、Application、Infrastructure、Runtime 均相对于 Assets/EasternDynasty/Scripts；Resources 相对于 Assets/EasternDynasty。完整图示见 [架构说明](../../Docs/Architecture.md)。

## 保持分层边界

- Core 包含可序列化模型、静态目录和规则；不引用 UnityEngine、UnityEditor、画面或磁盘路径。
- Application 通过 CampaignSession 管理当前 World、时钟、离线结算和保存调用。它依赖 ICampaignRepository 抽象，不使用 JsonUtility 或用户目录。
- Infrastructure 实现本地 JSON 存档与资源提供器。纹理消费者调用 GameAssets.LoadTexture 的稳定 ID，不自己拼接磁盘或 Resources 路径。Shader仍按既有固定Resources路径加载，不在当前PNG清单中。
- Runtime 目录属于 Presentation 程序集，负责 Unity 生命周期、输入、IMGUI 与地图渲染；创建并连接会话和适配器。
- Editor 只负责导入、检查和构建，不可被玩家程序集引用。
- World 的 partial 文件仍属于同一个状态聚合，不是可并发修改的独立服务。不要在后台线程写 World，也不要为了分层重复复制它的库存、领地或行军状态。

## 将 AI 资源接入本地

1. 查看资源清单，确认要补齐或替换的稳定 ID。地图使用 `map.youzhou.grain` 等 ID，人物使用 `portrait.<武将定义ID>`；人物名称不是 ID。
2. 在游戏工程外生成或取得有来源记录的最终 PNG，或放入 AssetInbox。不得把参考截图直接当正式素材，也不要把姓名、星级、等级烘焙进人物原画。
3. 关闭该工程的 Unity Editor，等待导入和构建进程退出，按 [资源流程](../../Docs/Asset-Pipeline.md) 调用导入脚本。例如补齐目录中已有的朱儁：

~~~powershell
& .\Tools\Import-Asset.ps1 -Id 'portrait.zhu_jun' -Kind portrait -SourcePath 'F:\素材\zhu_jun.png' -Purpose '朱儁武将肖像' -SourceKind imagegen -SourceReference '本次图像生成记录' -Rights '原创生成，来源记录见 SourceReference'
& .\Tools\Validate-Assets.ps1
~~~

已有不同内容时，确认本次任务确实包含替换后添加 -Replace；已有授权不需要重复征询。脚本会维护文件、清单与来源元数据，替换保留原 .meta。不要绕过检查手工覆盖正式资源。

4. 运行 Unity 导入与构建，检查实际卡面或地块显示。图片被清单登记不代表自动新增武将、地形或玩法；新类别还需修改对应领域定义及展示映射。
5. 交付时说明接入 ID、来源记录、验证结果与尚未绘制的内容。旧原型有三张肖像与16个明确占位；S1增量有245张真实官网立绘。分别报告各自覆盖，不能把旧占位称为资源齐全。

该流程当前支持本地 PNG。模型、动画、音频、网络下载和玩家运行时热更新不在本轮实现中。

## 存档与模型约束

- 不重命名 World 及模型的序列化字段，不更改类型、枚举值或现有 ID 的含义来“整理命名”。真正需要格式迁移时，先定义迁移规则和兼容检查，再修改版本。
- 旧版使用 campaign.json；幽州使用 youzhou-40-v1.json。两者分别有 .bak。读档必须验证 World.IsValid 与选中的地图槽位。
- 无效主文件可回退有效备份；不能把损坏的主文件覆盖到刚用于恢复的备份。
- 离线推进由 CampaignSession.Load 负责，最多七天；UI 不再重复调用离线结算。
- -edSmoke 与 -edReview 使用只读会话，仍可在内存推进游戏，但不会保存玩家进度。仓库测试必须将独立测试目录传给构造函数。
- 不通过移动工程来迁移或清除 Application.persistentDataPath 中的玩家存档。

## 选择必要的验证

| 修改类型 | 必须关注的检查 |
| --- | --- |
| 仅文字、注释或文档 | 核对实际接口、命令、链接与图示；不为文字变化重跑整套游戏 |
| Core 或 Application | Tools/Validate.ps1；保留旧版、幽州与架构检查 |
| 存档适配器 | 上项加 Unity JsonUtility 与隔离目录的实盘仓库检查 |
| 新增或替换图片 | Tools/Validate-Assets.ps1；Unity 构建前资源检查；实际场景截图 |
| 导入脚本或清单规则 | Validation/AssetPipelineChecks.ps1；不要用正式资产做破坏性测试 |
| IMGUI、地图、Shader、资源消费接口 | Unity 构建；Tools/Validate-Visual.ps1；受影响操作的实际点击与截图 |
| 手机输入、布局、返回键、暂停恢复 | Tools/Check-MobileInput.ps1；Windows 只读 -edSmoke -edMobileReview；Android 真机验证。预览和纯逻辑通过不等于真机通过 |
| 构建、运行、安装脚本 | 在中文路径执行相关命令；检查失败码、日志、安装包完整性与运行进程 |

静态 C# 编译不能证明 Shader 能工作，截图文件存在不能证明界面可用，测试通过也不能证明美术达到商业成品质量。优先验证此次改动影响的行为；无新失败或未决风险时，不反复重跑无关检查。

## 文件与协作约定

- 保留所有已有 .meta 与 GUID。添加脚本或资源后由 Unity 生成新 .meta；移动已有 Unity 文件时把其 .meta 一起移动。
- 不把 Library、Temp、Obj、Logs、Builds、UserSettings、.local、测试二进制、编译响应文件写作源代码依赖。新的机器路径放在 .local 配置或命令参数中。
- 工具从脚本所在位置解析工程目录，不依赖某个用户的 C 盘路径。PowerShell 示例中的中文路径必须正确引用，脚本不能把 JSON 编码当 shell 转义。
- 研究记录保留来源与日期；历史建议与当前规则冲突时，以用户最新要求和 Docs/Rules.md 为准。
- 并行代理只修改分配到的文件，合并后统一验证。不要一边更改资源和源码，一边让另一个 Unity 进程导入同一项目。
- 完成报告应包含实际改变、执行过的检查、证据位置和剩余差距。不要声称后台任务永久运行；代理完成后就是本轮完成。

## 当前实现边界

当前使用 IMGUI、可变 World 聚合和单客户端本地存档。没有多人权威服务器、商人商盟、十天贸易契约、沦陷赎回、同盟建国、太守车、完整约160人卡池或完整战法系统。它们仍是后续需求，不是架构文件中已经落地的组件。

## 当前 Android 与资源任务（2026-10-25）

用户授权并行补充资源和制作 Android APK。本轮通过 `.local/continuous-development/manual-hold.json` 隔离定时开发；交付后由集成者交还调度，实际状态查同目录 `status.json`。Unity 导入、Windows/Android 构建和桌面预览由一个执行者串行运行。唯一源码仍在本目录，纯 ASCII 暂存工程仅作构建或发布输入，不能成为第二个开发目录。

构建入口是 `Tools/Build-Android.ps1 -CheckEnvironmentOnly` 和 `Tools/Build-Android.ps1`，目标 Android 7.1/API25、IL2CPP ARM64/ARMv7、OpenGL ES 3、左右横屏。已通过纯 ASCII 暂存构建，交付到同级 `apk/EasternDynasty-S1-2026102509.apk`，版本 `0.2.0-s1`，以 `apk/build-info.json` 和 `Validation/AndroidBuild/20261025-095122-c34d8396/result.json` 为准。签名、包内两种架构、应用入口与页对齐均已核验；679个原工程文件哈希保持一致。**尚无 Android 真机验收。** Windows 候选构建成功不表示 `games` 已更新，须分别核对各平台交付收据。

移动端行为已接入：单指拖图/轻点选地或主城、双指缩放、面板单指滑动；UI 起始触摸、跨入 UI、多指切换和取消事件会阻止误选。手机禁用鼠标悬停边缘滚动，界面、安全区、拾取和诊断坐标采用同一换算。返回键先退出详情/弹窗/子页面，最后确认保存退出，保存失败不退出；后台切换保存普通会话并取消手势，恢复只结算一次真实时长（最多七日）。`-edSmoke`、`-edReview` 仍为只读会话。具体状态机和设备待验清单见 [移动端交互说明](../../Docs/Mobile-Interaction.md)，不得凭纯逻辑输入模拟宣称已经真机试玩。

资源清单现为 **258 张 ready、16 张旧原型 missing**；新增 `reference.youzhou.resource_hud`、`reference.youzhou.residence_detail` 两张历史参考纹理，`Runtime/ReferenceArtPresentation.cs` 为 `HudSymbols` 和 `CityPresentation` 提供局部图案，数值由当前游戏绘制。它们不是五张独立透明 sprite，也未扩充 245 个资料身份/41 张试玩招募卡。新增候选武将没有 S1 范围证据，不能直接加入招募池；详见 [资源更新审计](../../Docs/Resource-Update-20261025.md)。

Android 和 `-edMobileReview` 优先加载 `Resources.Load<Font>("Fonts/CJK")`，对应 `Assets/EasternDynasty/Resources/Fonts/CJK.otf` 的 Noto Sans SC 字体；失败才使用系统字体回退。字体来源为 `AssetInbox/font-cjk.source.json`，OFL 随包放在 `Assets/StreamingAssets/Licenses`。字体不计入 258 张 PNG 清单，不移除字体或许可来缩小 APK。

本轮已有验证证据：

- [Validation/android-resource-core.log](../../Validation/android-resource-core.log)：881 项规则、应用和移动输入检查通过，其中移动专项61项；Runtime 与 Editor 脚本引用编译通过。
- [build.log](../../build.log)：`EASTERN_DYNASTY_WINDOWS_BUILD_OK`，构建产物为 `Builds/Windows`，不代表安装至 `games`。
- [桌面真实点击回归](../../Validation/Visual/android-resource-regression/20261025-094612-62934647/result.json)：37项断言通过；本轮集成与后续 APK 状态汇总见 [Android 与素材集成记录](../../Docs/Android-Delivery-20261025.md)。用户已取消微信发送，交付仅保存到指定 `apk` 目录。
- [Validation/Visual/android-preview-20261025-1600x720](../../Validation/Visual/android-preview-20261025-1600x720)：1600×720、`-edSmoke -edMobileReview` 的29态预览通过，`player.log` 有 S1 与运行时冒烟成功标记；集成者已查看总览、完整卡牌、九格城区和图鉴的中文与布局。这是桌面安全区模拟，Android 安装、多指输入、软键盘、系统返回、后台恢复和设备性能仍需真机验证。

## 用户实测待解决问题（2026-10-25）

以下三项已在本轮候选源码修复，并通过对应专项运行验证。勾选表示源码修复有证据，不表示已覆盖玩家目录：2026-10-25 16:36，完整发布仍为 `waiting_for_desktop`，正在等待桌面空闲后重跑最终验收；`games` 保留旧可玩版。以 `Validation/Releases/s1-experience-20261025-1635/release.json` 为准，后续发布成功须更新此段。

### S1-PLAY-01：牌库浏览与卡牌详情体验缺失

- [x] 用户反馈：不能查看牌库里面的卡牌，也不能点击卡牌显示全部信息；主要是文字表示，与《率土之滨》的卡牌体验差距明显。
- 修复证据：245张资料卡可按阵营浏览并进入完整立绘/属性/战法/资料详情，41张试玩招募卡与历史资料身份分开。`Validation/Visual/experience-interactive/20261025-073438-cc6cdd5d/result.json` 的37项真实交互通过；16:27的 `youzhou-interactive/result.txt` 再次验证已拥有卡详情、抽卡和弹窗阻挡，16:29的 `s1-interactive/result.txt` 再次验证图鉴与阵营切换。具体布局对比仍按下一轮美术待办优化。
- 预期：牌库/招募池提供可浏览的具体卡牌列表；每张卡可点击进入详情，以完整立绘和卡面呈现，并展示已具备资料的阵营、星级、兵种、属性、等级/成长、战法说明及获取范围。资料图鉴、实际招募池和已拥有武将之间的入口与身份应清晰，缺失资料明确标注，不杜撰数值或技能效果。
- 验收：实际鼠标操作覆盖进入牌库、切换阵营、浏览列表、点击不同卡牌、查看详情和返回；检查文字裁切、滚动、图片加载、遮挡与返回后的筛选保留，并保存截图和交互证据，不能只凭按钮或文字已经存在判定完成。

### S1-PLAY-02：出征路径只能走直线

- [x] 用户反馈：出征算法只能走直线。
- 修复证据：核心87项地图检查覆盖绕障/桥梁/不可达/距离与存读档；`Validation/Visual/s1-experience-20261025-1630-routes-1440x900/20261025-083112-b9d04d6f/` 的112项Unity运行检查通过，并实际查看4张地图相机渲染图。天然地图20节点路线呈现转弯、桥上位置、隔离JSON读档后过桥及抵达；军旗Transform与折线顶点均核验。该组是隔离运行渲染证据，不是桌面点击序列。
- 预期：核对路径计算、军队实际经过的格子和画面轨迹，避免只对起终点做直线插值。出征应沿合法地块路径移动，遵守地图已有的地形通行与桥梁规则；不可达目标给出明确提示。路径预览、军队位置、行军耗时与最终到达结算应使用一致的路线数据。
- 验收：覆盖需要转弯、绕过不可通行地形、经过桥梁、目标不可达，以及行军中存档后读档继续等场景；以实际经过的地块和画面轨迹共同验证。

### S1-PLAY-03：相邻土地需要包含斜向

- [x] 用户要求：斜着也算相邻土地，即上下左右及四个斜角都属于相邻，按八方向处理，地块自身不算相邻。
- 修复证据：`Validation/MapInteractionChecks.cs` 覆盖八邻、自身/越界拒绝及禁止穿障碍角；16:29实际S1交互通过，`Validation/Visual/s1-interactive/01-target.json` → `01-attack-issued.json` → `02-owned.json` 验证目标(7,14)仅经城区(8,15)斜角接壤也可下令并占领。最新发布流程会将本轮鼠标截图复制到独立release目录，防止后续覆盖证据。
- 预期：统一核心相邻判定、可出征/可占领提示与相关 UI 高亮，避免核心规则和界面各自使用四方向、八方向而发生分歧。相邻关系与地形可通行性分开判断，不能因斜向相邻而直接穿越不可通行地形；对角移动跨障碍拐角、耗时和桥梁衔接的细则若缺少依据，追加到 `rag/question.md` 待核实。
- 验收：覆盖四个正方向、四个斜方向、间隔超过一格、自身、地图边缘和角落；并检查合法相邻地块的点击、出征和占领反馈一致，无越界或跨障碍异常。

## 当前并行优化范围与持续交付

2026-10-25 用户追加：左键拖动/地图边缘移动、己方领地边框区分、减少常驻文字并改善色彩布局、主城点击和含中心的九格城区建设、新手任务引导升级。上述首轮功能已接入候选构建，发布状态见上文。当前主城为单部队、单建设队列的可玩实现，不把原作所有主城设施作用宣称已经实现。

用户最新明确要求每1小时读出新RAG配置并自主学习、更新资源、改进玩法和全部UI，不等待逐项催促，继续授权多个子代理并行。现有主城内部、任务和即时练兵尚未获用户认可；功能回归通过不代表UI已还原。按 [小时UI与玩法待办](../../Docs/Hourly-UI-Gameplay-Backlog.md) 依次完成独立练兵、主城/设施/任务层级、结构化战斗回放、直接进入本场战斗观看。缺口追加同级 `rag/question.md` 的 S1UX-05/06，主城任务仍关联02/03/04。

调度、暂停入口与覆盖规则见 [持续开发说明](../../Docs/Continuous-Development.md)，RAG增量接口见 [读取说明](../../Docs/Rag-Sync.md)。宿主读RAG的seen状态不等于内容已应用，每轮要记录实际导入的版本/配置/资产。小时到期优先继续开发，不被旧版等待桌面状态阻塞；只有通过完整验证与图像审查的源码才自动更新Windows和APK。旧版正在使用时保留，禁止终止用户游戏或改写玩家存档。后台任务启动本身不表示上述UI已完成。

## 2026-10-26 开发与发布并行

按用户要求拆分两条计划任务：开发每小时在主工程持续改进，完成后冻结 Source 并入队；发布在独立 Project 副本构建、验收、交付，等待桌面期间开发继续。发布活动版本固定，后续未开始候选合并为最新版本。开发代理只执行纯逻辑检查，不启动 Unity 或客户端；发布者串行操作 Unity 和桌面。开发状态在 `.local/continuous-development`，发布状态与失败反馈在 `.local/publication`。详见 [持续开发说明](../../Docs/Continuous-Development.md)。
