# Eastern Dynasty 软件架构

更新日期：2026-10-29。工程根目录：F:\工作项目\率土之滨\source。目标运行环境是 Unity 6000.3.27f1、Windows、内置渲染管线和 IMGUI。

项目将游戏规则、征程生命周期、文件存档、资源加载及 Unity 展示分开，让后续开发能够按任务定位少量代码，并通过稳定资源 ID 接入本地图片。既有地图、数值、武将规则和存档字段保持兼容。实际构建与检查结果见 [架构工作记录](Architecture-Worklog.md)。

## 分层与依赖方向

~~~mermaid
flowchart TB
    P["Presentation<br/>Runtime：Unity生命周期、输入、IMGUI、地图"]
    A["Application<br/>CampaignSession、ICampaignRepository"]
    C["Core<br/>World、模型、Catalog、地图与规则"]
    I["Infrastructure<br/>JsonCampaignRepository、GameAssets、资源提供器"]
    E["Editor<br/>导入器、资产校验、场景与构建工具"]
    F[("文件系统<br/>本地存档、正式PNG、资源清单")]
    U["Unity API<br/>JsonUtility、Resources、Texture2D"]

    P --> A
    P --> C
    P --> I
    A --> C
    I --> A
    I --> C
    I --> F
    I --> U
    E --> P
    E --> A
    E --> C
    E --> I
~~~

图中的箭头表示编译或实现依赖，不是每帧调用顺序。Application 定义 ICampaignRepository，Infrastructure 实现它；会话通过接口使用仓库，不反向引用具体适配器。

| 层 | 目录与职责 | 不应承担 |
| --- | --- | --- |
| Core | Scripts/Core；状态、静态目录、地图、经济、武将、行军战斗及有效性判断 | Unity对象、文件路径、资源加载、界面布局 |
| Application | Scripts/Application；当前World、注入时钟、离线推进、创建与保存调用 | JSON格式、磁盘操作、MonoBehaviour、渲染 |
| Infrastructure | Scripts/Infrastructure；JSON存档和资源清单/提供器 | 战斗数值、UI控制、出生地决策 |
| Presentation | Scripts/Runtime；应用组装、输入、IMGUI、地图、诊断 | 自行拼接资产磁盘路径、重复离线收益、另建一份游戏状态 |
| Editor | EasternDynasty/Editor；导入设置、清单检查、场景和构建 | 玩家运行时依赖 |

五个 .asmdef 将这些边界落实到编译。Core 与 Application 使用 noEngineReferences；Editor 仅在 Editor 平台编译。Runtime 的文件夹名称为兼容现有工程保留，其程序集职责是 Presentation。

当前 UI 的行军、招募、培养等命令仍调用 World 的公开规则方法。Application 主要抽离征程生命周期，没有为每个按钮新增用例类，也没有引入服务容器或事件总线。

## 内部组件

~~~mermaid
flowchart LR
    Bootstrap["EasternDynastyGame<br/>组装与生命周期"]
    UI["EasternDynastyPresentation<br/>YouzhouPresentation"]
    Session["CampaignSession<br/>当前World与时钟"]
    Port["ICampaignRepository"]
    Repo["JsonCampaignRepository"]
    World["World<br/>唯一征程状态"]
    Rules["World partial规则<br/>Creation / Validation / Economy<br/>Generals / Combat"]
    Models["CampaignModels<br/>GeneralModels / Catalog<br/>YouzhouMapDefinition"]
    Map["MapView<br/>镜头、拾取、选择、行军标识"]
    Landscape["YouzhouLandscape / WorldLandscape<br/>网格、图块、归属"]
    Assets["GameAssets<br/>稳定ID、缓存、缺图策略"]
    Provider["IGameAssetProvider<br/>ResourcesGameAssetProvider"]
    Manifest[("asset-manifest.json<br/>正式PNG")]
    Diagnostics["EasternDynastyDiagnostics<br/>Smoke与Review"]

    Bootstrap --> UI
    Bootstrap --> Session
    Bootstrap --> Repo
    Session --> Port
    Repo -.实现.-> Port
    Session --> World
    UI --> World
    UI --> Assets
    World --- Rules
    World --> Models
    Bootstrap --> Map
    Map --> World
    Map --> Landscape
    Landscape --> Assets
    Assets --> Provider
    Provider --> Manifest
    Diagnostics --> Bootstrap
~~~

World 的 partial 文件共享同一个 CLR 类型和同一份数据，拆文件用于缩小阅读与修改范围，不创建并发服务。模型的序列化字段集中保留，数据列表仍可变。界面和地图读取同一 World 引用；开始或读取征程成功后，组装层更新展示引用并重建地图。

