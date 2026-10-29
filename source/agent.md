# Eastern Dynasty AI 接手指南

## 当前状态：停止

2026-10-29 用户要求“停止 agent，整理代码，然后停止，把文档写好”。本次整理完成后不得自行恢复小时开发、发布、RAG 学习或派生子 agent。历史持续优化授权已由这条指令覆盖；只有用户新的明确要求才恢复。

唯一源码目录是 F:\工作项目\率土之滨\source。先看 [当前交接](Docs/Project-Status.md)，再看 [源码导航](Docs/Code-Map.md)。不要把 F:\EasternDynastyBuild 下的冻结快照或旧 C 盘副本当开发分支。

当前中断轮次 s1-20261029-020752522 的主城工具栏代码与设计资产保留，纯逻辑和引用编译已通过，尚未做本轮 Unity/实际画面验收，也没有该轮 worker-result 或发布完成收据。恢复时先审阅中断差异，不直接交付。

## 最少阅读路径

| 任务 | 先读 |
| --- | --- |
| 环境、构建、运行、安装 | [README](README.md)、[工具索引](Tools/README.md) |
| 按功能定位文件 | [源码导航](Docs/Code-Map.md) |
| 层次、依赖与时序 | [架构](Docs/Architecture.md)、[存档契约](Docs/Architecture-Core-Contract.md) |
| 修改地图、卡池、战斗规则 | [规则](Docs/Rules.md)、[S1 说明](Docs/S1-Delivery.md)、[卡池依据](Docs/S1-Cards.md) |
| UI 或图像资源 | [专业设计流程](Docs/Professional-UI-Workflow.md)、[UI 验收要求](Docs/UI-Iteration-Requirements.md)、[资源管线](Docs/Asset-Pipeline.md) |
| 读取历史用户问题/逐轮改动 | [历史指南](Docs/History/Agent-Guide-20261029.md)、[UIIterations](Docs/UIIterations)、[待办记录](Docs/Hourly-UI-Gameplay-Backlog.md) |
| 已停止的后台架构 | [持续开发](Docs/Continuous-Development.md)、[独立发布](Docs/Parallel-Publication.md) |

只加载相关源文件；Library、Builds、Logs 和大量截图不是源码入口。设计导出图、运行截图、真实点击和安装包收据分别说明不同事实。

## 必须保持的边界

- 目标是 S1 幽州 40×40；历史 128×128 研究不适用于本轮。245 个资料身份、41 张试玩招募卡不等于官方完整 S1 卡池。
- Core/Application 是纯 C#；Unity、文件路径、存档和资源加载留在适配层。遵守 .asmdef 引用方向。World partial 文件共享唯一世界状态，不允许各 UI 或后台线程重复结算。
- 不重命名存档字段、改变枚举值或已有 ID；不复制一套界面库存。campaign.json 与 youzhou-40-v1.json 及各自备份保持隔离。
- 不修改玩家真实存档。回归使用独立目录或 -edSmoke / -edReview 只读会话；不结束用户游戏，不覆盖运行中的客户端。
- 图片用稳定 ID、资源清单和 Import-Asset.ps1；保留已有 .meta/GUID。S1Roster.Generated.cs 通过固定 RAG 快照生成，不手改。
- UI 按原图依据 → 可编辑设计 → 实际工具导出并查看 → 实现 → 游戏截图/交互验收。当前 IMGUI 不会因有 SVG/Figma 文件而自动变成 UI Toolkit。
- Figma 未连接不能称已调用；测量模板不等于成品界面。停止继续扩写自制程序美术替代专业设计；现有代码的必要维护不等于允许另造视觉方案。
- Unity 导入与构建串行。已停止的自动化状态不能当作任务授权；不删除暂停文件、不调用 --probe 或启动计划任务来“检查是否停止”。

## 选择验证

| 改动 | 适用检查 |
| --- | --- |
| 文档/注释 | 核对接口、命令、链接；无需启动游戏 |
| Core/Application/源码整合 | Tools/Validate.ps1；检查相关专项和引用编译 |
| 资产/来源 | Validate-Assets.ps1；构建时 Unity 导入校验与真实页面 |
| 存档 | 隔离目录的 JsonUtility/仓库检查；不能拿玩家档做破坏性测试 |
| UI/地图/Shader | 纯逻辑不足，需 Unity 构建、受影响页截图与实际操作 |
| Android 输入/返回/恢复 | Check-MobileInput.ps1，加桌面安全区预览和 Android 真机 |
| 自动化工具 | Check-ContinuousDevelopment.ps1 的沙箱测试；不得因此恢复后台 |

当前还存在 68 个脚本与 102 张图片等待主工程 Unity 生成 meta；不是本次丢失旧 GUID。停机整理已补齐工具栏两个已导出但未登记的图标引用，未继续做新设计。本次未启动 Editor，恢复时统一导入、核对并保留生成结果。

## 交付表述

说明实际改动、当次验证、证据和未完成项。静态编译不证明 Shader 或实际画面，设计图不证明游戏效果，包体验证不证明真机操作。Windows 以 games/version.json、Android 以 apk/build-info.json 为准；缺少新收据时不宣称已更新。
