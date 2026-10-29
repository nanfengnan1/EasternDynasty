# 本地图片待导入区

这里只放已生成或已获得使用授权的本地 PNG 及来源记录，不是 Unity Resources，也不会随客户端自动加载。不要把《率土之滨》等参考游戏的截图或宣传素材导入运行资源。

完整命令、参数、导入设置、失败恢复流程见 [资源接入说明](../Docs/Asset-Pipeline.md)。先关闭该工程 Unity，再调用 `Tools/Import-Asset.ps1`；不要直接拷贝覆盖 `Assets` 中的 PNG 或删除 `.meta`。

- `asset-manifest.schema.json` 描述正式清单结构；CLI 和 Editor 还有路径、图片和文件系统校验。
- `portrait-request.example.json` 是填写参数的参考记录，不是已生成图片或可直接执行的导入任务。
- `.asset-import.lock`、`.asset-transaction/` 由工具管理。存在未完成事务时用 `Tools/Import-Asset.ps1 -Recover` 恢复，不要手动清除证据。

填写真实来源和权属，不能把“AI 生成”自动等同于任何具体商业授权。新增资源 ID 不会自动新增游戏中的武将。
