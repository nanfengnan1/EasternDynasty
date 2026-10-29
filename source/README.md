# Eastern Dynasty

**2026-10-29：项目已按用户要求停止自动开发与发布。** 两条游戏计划任务已禁用，配置开关关闭，暂停文件保留；未完成的主城全景工具栏改动留在源码中，尚未发布。只有用户明确要求恢复后才重新启动。先看 [当前状态与交接](Docs/Project-Status.md)、[源码导航](Docs/Code-Map.md) 和 [文档索引](Docs/README.md)。

以灵帝后期、黄巾之乱为背景的 Unity 单人策略游戏原型，已有 Windows 客户端和 Android 试玩 APK。默认从幽州 40×40 地图开始，选择出生点，建立势力，积累资源、招募配将、行军占地并保存进度。

当前工程目录：F:\工作项目\率土之滨\source。迁移前的 C 盘工程只保留为旧副本，后续开发使用本目录。

## 项目现状

已实现幽州1600格、三个出生点、六座默认汉地城池、山地河流和三处桥梁；旧版13地区、13城池由独立入口进入。草料、粮食、石料、铁器、金币及试玩灵玉驱动招募、征兵与武将培养。部队由三名武将组成，按相邻土地扩张，战斗自动结算并生成战报。

当前开发范围是 **S1 幽州**。四类资源地块直接采样本地《率土之滨》参考图片，城池使用由参考图去文字、透明提取后的派生图；山地、森林边缘和桥梁仍有原型素材。支持拖动缩放、地块选择、领地归属与小地图定位。

已接入本地 S1 资料的245个独立武将身份和官网立绘，按汉、魏、蜀、吴、群、晋筛选。41张有对应招募入口证据的卡进入明确标识的试玩池，204张历史/待核实卡只进入图鉴；这不是当前官方完整S1卡池。新建幽州赠送皇甫嵩、王子服、郑泰作为本项目试玩队伍，旧存档武将保留。没有真实充值或支付。

部队可沿陆地和桥梁寻路出征、驻守己方领地、撤回和屯田。屯田展示政令消耗和资源收益，完成后返城；驻守显示九格内己方可防卫范围。当前单人版没有敌方玩家接敌模拟。规则参数、抽卡等概率与时间加速均为本地试玩配置，详见 [S1功能说明](Docs/S1-Delivery.md)、[武将与卡池依据](Docs/S1-Cards.md) 和 [地图美术来源](Docs/S1-Map-Art.md)。

交互优化加入左键拖动与边缘滚动、幽州八方向邻接和加权寻路、可点击完整卡面及属性/战法/资料详情。点击地图主城进入含中心的九格城区，可建设和升级设施；主要事宜提供十项连续任务、前往导航和一次性奖励。当前是单部队、单建设队列实现，设施数值为试玩配置，完整原作城建作用仍需扩展。对比与待办见 [迭代记录](Docs/Iteration-Review.md)。

源码已增加独立练兵、城建设施/任务页面、结构化战斗回放和本场战斗观看入口；仍是本地简化规则，界面效果与真实操作须逐版验收。历史自动化采用每小时开发、独立快照发布，现已停止，工作方式见 [自动化说明](Docs/Continuous-Development.md)。源码进度、候选验收和玩家安装包是三个不同状态，以 [交接记录](Docs/Project-Status.md) 及实际 `games/version.json` / `apk/build-info.json` 区分。

2026-10-25 本轮资源与 Android 适配已通过 881 项检查、Windows 构建、29 态手机布局预览和37项桌面实际点击回归。Android `0.2.0-s1` 已完成纯 ASCII 暂存构建、签名及包体校验，交付文件为同级 `apk/EasternDynasty-S1-2026102509.apk`；尚未完成 Android 真机测试。这些结果不表示同级 `games` 已更新，详见 [集成记录](Docs/Android-Delivery-20261025.md)。

尚未实现多人服务器、商人商盟、贸易契约、沦陷赎回、同盟建国、太守车、完整现行S1卡池和逐条战法结算。幽州地理是玩法布局，尚非官方幽州坐标复刻；战斗是简化实现。正式规则与未决事项见 [规则基线](Docs/Rules.md)。

## 安装开发环境

开发需要 Windows、PowerShell、Unity Hub，以及 ProjectSettings/ProjectVersion.txt 指定的 **Unity 6000.3.27f1**。在 Hub 完成账号登录和许可证激活，并安装该 Editor 的 Windows 构建支持。本项目使用内置渲染管线和 IMGUI；当前综合验证另需 Node.js（直接运行本地 `.mjs`，没有 `npm install` 步骤）。普通玩家不需要这些开发工具。自动化代理才使用 Codex CLI；现有游戏构建无需 OpenAI API 密钥。

