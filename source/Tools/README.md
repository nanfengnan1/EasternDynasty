# 工具入口

从工程根目录执行。2026-10-29 起后台开发和发布已按用户要求关闭；本索引不是恢复运行的指令。

| 用途 | 命令 | 影响 |
| --- | --- | --- |
| 查看停止状态/交付收据 | `& .\Tools\Get-ProjectStatus.ps1` | 只读，不初始化任务、启动 agent 或领取发布版本 |
| 定位 Unity | `& .\Tools\Setup.ps1 -SkipImport` | 检查环境并写本机配置 |
| 准备工程 | `& .\Tools\Setup.ps1` | 启动 Unity 导入与场景准备 |
| Windows 构建 | `& .\Tools\Build-Windows.ps1` | Unity 批处理，写 `Builds/Windows` |
| 运行构建 | `& .\Tools\Run.ps1` | 打开 `Builds/Windows` 客户端，普通模式可保存进度 |
| 只读试玩 | `& .\Tools\Run.ps1 -Review` | 临时征程，不保存玩家进度 |
| 安装已有构建 | `& .\Tools\Install-Build.ps1 -DestinationPath 'F:\游戏\EasternDynasty'` | 复制完整包；已有专用安装的覆盖需 `-Replace` |
| Android 环境检查 | `& .\Tools\Build-Android.ps1 -CheckEnvironmentOnly` | 检查 SDK/NDK/JDK，不构建 |
| Android 构建 | `& .\Tools\Build-Android.ps1` | 独立暂存构建，校验后写同级 `apk` |
| 规则/布局/引用编译 | `& .\Tools\Validate.ps1` | 运行 C#/PowerShell/Node 检查，不启动 Editor/客户端 |
| 图片与清单 | `& .\Tools\Validate-Assets.ps1` | 校验本地文件、来源与哈希；不是 Unity 导入 |
| 自动化验证 | `& .\Tools\Check-ContinuousDevelopment.ps1` | 沙箱测试与 dry-run，不注册计划任务 |
| UI 源稿导出 | `& .\Tools\Export-UIDesign.ps1 -SourcePath '<Docs/DesignUI 下的 SVG>'` | 实际调用 Inkscape，写导出图和收据 |
| 实际画面 | `& .\Tools\Validate-Visual.ps1 -Tag 'manual-review'` | 启动客户端，保存截图/运行证据 |
| 实际交互 | `Check-UiClicks.ps1`、`Check-Experience.ps1`、`Check-Routes.ps1` | 启动客户端/部分操作桌面；不是纯静态检查 |

`Common.ps1` 共享项目根目录、Unity 定位和进程参数处理。`Check-<功能>.ps1` 由综合验证按功能调用；先查 [源码导航](../Docs/Code-Map.md)，避免对文字改动反复跑游戏。

`Import-Asset.ps1`、`Import-S1Rag.ps1` 等导入命令会修改正式资源或生成数据，使用前按 [资源管线](../Docs/Asset-Pipeline.md) 核对输入。保留 `.meta` 和稳定 ID，不能借整理改写玩家存档。

`Start-ContinuousDevelopment.ps1` 会注册两条计划任务；`Tick-*.ps1` 会调用调度器；`Publish-Iteration.ps1` 涉及构建、桌面验收或安装。用户没有明确恢复要求时不要调用这些启动入口，也不要移除暂停文件或打开 `Automation/config.json` 中的开关。