### Core 文件职责

| 文件 | 主要内容 |
| --- | --- |
| World.cs | 序列化状态、地图属性/查询、order兼容属性与消息 |
| World.Creation.cs | 幽州与旧天下创建、出生点和开局 |
| World.Validation.cs | 存档结构、地图元数据、集合与状态校验 |
| World.Economy.cs | 每小时收入、时间推进、已有结算顺序 |
| World.Generals.cs | 招募、编队、训练、进阶、拆解与学习 |
| World.Combat.cs | 征兵、攻击条件、行军、守军与战斗 |
| World.Orders.cs | 驻守、屯田、练兵与订单阶段推进 |
| World.City.cs / CityModels.cs | 城区、设施、建设与任务规则/状态 |
| BattleReplayModels.cs | 冻结战斗记录与回放事件模型 |
| S1Roster.Generated.cs | 由 RAG 快照生成的资料武将目录，不手改 |
| CampaignModels.cs | 阵营、地块类型、资源、地块、行军、战报 |
| GeneralModels.cs | 武将定义、实例及等级规则 |
| Catalog.cs | 原型武将目录、区域及显示名称 |
| YouzhouMapDefinition.cs | 幽州40×40布局、出生点、城池、桥梁和版本ID |

模型字段、类型及 v1/v2 兼容规则详见 [Core与存档契约](Architecture-Core-Contract.md)。

## 启动与开始征程时序

~~~mermaid
sequenceDiagram
    actor Player as 玩家
    participant Unity
    participant Game as EasternDynastyGame
    participant Repo as JsonCampaignRepository
    participant Session as CampaignSession
    participant World
    participant Map as MapView
    participant Assets as GameAssets

    Unity->>Game: 场景加载后Bootstrap，Awake
    Game->>Repo: new(应用持久化目录)
    Game->>Session: new(repo, UTC时钟, 只读模式)
    Game->>World: 创建开始界面的预览地图
    Game->>Map: Setup(预览World)
    Map->>Assets: 按稳定ID加载地图图片
    Game-->>Player: 绘制出生界面
    Player->>Game: 选择出生点并开始
    Game->>Session: StartYouzhou(index, ruler)
    Session->>World: CreateYouzhou
    World-->>Session: 合法新征程
    Session-->>Game: World引用
    Game->>Map: Setup(当前World)
    opt 正常模式请求保存
        Game->>Session: Save()
        Session->>Repo: Save(World)
    end
    Game-->>Player: 地图与HUD
~~~

预览状态不等于已开始的征程。Start 只创建内存状态；是否立即保存由调用方决定。只读运行检查仍可创建、加载和推进内存状态，但 Session.Save 不改时间戳、不调用仓库。

## 本地资源接入与加载时序

资源接入分为开发期导入和玩家运行期加载。导入脚本处理已经存在的本地 PNG，不下载 URL；ResourcesGameAssetProvider 读取构建包内资源，不在玩家运行时安装任意图片。

~~~mermaid
sequenceDiagram
    actor Creator as AI或美术
    participant Import as Import-Asset.ps1
    participant Check as 本地清单与PNG检查
    participant Disk as 正式文件与清单
    participant Editor as Unity Editor
    participant Build as 构建前校验
    participant View as 地图或人物UI
    participant Assets as GameAssets
    participant Provider as ResourcesGameAssetProvider

    Creator->>Import: 本地PNG、稳定ID、用途、来源记录
    Import->>Check: 校验ID、类型、目标、图片、已有内容
    alt 冲突或格式不合法
        Check-->>Import: 失败
        Import-->>Creator: 原正式资源保持不变
    else 可导入或显式允许替换
        Import->>Disk: 受控写入图片与manifest，保留既有meta
        Disk-->>Import: 提交结果
        Import-->>Creator: 导入结果与记录
        Creator->>Editor: 导入工程并构建
        Editor->>Build: ValidateForBuild()
        Build->>Disk: 校验清单、源文件、哈希、导入结果
        Build-->>Editor: 通过才继续构建
        View->>Assets: LoadTexture(稳定ID, required)
        Assets->>Provider: 首次读取manifest并解析
        Assets->>Provider: 首次加载登记的Texture2D
        Provider-->>Assets: Texture2D或缺失
        Assets-->>View: 缓存纹理或按缺图策略返回
    end
~~~