~~~powershell
Set-Location 'F:\工作项目\率土之滨\source'
& .\Tools\Setup.ps1
~~~

Setup 检查本机环境，定位 Unity，写入仅本机使用的 .local/unity.json，并调用 Unity 导入和准备场景；不会下载 Unity、登录账号或更改系统执行策略。首次包依赖由 Unity 依据 Packages/manifest.json 与 packages-lock.json 恢复。Unity 定位依次考虑命令参数、UNITY_EDITOR_PATH 环境变量、本机配置和已知安装目录。

Unity 安装位置未自动找到时显式指定路径；只检查环境、暂不导入时使用 -SkipImport：

~~~powershell
& .\Tools\Setup.ps1 -UnityEditor 'D:\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe'
& .\Tools\Setup.ps1 -SkipImport
~~~

## Build

~~~powershell
& .\Tools\Build-Windows.ps1
~~~

脚本在后台启动 Unity 批处理构建，输出完整客户端到 Builds/Windows，构建日志为 build.log。需要覆盖本机 Unity 位置时传 -UnityEditor。构建前会检查资源清单与 Shader；失败时查看日志，不要分发旧构建冒充新结果。

也可在 Unity 中选择 **Eastern Dynasty → Build Windows**。同一项目同时只运行一个 Editor 导入或构建进程；构建前退出正占用输出目录的游戏客户端。

### Android APK

为同一 Unity 版本安装 **Android Build Support、Android SDK & NDK Tools、OpenJDK**，然后执行：

~~~powershell
& .\Tools\Build-Android.ps1 -CheckEnvironmentOnly
& .\Tools\Build-Android.ps1
~~~

目标为 Android 7.1 / API25 及以上的横屏手机，使用 IL2CPP、ARM64/ARMv7 和 OpenGL ES 3。验证成功后，脚本将 APK、SHA-256 和 `build-info.json` 写入 `F:\工作项目\率土之滨\apk`。已交付 `EasternDynasty-S1-2026102509.apk`（117,452,186字节，约112MiB），实际文件名与交付状态以该收据为准。构建参数、失败恢复和侧载安装见 [Android 说明](Docs/Android.md)。Windows 与 Android 的存档各自保存，尚无跨设备同步。

## Run

已有 Windows 客户端位于 `F:\工作项目\率土之滨\games\EasternDynasty.exe`，可直接双击运行，无需安装Unity。2026-10-29 核查时该目录没有 `version.json`，不能把它称为当前源码的新发布版。移动给其他目录时请复制整个 games 运行目录，保留 `_Data`、UnityPlayer.dll 和 MonoBleedingEdge。

运行本工程 `Builds/Windows` 中已构建的客户端（与同级 `games` 安装目录不同）：

~~~powershell
& .\Tools\Run.ps1
~~~

常用开发模式：

~~~powershell
& .\Tools\Run.ps1 -Review
& .\Tools\Run.ps1 -Legacy
& .\Tools\Run.ps1 -PixelWidth 1280 -PixelHeight 720
& .\Tools\Run.ps1 -Editor
~~~

-Review 启动不写玩家存档的临时征程，按 F8 保存诊断截图；-Legacy 进入旧版地图入口。-Editor 打开 Unity 工程。没有构建产物时先运行 Build-Windows.ps1；Run 不会静默下载依赖。

通过 Hub 打开工程时，添加本目录，等待导入，然后打开 Assets/Scenes/EasternDynasty.unity。菜单 **Eastern Dynasty → Play prototype** 可准备场景并进入试玩。

默认出生界面可选蓟城近郊、涿郡平原或渔阳河谷。进入后点击主城，按主要事宜开始建设，再向城区外资源地扩张。左/右/中键拖动、鼠标靠近地图边缘、WASD或方向键可移动地图；滚轮缩放，左键轻点选地，小地图点击定位。HUD和弹窗阻止地图输入，底部提供主城入口、保存和推演倍速。

手机支持单指拖地图、轻点选地或主城、双指缩放，以及卡库和面板的单指滑动。UI 触摸不会穿透地图，手机关闭悬停边缘滚动，横屏界面按刘海和手势条安全区缩放。返回键依次关闭卡牌/弹窗或返回地图，最后才确认保存退出；切换后台会保存普通会话，只读测试不写玩家档。实现和待做真机检查见 [移动端交互说明](Docs/Mobile-Interaction.md)。

