# 架构优化工作记录

本轮目标：在 F:\工作项目\率土之滨\source 内明确源码职责，为后续AI提供稳定的本地资源接入流程、接手文档和可执行构建运行命令。记录日期：2026-10-25。

## 实现范围

| 部分 | 内容 |
| --- | --- |
| Core | 按模型、目录、创建、校验、经济、武将、战斗拆分文件，保留World类型和序列化字段 |
| Application | CampaignSession与ICampaignRepository，注入时钟，离线结算与只读保存策略 |
| Infrastructure | 双槽JSON仓库；资源清单、稳定ID、提供器及缓存 |
| Presentation | 组装应用服务，使用统一资源入口，拆分开发诊断 |
| Editor与Tools | 程序集边界、资源校验、导入、环境准备、构建、运行和玩家包安装 |
| 文档 | README、AGENTS.md、agent.md、分层图、内部组件及五组时序图 |

## 最终验证结果

本轮三个后台代理的实现与文档已完成，由主任务整合并串行执行实际Unity检查。下列结果来自本轮代码，不复用上一轮画面验证作为通过证明。

| 检查 | 本轮结果 | 证据或执行入口 |
| --- | --- | --- |
| Core与Application | 353项通过：旧版146、幽州140、应用架构67；运行时与Editor静态编译通过 | Tools/Validate.ps1 |
| 资源导入 | 63项独立沙箱断言通过；幂等、显式替换、GUID保持、非法输入、文件锁回滚、中断恢复 | Validation/AssetPipelineChecks.ps1；[资源流程](Asset-Pipeline.md) |
| 正式资源 | 10张ready、16张missing肖像；构建前清单、文件与导入设置验证通过 | architecture-unity.log中的ASSET_CATALOG_VALID |
| 环境准备 | Setup实际定位目标Editor，导入工程并准备场景成功 | setup.log |
| Unity实盘仓库 | 39项通过；双槽、备份、非法候选、只读、异常保护，以及两地图各4状态往返 | architecture-unity.log；[隔离结果目录](../Validation/RepositoryChecks/217c34e9b3af4501adfb2e4884a5c37a/) |
| 程序集与构建 | 五个asmdef实际编译通过；Windows客户端构建成功 | build.log |
| 幽州Player | 1440×900，15个状态通过，日志无Error或Exception | [本轮幽州截图](../Validation/Visual/architecture-1440x900/) |
| 旧版Player | 1440×900，12个状态通过，日志无Error或Exception | [本轮旧版截图](../Validation/Visual/architecture-legacy-1440x900/) |
| 实际鼠标 | 10步交互截图与状态JSON断言通过，关键截图复核；覆盖弹窗隔离、城名、小地图、缩放和拖动 | [交互记录](../Validation/Visual/youzhou-interactive/) |
| 运行入口与存档保护 | Tools/Run.ps1 -Review成功加载7类地图资源，正常退出；玩家存档SHA保持不变 | [运行记录](../Validation/architecture-run-check.txt)；[存档原始哈希](../Validation/architecture-saves-before.json) |
| 安装与工具 | 167项通过，包含145个安装文件SHA、中文带空格目标、拒绝默认覆盖、标记限定替换与previous备份 | [工具检查结果](../Validation/ToolingChecks/1b7dc1d290634257a0d11146a36d26c5/result.txt) |

资源导入和仓库异常检查使用独立沙箱，没有把玩家真实存档或正式PNG当作破坏性测试目标。缺失肖像仍明确使用UI占位，不能将清单可通过校验解释为全部美术完成。

## 检查中修正的问题

- Unity JsonUtility会将浮点值序列化到相邻的最后一位，简单比较JSON全文会误报。实盘往返改为递归比较全部公开状态字段，浮点使用1e-12相对/绝对容差，仍检查模型、集合内容、行军状态和资源语义。
- Unity版本校验统一到Resolve-EdUnityEditor，显式参数、环境变量与本机配置均检查目标版本，并接受实际ProductVersion中的构建哈希后缀，避免绕过Setup后使用错误Editor。
- Editor占用检查改为解析完整-projectPath参数，避免把source-copy等相近目录误认为当前工程。相关边界已加入工具断言。

## 仍保留的边界

本轮未修改美术目标，也未新增多人、商贸或完整卡池。IMGUI、可变World聚合、代码内Catalog、PNG专用资源流程和单进程存档仍是当前实现边界。测试通过不代表商业美术质量达标。

“后台任务”是本轮并行代理工作，不是永久运行的爬虫或定时服务。后续工作可从AGENTS.md、agent.md和本轮验证入口继续，不需要依赖某个仍存活的代理进程。