地图 ID 统一为 `map.youzhou.<name>`，name 为 grain、pasture、stone、iron、city、mountain、forest；人物 ID 为 `portrait.<Catalog中的武将ID>`。Resources路径、磁盘路径、尺寸、哈希和来源由清单与导入流程管理。不得在 UI 中把中文姓名拼成文件名。Shader仍按既有固定Resources路径加载，未纳入PNG目录。

GameAssets 在 Unity 主线程使用，按 ID 缓存纹理。必需资源未登记、未就绪或无法加载时抛出明确异常；允许缺失的肖像返回 null 并仅警告一次，由 UI 显示“待绘制”。ClearCache 只清理自身引用，不销毁已被画面或图集使用的纹理。提供器可通过 IGameAssetProvider 替换，但本轮默认实现仍是 Resources。

常规导入异常会尝试回滚；图片与清单是两个文件，进程硬中止可能留下中间状态，不能视为跨文件瞬时原子事务。未完成的事务日志会阻止继续导入和构建，需要按记录执行 -Recover；恢复也会拒绝覆盖中断后出现的外部修改。详细命令、清单结构、替换和中断恢复见 [资源流程](Asset-Pipeline.md)。新增资源 ID 不会自动新增游戏对象；新武将仍需 Catalog 定义，新地形仍需模型、地图与渲染映射。

## 输入与行军结算时序

~~~mermaid
sequenceDiagram
    actor Player as 玩家
    participant Map as MapView
    participant UI as Presentation
    participant World
    participant Game as EasternDynastyGame.Update
    participant Session as CampaignSession

    Player->>Map: 点击地图
    Map->>Map: 检查HUD阻挡与视口
    Map->>Map: 射线与地面/网格求交
    Map-->>UI: selected地块ID
    UI->>World: AttackProblem(id)
    World-->>UI: 可出征或禁止原因
    Player->>UI: 派兵占领
    UI->>World: March(id)
    World->>World: 扣消耗、记录ArmyOrder
    loop 每帧
        Game->>Session: Tick(帧间隔 × 推演速度)
        Session->>World: Tick(seconds)
        World->>World: 收益与行军时间推进
        alt 抵达目标
            World->>World: 战斗、占领或失败、经验与战报
            World->>World: 设置返程
        else 返程结束
            World->>World: 清空order，部队驻守
        end
        Map->>World: 读取行军和归属
        Map->>Map: 更新路线、旗标、领地覆盖
    end
    UI-->>Player: 状态与战报
~~~

战斗结算属于 Core，动画不决定胜负。幽州扩张采用八方向邻接（含斜角，自身不算相邻）；路径计算同时检查可通行地形，不能穿越障碍角。行军按实际合法路径推进，预览、耗时和军旗沿同一路线。桥格可通行与占领，但不产生被动粮食收入。幽州采用8×8分块，共25块；图片共用图集，拾取不为1600格分别创建 Collider。

## 保存时序

~~~mermaid
sequenceDiagram
    participant UI as 手动/自动/退出保存
    participant Session as CampaignSession
    participant Clock as 注入UTC时钟
    participant World
    participant Repo as JsonCampaignRepository
    participant Disk as 文件系统

    UI->>Session: Save()
    alt 无征程或只读模式
        Session-->>UI: 无副作用返回
    else 可保存
        Session->>World: IsValid
        Session->>Clock: 读取当前UTC
        Session->>World: 暂设savedUtcTicks
        Session->>Repo: Save(World)
        Repo->>World: 校验并生成JSON
        Repo->>Disk: 同目录唯一tmp，写入并Flush
        alt 首次保存
            Repo->>Disk: File.Move(tmp, 主文件)
        else 已有有效主文件
            Repo->>Disk: File.Replace，旧主文件轮换为bak
        else 已有损坏主文件
            Repo->>Disk: 替换主文件，保留既有bak
        end
        alt 写入成功
            Repo-->>Session: 成功
            Session-->>UI: 成功
        else I/O或替换失败
            Repo-->>Session: 抛出异常
            Session->>World: 恢复原savedUtcTicks
            Session-->>UI: 抛出异常供界面提示
        end
    end
~~~

自动保存周期由 Unity 组装层控制，当前为30秒；Application 不依赖帧循环。仓库不采用“删掉主文件再写”的失败降级方式。实例锁保护同一仓库实例，不提供跨进程存档冲突解决。

## 读取与离线结算时序

