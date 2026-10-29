> 当前状态（2026-10-29）：用户已要求停止全部项目 agent。自动开发与发布已关闭；本文的周期、下一轮和启动命令仅说明历史机制，不能自行执行。以 [交接记录](Project-Status.md) 为准。

# 原作参考与专业 UI 工具流程

2026-10-27 用户要求停止凭想象手写粗糙 UI，研究并调用专业 UI 工具，参考《率土之滨》原作。本文件优先于旧轮次的自行绘制美术方案；每轮 UI 改进仍需落实，但没有设计依据不能为了凑次数继续加自创外观。

## 当前真实能力

| 工具 | 当前状态 | 本项目用途 |
| --- | --- | --- |
| Inkscape 1.4.4 | 已从官方发行包解包到本地，版本调用成功；使用项目导出入口产出预览和对象切片 | 可编辑 SVG 布局、边框/图标组件、规范、PNG 导出和几何查询 |
| Figma | 已查到官方插件，当前 installed=false，未连接，未调用 | 连接后使用真实 frames/components/variables/auto-layout；不能伪称已经创建 .fig 或云端稿 |
| Unity UI Builder | 当前 Unity 6000.3 支持，但现有视图为 IMGUI，未发现 UXML/USS | 以后按完整面板试点迁移，需要真实数据/输入适配；不是导入图片就自动得到游戏 UI |

本地 Inkscape 路径记录在 `.local/design-tools.json`；机器上的工具位置是 `F:\EasternDynastyBuild\DesignTools\Inkscape-1.4.4\PFiles64\Inkscape\bin\inkscape.com`。原包 SHA 与可信 winget 元数据相符；系统证书链检查返回 UnknownError，未据此声称签名链验证通过。工具没有装进 Assets，不改系统文件关联。

Figma 可编辑画布依据：[官方能力说明](https://developers.figma.com/docs/figma-mcp-server/write-to-canvas/)。设计上下文仍需实现为项目代码：[交付边界](https://developers.figma.com/docs/figma-mcp-server/server-returning-web-code/)。Unity 官方说明：[UI Builder](https://docs.unity3d.com/6000.3/Documentation/Manual/UIBuilder.html)。Inkscape [SVG编辑数据](https://inkscape-manuals.readthedocs.io/en/latest/saving.html)与[命令行导出](https://wiki.inkscape.org/wiki/Using_the_Command_Line)。详细研究附在 [工具研究](DesignUI/tool-research.md)。

## 每轮执行顺序

1. **选原图，不即兴发明。** 先打开目标页的真实原作参考与游戏基线。记录资料 ID、文件路径/SHA、原始分辨率、年代及 S1 适用性；没有对应依据的外观列为缺口，不自行添加新的图标、建筑或装饰样式。
2. **先在设计文件中解决视觉问题。** 测量区块、锚点、比例、文字层级与留白，使用 Inkscape 可编辑 SVG 或已连接 Figma 的真实分层组件。原图锁定为参考层；图片、边框、图标、文字、状态与点击范围分开。整张截图嵌入 SVG 仍是位图，不算恢复了可编辑 UI。项目首份 [地图参考测量模板](DesignUI/reference-layout-v1/map-hud-reference-measurement.svg)只有参考图及八个可编辑测量区域，**不是成品 UI**。
3. **真实调用工具并检查输出。** 使用下方导出入口生成 PNG、对象几何及收据，实际打开预览，修正与原图的构图/比例/层级差距。每轮结果列出工具版本、源稿、预览、导出收据与检查结论；只写 SVG/提示词而没调用工具不能说已经设计验收。
4. **按源稿实现，不在代码中重新创作。** 只有对照过的页面设计才进入 Runtime；代码负责状态、布局适配、事件与交互。停止继续扩写 CitySceneRaster、HudLine 等自制美术来替代正式设计；既有程序绘图只维护正确性和兼容性，真实地图标记/动态范围等几何例外。不要在本轮大规模更换 UI 系统。
5. **资源受控接入。** 保留稳定 ID 和 meta，通过既有 Import-Asset 流程导入选定导出图。低清历史截图只作测量，不能放大裁切后声称高清素材；动态数字/中文用真实文本控件，不烘焙进整屏图片；不要把原作截图当可点击游戏界面。
6. **三方对照验收。** 记录原图、设计工具导出图、发布者生成的真实游戏截图。开发仅跑纯逻辑/编译，不启动 Unity/客户端或占用桌面；发布者检查桌面/手机安全区、中文、真实点击、返回与存档。设计稿/导出成功不能替代运行验收。

## 文件与调用

每轮在 `Docs/DesignUI/<版本>/` 保存源稿、`design-spec.json`（原图、组件锚点/尺寸、字体、颜色、状态与运行时入口）、预览及收据。`Docs/UIIterations/<版本>.md` 必须引用这些文件并区分“测量模板 / 设计完成 / 已接入 / 运行验收”。这些目录随发布快照复制；不要把仅存在系统临时目录的文件作为设计依据。

```powershell
# 本轮已验证的参考测量模板；仅导出，不启动游戏或修改正式资产。
.\Tools\Export-UIDesign.ps1 -SourcePath 'Docs\DesignUI\reference-layout-v1\map-hud-reference-measurement.svg' -ObjectIds 'bottom-navigation'
# 实际页面源稿采用自己的版本目录；先查看导出PNG，再接入Runtime。
.\Tools\Export-UIDesign.ps1 -SourcePath 'Docs\DesignUI\<版本>\map-hud.svg'
```

导出产物为源稿同目录的 `exports/*-preview.png`、对象 PNG、`geometry.csv` 和 `export-receipt.json`；此入口只接受 Docs/DesignUI 内的 SVG，参考图嵌入文件，避免外链失效。导出收据保存实际工具版本、源文件/输出 SHA 和图像尺寸。

若 Figma/生图等服务未连接或当前代理没有工具，明确记录缺少的能力，使用已验证本地工具继续可做工作。禁止假称调用、索要或输出凭据、擅自改用收费 API。需要专业栅格素材时，可在宿主确实有内置图像工具时按 imagegen 技能调用；概念位图仍不是可编辑布局，更不是已运行 UI。没有工具或素材不能作为继续自创粗糙美术的理由。

## 下一轮优先任务

已有模板采用2015历史资料仅用于打通测量/导出流程，不锁定最终视觉年代；下一轮优先寻找RAG中更清晰、明确对应目标页面的原图，有更合适参考时先更新设计基线。

从地图 HUD 开始，复用原图中的紧凑边角信息和图像导航结构：先完成其中一个完整区域（优先底部图像导航）的分层设计和接入，其余区域保留测量，不要求一轮重写全屏。使用现有正式图标/画像，保留游戏已有命令。先导出并查看一张桌面设计预览及一张单独布局的手机横屏设计预览；通过对照检查后再替换该完整区域。只移动旧黑色矩形或继续自制程序纹理不算完成这项任务。