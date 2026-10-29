# Eastern Dynasty 本地研究资料库

用户要求将 UI、人物、武将与地图建模研究持续保存到本地，供后续视觉改造使用。本目录是研究索引，游戏运行资产仍位于 Assets；参考资料不自动成为可发行素材。

## 两个研究任务的分工

| 任务 | 重点 | 本地输出 |
| --- | --- | --- |
| art_reference_research | 《率土之滨》具体视觉、人物与武将、地图表现，Eastern Dynasty 原创美术规范 | `../Docs/Research/`、`../References/Research/` |
| ui_modeling_collector | UI 信息架构、地图建模、Unity 与 Blender 实现技术、可续跑资料采集 | `UI-Modeling/`、`../References/UI-Modeling/` |

两个任务的本轮研究已完成，第二个任务的三个批次及去重验证均已落地。随后已经完成两轮游戏视觉实现和实际截图复核，入口见 [视觉交付与剩余差距](../Docs/Research/Visual-Delivery.md)。资料包括来源链接、访问日期、版本背景、是否实际查看、对应分析和下一步待办。

## UI 与建模资料库

第二个任务已保存以下文件；以实际文件和 status.json 为准：

- `UI-Modeling/README.md`：资料导航与本轮成果。
- `UI-Modeling/source-index.json`：可追溯的来源清单。
- `UI-Modeling/status.json`：当前批次、完成项、待办及更新时间。
- UI 技术笔记、地图建模笔记、针对原型的优先修正列表。
- 接受明确 URL 清单的增量采集脚本，以及实际采集记录。

## 持续方式

本轮是用户明确要求启动的独立代理任务。任务分批持久化，后续可读取已有索引与状态继续增量更新，避免重新收集同一资料。

当前没有注册重复执行的定时任务；可复用脚本和状态文件本身不代表后台调度已启用。需要周期调度时，参见 `../Docs/ResearchTask.md`。