## Install 玩家客户端

玩家无需 Unity Editor。将完整 Windows 构建包复制到一个独立目录：

~~~powershell
& .\Tools\Install-Build.ps1 -DestinationPath 'F:\游戏\EasternDynasty'
~~~

该脚本复制整个 Builds/Windows 并检查文件哈希。默认拒绝覆盖非空目标；只有带 .eastern-dynasty-install.json 标记的既有安装可用 -Replace 更新，旧安装会保留为同级 previous 备份目录。更新前退出目标目录正在运行的游戏。目标应是专门的游戏安装目录，不要指向工程根目录或包含其他资料的目录。它不修改注册表，不生成系统安装器，也不创建真实付费入口。

持续交付按用户要求额外使用 `-DiscardPrevious`：成功安装后只清理本次产生的旧包，历史备份保持原状。检测到运行中的目标程序或不能确认其路径时拒绝覆盖；发布流程等游戏退出后重试。验证收据会再次检查实际暂存文件，避免验证后构建被改写。

安装后运行目标目录下的 EasternDynasty.exe。分发时必须包含相邻的 EasternDynasty_Data、UnityPlayer.dll 及其余构建文件，不能只复制 exe。

## 本地资源接入

正式资源以稳定 ID 登记在 Assets/EasternDynasty/Resources/asset-manifest.json。AI 或美术先准备本地 PNG，再使用导入脚本更新清单、文件和来源记录；游戏通过 GameAssets 统一加载。示例：

~~~powershell
& .\Tools\Import-Asset.ps1 -Id 'portrait.zhu_jun' -Kind portrait -SourcePath 'F:\素材\zhu_jun.png' -Purpose '朱儁武将肖像' -SourceKind imagegen -SourceReference '本次图像生成记录' -Rights '原创生成，来源记录见 SourceReference'
& .\Tools\Validate-Assets.ps1
~~~

导入前关闭该工程的 Unity Editor，等待导入和构建进程退出。替换已有不同内容需 -Replace，已有 .meta 保持不变。清单登记不会自动增加武将或地形玩法。完整规范、限制和失败恢复见 [资源流程](Docs/Asset-Pipeline.md)。

截至 2026-10-29，主工程清单有 **360 张 ready 图片、16 张旧原型 missing 肖像**，其中包含后续 Inkscape 设计组件；数量不代表原作还原质量或已交付。最新资源检查还有 102 张图片待 Unity 生成 `.meta`，尚未做主工程新一轮导入。历史资源记录见 [2026-10-25 更新](Docs/Resource-Update-20261025.md)，专业 UI 的可编辑稿及导出方式见 [设计流程](Docs/Professional-UI-Workflow.md)。嵌入的 `Resources/Fonts/CJK.otf`（Noto Sans SC）不计入 PNG 数量，OFL 许可随包保留在 `Assets/StreamingAssets/Licenses`。

## 验证

~~~powershell
& .\Tools\Validate.ps1
& .\Tools\Validate-Assets.ps1
& .\Tools\Check-MobileInput.ps1
& .\Validation\AssetPipelineChecks.ps1
& .\Validation\ToolingChecks.ps1
& .\Tools\Validate-Visual.ps1 -Tag 'local-check'
& .\Tools\Validate-Visual.ps1 -Legacy -Tag 'legacy-check'
& .\Tools\Check-UiClicks.ps1
~~~

Validate.ps1 运行规则、应用层、移动输入、UI 几何/设计来源及 Unity 引用编译，不启动 Editor 或游戏；Validate-Assets 检查清单和本地资源。具体检查入口见 [工具索引](Tools/README.md)。ToolingChecks 会复制已有完整安装包；视觉/点击检查会启动客户端并操作桌面，仅在需要实际验收时执行。截图状态数量随代码增加，以当次收据为准。Unity 的 **Eastern Dynasty → Validate architecture and assets** 菜单另做导入和隔离目录的 JsonUtility 仓库检查。

架构重构阶段的历史检查见 [架构工作记录](Docs/Architecture-Worklog.md)。2026-10-25 本轮通过 **881 项规则、应用和移动输入检查**（原 820 项加 61 项），日志见 [android-resource-core.log](Validation/android-resource-core.log)；Windows 构建的成功标记见 [build.log](build.log)。另已完成 1600×720 的 `-edSmoke -edMobileReview` 只读预览，29 态及成功日志在 [手机布局预览目录](Validation/Visual/android-preview-20261025-1600x720)，已人工查看总览、完整卡牌、九格城区与图鉴的中文和布局。这是在 Windows 模拟手机安全区，不能代替 APK 安装和真机多指触控、后台恢复验证。

