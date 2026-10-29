# 仓库源码副本（2026-10-29）

此工程从 F:\工作项目\率土之滨\source 复制，保留原开发目录。服务器仓库位置为 /home/alexan/program/github/EasternDynasty/source；另存一份到 X:\program\github\demo-project\EasternDynasty。

包含游戏源码、已有 Unity meta、Packages/ProjectSettings、正式资源、设计源稿和导出依据、构建工具与验证源代码。未复制 Unity 缓存、Builds、玩家存档、历史运行截图/验证产物、.local 机器配置、登录凭据或完整外部 RAG 库。Docs/DesignUI 内图片是设计依据，保留用于设计来源校验。

自动化配置保持 disabled；复制和 Git 提交不恢复后台任务，也不是游戏发布。历史文档中的 F 盘路径和证据路径说明原机器记录，不保证在新机器存在；旧验证收据不随这份源码副本全量搬运。

Unity 构建工具当前面向 Windows：在 Windows 安装 ProjectSettings/ProjectVersion.txt 指定版本后，从工程根目录执行 Tools/Setup.ps1 -SkipImport 生成本机配置，再按 README 构建。Linux 目录用于仓库管理，不表示 PowerShell/Unity Windows 构建流程已经移植。

仓库根 .gitattributes 将 source/** 设为 -text，防止 Git 换行转换破坏设计源稿、导出收据和正式资源的 SHA 校验。生成缓存与本机配置不入库。
