# Eastern Dynasty：UI 与建模技术资料库

本库由第二个独立研究代理建立。研究基线为 **Unity 6000.3.27f1 + 内置渲染管线 + 当前 IMGUI 原型**。本轮完成 UI、地图建模、Unity 落地三个有限批次；下一轮可以沿着索引继续采集、对比实施截图和修正方案。

“持续”在这里落实为分批采集、立即保存、支持续跑。**没有创建系统定时任务、常驻服务或无限循环，没有训练模型。** 本轮研究本身没有修改游戏 Assets 或 ProjectSettings。

## 从哪里开始

| 文件 | 内容 |
|---|---|
| [原型优先修正清单](Prototype-Priorities.md) | 可立即交给主开发代理的 P0/P1/P2；先改善现有界面和地图，再决定框架迁移 |
| [UI 技术笔记](UI-Notes.md) | HUD、武将卡、配队/招募、字号、命中区、分辨率适配、当前 IMGUI 路线 |
| [地图建模笔记](Map-Modeling-Notes.md) | 逻辑格与连续地形分离、河岸、山体、植被、城市/部落模块、相机与 LOD |
| [Unity 实施规范](Unity-Implementation.md) | 内置管线、共享材质、FBX/单位/轴向、图集、性能与验证 |
| [来源索引](source-index.json) | 每条来源的 URL、时间、版本、主题、查看状态、本地位置、哈希和使用范围 |
| [人工审核记录](review-records.json) | 阅读/看图状态与当时 SHA-256 绑定；源内容变化后需重审 |
| [状态](status.json) | 当前批次、本轮完成情况与下一轮待办 |
| [采集结果](Collection-Results.md) | 实际运行、去重、保存内容和未实施的事项 |
| [地图第二轮实施记录](Visual-Iteration-02.md) | 第一轮真实截图与性能证据、第二轮地表/农田/林簇调整和待验证项 |
| [采集器](collect.mjs) / [URL 清单](sources.json) | 无外部依赖、有限次、显式清单、超时、哈希与失败记录 |

率土具体人物与视觉参考由另一个代理整理：[视觉研究](../../Docs/Research/2026-10-24-Visual-Study.md)、[首轮美术任务](../../Docs/Research/First-Pass-Backlog.md)、[美术来源目录](../../Docs/Research/Source-Catalog.md)。本库不重复武将名册和网易参考图。

## 已保存与查看

本轮共有 **13 篇一手技术文档 + 4 张官方技术插图**。文档仅保存元数据、标题/小标题、图片 URL 与哈希，正文用自己的分析整理；没有镜像保存整篇文章。图片共 647,688 字节，置于 `References/UI-Modeling/`，已实际打开查看。

| 实际查看的图 | 用途 |
|---|---|
| [Canvas Scaler Inspector](../../References/UI-Modeling/ugui-scaler-inspector-image-f70a70e20ba0.png) | 辨识缩放组件与像素密度设置 |
| [连续沙质地形](../../References/UI-Modeling/unity-sandy-terrain-image-1eb364ef87f9.png) | 地表连续性、几何明暗和纹理重复观察 |
| [邻接地形块](../../References/UI-Modeling/unity-neighbor-terrain-image-5652956341b0.png) | 区分编辑分块与玩家可见地块 |
| [LOD 屏幕占比](../../References/UI-Modeling/unity-lod-image-974c18333638.png) | 理解依据投影尺寸切换模型 |

图片版权属于 Unity，仅供本地研究分析，**不代表获准打包进可发行游戏**。Blender 文档版本固定 4.5 LTS，本机 Blender 版本尚未验证。uGUI 引用 2.0.0 文档，当前项目尚未安装该包；主项目改包前需确认版本。没有套用 Unity 6.5/6.6 新功能结论。

## 续跑方式

需要本机 Node.js 20 或以上；本轮实际使用 Node.js 24.14.0，无需安装依赖。

```powershell
Set-Location 'F:\工作项目\率土之滨\source\ResearchLibrary\UI-Modeling'
node .\collect.mjs --batch=1
node .\collect.mjs --batch=2
node .\collect.mjs --batch=3
```

运行全部清单（含参考图）：

```powershell
node .\collect.mjs
```

仅当需要在终端阅读相应原文片段时使用 `--inspect`。默认不保存文章全文。所有运行记录位于 `runs/`，失败追加到 `failures.jsonl`（没有失败时该文件可以不存在）。

增加来源时先在 `sources.json` 写入明确 URL、唯一 id、资料类型、主题、批次、版本和使用范围；只有清单里的主机允许访问。采集器不递归跟踪链接；每次请求超时 20 秒、体积上限 8 MiB、请求间隔 1.2 秒、最多处理 5 次重定向，403/429 等直接记录失败，不绕过访问限制。采集输出目录固定在本库和本库参考图目录。

同一时间只运行一个采集器。`.collector.lock` 用于避免两个进程互相覆盖索引；若上次异常终止留下锁，先根据锁内 PID 确认没有采集进程，再移除该锁。不要仅因为等待较久就删锁。

新增页面下载后默认是 `downloaded_pending_review`，必须真正阅读/看图后才更新 `review-records.json`。审核记录需绑定 `reviewed_sha256`；同 URL 内容改变将标为 `changed_pending_review`。这避免把“下载成功”冒充“已学习”。

## 下一轮研究的触发条件

优先等待主开发代理给出实施后的地图、选地、武将、开局截图和同机性能记录，然后按仍然存在的差距继续采集。后续可补：原创城市套件验证、完整配队交互参考、低分辨率中文可读性、实际 Player 的图集/实例化收益。
