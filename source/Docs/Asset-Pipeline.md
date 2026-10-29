# 本地美术资源接入

当前唯一工程目录是 `F:\工作项目\率土之滨\source`。本流程登记已经存在、来源已确认的本地 PNG，不生成图片、不下载 URL、不改变图片像素。新增图片后需要 Unity 导入并重新构建，已发布的客户端不会自动获得图片。

2026-10-25 S1 增量：新增245张 `portrait.s1_*` 官网裁切立绘和 `reference.youzhou.resources` 地图参考原图；`map.youzhou.city` 替换为有完整来源记录的透明派生图。详见 [卡片导入](S1-Cards.md) 与 [地图导入](S1-Map-Art.md)。下文“10张/16占位”是架构重构阶段基线；本批完成后为256张 ready、16张旧原型肖像 missing。S1图鉴使用的245张都是真实文件。

新增 `Kind=map-reference`，ID 为 `reference.youzhou.<name>`，Resources 路径为 `YouzhouReferences/<name>`。它允许保留非透明参考图片原有宽高，几何UV负责裁切显示；不放宽 `map-tile` 的透明3:2约束。肖像允许2:3附近±0.05比例，保留官网240×348及唯一240×340原图，显示层适配而不拉伸文件。文件仍必须PNG且通过真实解码、CRC和哈希检查。批量校验每次重新读取真实字节SHA，只对相同SHA的已验证解码结果做进程内缓存。

2026-10-27 专业 UI 导出新增 `Kind=ui-sprite`，稳定 ID 为 `ui.youzhou.<name>`，Resources 路径为 `YouzhouUI/<name>`。宽高比不限，可用透明轮廓或不透明矩形面板；如实记录输入的 alpha 与透明状态，继续校验 PNG、尺寸和 SHA。只登记通过 [专业 UI 流程](Professional-UI-Workflow.md) 实际导出的 PNG，不用 `map-reference` 冒充 UI 皮肤。`SourceKind=licensed-local` 只表示本地来源分类；`SourceReference` 必须准确记录可编辑设计源稿及导出收据位置，`Rights` 必须说明实际设计来源与参考边界，不因分类名声称原作图像已获许可。低清研究截图不得放大成正式 UI 图片。

## 一条命令导入

先关闭该工程的 Unity Editor，把真实的朱儁肖像放到 `AssetInbox/zhu_jun.png`，在工程根目录运行：

```powershell
& .\Tools\Import-Asset.ps1 `
  -Id portrait.zhu_jun `
  -Kind portrait `
  -SourcePath '.\AssetInbox\zhu_jun.png' `
  -Purpose '朱儁武将卡肖像' `
  -SourceKind imagegen `
  -SourceReference 'AssetInbox/本次生成记录.json' `
  -Rights '本项目生成记录；本字段不自行授予商业授权' `
  -CreatedUtc '2026-10-25'
```

示例输入文件不随工程伪造提供，应先放入实际成图并填写真实来源。`SourcePath` 也支持其他本地绝对路径；拒绝 URL、UNC 网络路径、`..` 路径段及符号链接/目录联接。`SourceReference` 仅记录来源，可填写生成记录、合同编号或来源链接；命令不会访问该链接。

参数：

| 参数 | 含义 |
| --- | --- |
| `Id` | 小写稳定 ID：`portrait.<generalId>`、`map.youzhou.<name>`、`reference.youzhou.<name>`、`ui.youzhou.<name>` |
| `Kind` | `portrait`、`map-tile`、`map-reference`、`ui-sprite`；与 ID 严格配对 |
| `SourcePath` | 已存在的本地 `.png` |
| `Purpose` | 在游戏里的用途 |
| `SourceKind` | `imagegen`、`user-provided` 或 `licensed-local` |
| `SourceReference` | 可追溯的来源记录，不是自动下载地址 |
| `Rights` | 实际权属/许可说明；工具不推断商业授权 |
| `CreatedUtc` | 可选生成/创作日期，ISO 日期或时间；未知时省略 |
| `Replace` | 显式允许替换已登记 ID 的图片或元数据 |
| `ProjectRoot` | 默认脚本所在工程；自动测试使用独立沙箱根目录 |

