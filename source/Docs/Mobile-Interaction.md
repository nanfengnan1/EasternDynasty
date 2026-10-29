# Android 触控与屏幕适配

本次面向S1幽州40×40单人试玩。Android APK的SDK、构建和签名见 `Android.md`；本页记录运行时输入与生命周期行为，不能代替真机验收。

## 操作

- 单指轻点选择土地或打开主城；移动超过按屏幕DPI计算的阈值后拖动地图，松手不再同时选地。
- 两指张开/合拢缩放，并可随两指中心平移。双指手势结束后，剩余一根手指不会误点；全部松开后才能开始下一次操作。
- 手势必须从地图未被UI覆盖的区域开始。从按钮或弹窗滑入地图不会触发行军或选地；手势进入遮挡区即取消，第三根手指和系统取消事件也不会留下点击。
- 手机上的触摸使用独立输入分支；不读取触摸模拟的右/中鼠标按钮，也不启用悬停边缘滚动。桌面鼠标、滚轮和WASD行为保持原样。
- 武将、卡库、长战法说明、城建详情和任务列表支持单指滑动。滑动时取消原按钮的按压状态，防止松手误开卡牌；原滚动条仍可使用。目前没有惯性滚动。

## 布局与文字

所有HUD、弹窗、地图视口、世界坐标标签和遮挡框使用同一安全区域转换。`Screen.safeArea` 的左下坐标被转换为IMGUI左上坐标；安全区外绘制背景，关键按钮不靠刘海/系统导航区域。移动端采用最少1440逻辑宽、820逻辑高的整体适配，保持现有固定布局并略放大手机文字；桌面仍为原来的1440×900参考。

移动端优先加载随包的 `Resources/Fonts/CJK`。本轮根任务提供Noto Sans SC字体及OFL许可，字体源记录在 `AssetInbox/font-cjk.source.json`。仅在资源缺失时使用设备的Noto/Droid/sans-serif字体回退，并产生明确警告。

## 返回与保存

Android返回依次取消确认框、关闭卡牌详情或抽卡结果、从图鉴返回招募、从其他页面返回地图、关闭已选土地面板。地图根页面返回时弹出退出确认；保存失败不会执行退出。软键盘可见或刚关闭的帧会先留给键盘处理。

进入后台时保存普通会话并取消当前触摸。恢复时按真实经过时间结算一次，最多七日，不受在线时间倍速影响；跳过恢复后的首帧增量，避免重复计算后台时长。连续重复暂停/恢复事件不会重复结算。`-edSmoke`、`-edReview` 仍是只读会话，不写玩家存档。

## 验证入口及证据范围

```powershell
& .\Tools\Check-MobileInput.ps1
```

已执行：61项纯C#检查，覆盖手指抖动/拖动/取消、UI起点、跨HUD、双指缩放和指序变化、第三指、剩余手指不误点、安全区坐标、平板/低分辨率布局、返回层级、真实时间恢复、防重复及七日上限，日志为 `Validation/mobile-input-checks.log`。运行时源码也已使用本机Unity 6000.3.27f1引用程序集、`UNITY_ANDROID` 编译标志静态编译通过（响应文件 `Validation/mobile-static.rsp`）。没有启动Unity、模拟器或手机来把这些结果冒充设备验证。

根任务可在独立桌面只读冒烟会话追加 `-edMobileReview`，例如：

```powershell
# 由根任务在没有并行Player时启动；普通游戏不要使用这些测试参数。
Builds\Windows\EasternDynasty.exe -edSmoke -edMobileReview -screen-fullscreen 0 -screen-width 1920 -screen-height 864 -logFile Validation\mobile-player.log
```

该模式启用移动布局和随包字体，并模拟左右与底部安全区域；仍执行现有29个幽州截图状态。仅用于桌面视觉核验，不模拟真实手指或声称Android安装启动已通过。

真机仍需核验：单指/双指操作、卡库滑动及详情返回、系统软键盘中文输入、左右横屏切换、Android返回手势、锁屏/切后台再恢复、重启后的存档继续。当前没有连接的安卓设备，最终交付应明确这项限制。

官方API依据：Unity的[触摸鼠标模拟](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Input-simulateMouseWithTouches.html)说明多个触点可转成不同鼠标按钮；[安全区域](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Screen-safeArea.html)以Player窗口和左下角为坐标基准；[动态系统字体](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Font.CreateDynamicFontFromOSFont.html)可按设备已安装字体回退。

主要文件：`MobileMapGesture.cs`、`MobileMapInput.cs`、`MobileScreenLayout.cs`、`MobileNavigation.cs`、`MobileSuspendClock.cs`、`MobilePresentation.cs`；均位于 `Assets/EasternDynasty/Scripts/Runtime`。跨平台规则/存档字段未修改。
