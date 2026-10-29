# 源码导航

整理日期：2026-10-29。唯一开发目录是 `F:\工作项目\率土之滨\source`。以下以当前文件为准，不为整理移动 Unity 文件或更改已有 GUID。后台处于停止状态。

## 分层入口

`Assets/EasternDynasty/Scripts` 下有四个运行时程序集；`Assets/EasternDynasty/Editor` 是第五个、仅编辑器程序集。依赖由 `.asmdef` 定义。

| 层 | 首要文件 | 职责 |
| --- | --- | --- |
| Core | `World.cs`、`World.*.cs`、`CampaignModels.cs`、`CityModels.cs`、`GeneralModels.cs` | 唯一世界状态、规则、可序列化模型 |
| Application | `CampaignSession.cs`、`ICampaignRepository.cs` | 会话、注入时钟、离线推进、保存接口 |
| Application 只读展示数据 | `TrainingPlanPreview.cs`、`TrainingExecutionStatus.cs`、`BattleReplayFrame.cs`、`BattleReportArchive.cs`、`CityQuestObjectivePlan.cs` | 从真实世界/冻结战报生成界面数据，不代替 Core 结算 |
| Infrastructure | `Persistence/JsonCampaignRepository.cs`、`Assets/GameAssets.cs`、`Assets/AssetCatalog.cs` | 本地存档、稳定资源 ID 与缓存 |
| Runtime / Presentation | `EasternDynastyGame.cs`、`EasternDynastyPresentation.cs`、`YouzhouPresentation.cs` | 生命周期、输入、IMGUI 页面组合 |
| Editor | `Assets/EasternDynasty/Editor` | 场景准备、导入验证、Windows/Android 构建 |

Core 与 Application 不引用 Unity；Presentation 可以调用 Application/Core 并连接 Infrastructure。`World` 的 partial 文件共用同一对象，不能当作多个并行服务。

## 按功能查找

下表路径相对于 `Assets/EasternDynasty/Scripts`。`Runtime/Diagnostics` 中的诊断与同名页面配套。表中功能表示源码存在，不代表玩家安装包已包含。