同一 ID、图片 SHA-256 和来源/用途元数据完全一致时返回 `UNCHANGED`，不会重写清单、PNG 或 `.meta`。若内容或元数据不同，必须显式加 `-Replace`。原 `.meta` 全字节保留，GUID 不变。新文件的 `.meta` 由下一次 Unity 导入生成，离线校验会明确报告待导入数量。

工具不会覆盖未登记的现有目标，即使指定 `-Replace`；遇到孤立 `.meta`、手动修改导致的哈希不符或重复 Resources 路径也会停止，需先检查文件归属。不要通过删除用户 `.meta` 绕过检查。

新 ID 的登记只增加美术资源，不自动创建武将定义、兵种、招募池或地图地类。新增武将仍需单独修改游戏内容配置。

## 清单与稳定 ID

清单位于 `Assets/EasternDynasty/Resources/asset-manifest.json`。`schemaVersion=1`，使用 `entries` 数组及普通字段，兼容 Unity `JsonUtility`，不序列化 Dictionary。结构示例见 `AssetInbox/asset-manifest.schema.json`。

| ID | Resources 路径 | 状态 |
| --- | --- | --- |
| `map.youzhou.grain/pasture/stone/iron/city/mountain/forest` | 分别为 `Youzhou/<name>` | 7 张真实 PNG，构建必需 |
| `portrait.huangfu_song/han_officer/han_scout` | 分别为 `Portraits/<generalId>` | 3 张真实 PNG，构建必需 |
| 其余 16 个现有武将 ID | `Portraits/<generalId>` | `missing`，允许界面显示待绘制占位 |
| `ui.youzhou.<name>` | `YouzhouUI/<name>` | 正式 UI 导出，须有设计源稿和导出收据 |

上表斜线列表表示多个独立 ID，不是一个含斜线的 ID。Resources 路径由 ID 与类型推导，使用正斜线、不含扩展名、大小写固定；调用者不能指定任意输出目录。

每个 ready 记录包含用途、来源、生成日期、登记时间、原始宽高、是否包含 alpha、是否实际透明和原 PNG SHA-256。哈希针对磁盘原图，不是 Unity 压缩/缩放后的纹理。现有 10 张图片的来源指向项目保留的 image_gen 生成记录，没有把“原创生成记录”写成商业许可保证。

missing 记录必须 `required=false`、`placeholderPolicy=ui-placeholder`，宽高为 0、SHA 为空，没有虚构图片元数据。现有覆盖率是 **10 张真实图片 / 16 张肖像待绘制**；占位不算资产齐全。

## Runtime 入口

```csharp
// namespace EasternDynasty
Texture2D grain = GameAssets.LoadTexture("map.youzhou.grain");
Texture2D portrait = GameAssets.LoadTexture("portrait." + generalId, false);
// portrait == null 时由界面绘制明确的待绘制占位，不借用其他人的肖像。
```

- `GameAssets.LoadTexture(string id, bool required=true)`：已加载纹理按 ID 缓存；未知可选 ID 或显式 missing 肖像返回 null，每个 ID 每次缓存会话只警告一次。请求必需资源失败抛出含 ID 和原因的异常。清单标记 `required=true` 的资源不能被调用方降级为可选。
- `ReadyCount` / `MissingCount`：清单里的状态计数，不替代构建时文件检查。
- `ClearCache()`：清空索引、纹理引用及告警去重；不会销毁仍被 UI 或图集引用的 Unity Texture。
- `ConfigureProvider(IGameAssetProvider provider)`：替换纹理/清单提供者并清空缓存。接口是 `LoadManifestJson()` 与 `LoadTexture(AssetRecord)`；默认 `ResourcesGameAssetProvider` 使用 Unity Resources。运行在 Unity 主线程。

渲染层只依赖 ID。未来替换 provider 不需更改渲染层里的 Resources 路径。本轮没有实现 Addressables、异步网络加载或已构建 player 的本地 PNG 热安装。Shader 仍可明确使用 Resources 加载，不属于这份纹理目录。

## 导入设置与构建门槛

`PortraitImport` 与 `YouzhouTextureImport` 使用 `AssetCatalogTools.ApplyProfile`。本轮保留既有有效设置，不调整画面品质：