历史桌面交互结果见 [2026-10-25 集成记录](Docs/Android-Delivery-20261025.md)，不能替代当前源码验收。现行独立发布在主工程 `Validation/Releases/<版本>/publication.json` 保存指针，实际 `release.json`、截图和构建证据在该版本快照 `Project/Validation` 中；旧流程记录可能只有 `release.json`。Android 构建证据位于 `Validation/AndroidBuild/<运行ID>`，最终安装以各平台交付收据为准。

静态编译不能代替 Unity 导入、JsonUtility、Shader 或真实画面验证。资源栏和民居目前使用历史参考纹理局部图案，其他独立原版 HUD、城建设施、桥梁和军队方向图仍有缺口；测试通过不表示美术已经完整还原。

## 目录与架构

| 目录或文件 | 用途 |
| --- | --- |
| Assets/EasternDynasty/Scripts/Core | 可序列化模型、武将目录、地图与规则 |
| Assets/EasternDynasty/Scripts/Application | 当前征程、注入时钟、离线结算、仓库接口 |
| Assets/EasternDynasty/Scripts/Infrastructure | JSON 存档与资源加载适配器 |
| Assets/EasternDynasty/Scripts/Runtime | Unity 生命周期、IMGUI、地图、输入与诊断 |
| Assets/EasternDynasty/Editor | 资源导入、构建前检查、场景和构建工具 |
| Assets/EasternDynasty/Resources | 已登记的本地正式资源及 Shader |
| AssetInbox | 待导入资源与使用说明，不直接打进游戏 |
| Tools | 安装环境、构建、运行、安装包与验证命令 |
| Automation | 开发、发布、RAG 读取和队列模块；当前关闭 |
| Validation | 检查源代码、隔离测试数据、截图与日志证据 |
| Docs | 规则、架构、资源规范与研究记录 |
| Library、Temp、Builds、.local | 本机缓存、产物和配置，不作为源码依赖 |

[AGENTS.md](AGENTS.md) 是后续 AI 的短入口，[agent.md](agent.md) 提供按任务读取、分层约束和验证矩阵。[架构说明](Docs/Architecture.md) 包含分层图、内部组件及启动、资源、行军、存档时序。

只读查看任务开关、暂停状态和客户端收据，不启动任何 agent：

~~~powershell
& .\Tools\Get-ProjectStatus.ps1
~~~

## 存档

Windows 存档通常位于：

~~~text
%USERPROFILE%\AppData\LocalLow\EasternDynasty\EasternDynasty\
~~~

幽州为 youzhou-40-v1.json，旧版为 campaign.json，各有同名 .bak。两个入口只读各自地图槽位；主文件无效时可尝试有效备份。正常游戏每30秒自动保存，退出时保存，离线最多结算七天。-Review 与自动运行检查不写玩家进度。

工程移动或安装包更新不会自动迁移、覆盖或清除该目录。实际位置由 Unity 的 Application.persistentDataPath 决定。

## 常见问题

| 现象 | 检查位置 |
| --- | --- |
| 找不到 Unity | 查看 ProjectVersion.txt，运行 Setup 并传 -UnityEditor；不要把 Hub.exe 当 Editor |
| Unity 许可证错误 | 在 Hub 完成登录和激活；Setup 不负责账号操作 |
| 项目被另一 Editor 占用 | 关闭该工程的另一个 Editor，串行运行导入与构建 |
| 游戏缺失或构建文件被占用 | 先构建；关闭占用 Builds/Windows 的客户端后重建 |
| 资源 ID 未登记或必需图片缺失 | 运行 Validate-Assets，核对清单 ID、状态、哈希、resourcePath 与正式文件 |
| PNG 导入后没有显示 | 等待 Unity 导入并重建；检查消费代码是否使用对应稳定 ID；本轮不是运行时热更新 |
| 武将显示待绘制 | 检查该人物在清单中是否明确为 missing；不要误用别人的肖像掩盖缺图 |
| 读档失败 | 核对地图入口与 .bak；不要直接覆盖玩家存档做调试 |
| PowerShell 脚本被系统策略限制 | 查看本机允许的脚本执行方式；项目不会自动修改系统执行策略 |
| 检查失败 | 查看命令输出与 build.log、Validation 中对应日志；不要把历史通过记录当本轮结果 |