| 功能 | 源码入口 | 对应验证 |
| --- | --- | --- |
| 幽州地图/出生/存档校验 | `Core/YouzhouMapDefinition.cs`、`World.Creation.cs`、`World.Validation.cs` | `YouzhouChecks.cs`、`ArchitectureChecks.cs` |
| 八方向邻接、路径、行军/驻守/屯田/练兵 | `Core/World.cs`、`World.Combat.cs`、`World.Orders.cs` | `MapInteractionChecks.cs`、`S1OrderChecks.cs`、`TrainingChecks.cs` |
| 城区/设施/任务 | `Core/World.City.cs`、`CityModels.cs`；`Application/City*` | `CityProgressionChecks.cs`、`CityQuest*Checks.cs` |
| 战斗与冻结回放 | `Core/World.Combat.cs`、`BattleReplayModels.cs`；`Application/Battle*` | `BattleReplayChecks.cs`、`BattleReplayFrameChecks.cs` |
| 武将数据/招募/成长 | `Core/Catalog.cs`、`S1Roster.Generated.cs`、`World.Generals.cs` | `S1RosterChecks.cs`；生成文件不能手改 |
| 拾取/镜头/手势 | `Runtime/MapView.cs`、`MapPointerGesture.cs`、`MobileMapGesture.cs`、`MobileMapInput.cs` | `Check-MobileInput.ps1`、`Check-UiClicks.ps1` |
| 地形/河流/桥梁/领地 | `Runtime/YouzhouLandscape.cs`、`YouzhouTerrainLayout.cs`、`YouzhouRiverLayout.cs` | `Check-MapTerrain.ps1`、`Check-RiverLayout.ps1` |
| 地图常驻 UI | `Runtime/MapHudPresentation.cs`、`MapHudHeaderPresentation.cs`、`MapHudQuestPresentation.cs`、`MapHudArmyPresentation.cs` | `Check-MapHudNavigation.ps1`、`Map*ExperienceChecks.ps1` |
| 小地图/名签 | `Runtime/MinimapPresentation.cs`、`MinimapViewportGeometry.cs`、`MapCityLabelPresentation.cs` | `Check-MinimapAtlas.ps1`、`Check-MapCityLabel.ps1` |
| 土地详情/指令 | `Runtime/LandSelectionPresentation.cs`、`LandSelectionLayout.cs`、`LandCommandMenuLayout.cs` | `Check-LandSelectionLayout.ps1`、`LandExperienceChecks.ps1` |
| 主城/全景/工具栏 | `Runtime/CityPresentation.cs`、`CityOverviewPresentation.cs`、`CityPanoramaCamera.cs`、`CityPanoramaToolbarPresentation.cs` | `Check-CityPanorama.ps1`、`Check-Experience.ps1`；工具栏为中断轮次 |
| 主城顶部/部队/事务/底栏 | `Runtime/CityHeaderDesignPresentation.cs`、`CityArmyDesignPresentation.cs`、`CityAffairsDesignPresentation.cs`、`CityDockDesignPresentation.cs` | 对应 `Check-City*Layout.ps1` |
| 设施/任务 | `Runtime/CityFacility*Presentation.cs`、`CityQuest*Presentation.cs`、`CityConstructionPresentation.cs` | 对应布局检查、`Check-Experience.ps1` |
| 武将图鉴/已拥有/详情 | `Runtime/S1ArchivePresentation.cs`、`OwnedRosterPresentation.cs`、`CardDetailPresentation.cs`、`CardDossierPresentation.cs`、`CardChromePresentation.cs` | `Check-ArchiveCardLayout.ps1`、`Check-CardDossierSkill.ps1`、`Check-CardChrome.ps1` |
| 练兵/补兵 | `Runtime/Training*Presentation.cs`、`ReinforcementPresentation.cs`；`Application/Training*`、`ReinforcementPlan.cs` | `Training*Checks.cs`、`ReinforcementChecks.cs` |
| 战报/回放/观看 | `Runtime/BattleReportArchivePresentation.cs`、`BattleReplayPresentation.cs`、`BattleWatchPresentation.cs`、`BattleFieldPresentation.cs` | `Battle*Checks.cs` 和实际页面回归 |
| 手机布局/返回/后台恢复 | `Runtime/MobileScreenLayout.cs`、`MobilePresentation.cs`、`MobileNavigation.cs`、`MobileSuspendClock.cs` | `Check-MobileInput.ps1`；仍需 Android 真机 |

`*Layout.cs` 多为独立几何计算；`*Presentation.cs` 绘制和交互；Diagnostics 验证真实页面状态。三个职责分开阅读。新增功能优先沿现有边界扩展，不把验证断言当作玩法实现。

## 工具、资源、证据

| 位置 | 内容与边界 |
| --- | --- |
| `Assets/EasternDynasty/Resources/asset-manifest.json` | 正式图片身份、来源、哈希；通过 `Tools/Import-Asset.ps1` 接入 |
| `Docs/DesignUI/<版本>` | SVG 源稿、布局、Inkscape 实际导出、设计预览 |
| `Docs/UIIterations/<版本>.md` | 原图依据、与当前游戏差距、本轮改动及待验收项 |
| `Validation/*.cs`、`*.ps1`、`*.mjs` | 验证源代码；不是可以整体删除的缓存 |
| `Validation/bin`、截图/日志子目录 | 生成产物与历史证据，不参与游戏运行 |
| `Automation/runner.mjs` | 小时开发与冻结入队；当前 disabled |
| `Automation/publisher.mjs` | 固定候选的构建/验收/交付；当前 disabled |
| `Automation/publication-queue.mjs` | 队列、活动版本、不可变输入校验 |
| `Automation/rag-sync.mjs` | 读取摘要与变化；读过不等于应用 |
| `.local` | 机器配置、暂停文件、运行状态和本次整理备份 |
| `F:\EasternDynastyBuild\Releases` | 历史冻结 `Source` 与独立验证 `Project`；不是开发目录 |

本轮没有搬动或删除上述源代码、资产、存档及快照。过长的逐轮导航已从 `agent.md` 收到 [历史指南](History/Agent-Guide-20261029.md)，当前入口只保留长期有效的任务导航。
