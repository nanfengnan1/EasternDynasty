# Unity 6000.3 实施与资产规范

研究基线：Unity **6000.3.27f1**、现有内置渲染管线、运行时 IMGUI。本文没有修改游戏代码，也没有在编辑器中验证新资产；以下是供主开发代理实施的具体方案。后续实际导入要以项目安装包版本为准。

## 保持现有架构的第一轮

`MapView` 保留逻辑格到世界坐标映射与拾取接口；显示由连续底地、地理层、资源/建筑层、归属/选择层组成。替换视觉对象后用同一存档核对选地、出征、返回和占领，避免显示与碰撞错位。先完成一块包含主城、森林、矿地、山脉和河流的可比较区域，再扩大全地图。

资源模型和植被采用少量共享 Mesh/Material，先缓存材质与网格；避免每格生成独立材质实例。城市选中效果用单独覆盖圈或描边，避免为一栋城复制全套材质。游戏逻辑格可继续用低成本拾取，装饰树、城墙瓦片不需要逐个加碰撞。

## 连续地表、材质和渲染

第一轮选用现有内置管线支持的简单 lit 材质/项目着色器即可。基础色承担区域风格，受光承担体积，细节贴图承担近景质感。不能把 URP Renderer Feature、URP TerrainLit 的通道约定直接写进内置管线。

可选地表方案：A. 分块网格 + 顶点色/共享材质，成本小、适合当前规则格；B. Unity Terrain + Terrain Layers，适合更大范围地形雕刻与绘制。不要同一轮同时维护两种完整地表实现。

若选 Terrain，先使用草、土、岩、河岸四层，并复用 TerrainLayer 资产。超过四层可能增加内置管线 pass，未绘制但已分配的层也需检查。[依据：Unity 6000.3 Terrain Layers](https://docs.unity3d.com/6000.3/Documentation/Manual/class-TerrainLayer.html)

同模型同材质的大量树木/岩石是 GPU Instancing 的候选，但要检查项目 Shader 是否支持实例化；这不是自动对所有对象生效。先用 Frame Debugger 对比再决定开启。当前内置管线不能直接套用 Shader Graph 实例化流程。[依据：Unity 6000.3 GPU Instancing](https://docs.unity3d.com/6000.3/Documentation/Manual/GPUInstancing.html)

## LOD 与可见性

针对城市/树林/营地分别建立 LODGroup，依据真实相机屏幕占比调阈值。先只做两档可见几何和远景简化标记，测试轮廓跳变再增加档位；小资源图标的文字层可独立存在，防止远景模型剔除同时让策略信息消失。

程序合并地表要按块合并，保留块级可见性；不要把全天下合成一个巨大 Mesh。首轮无需实现无限世界流式加载。自动减面后检查 UV 接缝、屋檐、城门和地面接触点。[依据：Unity LOD Group](https://docs.unity3d.com/6000.3/Documentation/Manual/class-LODGroup.html)、[Blender 4.5 Decimate](https://docs.blender.org/manual/en/4.5/modeling/modifiers/generate/decimate.html)

## 模型交付约定

| 项目约定（提案） | 首次交付检查 |
|---|---|
| 统一 1 Unity unit = 1 m 的资产尺度基准，游戏地图视觉可另设抽象比例 | 用 1 m 校准立方体往返导入，观察 Unity bounds，不能仅相信导出窗口数值 |
| 建筑 pivot 在地面中心；墙段 pivot 在拼接端点 | 旋转与并排放置时不悬空、不留洞 |
| 采用可复现 FBX 导出，保留源 `.blend` 在美术源目录 | 记录 Blender 版本、单位、Forward/Up、是否应用修改器、Unity importer 设置 |
| 所有模型优先共享 1–2 个材质槽 | 不靠每个窗户新建材质区分颜色 |
| 屋顶/墙面/木材采用明确 UV；同类模块共享纹理 | 检查斜视图、远景 mipmap 与接缝 |
| 静态装饰默认不导入相机、灯光和无用动画 | 避免 FBX 中制作场景污染游戏场景 |
| 只有运行时确实需读取几何数据才启用 Mesh Read/Write | 避免无必要的 CPU 侧数据副本；程序生成 Mesh 另行判断 |

Unity 导入器支持单位与轴转换、法线、切线和 Read/Write 设置。[Unity 6000.3 Model Import Settings](https://docs.unity3d.com/6000.3/Documentation/Manual/FBXImporter-Model.html)

Blender 4.5 FBX 文档明确了轴转换和导出修改器的机制，但仍有部分参数标为 Todo，且材料交换存在格式限制。因此不宣称一组 FBX 勾选项适合所有模型；先用校准样件往返验证，复杂 Blender 节点材质在 Unity 重建。[Blender 4.5 FBX](https://docs.blender.org/manual/en/4.5/addons/import_export/scene_fbx.html)

## UI 图集和文字

**当前 IMGUI 优化**：先缓存 GUIStyle、颜色和引用的纹理，避免在反复绘制时重建资源；对窗口尺寸变更重新计算布局，图片保持纵横比。此为项目实施建议，须由实际分配/帧数据验证。

**后续 uGUI**：常驻 HUD 小图标、通用卡框/按钮、武将肖像分组管理，不把全部高清人物和小图标塞进一个超大图集。Canvas UI 图集关闭旋转；保留边缘 padding，纹理 Read/Write 只在需要时启用。[Unity 6000.3 Sprite Atlas](https://docs.unity3d.com/6000.3/Documentation/Manual/class-SpriteAtlas.html)

**后续 UI Toolkit**：动态 atlas 可减少纹理切换，但大肖像是否入图集由 Panel Settings 的大小/格式/过滤规则决定；不能假设任意纹理都会自动合批。结合 Frame Debugger 查看实际结果。[Unity 6000.3 Runtime UI performance](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-performance-consideration-runtime.html)、[Panel Settings](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-Runtime-Panel-Settings.html)

字体选型需同时验证中文覆盖、许可、fallback、清晰度与图集内存。正式资产清单记录来源和授权；本研究目录的 Unity/网易截图仅供分析。

## 验证步骤与性能记录

1. 同一存档、相机、分辨率保存前后地图、武将、开局截图；确认主操作与状态更加清楚。
2. 覆盖新建、选地、编队、出征、回城、占领、重载存档，尤其检查 UI 点击是否误触地图。
3. 在实际 Player 运行常用近/中/远三档和高密城市区，分别记录 CPU/GPU 帧时间、Draw Calls/Batches、三角形数、纹理/网格内存及 GC 分配；性能数值必须附分辨率与硬件。
4. Frame Debugger 检查地表是否多 pass、树木是否实例化、透明层是否反复覆盖、UI 是否因纹理切换增加批次。该工具支持当前内置渲染管线。[Unity 6000.3 Frame Debugger](https://docs.unity3d.com/6000.3/Documentation/Manual/FrameDebugger.html)
5. 首个目标可暂定桌面常用画面 60 fps，即总帧预算约 16.7 ms；这是项目试验目标，尚无目标机器实测结果。GPU/CPU 瓶颈确认后才决定减面、降纹理或减少 UI 更新。

验收不能只写“已开 LOD/已开图集”。要给出同机前后截图、性能记录和玩法回归结果。
