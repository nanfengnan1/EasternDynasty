# 专业 UI 工具与 Unity 交付研究（2026-10-27）

研究者：ui_design_tools_research。仅写本 research 目录；未改主工程、调度、资产，未启动 Unity/桌面或安装程序。

## 核实结果

| 工具 | 官方确认的能力 | 当前任务边界 |
| --- | --- | --- |
| Figma remote MCP | use_figma 可写入原生 frame、component、variant、variable 与 auto layout；设计内容可编辑。 | 根任务已查插件 installed=false，当前不能宣称已调用。写入还取决于连接、文件编辑权限及账户支持范围。 |
| Figma 设计交付 | get_design_context 提供结构和视觉上下文，get_screenshot 提供设计基准；输出需由代理翻译成项目实现。 | 默认 React/Tailwind 表达不是可直接运行的 Unity C#，没有核实到自动把现有 Unity IMGUI 转为生产级 UI 的官方一键工具。 |
| Figma code-to-canvas | generate_figma_design 把运行中的网页 UI 送入设计画布。 | 官方描述对象是 web apps/sites，不应称作 Unity 客户端捕获工具。上传 PNG 仍是位图；SVG 可形成可编辑矢量层，但只含嵌入位图的 SVG 不会凭空恢复全部组件。 |
| Inkscape | Inkscape SVG 保留编辑数据；可导入/嵌入参考图片，按页或对象导出 PNG，命令行导出不打开 GUI。 | 可作为本地可编辑模板、图标、框饰、切图和构图工具；不会自动提高原图分辨率或替代美术判断。 |
| Unity UI Builder | 可视化编辑 UXML/USS；通过 UI Toolkit 构建界面。 | 根工程当前使用 OnGUI/GUI.Button/GUI.DrawTexture，检索未见 UXML/USS。UI Builder 不能直接编辑这批 IMGUI 布局，需要视图层迁移和交互验收。 |

Figma 能力依据：[画布编辑](https://developers.figma.com/docs/figma-mcp-server/write-to-canvas/)、[工具目录](https://developers.figma.com/docs/figma-mcp-server/tools-and-prompts/)、[设计上下文不是生产代码](https://developers.figma.com/docs/figma-mcp-server/server-returning-web-code/)。官方建议先取结构、再取截图、下载资产后实现并对照验证：[集成规则](https://developers.figma.com/docs/figma-mcp-server/add-custom-rules/)。

Inkscape 依据：[保留 SVG 编辑数据](https://inkscape-manuals.readthedocs.io/en/latest/saving.html)、[嵌入参考图](https://inkscape-manuals.readthedocs.io/en/latest/import-pictures.html)、[PNG 导出](https://inkscape-manuals.readthedocs.io/en/latest/export-png.html)、[命令行与对象几何查询](https://wiki.inkscape.org/wiki/Using_the_Command_Line)。以实际安装版本的 --help 为准；在线 man 页本次返回403，未假称读取成功。

Unity 依据：[6000.3 UI Builder](https://docs.unity3d.com/6000.3/Documentation/Manual/UIBuilder.html)、[UI 系统比较](https://docs.unity3d.com/6000.3/Documentation/Manual/UI-system-compare.html)、[IMGUI 迁移](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-IMGUI-migration.html)。官方总览仍将 uGUI 列为运行时推荐、UI Toolkit 为替代；UI Toolkit 适合大量多分辨率 HUD/菜单。不能仅因工具名称而宣布必须全量重写。

## 最小可执行流程（本项目建议，不是官方自动迁移承诺）

1. 固定一张原作同页参考、一张真实游戏基线，记录来源、年代/赛季适用性、尺寸和 SHA；原图作为锁定参考层。先测量界面区块、图片占比、字体层级、边距和按钮状态，再建立模板，避免直接开始写新的绘制算法。
2. 先做地图 HUD 或武将详情中的一个完整区域。SVG 使用有名称的真实分层对象：参考、布局、边框、图标、文字、状态、标注；动态数值和文字保持独立，原图只能作为参考或明确标识的嵌入图片。
3. 由 Inkscape 实际打开/解析与导出设计：保留 master.svg，另交付 1440×900 及 1600×720 预览、必要的透明 PNG 切片、对象几何/资源清单。核对渲染结果后才认为工具链可用，不能只生成 SVG 源码就称已调用工具。
4. 同屏比较原图、设计预览、当前游戏图；围绕比例/层次/留白/配色/图片统一性具体修订。低清原图放大不会提升细节，不把它裁成成品游戏资产。
5. 开发按设计对象与规格对齐控件，复用真实命令、状态与存档；短期可在既有视图接入已导出资产。若选择 UI Toolkit，先为一个独立面板建立 UIDocument/UXML/USS 和数据/事件适配，再由发布进程验证输入、中文、手机安全区和返回。不要在这一步顺手全量迁移。
6. 发布使用真实运行截图对比设计稿，不用截图态数量代替观感。完成标准包括页面对应的设计源文件、工具实际导出记录、素材身份及运行截图；位图概念图不能称为可编辑 Figma 稿或已实现 UI。

## 根任务已提供的现场约束

- Figma 两天前已推荐但未连接，本轮不重复弹出推荐。
- 根已核对官方 winget 源中的 Inkscape 1.4.4、下载地址和 SHA，计划解包本地调用，不改系统文件关联；最终版本与调用成功须看根任务的真实导出记录。
- 已查看 STZB0395 地图参考500×280、STZB0505 卡牌参考900×505；低清图片只作对照/测量，不当高品质新资产。
- 根独占 source/Docs/DesignUI、Tools/Export-UIDesign.ps1、开发与发布提示更新；本代理只负责本 research 目录。

## 本地代码依据

- Assets/EasternDynasty/Scripts/Runtime/EasternDynastyPresentation.cs：OnGUI、GUI.Button、GUI.DrawTexture。
- Runtime/CardDetailPresentation.cs、CityFacilityCatalogPresentation.cs、CityOverviewPresentation.cs：同样直接使用 IMGUI 控件与贴图绘制。
- 搜索 Assets 下 *.uxml、*.uss 未发现结果。因此采用 UI Builder 需要真实迁移工作，不是打开 Editor 即自动成为可编辑 UI。