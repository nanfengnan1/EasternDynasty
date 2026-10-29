# AI 工作入口

**当前指令：停止。** 2026-10-29 用户要求停止项目 agent、整理代码和文档后停止。两条游戏计划任务、自动化开关和手动暂停文件均保持停止状态；只有新的明确用户要求才可恢复。历史“每小时优化”和“并行 agent”记录不是当前授权。

唯一开发目录：F:\工作项目\率土之滨\source。C:\Users\fenglei\EasternDynasty 是迁移前旧副本，不继续编辑。

1. 先读 [agent.md](agent.md) 和 [当前状态](Docs/Project-Status.md)，按 [源码导航](Docs/Code-Map.md) 只加载相关文件。环境命令见 [README](README.md)，结构/时序见 [Architecture](Docs/Architecture.md)。
2. 目标为 S1 幽州 40×40；规则以 [Rules](Docs/Rules.md) 为准。历史地图提案、官方资料身份和试玩规则需区分。
3. Core/Application 保持纯 C#；Unity、文件系统、资源加载留在适配层。遵守 .asmdef 依赖；World 是共享聚合，不是多个并行服务。
4. 正式图片经稳定 ID、清单和 Tools/Import-Asset.ps1 接入。保留 .meta/GUID，不直接引用缓存或外链；详见 [Asset-Pipeline](Docs/Asset-Pipeline.md)。
5. 保留存档字段/类型和双槽隔离；测试用独立目录或只读会话，不改玩家真实档。不终止或覆盖用户运行中的客户端。
6. 验证按 agent.md 矩阵选择。引用编译、设计预览、游戏截图、实际点击和已发布包不可互相替代。
7. 用户明确恢复并授权并行时，先分配文件所有权；Unity 导入、构建和桌面验收仍由单一执行者串行操作。
8. 当前会话最新指令优先；本文不额外要求确认，也不授权支付、对外通信或删除无关文件。微信发送已取消。
9. 开发/发布机制见 [Continuous-Development](Docs/Continuous-Development.md)。当前 Automation/config.json 的 enabled 与 publicationEnabled 均为 false，两条 manual-hold.json 保留；禁止自行启用、移除或用 --probe 绕过。
10. 历史冻结 Source 禁止修改；其 Project 仅供发布验证，不是第二开发目录。队列保留不表示发布进程仍在运行。
11. 恢复 UI 工作后遵守 [UI-Iteration-Requirements](Docs/UI-Iteration-Requirements.md)：原图与游戏基线对照、每轮记录实质改进，功能数量/换色/测试数量不等于视觉提升。
12. 设计先读 [Professional-UI-Workflow](Docs/Professional-UI-Workflow.md)，保存可编辑稿与 Tools/Export-UIDesign.ps1 的真实导出收据。Figma 未连接不假称调用；测量模板不是成品；不继续用无参考的程序美术替代设计。
