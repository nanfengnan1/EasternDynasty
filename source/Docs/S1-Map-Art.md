# S1 幽州地图图片接入

2026-10-25。本轮仅适配幽州 40×40。用户明确要求使用本地《率土之滨》图片，优先级高于旧接手文档中“参考截图不作正式素材”的默认约定。以下区分原图采样与生成加工图，不将历史截图称为现行完整素材库。

| 运行时 ID | 来源与处理 | 已知边界 |
| --- | --- | --- |
| `reference.youzhou.resources` | 本地 RAG `S1-RES-004`，320×320 JPG 四资源组合。仅无损转换为 PNG，完整逐像素比对；资源地块用网格 UV 采样原图区域 | 早期官网 2D 外观，原图未确认资源等级；采样略内缩避开原绿色边线。不是独立透明图集或当前版本承诺 |
| `map.youzhou.city` | 依据 RAG `S1-RES-010` 主城截图，通过内置 `image_gen` 去文字和背景生成透明加工图，1536×1024 | 属于派生加工图，建筑细部经过模型重建；不是官网原始 sprite。现共用一个外观表现主城和六座城池，未取得分城级原图 |

四资源图片原页为 [地图玩法](https://stzb.163.com/strategy/2015/10/22/20401_575761.html)，图像地址分别为 [资源组合](https://nie.res.netease.com/r/pic/20151103/08151107-3425-4db6-9609-5592dfb2c8e4) 和 [早期主城](https://nie.res.netease.com/r/pic/20151103/c2c56d34-350f-410b-a751-f40777ad2135)。本轮读取已有本地缓存，未声称重新联网核对版本。

原资源 JPG SHA-256：`b81b85839fc9949d282d2fa62312c98ff1f789a9f9d1d7d54f94a6643a4614ec`。原主城 JPG SHA-256：`7213b8e71ac5f0414809de7858af8529184db33b0a0af3f9e6618da2b1c024cb`。派生城池 PNG SHA-256：`178d9ac249e7178b9669edad891a1a40ec534e156a910e66d898f4155fba6b41`。

## 导入和渲染

在其他资源导入批次完成后运行：

```powershell
& .\Tools\Import-S1MapAssets.ps1 -CitySource '完整路径\已生成的城池.png' -PrepareOnly
& .\Tools\Import-S1MapAssets.ps1
& .\Tools\Validate-Assets.ps1
```

`-PrepareOnly` 只写 `AssetInbox/S1Map`，验证源哈希、原图转换后的 102400 个像素和城池透明度，保留旧城池 PNG。第二步通过统一事务导入器登记 `YouzhouReferences/resources.png` 并替换 `Youzhou/city.png`，验证旧 `.meta` 完全保留。Unity 导入和构建由主任务串行执行；导入成功不是实际显示验收。

`map-reference` 类型允许非透明 PNG 和原始比例，ID 必须为 `reference.youzhou.<name>`，仍执行来源、PNG 解码/CRC、尺寸、哈希、路径及冲突校验，不放宽 `map-tile` 的透明 3:2 要求。

原图坐标原点在左上角，顺序为左、下、右、上：

| 资源 | 原图四个角（像素） |
| --- | --- |
| 粮地 | (150,144)、(229,185)、(315,140)、(232,101) |
| 林地 | (154,48)、(233,91)、(317,43)、(237,3) |
| 石地 | (4,234)、(78,272)、(154,231)、(80,194) |
| 铁地 | (76,193)、(155,231)、(230,187)、(155,148) |

渲染代码将以上四角向各自中心内缩 4%，避开历史截图中绿色归属边线。2026-10-27的 `s1-20261026-183252011` 将四资源由满格采样改为本格内每轴70%–84%的局部景物，尺寸与轻微偏移只由坐标决定；原采样UV和正式图片不变，连续地表与独立柔边Shader连接景物。原纹理没有mipmap，远景用最多两源像素半径的显式采样过滤。完整逻辑格仍可拾取，河桥、归属与选择覆盖保持独立；新Shader及观感待发布验收，见[本轮UI记录](UIIterations/s1-20261026-183252011.md)。游戏的资源等级仍为逻辑叠加文字，不据图片推定等级。城池的投影宽度 1.45、地面轴心 0.453、纵向修正 0.96不变。

四类资源的单格有效原图区域约 150×80 像素，近距离放大仍受原始清晰度限制。山地、山缘林地仍保留上一版生成图；河流和桥梁仍使用程序材质/几何。军队由九名士卒轮廓及旗帜组成，属于程序几何，不是取得了《率土之滨》原始兵模或动画。缺失独立原图、各等级变化和兵种方向帧等，交由主任务统一记录到 `rag/question.md`。

## 城池加工记录

使用内置 `image_gen` 编辑模式，`transparent_background=true`，参考路径指向上面的原主城 JPG。生成记录为 `01a1717d-cb88-7812-af47-761d170077c2/exec-a7209b0d-72ac-47d3-8264-a31fca48c62a.png`。原始输出保留在生成目录，项目使用导入后的本地 PNG。

最终提示词：

```text
Edit target: provided historical Chinese strategy-game city reference. Create ONE clean reusable game sprite of this exact walled city. Preserve the source architecture, red timber buildings, tiled roofs, city walls, colors, isometric view and detailed old-school hand-painted appearance. Remove the black/gold text plaque and every text/UI element entirely, reconstruct only the tiny obscured roof areas. Extract the entire city and its single rhombus ground footprint on a genuinely transparent background. Remove surrounding fields, mountains, corner decorative frame, green ownership lines, all UI text. Canvas landscape 3:2. Full city centered, no clipping, rhombus ground tips near the left/right margins, modest transparent margin above roofs. Orthographic view approximately 30 degrees elevation, 45 degrees azimuth. Keep image as source-faithful as possible; do not invent a different city or modern visual style. No lettering, no people portraits, no labels.
```

来源字段只记录实际出处，不自动授予商业许可或证明所有加工细节与原图完全相同。

2026-10-25 正式导入通过：reference.youzhou.resources 原图像素PNG SHA-256=a91a2051b5a20d0776917ce24372d5c48b98b9fafecc6a12acdde7d9fd1d9b92；map.youzhou.city 派生PNG SHA-256=178d9ac249e7178b9669edad891a1a40ec534e156a910e66d898f4155fba6b41。替换前后 city.png.meta 完全一致，GUID=96df9e194d93e8a4aaaee3cf16a286d7，meta SHA-256=9f74f4196f04aa815918d212d9be71f86fe732431ccd35cb1660e4446872686b。本项不替代 Unity 构建及场景验收。

资源沙箱回归已通过 78 项断言，日志为 `Validation/s1-asset-pipeline-checks.txt`。新增覆盖原图参考导入及哈希、受控目录与 ID 拒绝、240×340 肖像原生尺寸和字节保留，以及同路径/同时间戳/同长度 PNG 改变后仍被内容哈希与 CRC 检查拒绝；另保留原有导入、回滚、锁、恢复检查。沙箱结束后自行清理，正式 PNG 与 .meta 不作为测试写入目标。
