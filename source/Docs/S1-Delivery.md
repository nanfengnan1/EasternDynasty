# S1 幽州功能与交付记录

本轮工作限定在 `F:\工作项目\率土之滨\source` 的幽州40×40地图。参考资料来自本机RAG固定快照，缺口已追加 `F:\工作项目\率土之滨\rag\question.md` 的 S1DEV 段；没有干预RAG采集、数据库和自动化任务。

## 可玩内容

- 新建幽州提供三个出生点，六座城池和三处渡河通道。四类资源地块采样真实本地参考图；城池为去固定UI文字的透明派生加工图。土地等级仍来自本项目布局，不把原截图猜成完整分级资源图集。
- 当前控制一支三将部队，尚未扩展多队列并行出征；可视化士卒、旗帜、陆路行军线及状态/兵力/倒计时。使用四邻格寻路，河流和山地不能直穿，桥梁可通过。当前原型允许经过未占领的可通行土地，但进攻目标仍必须与己方领地相邻。
- 驻守：派往己方可通行土地，抵达后持续驻扎；本格与周围八格中己方可通行领地显示绿色防卫覆盖。单人原型没有敌方玩家和驻守优先接敌战斗。
- 撤回：行军、驻守、屯田期间可撤回；现代路线保存当前进度，撤回不会瞬移回起点。返城后才可补兵和更换编队。旧版没有路径字段的行军仍可继续完成。
- 屯田：只能在普通己方资源地发起，城市、主城、桥梁不适用。先行军再作业，消耗2政令，作业90秒。预览目标地等级×600及周边八格己方资源地等级×150的对应资源；完成时按当前领地状态结算一次并返城。取消或失去目标不获得未完成收益，已花政令不退。
- 政令上限20，每3600游戏秒恢复1。上述时间、收益、覆盖范围和数值都是项目试玩配置，准确官方S1规则仍待资料验证。底栏推演速度影响在线游戏秒；离线最多七日。
- 245张资料卡与官网裁切立绘分汉、魏、蜀、吴、群、晋；招募页进入图鉴，可搜索、按阵营筛选、查看属性和战法文本。41张有明确招募入口证据的候选可在试玩池获得，204张只档案展示。金币200抽四星，灵玉100抽四至五星，按符合筛选的卡片等概率。晋没有符合条件的四星，空池不会扣款。

## 存档与规则边界

保留旧存档槽 `campaign.json` 和 `youzhou-40-v1.json`、原有字段和类型。新增可选 `seasonMechanicsVersion/commands/commandRecovery` 与订单 `action/phase/routeVersion/path/routeStart/routeEnd`。未含新字段的幽州存档首次推进时初始化20政令；旧订单按原方式完成。S1名称、阵营与卡ID独立保存，不合并同名不同星级卡片。新建试玩队伍为皇甫嵩、王子服、郑泰，不声称是原版官方新手赠将；旧存档队伍不迁移。

武将原始属性、自带战法名字和说明来自资料。战斗仍采用原型八回合公式，完整战法效果、拆解对应关系和触发顺序未实现；S1卡拆解被禁用，避免消耗卡片却不产生真实战法。城市坐标、地块等级、基础产量、招募定价和开服解锁条件也未达到官方全量复刻。

## 验证入口

```powershell
& .\Tools\Validate.ps1
& .\Tools\Validate-Assets.ps1
& .\Validation\AssetPipelineChecks.ps1
& .\Tools\Build-Windows.ps1
# Unity实际存档检查：ProjectTools.VerifyArchitecture，经Tools/Common.ps1调用
& .\Tools\Validate-Visual.ps1 -Tag s1-youzhou
& .\Tools\Validate-Visual.ps1 -Tag s1-legacy -Legacy
& .\Tools\Check-UiClicks.ps1
& .\Tools\Check-UiClicks.ps1 -S1
& .\Tools\Install-Build.ps1 -DestinationPath 'F:\工作项目\率土之滨\games'
```

规则检查覆盖路径不穿越不可通行地块、驻守持续、覆盖清理、撤回位置连续、屯田一次结算/取消/目标丢失、政令恢复/上限、非法订单与旧状态兼容；武将检查覆盖阵营、独立身份、有效招募证据、空池/缺款、随机候选可达和旧版隔离。Unity仓库检查增加驻守、屯田及返程各阶段的实际JSON落盘读回。

## 实际结果

2026-10-25 实际完成：

- `Tools/Validate.ps1`：146旧版规则、140幽州规则、70架构、118军队指令、143武将/卡池，共617项断言通过；Runtime/Editor静态编译通过。
- Unity实际导入256张ready资源（旧原型16张仍明确missing）；五个程序集编译及Windows构建成功。`ProjectTools.VerifyArchitecture` 通过56项JsonUtility实盘仓库检查，含驻守/屯田/部分返回、无新字段旧JSON兼容。
- 资源沙箱78项通过，包括新map-reference类型、原生240×340肖像、同时间同长度内容改变的哈希缓存保护、事务回滚与恢复。
- 1440×900：幽州S1的23个状态、旧版12个状态运行/截图回归通过。实际鼠标验证原有招募/编队/选地/小地图/缩放/拖动及S1占地→驻守→撤回→屯田→返回、图鉴与魏阵营筛选。首次S1鼠标回归遇一次操作系统焦点/截屏快捷键未接收，测试工具增加聚焦与有限重试后完整通过；没有以旧截图冒充新状态。
- 交付完整包到 `F:\工作项目\率土之滨\games\EasternDynasty.exe`，145个运行文件逐个SHA-256一致。原目标目录保留在同级 `games.previous-20261025-055907-b4fbe4`；没有删除旧目录。
- 直接从games交付目录运行1280×720测试，23个S1状态全部通过，正常退出；已目视检查图鉴、资源地图、驻守与屯田画面。玩家实际存档与测试前哈希一致。

证据：[`Validation/S1/delivery.json`](../Validation/S1/delivery.json)、[`core-and-static.log`](../Validation/S1/core-and-static.log)、[`unity-architecture.log`](../Validation/S1/unity-architecture.log)、[`s1-asset-pipeline-checks.txt`](../Validation/s1-asset-pipeline-checks.txt)、[S1实际鼠标状态与截图](../Validation/Visual/s1-interactive)、[安装目录实跑结果](../Validation/Visual/s1-installed-1280x720/result.txt)。

资料覆盖率、原图分辨率及完整官方玩法缺口不以“测试通过”替代：本轮仍不是官方S1完整复刻，山河桥梁、各等级地块、多队并行、完整战法和敌对驻军接敌需继续补资料与开发。
