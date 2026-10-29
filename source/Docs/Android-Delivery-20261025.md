# 2026-10-25 安卓与素材集成记录

本轮按用户要求并行处理素材核验、安卓构建和触控适配，由根任务串行执行 Unity 与运行验证。用户随后取消微信发送，最终交付只保存到 `F:\工作项目\率土之滨\apk`。

## 实际集成

- 通过现有资源管线增加历史资源栏和民居参考图，运行时按图案区域采样，不显示截图里的旧库存数字。PNG清单为258张可用、16张旧原型占位，Unity导入后待生成meta为0；既有GUID保持。
- 地图支持单指拖动和双指缩放；界面支持触摸滚动、安全区、安卓返回层级与暂停保存/恢复结算。实现范围与设备待测项见 [触控说明](Mobile-Interaction.md)。
- 内置 Noto Sans SC 中文字体，来源与SHA记录在 `AssetInbox/font-cjk.source.json`，随包许可证为 `Assets/StreamingAssets/Licenses/NotoSansSC-OFL.txt`。字体不计入PNG数量。
- 全量素材新增候选149个身份、139张不同立绘；当前缺少其S1卡池证据，完整审计在 `AssetInbox/ResourceUpdate-20261025/s1-candidate-audit.json`。没有将未知范围卡片加入当前招募池。详情见 [素材记录](Resource-Update-20261025.md)。

## 本轮已执行验证

| 检查 | 实际结果与证据 |
| --- | --- |
| 规则、应用、移动输入逻辑和静态编译 | 820项既有检查加61项移动检查通过；`Validation/android-resource-core.log` |
| 资源清单 | 258 ready / 16 missing / 0 pending meta；`Tools/Validate-Assets.ps1` |
| Windows构建 | 新运行时代码和资源构建成功；`Builds/Windows/EasternDynasty.exe`，未表示已安装至games |
| 20:9手机界面预览 | 1600×720、29态通过；`Validation/Visual/android-preview-20261025-1600x720/`；已实际查看总览、全卡详情、主城九格、图鉴截图 |
| 桌面真实操作回归 | 37项断言通过、38组新截图/状态；`Validation/Visual/android-resource-regression/20261025-094612-62934647/result.json` |

手机预览运行于Windows，模拟安全区并加载嵌入字体，不能代替Android真机的触摸、GPU、安装与存读档验证。本机 `adb devices -l` 未发现已连接设备。

## APK构建状态

首次直接从中文项目路径构建被Unity Android的路径检查拒绝，失败记录为 `Validation/AndroidBuild/20261025-094104-3c96c7a7/`；该次没有生成可交付APK，Windows设置已恢复。随后采用纯ASCII暂存构建成功，源工程未迁移。

2026-10-25 18:00（北京时间）已交付：

- 文件：`F:\工作项目\率土之滨\apk\EasternDynasty-S1-2026102509.apk`。
- 版本：`0.2.0-s1`，包名 `com.easterndynasty.s1`，版本码 `2026102509`。
- 大小：117,452,186字节，约112MiB。
- SHA-256：`185D854C6D0FA208BFB2DB01BCA5E78FAA708B70EF6DA660F69B748FBC010BEB`。
- 实际清单：最低API25、目标API36；ARM64与ARMv7、IL2CPP、OpenGL ES 3，启动Activity正确。
- APK v2签名验证通过，16KB ZIP页对齐检查通过；中文字体设置包含字库数据，OFL许可证已在APK内核验。尚未连接Android设备，不声称安装或真机运行通过。
- 679个原工程文件与构建快照逐项SHA一致；暂存目录 `F:\EasternDynastyBuild\Android\20261025-095122-c34d8396`，不会作为第二个开发目录。

构建、签名、清单和对齐证据保存在 `Validation/AndroidBuild/20261025-095122-c34d8396/`，最终收据为同级 `apk/build-info.json`；用户取消微信发送，本轮未发送文件。
