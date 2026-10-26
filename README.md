# EasternDynasty：游戏 AI 协作与目录设计

本项目通过资料研究 AI、RAG 知识管理 AI、游戏研发 AI、Skills/工具 AI 和独立审查 AI，形成“研究 → 知识发布 → 开发应用 → 验收交付 → 问题反馈”的循环。

本文说明 **AI 之间的任务交接、调度时间、状态反馈和版本发布方式**，不涉及公司人员编制。

- 整理日期：2026-10-26。
- 实际部署根目录（`PROJECT_ROOT`）：`F:\工作项目\率土之滨`。
- Git 文档与代码仓库（`REPO_ROOT`）：`X:\program\github\EasternDynasty`。
- 本次核对时，X 盘仓库已有 `README.md`、`LICENSE` 和 `skills/`；`source/`、`rag/` 等完整实现仍在 F 盘，尚未同步到该仓库。
- 本文使用文内导航；实现文件以“根目录 + 相对路径”说明，单独复制 README 后仍可完整阅读。
- 当前状态：已有研究、RAG 更新、每小时研发，以及独立发布队列与快照任务。具体候选是否已通过验收、完成交付，以实际收据为准。
- 目标架构：补齐统一汇报、知识缺口反向派单、每两天发布窗口，并验证独立发布与下一轮研发的完整交付流程。
- **本文包含现有实现与待实施方案；本次文档更新没有修改程序、定时任务或调度配置，也没有迁移运行中的工程。**
- 本文“游戏发布”指更新项目本地 Windows 客户端和 Android APK；“知识发布”指发布可查询的 RAG 数据版本，两者分别记录。