| 设置 | 幽州地图 | 武将肖像 |
| --- | --- | --- |
| 类型 / 色彩空间 | Default / sRGB | Default / sRGB |
| NPOT / Wrap | None / Clamp | None / Clamp |
| 最大尺寸 | 1024 | 1024 |
| 压缩 | Uncompressed | CompressedHQ |
| Mipmap / Readable | 开 / 开 | 关 / 关 |
| Filter | Trilinear | Bilinear |
| Alpha Source / Alpha Is Transparency | FromInput / 开 | FromInput / 关 |

`ui-sprite` 单独使用 Default / sRGB、NPOT None、Clamp、Bilinear、Uncompressed、FromInput、Alpha Is Transparency 开，Mipmap 与 Readable 均关。最大尺寸 8192 与 PNG 校验允许的单边上限一致，保留导出分辨率；总像素数仍不超过 16,777,216。构建检查拒绝 UI 的 Standalone/Android 单独覆盖，避免透明细线被另一个平台压缩。已有地图和肖像 profile 不变。UI 仍通过 `Texture2D` / `GameAssets.LoadTexture` 消费，Kind 名称不代表 Unity Sprite 导入模式。

旧 `.meta` 的 `DefaultTexturePlatform` 已是 1024；顶层与未启用的 Standalone 段出现 2048，不代表生效平台覆盖。原 postprocessor 也明确设置 1024。本次没有把生效的 2048 资产降为 1024。

地图 PNG 必须有真实透明像素、接近 3:2 比例；肖像接近 2:3。离线校验限制单图 64 MiB、单边 8192 像素、总计 16,777,216 像素，并检查 PNG 签名、块 CRC、结束块、实际解码、尺寸与 SHA-256。

```powershell
& .\Tools\Validate-Assets.ps1
& .\Validation\AssetPipelineChecks.ps1
```

第一条校验清单、图片、重复全局 Resources 路径和已存在 `.meta` 的 GUID 格式。第二条只在 `Validation/AssetPipelineSandbox/<随机ID>` 测试，结束时只清理该已验证沙箱路径，不修改正式 PNG 或 `.meta`。

Unity 构建调用 `EasternDynasty.AssetCatalogTools.ValidateForBuild()`；也可通过菜单 **Eastern Dynasty → Assets → Validate catalog** 执行。它验证基线必需 ID、原图哈希/尺寸、图片已导入、纹理比例、导入设置和 Resources 唯一路径；发现未完成事务会阻止构建。它不会自动修补异常清单来让构建“通过”。

## 写入与恢复边界

导入先读取并完整校验输入与现有目录，再在 `AssetInbox/.asset-transaction` 准备新 PNG、新清单和旧文件备份。独占 `.asset-import.lock` 阻止两个导入器同时操作；Unity 持有工程锁时导入拒绝运行。

提交时替换 PNG，再替换清单；常规异常会恢复提交前的完整文件状态。测试用真实清单文件锁验证了“PNG 替换成功、清单替换失败”的回滚，原 `.meta` 未改。

这不是操作系统提供的跨文件原子事务。进程被强杀、机器断电、磁盘故障或外部文件锁可能让恢复尚未完成；事务目录会保留，校验与构建会拒绝继续。关闭占用程序后运行：

```powershell
& .\Tools\Import-Asset.ps1 -Recover
```

下一次正常导入也会先尝试恢复。如果目标文件在中断后又被外部修改，恢复会拒绝覆盖并保留证据，不会强行丢弃用户改动。不要手动删除事务目录后绕过构建检查。

2026-10-25 验证：63 项沙箱断言通过，包含导入、幂等、显式 Replace、完整 `.meta`/GUID 保留、坏 PNG、CRC 损坏、类型/路径错误、重复 ID、资源路径冲突、未知目标保护、并发锁、Unity 锁、提交失败回滚及中断恢复。Runtime/Editor 静态编译通过；Unity 实际构建由主任务统一执行。

2026-10-27 `s1-20261027-114202570` 验证：`AssetPipelineChecks.ps1` 完成 103 项沙箱断言，新增透明/不透明 UI 原尺寸与字节保留、幂等、严格命名空间、未登记与重复 Resources 拒绝、C# 清单规则一致性；原有 `.meta` 保留、事务回滚与 PNG 内容校验全部继续通过。该检查未启动 Unity，UI profile 的实际导入和 Windows/Android 画面仍由发布者验证。

Unity 接口依据：[Resources.Load](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Resources.Load.html)、[JsonUtility](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.html)、[TextureImporter.alphaIsTransparency](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TextureImporter-alphaIsTransparency.html)。