~~~mermaid
sequenceDiagram
    actor Player as 玩家
    participant Game as Presentation
    participant Session as CampaignSession
    participant Repo as JsonCampaignRepository
    participant Disk as 主文件与bak
    participant World
    participant Map as MapView

    Player->>Game: 继续所选地图征程
    Game->>Session: Load(youzhou, out restoredBackup)
    Session->>Repo: Load(youzhou)
    Repo->>Disk: 读取该槽主文件
    Repo->>World: JsonUtility反序列化、IsValid、槽位匹配
    alt 主文件不存在或不可用
        Repo->>Disk: 读取同槽bak
        Repo->>World: 同样完整校验
    end
    alt 无可用候选
        Repo-->>Session: 抛异常
        Session-->>Game: 保留原World并提示失败
    else 候选有效
        Repo-->>Session: 独立World与备份标记
        Session->>World: 再验证并Tick(离线0到7天)
        Session->>Session: 替换当前World
        Session-->>Game: 新World与备份标记
        Game->>Map: Setup(新World)
        Game-->>Player: 已读取或已从备份恢复
    end
~~~

Load 本身不修复磁盘、不保存、不更新时间戳。未来时间戳的离线时长按0处理；过去时间戳最多结算七天。UI 不重复离线结算，也不把“文件存在”当作“存档有效”。

| 槽位 | 文件 | 校验约束 |
| --- | --- | --- |
| 旧天下 | campaign.json 与 .bak | v1、36×27、972格；兼容旧JSON缺失地图字段 |
| 幽州 | youzhou-40-v1.json 与 .bak | v2、mapId=youzhou-40-v1、40×40、1600格、revision=1 |

文件名由固定槽位选择，不能由未验证的存档字段拼接任意路径。主文件与备份均失败时明确报错，不静默建立新游戏覆盖存档。

## 构建与验证边界

Tools/Common.ps1 统一项目路径与 Unity 发现；Setup 准备环境，Build-Windows 调用 Editor 构建，Run 运行已有客户端或打开 Editor，Install-Build 复制完整玩家包并校验。机器私有路径保存在 .local，不成为源码依赖。

检查分三层：纯 C# 规则/应用检查、Unity Editor 导入与实盘检查、Player 画面与交互检查。Core字段兼容、固定时钟会话、清单错误、文件冲突、缺图与弹窗输入隔离需要各自对应的证据。截图与游戏进度测试不会证明美术已达到参考游戏成品质量。

## 当前局限与扩展方向

1. World仍是公开集合组成的可变聚合，IMGUI仍直接调用部分规则方法。未来多人服务端需要独立命令边界和服务端权威状态，当前没有该实现。
2. Catalog中的武将与规则仍是代码目录；资源清单只管美术，不替代玩法配置系统。
3. GameAssets提供器接口为未来资源后端保留替换点，尚未实现Addressables、远程下载、热更新、模型、音频或动画管线。
4. 诊断从主生命周期文件拆开，但仍与Presentation共用partial类，方便检查私有UI状态；它不是独立测试运行器程序集。
5. 图片每类只有一个主要外观，城池与地形变化仍有限。分层、缓存和自动检查不等于美术质量完成。

后续功能应先沿现有边界增加最小实现和验证，再根据实际复杂度引入新模块；不要为了架构图完整而创建未使用的服务层。

## 当前 UI 与后台模块（2026-10-29）

当前 Runtime 已拆为页面 Presentation、纯几何 Layout 和页面 Diagnostics；完整入口见 [源码导航](Code-Map.md)。训练、补兵、城建任务与回放在 Application 中有只读展示模型；这些对象从真实 World 或冻结战报构造，不拥有另一套库存，不重新结算战斗。

~~~mermaid
sequenceDiagram
    participant V as 练兵或回放页面
    participant A as Application 只读展示模型
    participant W as Core World / 冻结战报
    participant D as 页面诊断
    V->>A: 请求当前阶段、预览或回放帧
    A->>W: 读取真实订单 / 冻结事件
    A-->>V: 展示数据，不推进结算
    opt 用户执行有效游戏命令
        V->>W: 调用已有规则方法
        W-->>V: 结果与新状态
    end
    D->>V: 校验真实文字、范围与交互目标
~~~

资源设计在 Docs/DesignUI 保存 SVG/规格，Tools/Export-UIDesign.ps1 实际调用 Inkscape 导出；正式图片由 Import-Asset.ps1 写清单，Runtime 再按稳定 ID 消费。当前仍是 IMGUI，没有完成 UI Toolkit 或 Figma 原生视图迁移。

Automation 的 runner、publication-queue、publisher 分别负责开发、冻结排队、独立发布；rag-sync 仅读取资料变化。冻结 Source 不可变，Project 只供 Unity 构建/验收。**这些模块当前全部停用**，配置与暂停状态见 [交接记录](Project-Status.md)。本次整理不更改玩法/存档模型，也不搬动 Unity 资产。