快速导航：[AI 组件](#ai-components) · [时间与现状](#timing) · [交互架构图](#interaction-flow) · [发布与并行研发](#publication) · [现有目录](#directory-layout) · [目标仓库](#repository-design) · [AI 读写与交接](#directory-ownership) · [迁移约定](#portability) · [文件定位与查询](#file-entrypoints)

<a id="ai-components"></a>

## 1. AI 与执行组件

| 标识 | 组件 | 接收的输入 | 交付的结果 |
| --- | --- | --- | --- |
| A | 持续学习 / 资料研究 AI | 待研究问题、已有资料、来源线索、证据要求和优先级 | 文档、真实图片、来源、研究结论、未解决项及完整资料批次 |
| B | RAG 知识管理 AI | A 的研究批次、C 的资料缺口、知识冲突 | 校验与更新请求、知识发布结果、需要继续研究的任务 |
| C | 游戏研发 AI | 开发待办、RAG 增量、上一轮结果、Skills 与工具 | 代码、配置、已导入素材、检查证据、实际应用记录和后续待办 |
| D | Skills / 工具 AI | C 提交的能力缺口、重复任务、工具失败案例 | 可复用 Skill、工具封装、参数说明和检查规则 |
| E | 独立审查 AI | 候选版本、实际运行截图、交互及检查证据 | 通过或不通过、具体问题、证据位置和剩余差距 |
| — | 调度器与任务队列 | 定时触发、批次完成、缺口提交、失败等事件 | 任务分派、去重、排队、重试及状态记录 |
| — | RAG 数据库与查询服务 | 已校验资料和查询请求 | 带来源、版本与适用范围的结构化资料及检索结果 |
| — | 校验、入库及发布执行程序 | AI 的受控请求、资料批次或冻结发布快照 | 真实执行结果、版本号、校验记录与交付文件 |
| — | 终端汇报程序 | 各阶段状态和事件记录 | 综合进度、关键结果和阻塞说明 |

B 是执行知识管理任务的 AI；RAG 是供 AI 查询的数据库与服务。调度、监控、入库和构建中的确定性步骤由程序执行。

<a id="timing"></a>

## 2. 当前实际配置与目标时间

以下“当前实际”依据 2026-10-26 对配置、调度代码和运行记录的检查。运行状态会变化，后续以实际配置和状态文件为准。

| 交互 | 当前实际 | 目标配置 |
| --- | --- | --- |
| 终端通知进展 | 每 10 分钟汇报研究、数据库更新及 RAG 状态；尚未统一覆盖研发与游戏发布 | 每 10 分钟综合汇报所有 AI；失败、知识发布、游戏发布完成时即时追加通知 |
| 游戏版本发布 | 开发完成后冻结快照并入队，独立发布任务每 5 分钟检查队列；尚无每 48 小时发布门槛 | 每 48 小时一个发布窗口；冻结快照后，发布与下一轮研发并行 |
| A 通知 B 更新知识 | 完整批次生成后进入数据库队列，可在同一次调度内继续处理；另有每 15 分钟扫描 | 批次完成立即通知，空闲即处理；每 5 分钟扫描补漏 |
| B 通知 A 学习什么 | 尚无独立的主动知识审计派单流程；目前依赖问题清单、既有未解决项及每日主题 | 每 30 分钟归并、审查知识缺口；研发阻塞或证据冲突立即入队 |
| C 重新读取 RAG 并优化 | 约每 1 小时启动一轮，轮次开始读取 RAG；新开发调度说明采用每 5 分钟检查到期时间 | 保留每 1 小时一轮，先读取完整可用的 RAG 增量，再继续开发 |
| C 反馈资料不足 | 每轮开发要求将缺口去重追加到 `rag/question.md` | 发现即提交结构化缺口，轮次结束补齐处理结果，由 B 统一归并 |
| A 持续研究 | 问题研究每小时最多启动一批；每天北京时间 09:00 排入主动探索任务 | 保留每小时研究及每天 09:00 主动探索，优先处理阻塞研发的问题 |
| C 与 D 交接能力需求 | 已有武将设计 Skill 和资源接入工具，尚未形成完整自动派单流程 | 按需触发；能力缺口提交后排队，验证通过后供 C 使用 |
| E 审查候选 | 已有自动验证后的独立只读视觉审查 | 保留审查门槛，审查证据绑定冻结发布快照 |

时间表示扫描、启动或通知频率，不代表任务必定在该时间内完成：

- “立即通知”表示立即记录事件并进入队列，处理时间仍受当前作业和资源占用影响。
- 同一职责的长任务不重叠启动；研发、研究可以使用各自队列推进，数据库写入保持串行。
- 到达发布窗口后，验收未通过或客户端正在使用时，保留现行可玩版本并记录延期原因。
- 当前本机任务依赖电脑开机、用户登录和相关运行环境可用，离线时不会执行。
- 当前研究调度配置的失败重试延迟为 15 / 60 / 240 分钟，最多尝试 3 次；具体执行受总尝试次数限制。无新证据的问题有至少 24 小时冷却，避免反复研究相同内容。

当前配置入口：

- RAG 研究调度配置（`rag/automation/config.json`）
- 游戏研发调度配置（`source/Automation/config.json`）
- 每 10 分钟汇报配置（`rag/status/progress-schedule.json`）

<a id="interaction-flow"></a>

## 3. 目标交互架构图

下图是补齐后的目标架构，包含尚待实施的时间规则和交接机制。

```text
                    调度器 / 任务队列 / 事件记录
                    负责启动、交接、重试和去重
                               |
        +----------------------+----------------------+
        |                      |                      |
        v                      v                      v
 A：持续学习 AI          B：RAG 知识管理 AI      C：游戏研发 AI
 每小时启动一批研究      每30分钟审查知识缺口    每小时启动一轮
 每天09:00主动探索       收到资料立即排队更新    每轮先读取RAG增量
        |                      |                      |
        |   学习任务           |   发现资料不足       |
        |<---------------------|<---------------------|
        |   问题、优先级       |   缺什么、用于哪里   |
        |   已查证据、完成标准 |   阻塞程度、已查内容 |
        |                      |                      |
        |   研究批次完成       |                      |
        |--------------------->|                      |
        |   文档、图片、来源   |                      |
        |   结论、未解决项     |                      |
        |                      v                      |
        |               校验 → 去重 → 关系更新       |
        |                      → 文本向量更新         |
        |                      |                      |
        |                      v                      |
        |                  RAG新版本                  |
        |                      |                      |
        |                      |--------------------->|
        |                      |   发布版本与变更摘要 |
        |                      |   C在下一轮按需检索  |
        |                      |                      v
        |                      |               实现代码 / 玩法 / UI
        |                      |                      |
        |                      |               缺少方法或工具？
        |                      |                      |
        |                      |                      v
        |                      |              D：Skills / 工具 AI
        |                      |              按能力缺口触发
        |                      |              完善步骤、工具和检查
        |                      |                      |
        |                      |               返回可用能力给 C
        |                      |                      |
        |                      |                      v
        |                      |               轮次自检与交接
        |                      |               已完成 / 剩余问题
        |                      |               已应用的RAG版本
        |                      |                      |
        |                      |                      v
        |                      |                 开发候选记录
        |                      |                      |
        |                      |        每48小时到达发布窗口
        |                      |                      |
        |                      |             冻结完整源码/资源
        |                      |                      |
        |                      |    +-----------------+----------------+
        |                      |    |                                  |
        |                      |    v                                  v
        |                      | 发布快照 S：保持不变           开发目录：继续变化
        |                      |    |                                  |
        |                      | 自动测试与运行取证             C继续下一轮优化
        |                      |    |                          每小时读取最新RAG
        |                      |    v
        |                      | E：独立审查 AI
        |                      | 检查实际截图、交互与证据
        |                      |    |
        |                      |    +-- 不通过 → 问题返回 C，保留现行版
        |                      |    |
        |                      |    +-- 通过 → 发布执行器
        |                      |                Windows / Android
        |                      |
        +----------------------+---- 各阶段持续写入事件记录
                                             |
                                             v
                                     终端综合汇报程序
                                     每10分钟汇总所有AI
                                     关键结果即时追加
```

## 4. 五条关键交接链路

### 4.1 A → B：研究成果通知入库

1. A 完成一个有界研究批次，保存文档、图片、来源、事实和未解决项。
2. 文件齐备后原子发布 `batch.json`，未完成的批次不进入更新队列。
3. 调度器记录批次完成事件，按批次 ID 和内容指纹去重，通知 B。
4. B 审核资料并请求固定的校验、更新动作，由宿主执行程序完成实际更新。
5. 收到包含对应批次 ID 的成功发布结果后，才记录知识发布完成。
6. 发布结果供 C 读取，同时进入终端汇报。

目标为完成即通知、每 5 分钟扫描补漏。现有系统已实现批次交接、去重和受控更新，补漏扫描仍为 15 分钟。

### 4.2 B → A：知识缺口反向派单

B 定期结合以下输入生成具体学习任务：

- C 提交的资料缺口与研发阻塞。
- 已发布资料中的未知字段、冲突证据、适用版本缺失。
- A 上一批的未解决项与下一批研究建议。
- 每日主动探索主题中尚未覆盖的子题。

目标是每 30 分钟归并并审查一次；研发阻塞和新发现的证据冲突即时入队。任务应保留已有证据和完成标准，避免重复研究。

这一主动审计派单流程尚待完善。当前调度主要使用 `question.md`、历史研究结果及每日主题。

学习任务示例：

```text
任务ID：KNOW-0042
提出方：游戏研发 AI
接收方：持续学习 AI
问题：主城设施升级能否取消，取消后如何返还资源？
用途：实现取消建设功能
已有资料：两个历史公告，当前S1适用性未确认
需要补充：适用版本、操作入口、返还规则、原始来源
优先级：阻塞研发
完成结果：确认结论 / 冲突证据 / 仍未知
```

### 4.3 RAG / B → C：知识应用到游戏

C 每轮开始读取 RAG 活动版本、更新状态和相关主题，按需查看原文、原图及增量：

```text
知识已发布
    → C已读取
    → 判断来源、版本和适用性
    → 实际修改代码、配置或资源
    → 验证
    → 登记已应用及未应用项
```

“已读取”与“已应用”分别记录。RAG 没有新版本时，C 继续处理已有未完成任务。RAG 不可用或扫描不完整时记录降级情况，并保留有效观察游标。

发现资料不足时，C 立即提交缺口；问题交给研究队列后，C 继续推进不依赖该资料的工作。

### 4.4 C ↔ D：Skills 与工具能力补齐

```text
C发现重复任务或能力不足
    → 提交输入、期望输出、当前失败案例
    → D完善Skill或工具
    → 用代表性任务验证
    → 登记能力版本与使用限制
    → C调用新能力完成任务
    → 将实际问题反馈给D
```

现有武将卡设计能力位于 design_character_skills（`skills/design_character_skills/SKILL.md`）。该 Skill 定义方法和检查要求，实际图片由图像工具生成；游戏资源接入由专用导入工具执行。

D 的自动派单、能力版本交接及统一验证尚待补齐。Skill、工具更新记录与游戏事实资料分开管理。

### 4.5 C → E → 发布程序：验收与交付

C 提交代码、素材和自检证据后，由执行程序生成运行取证，再交给 E 独立审查：

- 检查逻辑、资源、存档兼容及实际交互。
- 查看本轮真实截图，包括桌面和手机布局预览。
- 不通过时返回可定位的问题与证据。
- 通过后，由发布执行程序生成并交付 Windows 和 Android 产物。
- APK 构建与包体验证、手机布局预览、Android 真机验证分别记录。

<a id="publication"></a>

## 5. 每两天发布与并行研发

### 5.1 发布周期

目标为 **每 48 小时一个发布窗口**，首次窗口和时间基准在实施时配置。

窗口到达时，从最近完整完成的开发轮次创建不可变候选，包含：

- 完整源码、配置、资源及其文件哈希。
- 已应用的 RAG 版本和素材身份。
- Skill / 工具版本及相关生成、导入记录。
- 轮次改动、已知问题和验证要求。

新发布候选不得截取正在写入中的开发目录。先完成交接和一致性校验，再将快照交给发布流程。

### 5.2 并行方式

```text
第48小时：
  完整开发轮次
       |
       +-- 冻结快照 S1 → 自动测试 → E审查 → 构建 → 发布 S1
       |
       +-- 开发目录继续下一轮优化，准备 S2

S1验收或发布失败：
  保留现行可玩版本
  返回失败问题给 C
  修复形成新候选后重新验收
```

发布过程读取独立冻结快照，C 在开发目录继续修改。验收结果、构建产物和最终交付都绑定同一快照，不能借用旧证据发布后来的改动。

本次目录复核发现并行发布实现已更新，早期“研发与发布共用执行锁”的描述已过时：

1. `source/Automation/publication-queue.mjs` 负责冻结输入和发布队列。
2. `source/Automation/publisher.mjs` 由独立的 `EasternDynasty-S1-Publication` 任务调用；已核实任务注册，巡检间隔为 5 分钟。
3. 开发状态位于 `source/.local/continuous-development/`，发布状态和队列位于 `source/.local/publication/`。
4. 快照基目录由 `source/Automation/config.json` 的 `snapshotBase` 指定，当前为 `F:\EasternDynastyBuild\Releases`。
5. 每个版本的 `Source/` 保留冻结输入，`Project/` 用作可产生 Unity 导入文件的独立工作副本。

已看到快照目录和发布队列实现，但本次文档更新没有执行构建或完成一次端到端并行发布验收。候选的实际成功状态必须读取发布记录。**每 48 小时发布窗口仍是目标配置**，不能把当前 5 分钟队列巡检当成两天发布周期。

源码研发可与独立快照的发布并行；同一 Unity 工程的导入、Editor 验证和构建仍串行执行。桌面自动交互也需要独占条件，不能影响正在运行的用户游戏。

## 6. 统一终端通知

目标汇报分为两类：

| 类型 | 时间 | 内容 |
| --- | --- | --- |
| 综合汇报 | 每 10 分钟 | 所有 AI 当前任务、本期实际完成内容、产物、阻塞、下一步及下次发布窗口 |
| 关键事件 | 事件发生后立即追加 | 任务失败、知识发布完成、验收不通过、游戏发布完成、需要外部处理的阻塞 |

汇报由程序读取现有状态和事件生成，不要求每 10 分钟启动一次 AI。

```text
[15:00 综合进展]
学习 AI：正在核实什么；本期新增了哪些资料。
RAG AI：发布了什么版本；还有哪些缺口等待研究。
研发 AI：完成了什么；实际应用了哪些知识与素材。
Skills AI：新增或修复了什么能力。
审查/发布：候选版本、验收结果、下次发布窗口。
阻塞项：原因、负责处理的AI、下一次处理时间。
```

“无新进展”“未发现新证据”“正在等待桌面”“尚未发布”均如实显示。文件存在、任务启动或 AI 自述完成，不能单独作为成功依据。

当前每 10 分钟汇报已经运行，主要覆盖研究与 RAG。目标是增加 C、D、E 和版本发布信息，并补齐关键事件即时通知。

## 7. 统一交接记录与状态

建议所有任务至少携带以下信息：

```text
任务：
  task_id
  提出方 / 接收方
  目标与验收条件
  优先级、阻塞对象
  输入文件与资料版本
  已查证据、缺失内容
  创建时间、关联任务

结果：
  task_id
  状态与实际完成时间
  产物位置、版本、文件哈希
  验证证据
  未解决项及原因
  下一步与接收方
```

任务处理状态与成果发布阶段分别记录：

```text
任务状态：
待处理 → 已排队 → 运行中 → 成功 / 无新增 / 失败 / 阻塞

成果阶段：
研究完成 → 入库完成 → 开发已应用 → 验收通过 → 已交付
```

同一资料不一定要应用到游戏；被判断为历史参考、当前适用性未知或不属于本轮范围时，记录未应用原因。原创玩法、原创技能和项目美术保留项目创作标识与生成记录。

## 8. 待改造清单

| 项目 | 当前基础 | 需要补齐 |
| --- | --- | --- |
| 全流程终端汇报 | 已有每 10 分钟 RAG 汇报 | 纳入研发、Skills、审查与游戏发布；关键事件即时通知 |
| 两天发布 | 已有独立发布队列和候选管理 | 增加 48 小时发布窗口与到期选择规则 |
| 发布与研发并行 | 已新增独立发布任务、队列、冻结输入和工作副本 | 依据运行收据继续验证并行、延期重试和完整交付；本次文档更新不代替运行验收 |
| A → B 资料通知 | 已有批次交接、去重、校验与更新 | 保留完成即交接，将扫描补漏改为 5 分钟 |
| B → A 主动学习任务 | 已有问题队列、历史研究和未解决项 | 每 30 分钟知识审计、缺口归并和反向派单 |
| C → B 缺口反馈 | 开发提示要求追加问题清单 | 结构化缺口、即时提交、接收确认与处理结果回传 |
| C ↔ D 能力交接 | 已有设计 Skill 与资源工具 | 按需派单、代表性验证、能力版本及回传机制 |
| 统一任务记录 | 已有各子系统状态、日志和批次 ID | 串联研究、知识、开发、审查及发布的关联 ID |

<a id="directory-layout"></a>

## 9. 现有目录与路径基准

### 9.1 仓库位置与运行位置

README 所在位置与实际运行位置分别记录。以下变量是文档约定，**不代表现有脚本自动支持同名环境变量**；迁移时必须修改实际配置或使用脚本已有参数。

| 文档变量 | 当前绝对路径 | 作用 |
| --- | --- | --- |
| `REPO_ROOT` | `X:\program\github\EasternDynasty` | Git 仓库与本 README 的交付位置，目前只有部分项目内容 |
| `PROJECT_ROOT` | `F:\工作项目\率土之滨` | 完整部署根目录，游戏、RAG、设计输出和 Skills 位于这里 |
| `GAME_ROOT` | `F:\工作项目\率土之滨\source` | 唯一活动 Unity 开发工程，等于 `PROJECT_ROOT/source` |
| `RAG_ROOT` | `F:\工作项目\率土之滨\rag` | 数据库、知识更新、检索 API 与研究调度 |
| `RESEARCH_ROOT` | `C:\Users\fenglei\Downloads\率土之滨_素材集_2026-10-25` | 原始资料、研究批次与分类资源库 |
| `SNAPSHOT_ROOT` | `F:\EasternDynastyBuild\Releases` | 独立发布快照，由开发配置的 `snapshotBase` 指定 |

例如本文提到 `rag/automation/config.json`，当前真实位置是：

```text
PROJECT_ROOT + rag/automation/config.json
= F:\工作项目\率土之滨\rag\automation\config.json
```

它目前不在 `X:\program\github\EasternDynasty\rag\automation\config.json`。仅提交 README 不会把运行文件一同提交。

X 盘仓库本次核对到的内容：

```text
X:\program\github\EasternDynasty\
├─ README.md                       本文，可独立阅读
├─ LICENSE                         仓库现有许可证文件
└─ skills\
   └─ design_character_skills\      已有武将卡片设计 Skill

尚未同步到此仓库：
  source/、rag/、output/、games/、apk/
```

`.git/` 为 Git 内部目录，以上省略。完整代码同步后，应更新“尚未同步”列表。

### 9.2 当前完整部署目录

以下省略缓存、历史日志、旧版本备份及部分业务文件。“运行时生成”表示该文件可能在首次执行或对应事件发生后才出现。

```text
PROJECT_ROOT/
├─ README.md                              架构文档的部署副本
│
├─ rag/                                   知识与研究子系统
│  ├─ question.md                         待研究问题，研发可追加去重缺口
│  ├─ index.html                          本机操作说明入口
│  ├─ app/                                查询、导入、关系和向量处理代码
│  │  ├─ server.mjs                       只读查询 HTTP 服务
│  │  ├─ client.mjs                       本机自然语言查询客户端
│  │  ├─ openapi.json                     查询接口契约
│  │  ├─ update-rag.mjs                   统一知识更新入口
│  │  ├─ batch-ingest.mjs                 研究批次导入
│  │  ├─ relations.mjs                    规范关系刷新
│  │  ├─ embedding.mjs                    向量相关处理
│  │  ├─ library-sync.mjs                 分类资源库归档
│  │  └─ watch.mjs                        常驻资料变化检查
│  ├─ automation/                         A、B 的任务调度与交接
│  │  ├─ config.json                      时间、资料根目录、批次范围配置
│  │  ├─ scheduler.mjs                    扫描、排队、去重、优先级与重试
│  │  ├─ worker.mjs                       启动研究或数据库 AI 作业
│  │  ├─ database-broker.mjs               校验/更新固定动作的宿主入口
│  │  ├─ progress-report.mjs              定时终端汇报
│  │  ├─ prompts/
│  │  │  ├─ research.txt                  A 的任务与结果约束
│  │  │  └─ database.txt                  B 的任务与受控操作约束
│  │  ├─ state.json                       队列、历史和研究进度
│  │  ├─ runs/<job_id>/                   每次任务上下文、结果和交接记录
│  │  └─ logs/                            调度、执行与汇报事件
│  ├─ config/                             服务配置与本机私有配置
│  │  ├─ service.json                     服务、资料来源等实际配置
│  │  ├─ connection.json                  数据库连接位置
│  │  ├─ credentials.json                 数据库角色凭据，仅本机保管
│  │  └─ service-auth.json                查询接口令牌，仅本机保管
│  ├─ sql/                                数据库结构与迁移
│  ├─ scripts/                            启停、状态、备份、恢复与任务安装
│  ├─ docs/                               接口、自动学习、迁移及批次说明
│  ├─ deploy/                             其他部署方式的配置
│  ├─ imports/                            已导入专题、配置与快照资料
│  ├─ assets/                             已发布图片原始字节
│  ├─ documents/                          已发布文档和附件
│  ├─ data/                               PostgreSQL 本机数据目录
│  ├─ models/                             本地向量模型与权重
│  ├─ runtime/                            运行依赖、安装及缓存
│  ├─ backups/                            数据库备份
│  ├─ exports/                            带资源的迁移导出包
│  ├─ logs/                               服务与更新执行日志
│  └─ status/                             对外查看的状态和验收记录
│     ├─ automation.json                  A/B 调度状态
│     ├─ knowledge-update.json            最近一次知识更新结果
│     ├─ library-sync.json                分类资源库同步状态
│     └─ progress-latest.txt              最近一次终端汇报
│
├─ source/                                活动 Unity 工程
│  ├─ README.md                           游戏运行与开发说明
│  ├─ AGENTS.md / agent.md                AI 开发边界与任务导航
│  ├─ Assets/EasternDynasty/
│  │  ├─ Scripts/
│  │  │  ├─ Core/                         纯 C# 规则与状态
│  │  │  ├─ Application/                  会话、用例与只读展示投影
│  │  │  ├─ Infrastructure/               持久化与资源访问适配
│  │  │  └─ Runtime/                      Unity 展示、输入和诊断
│  │  ├─ Editor/                          编辑器导入、检查和构建
│  │  └─ Resources/
│  │     ├─ asset-manifest.json            稳定资源 ID、来源与哈希清单
│  │     └─ ...                           已接入游戏的正式美术资源
│  ├─ Packages/                           Unity 包依赖
│  ├─ ProjectSettings/                    Unity 工程设置
│  ├─ Automation/
│  │  ├─ config.json                      小时开发及独立发布配置
│  │  ├─ iteration-prompt.txt             C 的任务提示与交接要求
│  │  ├─ runner.mjs                       开发轮次入口
│  │  ├─ rag-sync.mjs                     RAG 版本与资料增量观察
│  │  ├─ publication-queue.mjs            发布候选及冻结快照
│  │  ├─ publisher.mjs                    独立发布执行入口
│  │  └─ release-evidence.mjs             发布证据关联与检查
│  ├─ Tools/                              PowerShell 操作与验证入口
│  │  ├─ Import-Asset.ps1                 单项正式资源受控导入
│  │  ├─ Validate.ps1                     规则和工程检查
│  │  ├─ Publish-Iteration.ps1            候选验收流程
│  │  ├─ Build-Windows.ps1                Windows 构建
│  │  └─ Build-Android.ps1                Android 构建与包体检查
│  ├─ Docs/                               规则、架构、待办和交接文档
│  ├─ AssetInbox/                         待接入资源与导入事务区域
│  ├─ References/ / ResearchLibrary/      本地参考资料
│  ├─ Validation/                         检查代码与验证证据
│  │  └─ Releases/<version>/              按版本归档的发布证据
│  ├─ Builds/                             本地构建输出
│  └─ .local/                             本机状态，不是可共享代码
│     ├─ continuous-development/
│     │  ├─ status.json / state.json      当前开发状态与下一轮时间
│     │  ├─ runs/<run_id>/rag/            brief、delta、snapshot读取记录
│     │  └─ manual-hold.json              人工暂停后续开发（按需创建）
│     └─ publication/
│        ├─ queue/                        发布候选记录
│        ├─ job-status/                   各候选进度
│        ├─ status.json                   发布者状态（运行时生成）
│        ├─ last-failure.json             发布失败反馈（按需生成）
│        └─ manual-hold.json              人工暂停后续发布（按需创建）
│
├─ skills/
│  └─ design_character_skills/
│     ├─ SKILL.md                         设计流程与检查约束
│     ├─ 使用说明.md                      调用示例
│     ├─ agents/                          AI 宿主入口描述
│     ├─ references/                      参数契约、风格与提示词模板
│     └─ examples/                        示例请求、参考图和结果
│
├─ output/                                原创设计交付，尚非 Unity 正式资源
│  ├─ index.json                          成品索引
│  ├─ _batches/                           批次记录
│  └─ <阵营>/                             魏国、汉、群、燕及其他阵营
│     ├─ <角色>-v1.png                    完整成图
│     ├─ <角色>-v1.prompt.txt             生成提示词
│     ├─ <角色>-v1.request.json           输入参数
│     ├─ <角色>-v1.result.json            成图检查结果
│     └─ <角色>-v1.design.json            设计依据（适用时存在）
│
├─ games/                                 已安装的 Windows 客户端
├─ apk/                                   Android APK 与包体交付收据
└─ mcp/                                   预留接入目录，本次核对为空
```

`output/` 中的完整卡片是设计产物。进入游戏前，需要按用途准备原画、图标或 UI 资源，再通过资源清单与导入工具接入；动态姓名、星级和数值等显示由游戏 UI 管理。

### 9.3 外置研究资料与发布快照

```text
RESEARCH_ROOT/
├─ <已有专题资料>/                         武将、玩法、地图等既有资料
├─ S1_持续学习/
│  └─ 批次/<batch_id>/
│     ├─ batch.json                        最后发布的批次清单
│     ├─ findings.md                       新结论、变化及下一批建议
│     ├─ facts.jsonl                       带证据的结构化事实
│     ├─ unresolved.json                   未解决项及所需证据
│     └─ <本批引用文件>/                    文档、来源快照和真实图片
└─ S1_分类资源库/
   ├─ 目录索引.json                        分类入口
   ├─ 01_城建/<建筑名>/                    图片、资料、档案和待补充项
   └─ <其他类别>/                         武将、战法、玩法、地图及UI

SNAPSHOT_ROOT/
└─ <version>/
   ├─ Source/                             冻结输入与校验基线
   ├─ Project/                            发布者独占的 Unity 工作副本
   └─ <快照清单及运行证据>/                 文件名以执行程序生成结果为准
```

完成的研究批次保持不可变，后续补证创建新批次。发布快照不接受研发任务继续写入；Unity 工作文件写入版本专属 `Project/`。批次与快照都需要关联到对应任务和版本。

<a id="repository-design"></a>

## 10. 目标仓库目录设计

### 10.1 仓库保存可协作的实现

下面是补齐 X 盘仓库的设计，不表示所有目录已经提交或创建。本次只更新 README。

```text
REPO_ROOT/
├─ README.md                              架构、调度、目录和迁移入口
├─ LICENSE                                仓库现有许可证
├─ .gitignore                             [规划] 排除本机状态、密钥与产物
├─ docs/                                  [规划] 通用说明与交接契约
│  ├─ architecture/
│  ├─ contracts/                          任务、结果、事件与产物字段
│  └─ migration/
├─ orchestration/                         [规划] 跨 A/B/C/D/E 的协调层
│  ├─ config/                             调度策略与本机配置样例
│  ├─ contracts/                          消息和状态 schema
│  ├─ adapters/                           对接 RAG 与研发/发布入口
│  └─ reporting/                          全流程汇报与关键事件通知
├─ rag/                                   [待同步现有实现]
│  ├─ app/ automation/ sql/ scripts/
│  ├─ docs/ deploy/
│  └─ config/*.example.json               不含凭据的配置样例
├─ source/                                [待同步现有实现]
│  ├─ Assets/ Packages/ ProjectSettings/
│  ├─ Automation/ Tools/ Docs/
│  ├─ Validation/                         检查代码与必要的小型基准
│  └─ AGENTS.md / agent.md / README.md
└─ skills/                                [已有部分内容]
   └─ design_character_skills/
```

统一协调层连接现有入口。实施时需要明确队列归属并逐步切换旧触发方式，避免重复启动同类写入任务。

`source/Assets` 下的正式资源与 `.meta` 是游戏工程的一部分，不能随缓存和构建输出一并忽略；大型资产采用何种版本存储方式应另行确定。

### 10.2 运行数据与仓库代码分开管理

目前实际运行目录仍按第 9 节分布。以下是未来统一工作区的可选设计，需程序和配置支持后才能迁移：

```text
WORKSPACE_ROOT/                           [规划，本机或专用数据盘]
├─ state/                                游标、重试和运行状态
├─ queues/
│  ├─ research/                          研究任务
│  ├─ knowledge/                         入库及缺口审计任务
│  ├─ development/                       研发任务
│  ├─ capabilities/                      Skills/工具任务
│  └─ publication/                       发布候选任务
├─ runs/<task_id>/<attempt>/             上下文、结果和执行记录
├─ events/                               事件日志与通知游标
├─ research/batches/                     完整研究批次
├─ knowledge/
│  ├─ assets/ documents/                 已发布资料
│  └─ exports/ backups/                  可恢复的导出及备份
├─ art-output/<阵营>/                    美术设计交付
├─ snapshots/<version>/                  冻结输入及发布工作副本
├─ deliveries/
│  ├─ windows/                           已验证的 Windows 交付
│  └─ android/                           已验证的 APK 交付
└─ private/                              本机凭据与部署配置
```

该设计不改变当前路径。现有程序仍使用 `rag/config`、`rag/automation/config.json`、`source/Automation/config.json` 等实际配置。

### 10.3 哪些内容进入 Git

| 内容 | 建议管理方式 |
| --- | --- |
| 源码、脚本、schema、提示词、说明文档 | 进入仓库，记录可复现版本 |
| 不含秘密的默认配置和样例 | 进入仓库，区分默认值与本机覆盖值 |
| Unity 正式资源与 `.meta` | 保留版本关联、来源和稳定资源 ID |
| 数据库密码、接口令牌、本机认证 | 本机私有保管，不写入 README 或公共仓库 |
| `rag/data`、状态、PID、锁、机器专属路径 | 留在部署环境，不作为通用配置 |
| `node_modules`、Unity `Library/Temp`、安装缓存 | 由环境准备或依赖恢复生成 |
| APK、客户端、快照、大量截图和原始资料 | 作为交付或数据备份，通过清单和哈希关联源码 |
| `Validation` 下检查代码 | 进入仓库，与运行生成的截图、日志区分 |
| 玩家存档与用户数据 | 留在运行环境，检查使用独立数据 |

以上是目录边界设计。本次未创建 `.gitignore`，也没有移动、删除或提交其他目录中的内容。

<a id="directory-ownership"></a>

## 11. AI 读写范围与交接位置

| 组件 | 主要读取 | 当前写入或交接位置 | 完成条件 |
| --- | --- | --- | --- |
| A 研究 AI | `RAG_ROOT/question.md`、相关专题、历史研究和来源 | `RESEARCH_ROOT/S1_持续学习/批次/<batch_id>/` | 文件齐备，最后原子发布 `batch.json` |
| B 知识更新 AI | 指定批次、问题和数据库说明 | 本次 `RAG_ROOT/automation/runs/.../`，通过 `broker/requests` 发固定动作请求 | 宿主校验后更新，核对对应批次发布结果 |
| 宿主知识更新程序 | 完整批次与私有部署配置 | 数据库、`assets/`、`documents/`、`status/`、分类库 | 更新及必要向量处理完成，返回版本与证据 |
| C 研发 AI | RAG、`GAME_ROOT/Docs`、上一轮结果及发布失败反馈 | 活动工程、轮次记录、去重后的 `RAG_ROOT/question.md` | 自检和交接完成，由宿主冻结候选 |
| D Skills/工具 AI | 能力缺口、失败案例、已有 Skill 和契约 | 受分派的 `skills/<skill_name>/` 或工具文件；成图进入 `output/` | 代表性验证及版本记录；自动交接仍待完善 |
| E 审查 AI | 指定候选的截图、交互和检查证据 | 本轮审查结果文件 | 证据属于同一候选，给出通过或具体问题 |
| 发布执行程序 | 固定队列项与冻结输入 | `SNAPSHOT_ROOT/<version>/Project/`、状态、`games/`、`apk/` | 验收、源码身份和交付校验通过 |
| 终端汇报程序 | 状态与事件 | `rag/automation/logs/progress-reports.log`、`rag/status/progress-latest.txt` | 汇总真实结果，不改变研发或知识状态 |

B 的数据库更新通过宿主入口执行。AI 工作目录中的请求文件与真正的发布结果分别记录。

### 11.1 文件交接关系

```text
A 的研究批次
  RESEARCH_ROOT/S1_持续学习/批次/<batch_id>/batch.json
       |
       v
B 的受控请求
  RAG_ROOT/automation/runs/<job_id>/.../broker/requests/
       |
       v
宿主更新结果
  RAG_ROOT/status/knowledge-update.json + RAG知识版本
       |
       v
C 的读取记录
  GAME_ROOT/.local/continuous-development/runs/<run_id>/rag/
       |
       v
C 的实际应用与候选
  GAME_ROOT/Docs/交接记录 + .local/publication/queue/<version>.json
       |
       v
发布快照
  SNAPSHOT_ROOT/<version>/Source/ + Project/
       |
       v
E 的验收与最终交付
  验收收据 + GAME_ROOT/Validation/Releases/<version>/
  PROJECT_ROOT/games/ 或 PROJECT_ROOT/apk/
```

“交接记录”“验收收据”是类别名称，实际文件名以程序和任务结果为准。`...` 表示作业尝试目录等中间层，不能原样当成路径。

### 11.2 可迁移的产物定位

新增统一交接格式时，建议保存“逻辑根目录 + 相对路径”，由部署配置解析真实位置：

```json
{
  "task_id": "KNOW-0042",
  "artifact": {
    "root_id": "research",
    "relative_path": "S1_持续学习/批次/example-batch/findings.md"
  },
  "status": "research_completed"
}
```

这是待统一的契约示例，不表示现有接口都已支持。现有批次继续遵循当前 schema 和相对批次根目录的路径约定。可迁移的路径不能代替来源、版本、适用范围和文件哈希。

<a id="portability"></a>

## 12. README 可移植与完整项目迁移

### 12.1 只复制 README

- 文内导航使用固定锚点，不依赖相邻文件。
- 实现路径以代码文本说明所属根目录，不再生成悬空的相对文件链接。
- 状态、目录职责、交接流程和目标设计都在本文说明。
- 在 GitHub 或其他电脑阅读时，`F:`、`X:`、`C:` 是当前部署示例，不是远程访问入口。
- 换目录后，先查看第 9 节的根目录映射，再定位实现文件。

### 12.2 把完整实现同步到 X 盘仓库

代码同步是单独工作，本次仅同步 README。后续按第 10 节选择源码、脚本、契约、文档和正式资源，逐项区分本机配置、凭据与运行产物。

仓库具备完整实现后，可以将其作为开发来源、把运行数据放在独立部署目录；也可以在其他机器从仓库建立新部署，再配置资料、快照、依赖和任务。

F 盘现有自动开发仍应使用原活动工程，不能因为 README 在 X 盘，就把 `GAME_ROOT` 改成不完整的仓库副本。

### 12.3 换机器部署

1. 取得完整代码、正式资源及对应版本。
2. 明确 `PROJECT_ROOT`、`RESEARCH_ROOT` 和 `SNAPSHOT_ROOT`。
3. 安装或恢复依赖，配置本机身份与凭据。
4. 用数据库逻辑备份恢复 RAG，核对图片、文档和模型；运行中的数据库数据目录不作为跨机迁移包。
5. 修改实际配置中的路径和地址；`127.0.0.1` 表示调用者本机。
6. 检查 RAG 查询、批次读取、资源清单和构建输入。
7. 在目标机器重新注册所需任务，确认状态后启用自动化。
8. 不沿用旧机器的 PID、锁、任务运行状态或过时绝对路径。

复制文档不自动安装依赖、恢复数据库、注册任务或改变当前环境。

<a id="file-entrypoints"></a>

## 13. 文件定位与只读查看

下表路径相对于 `PROJECT_ROOT`，当前在 F 盘完整部署中查找。文件不在 README 旁边时，也可以按用途和路径定位。

| 用途 | 相对 `PROJECT_ROOT` 的位置 | 内容 |
| --- | --- | --- |
| 游戏入口 | `source/README.md` | 运行、构建和当前玩法范围 |
| 开发边界 | `source/AGENTS.md`、`source/agent.md` | 唯一工程、导航、读写与验证要求 |
| 研究问题 | `rag/question.md` | 资料缺口和研究方向 |
| A/B 调度 | `rag/automation/scheduler.mjs`、`rag/automation/worker.mjs` | 作业发现、优先级、交接和重试 |
| A/B 提示词 | `rag/automation/prompts/research.txt`、`rag/automation/prompts/database.txt` | 研究与入库约束 |
| 研究时间 | `rag/automation/config.json` | 扫描、研究频率、资料根目录及批次位置 |
| 学习说明 | `rag/docs/自动化持续学习说明.txt` | 调度、事件和恢复 |
| 批次契约 | `rag/docs/持续学习批次与更新接口.txt` | 字段、证据、路径与发布条件 |
| 查询接口 | `rag/docs/其他AI如何访问.txt`、`rag/app/openapi.json` | 知识、卡片、素材、来源及版本查询 |
| C 的任务提示 | `source/Automation/iteration-prompt.txt` | 研发步骤、证据及缺口交接 |
| 研发入口 | `source/Automation/runner.mjs` | 轮次执行、RAG 读取及候选交接 |
| 发布入口 | `source/Automation/publication-queue.mjs`、`source/Automation/publisher.mjs` | 冻结、队列与发布 |
| 研发/发布配置 | `source/Automation/config.json` | 小时开发、发布开关和快照目录 |
| 并行发布说明 | `source/Docs/Continuous-Development.md` | 两条任务、并行边界与失败反馈 |
| RAG 增量读取 | `source/Docs/Rag-Sync.md` | brief/delta/snapshot 和观察状态 |
| 研发待办 | `source/Docs/Hourly-UI-Gameplay-Backlog.md` | 完成、未完成与下一轮承接 |
| 资源导入 | `source/Docs/Asset-Pipeline.md` | 稳定 ID、来源、哈希和导入检查 |
| 武将设计能力 | `skills/design_character_skills/SKILL.md` | 流程、参数、参考图及成图检查 |
| 终端汇报配置 | `rag/status/progress-schedule.json` | 已注册的间隔和日志位置 |

只读查看部署状态，换机器时修改 `$projectRoot`：

```powershell
# 填写实际部署目录；仅含README的仓库没有这些运行状态。
$projectRoot = 'F:\工作项目\率土之滨'

$stateFiles = @(
    'rag\status\progress-latest.txt',
    'rag\status\automation.json',
    'rag\status\knowledge-update.json',
    'source\.local\continuous-development\status.json',
    'source\.local\publication\status.json'
)

foreach ($relativePath in $stateFiles) {
    $statePath = Join-Path $projectRoot $relativePath
    Write-Output ("状态文件：" + $statePath)
    if (Test-Path -LiteralPath $statePath -PathType Leaf) {
        Get-Content -LiteralPath $statePath -Encoding UTF8
    } else {
        Write-Output '尚未生成，或部署位置不同；请按目录映射核对。'
    }
}
```

配置描述计划如何运行，状态和交付收据说明实际已经完成什么。实现或同步目录后，应同时更新第 2 节“当前实际”、第 8 节待改造项和第 9 节目录映射。

