# 40×40 幽州核心实现与验证

日期：2026-10-25。用户已明确授权40×40幽州重构；该尺寸取代此前128×128工程提案。本次只实现核心数据、校验和核心测试，没有修改UI、MapView、Editor或Tools，没有启动Unity，没有读取/写入用户 campaign 存档。

## 交付文件

- [World.cs](../../Assets/EasternDynasty/Scripts/Core/World.cs)：新旧地图上下文、幽州工厂、地图校验、四邻查询、桥梁收入语义。
- [YouzhouMapDefinition.cs](../../Assets/EasternDynasty/Scripts/Core/YouzhouMapDefinition.cs)：独立幽州规划布局。
- [YouzhouChecks.cs](../../Validation/YouzhouChecks.cs)：新增核心检查。
- [CoreChecks.cs](../../Validation/CoreChecks.cs)：未修改，原146项检查全部保留。

## 可供UI与渲染使用的接口

| 接口 | 行为 |
|---|---|
| `World.Create(origin, region, ruler)` | 保留旧36×27/13区地图和原调用方式 |
| `World.CreateYouzhou(int startIndex = 0, string ruler = "新任太守")` | 创建独立40×40幽州；非法出生索引抛ArgumentOutOfRangeException |
| `world.IsYouzhou` | 仅当saveVersion=2且mapId匹配幽州标识时为true；载入后仍须先调用IsValid |
| `world.MapWidth / MapHeight` | 幽州40×40，旧图36×27；不会按不可信存档宽高分配任意地图 |
| `world.MapName` | 幽州或天下 |
| `world.RegionName(int)` | 当前地图内地区名，非法索引返回“未知地区” |
| `world.TryGetLand(int x,int z)` | O(1)坐标查格，越界或缺格返回null，无跨行取邻居 |
| `YouzhouMapDefinition.StartNames` | 蓟城近郊、涿郡平原、渔阳河谷；默认索引0 |
| `YouzhouMapDefinition.StartX/StartZ(index)` | 三处出生坐标，非法索引抛异常 |
| `Land.bridge` | 新增公开bool；普通旧格缺省false |

## 地理与桥梁契约

1600格按 `id=z*40+x` 固定编号。该地图是玩法规划示意，不是历史地理复原；没有把旧36×27天下图缩放或平铺。

| 内容 | 当前配置 |
|---|---|
| 出生点 | (9,16)、(7,6)、(30,18)；全部汉地文化 |
| 6座城 | 蓟城(11,18)、涿城(9,7)、渔阳城(28,20)、上谷城(8,30)、右北平城(33,30)、辽西城(32,9) |
| 山脊 | 北部与西北部；城池/出生不会清除不可走山水 |
| 河流 | 中东部南北贯通、每行2格宽，中心随z缓变 |
| 桥梁 | z=9、19、30各一处，每处2格，分别称南渡桥、蓟东桥、北渡桥 |
| 水/桥格数量 | 74格不可通行Water + 6格可通行bridge |

桥梁使用 **`bridge=true + kind=Grain + Walkable=true`**，不是可走Water。UI与renderer必须先检查bridge：画桥、显示桥名，不画粮田，不按粮田显示每小时产粮。桥梁仍可按普通据点占领，计入领地数；不提供被动资源收入，战斗仍保留一次性金币/经验奖励。行军依旧复用现有直线时长和邻格扩张规则，本次没有引入地形寻路。

全部6城均为汉地，不以虚构异文化城强行演示跨文化卡池解锁。出生选择不改变底图，只改变玩家主城/归属。

## 存档元数据与验证

新增字段 `mapId / mapWidth / mapHeight / mapRevision`。幽州固定：

```text
saveVersion = 2
mapId = youzhou-40-v1
mapWidth = 40
mapHeight = 40
mapRevision = 1
```

旧v1缺省这些字段仍按36×27校验；也允许明确的legacy-campaign-v1及旧尺寸。新字段使用OptionalField支持CLR版本容错；Unity对新增字段的实际JSON行为仍须在Editor内验证，不能用本轮CLR测试替代。

IsValid拒绝未知地图、错误版本/尺寸/修订、新旧地图混装、不完整/重复/错坐标Land、非法地区/资源类型/文化/等级、幽州异文化地块、不一致桥梁、重复主城或主城归属错误、幽州城数不符、无效阵容/卡牌、越界/不可走目标及非法行军时间、非有限或负资源。

校验不会自动改写版本，不会把旧ID按40宽解释，也不从土地数量猜宽度。独立幽州保存槽、入口、备份与退出保存由主代理在应用层接入；Core不直接进行文件保存。

## 查询与收入

AttackProblem的相邻己方土地查询改成四次TryGetLand，保持曼哈顿四邻规则，避免id±1在边缘跨行。收入采用一次完整遍历，同时计算普通地收入和城池加成，排除主城与桥梁；没有引入缓存，因此现有公开owned/level/capital字段直接变化会立即反映在收入中。

OwnedCount与收入仍按数据扫描，1600格阶段保留正确性。UI可降低显示刷新频率；若未来做更大规模缓存，应先收拢所有权和等级修改入口，不把缓存变成另一份存档真相。

## 实际测试结果

使用已安装Unity 6000.3.27f1的Mono/csc运行纯核心测试，未启动Unity。

```text
PASS: 146 checks across 13 starts, recruitment, progression, skill transfer,
economy, marches, battles, offline settlement and unlock rules.

PASS: 140 Youzhou checks; 3 starts, 1600 tiles, 6 Han cities,
74 water tiles, 6 bridge tiles, topology, neighbors, economy,
metadata rejection and CLR JSON roundtrips.
```

新增检查覆盖：三处出生都能到达6城和全部桥格；去除桥后无法跨河；四邻角落/边缘/跨行/越界；出生不重生成地理；桥不产粮；直接改owned/level后收入准确；汉地城不开放外文化池；离线大步长与逐秒推进一致；未知/错配地图和损坏记录被拒绝；CLR JSON驻守、出征、返城、回城四状态往返。

还使用CLR JSON读取了既存 `Validation/serialization-{idle,outbound,returning,returned}.json` 四份Unity v1样本，均按旧36×27恢复并通过校验。此项证明当前验证器能接收这些旧数据，**不代表本轮运行过Unity JsonUtility**。

测试产物为 `Validation/bin/CoreChecks.YouzhouCompatibility.exe` 与 `Validation/bin/YouzhouChecks.exe`。编译旧核心测试时需同时提供World.cs与YouzhouMapDefinition.cs；新检查另需引用System.Runtime.Serialization.dll，并传项目根目录作为参数来检查既存v1样本。已通知主代理更新Tools侧核心源码清单，本代理未修改Tools。

## 后续集成检查

1. 新入口和独立幽州slot保持旧campaign与其备份不变，测试/预览继续禁止保存用户进度。
2. UI/renderer按bridge优先显示桥梁，无被动资源收益；地区文案通过RegionName，不再索引旧Catalog.Regions。
3. Editor实际执行Unity JSON新旧四状态校验，再做客户端方格拾取、跨块、出生切换和桥梁表现验证。
4. 上述核心测试通过只说明数据/规则与兼容性，不作为幽州资源美术或武将卡画质验收。
