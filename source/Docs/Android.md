# Android 试玩 APK

Android 版沿用 S1 幽州40×40、卡牌、行军、城建和任务引导。目标为本地侧载试玩，不发布应用商店。实际构建是否成功、APK 文件、大小、SHA-256 和验证日志，以同级 `apk/build-info.json` 为准；脚本文件存在不代表 APK 已经生成。

## 手机要求与构建配置

- Android 7.1 / API25 或更高，这是 Unity6000.3 的最低支持版本。
- 同一 APK 包含 ARM64 和 ARMv7，使用 IL2CPP；不包含 x86/x86_64 模拟器库。
- OpenGL ES 3.0，左右横屏自动旋转，禁止竖屏。安全区由构建配置和运行时界面适配共同处理。
- 包名固定为 `com.easterndynasty.s1`，版本码单调递增，APK 使用本机 Unity 调试密钥签名。它是有效的本地安装签名，不是商店发行签名。
- 所有游戏资源随 APK 打包，不需要连接电脑或在线下载素材。Windows 和 Android 使用各自的本地存档，当前没有跨设备同步。

Unity6.3提高最低系统要求的依据见[官方版本说明](https://docs.unity.com/en-us/engine/6000.3/manual/whats-new/unity63)。构建默认关闭 Vulkan，并使用 Minimal managed stripping、关闭 engine stripping 和 Java minify，优先保留当前 JsonUtility 模型、启动代码及资源加载路径；这不代替真机存读档与显示验证。

## 开发环境

在 Unity Hub 为工程指定的 **6000.3.27f1** 安装 Android Build Support，并同时安装 Android SDK & NDK Tools、OpenJDK。工具读取当前工程的 `.local/unity.json` 或 `-UnityEditor`；不将某台机器的安装路径写成源码依赖。

该版本的官方依赖为 OpenJDK17、NDK r27c / 27.2.12479018、SDK build-tools36.0.0、command-line-tools16。SDK 的具体支持版本见[官方依赖表](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/android/getting-started/sdksetup/supported-dependency-versions)。构建脚本检查 Hub 配套目录、NDK版本、JDK版本、SDK平台和签名/清单检查工具。

```powershell
Set-Location 'F:\工作项目\率土之滨\source'
& .\Tools\Build-Android.ps1 -CheckEnvironmentOnly
& .\Tools\Build-Android.ps1
```

默认交付位置是 `F:\工作项目\率土之滨\apk\EasternDynasty-S1-<版本码>.apk`，同时生成 `.sha256` 和 `build-info.json`。可选 `-VersionName '0.2.0' -VersionCode <正整数>`；不指定版本码时使用当前UTC年月日小时，并高于已登记交付的版本码。拒绝重用已存在的同版本 APK，失败不会把旧 APK 当成新结果。

默认保留上一包。自动迭代可传 `-ReplacePrevious`：只有新 APK、校验文件及成功收据全部写好后，才考虑清理启动时 `build-info.json` 明确登记的上一包。旧文件必须直接位于同一输出目录、名称为 `EasternDynasty-S1-<整数>.apk` 且匹配收据版本码，SHA-256 必须与旧收据一致，不能是新包；路径含重解析点、文件已改变或正在使用时保留。清理使用独占文件句柄，在同一锁定句柄内验证并标记删除，不存在先解锁再删除的间隙。只删除该包及内容匹配的 `.sha256`，不扫描删除其他历史包或用户文件。由于占用而保留的旧文件不会在后续轮次被无差别清理。

小时迭代建议同时传 `-CleanWorkspaceOnSuccess`，避免成功构建留下的大量快照和Library占满磁盘：

```powershell
& .\Tools\Build-Android.ps1 -ReplacePrevious -CleanWorkspaceOnSuccess
```

此开关默认关闭，仅在APK已成功交付并写入收据后处理本次新建目录。脚本再次核验交付APK哈希，确认目录绝对路径严格等于暂存基目录加本次运行ID、所有权标记的源目录及运行ID一致、目录及祖先和内部不存在重解析点、对应Unity进程已退出，并检查文件未被占用。随后把 `.build.json` 保存为验证目录中的 `android-build.json`，同时归档Unity日志、源码快照和目录标记并校验哈希，再删除该本次目录。不会清理历史目录、失败构建目录或其他文件。

`build-info.json` 和验证目录的 `result.json` 包含 `workspaceCleanup`，可能为 `disabled`、`removed`、`retained` 或 `cleanup-incomplete`；清理结果另存 `workspace-cleanup.json`。`androidWorkspace` 是构建时路径，`workspaceCleanup.status=removed` 时它已不存在，证据仍在项目 `Validation` 目录。路径、占用或证据检查失败时保留目录；删除过程中发生文件系统错误时记录未完成状态并保留剩余内容。清理失败不会将已交付APK改判为构建失败。

需要改变安装环境路径时：

```powershell
& .\Tools\Build-Android.ps1 -UnityEditor 'D:\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe'
```

## 构建与验证过程

`Assets/EasternDynasty/Editor/AndroidBuildTools.cs` 通过独立入口设置 Android 构建参数。脚本首先用 `-buildTarget Android` 启动 Unity，确保导入平台和条件编译符号一致；再检查资源清单、Shader 和 BuildReport。

Unity Android工具会在进入Gradle前拒绝当前中文工程路径；2026-10-25的首次构建已实际确认这一点。因此脚本先在工程所在盘创建纯英文独立快照：`F:\EasternDynastyBuild\Android\<运行ID>\Project`。完整复制 `Assets`、`Packages`、`ProjectSettings`，包括全部 `.meta`，逐文件核验 SHA-256 并保存 `source-snapshot.json`。不复制 `Library`，首次构建会重新导入资源；不镜像、不删除或覆盖既有暂存目录。每次构建均建立新的有标识目录，原工程路径保持不变。

可以单独验证复制流程，此命令不启动Unity、不生成APK：

```powershell
& .\Tools\Build-Android.ps1 -PrepareWorkspaceOnly
```

候选输出位于纯英文暂存目录的 `Builds/EasternDynasty-S1.apk`，Unity日志位于同目录的 `Logs/unity-android.log`。只有确认 APK 包含 Android 清单、Java入口、Unity数据和两种架构的 Unity/IL2CPP 原生库，并通过 `apksigner verify`、`aapt dump badging` 和 `zipalign -c -P 16 -v 4` 后，才复制到交付目录并校验 SHA-256。签名的定义见[Unity 调试签名说明](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/playersettings/android/usecustomkeystore)，ZIP页对齐检查见[Android zipalign 文档](https://developer.android.com/tools/zipalign)。页对齐检查只验证 APK 对齐，不能单独证明所有设备的运行兼容性。

只对暂存快照切换Android平台。C#入口在 `finally` 恢复自身临时配置，暂存项目可保持Android平台；原工程不会由此脚本打开或切平台，原 `ProjectSettings` 和 `Library` 不做写入。复制结束时核验源目录及暂存目录的文件集合与哈希；构建结束和最终交付前，重新对原 `Assets`、`Packages`、`ProjectSettings` 的全部文件（包含 `.meta`）与构建前 `source-snapshot.json` 比较。任何新增、删除或内容变化都会阻止交付，旧收据与旧包保持原样。Unity正常修改的暂存设置不作为原源码基线。不存在原工程的Android恢复操作，也不再提供 `-KeepAndroidTarget`。开始前应完成其他agent的资源/代码编辑并关闭原工程Unity；构建期间保持源文件冻结，避免快照不一致。

工程中的 `Validation/AndroidBuild/<运行ID>/` 保存 `source-snapshot.json`、复制回来的 `unity-android.log`、`signature.log`、`manifest.log`、`zipalign.log` 与 `result.json`。成功交付后另写 `previous-artifact-cleanup.json`，记录旧包被删除或保留的原因。失败会保留暂存候选和日志供排查，交付目录不会登记失败包。若进程被外部强制结束，日志仍可在纯英文暂存目录查找；处理前确认对应Unity进程状态，不能同时启动多个构建Editor。

## 安装与设备测试

把 `build-info.json` 指定的 APK 传到手机，打开它，按系统提示允许该文件管理器安装应用。以后更新使用相同包名和签名安装；不要为更新而卸载旧版，以免删除设备上的存档。此开发密钥只用于本机连续试玩，不能随意更换后假定可覆盖原安装。

已连接并授权 USB 调试的设备可使用Android SDK的adb：

```powershell
adb devices
adb install -r 'F:\工作项目\率土之滨\apk\EasternDynasty-S1-<版本码>.apk'
adb shell am start -n com.easterndynasty.s1/com.unity3d.player.UnityPlayerActivity
```

真机重点检查：中文显示、刘海/手势条安全区、单指拖图、双指缩放、卡库滚动、点击主城建设、任务领奖、出征/驻守/屯田、后台切换保存和重启读档。构建脚本的 `deviceTested=false` 表示只完成编译与安装包验证；实际设备结果另存验证记录，不把Windows画面或 APK 签名通过称为手机已试玩通过。
