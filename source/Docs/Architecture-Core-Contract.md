# Core、Application 与存档适配器契约

更新日期：2026-10-25。工程根目录：`F:\工作项目\率土之滨\source`。目标 Unity：6000.3.27f1。

本轮只调整职责和依赖方向，不修改画面、数值、地图定义或玩法。命名空间均保留 `EasternDynasty`。`World` 继续作为可变、可序列化的游戏状态；没有引入事件总线、领域框架或第二份经济缓存。

## 文件与边界

所有代码目录相对于 `Assets/EasternDynasty/Scripts/`。

| 文件 | 职责 |
| --- | --- |
| `Core/CampaignModels.cs` | Homeland、LandKind、Supplies、Land、ArmyOrder、BattleRecord 及其既有行为 |
| `Core/GeneralModels.cs` | GeneralDefinition、GeneralCard 及既有等级等规则 |
| `Core/Catalog.cs` | 既有地区、武将和显示名称静态目录 |
| `Core/World.cs` | World 序列化字段、地图属性与查询、order 兼容属性、消息记录 |
| `Core/World.Creation.cs` | 旧天下与幽州开局、地图生成、出生地规则 |
| `Core/World.Validation.cs` | 既有地图、模型、进度和资源校验 |
| `Core/World.Economy.cs` | 资源收益、时间推进和已有结算顺序 |
| `Core/World.Generals.cs` | 招募、编队、训练、进阶、拆解、学习 |
| `Core/World.Combat.cs` | 征兵、攻击条件、行军、守军和战斗结算 |
| `Core/YouzhouMapDefinition.cs` | 原幽州 40×40 定义，未修改 |
| `Application/ICampaignRepository.cs` | 存档仓库抽象，纯 C# |
| `Application/CampaignSession.cs` | 当前征程、注入时钟、离线推进和保存时机 |
| `Infrastructure/Persistence/JsonCampaignRepository.cs` | Unity JsonUtility、文件槽位、校验、备份与原子替换 |

依赖方向为：`Application → Core`，`Persistence → Application + Core + UnityEngine`。Core 与 Application 不引用 Unity、Runtime、Editor，不读取文件或 `Application.persistentDataPath`。Application 用 `System.IO.InvalidDataException` 表达无效存档，并不执行文件 I/O。

World 使用 partial 拆分；这些文件仍是同一个 CLR 类型，不是多个状态对象。序列化字段仅定义在 `World.cs`，没有新增序列化字段，也没有移除、重命名或改变已有字段类型。模型的 `[Serializable]`、新增地图元数据及桥字段的 `[OptionalField]` 均保留。

## 公开应用接口

```csharp
public interface ICampaignRepository
{
    bool Exists(bool youzhou);
    World Load(bool youzhou, out bool restoredBackup);
    void Save(World world);
}

public sealed class CampaignSession
{
    public const double MaximumOfflineSeconds = 7 * 86400;
    public CampaignSession(ICampaignRepository repository,
        Func<DateTime> utcNow, bool readOnly = false);
    public World World { get; private set; }
    public World StartYouzhou(int startIndex, string ruler);
    public World StartLegacy(Homeland origin, int region, string ruler);
    public World Load(bool youzhou, out bool restoredBackup);
    public void Save();
    public void Tick(double seconds);
    public bool HasSave(bool youzhou);
}
```

`World` 初始为空。仓库或时钟为 null 时构造函数抛 `ArgumentNullException`。生产时钟注入 `() => DateTime.UtcNow`；测试注入固定 UTC 时刻。

- **开始征程**：Start 调用已有 World 工厂，成功后替换当前 World 并返回同一引用；不自动写盘。调用方确定保存时调用 Save。参数无效、创建失败时保留原 World。
- **加载**：仓库必须返回独立快照；会话再次检查 `IsValid` 与所选地图槽位。通过后用 `savedUtcTicks` 计算离线时长，负数按 0，正数最多 7 天，调用已有 `World.Tick`，最后替换当前 World。时间戳为 0 或负数时不结算离线。未来时间戳不会倒退游戏时间。Load 本身不保存，也不更新时间戳。
- **加载失败**：当前 World 保持原引用。`restoredBackup` 先置 false，只有成功时传出仓库的实际回退结果。无效快照或槽位不符抛 `InvalidDataException`；仓库 I/O 异常不被会话吞掉。
- **保存**：没有 World 或 `readOnly=true` 时完全无副作用：不校验、不读取时钟、不更新时间戳、不调用仓库。其他情况先校验，更新 `savedUtcTicks` 后调用仓库；仓库抛异常时恢复原时间戳并继续抛出异常，避免显示未发生的成功保存时间。
- **推进**：Tick 委托已有 World.Tick；未开始征程时不做事。只读模式允许预览、时间推进和离线加载，只禁止持久化。
- **存在性**：HasSave 委托 Exists。存在并不等于内容有效；只有 Load 完成后才能认为可用。

Presentation 必须把当前引用更新为 `session.World`，避免开局或读档后仍使用旧对象。自动保存、退出保存、UI 错误提示由调用方控制。禁止在调用方再结算一遍离线收益。加载后若要确认保存新的进度，明确调用一次 Save；只读 smoke 环境不得调用其他路径写盘。

仓库和会话采用单客户端、主线程调用假设。仓库的实例锁保护同一实例的文件操作，不提供跨进程存档冲突解决；World 的公开集合仍可修改，也不是线程安全状态。不要在后台线程同时修改 World 与执行 Save。

## JSON 槽位与兼容

```csharp
public sealed class JsonCampaignRepository : ICampaignRepository
{
    public const string LegacyFileName = "campaign.json";
    public const string YouzhouFileName = "youzhou-40-v1.json";
    public JsonCampaignRepository(string saveDirectory);
    public bool Exists(bool youzhou);
    public World Load(bool youzhou, out bool restoredBackup);
    public void Save(World world);
}
```

构造参数必须是非空目录，内部转为绝对路径。构造、Exists、Load 不创建目录；Save 才创建。仓库不自行选择用户目录，不读取 Unity 的应用路径。

| 槽位 | 主文件 | 允许的存档 |
| --- | --- | --- |
| `youzhou=false` | `campaign.json` | v1 天下 36×27、972 格；继续接受旧 JSON 缺少地图字段的零/null 元数据 |
| `youzhou=true` | `youzhou-40-v1.json` | v2、`mapId=youzhou-40-v1`、40×40、1600 格、修订 1 |

文件名不从存档内的 mapId 拼接，避免未知地图落到任意路径。两种 World 均执行现有 `IsValid`；旧档不自动转换为幽州。地图类型与所请求槽位必须相同。

读取顺序是主文件、同名 `.bak`。每个候选都反序列化并完整校验。主文件损坏、元数据不兼容、槽位不符或 I/O 不可读时尝试备份。仅备份成功时返回 `restoredBackup=true`。两文件都不存在抛 `FileNotFoundException`；存在但均不可用抛包含候选失败原因的 `InvalidDataException`。加载不自动修复主文件，保持读取操作无写入。

保存先校验并生成 JSON，再在目标目录写唯一 `.<GUID>.tmp`，使用 `Flush(true)`。首次保存用同目录 `File.Move`；主文件已存在时用 `File.Replace`。已有主文件有效则把旧主文件轮换到 `.bak`；已有主文件内容无效则替换主文件但保留已有 `.bak`，避免将损坏内容覆盖到刚用于恢复的有效备份。读取现有主文件发生 I/O 错误时保存失败，不把无法核实的文件轮换为备份。

替换失败向调用方抛异常，不采用“删除主文件后重写”的降级方式。临时文件正常/失败后尽量清理；进程崩溃遗留的 `.tmp` 不参与 Exists 或 Load。Windows 本地文件系统的替换行为需要 Unity 实盘检查，静态编译不能证明其运行结果。

## 本轮已执行验证

1. 旧 `Validation/CoreChecks.cs`：**146 项通过**，覆盖 13 个出生地、武将、经济、行军战斗和离线结算。
2. 既有 `Validation/YouzhouChecks.cs`：**140 项通过**，包含三出生地、1600 格、连通、桥梁、地图元数据、CLR JSON 往返及已有旧版 JSON 文件加载。
3. 新 `Validation/ArchitectureChecks.cs`：**67 项通过**。内存仓库用独立序列化快照、固定 UTC 时钟，覆盖 World 公开序列化字段集合与类型、双槽、创建失败、2 小时/7 天/未来时间/小数秒离线、离线战斗与返城、只读不写、失败时间戳回滚、非法与错槽加载保留当前 World。
4. Core、Application、Persistence 按独立 DLL 编译，通过 Unity 6000.3.27f1 自身的引用程序集。Core 与 Application 编译未引用任何 UnityEngine DLL。

以上为分层实现阶段的独立检查，没有使用用户真实存档。最终集成已完成 Unity JsonUtility 实盘仓库39项检查、五个程序集编译、Windows构建及客户端回归；结果与证据见 [架构工作记录](Architecture-Worklog.md)。内存仓库的 CLR JSON 测试和实际 Unity 检查分别保留，不互相替代。

### Windows 中文路径验证方式

推荐使用项目的标准验证入口。根任务已在 F 盘中文路径再次执行并通过旧版 146 项、幽州 140 项、新架构 67 项；脚本使用 Unity 附带 Mono 编译，并将检查 exe 的**绝对路径**传给 Mono 执行，无需更换 Windows CLR 引用：

```powershell
# 在 F:\工作项目\率土之滨\source 执行。
& .\Tools\Validate.ps1
```

先前失败的是手工向 Mono 传入 `Validation/bin/ArchitectureChecks.exe` 相对路径的调用；不能据此判断标准验证脚本或中文工程目录不可用。遇到同类问题先使用上述入口或传绝对路径，不必移动工程或修改已验证的框架引用。

仅在特定手工调用仍失败时，可使用已验证的备选：由 Unity mono+csc 加 `/nostdlib+`，显式引用本机 Windows .NET Framework 4 的 `mscorlib.dll`、`System.dll`、`System.Core.dll`、`System.Runtime.Serialization.dll`，再由 Windows CLR 直接运行该次编译的检查 exe。不要把使用 Mono 默认库编译的 exe 直接交 Windows CLR；两者的 Queue 类型所在程序集可能不同。此备选只用于独立测试 exe；Unity 脚本静态检查继续引用该 Editor 的 UnityReferenceAssemblies 和 Managed/UnityEngine DLL。

## 交给根任务的 Unity 实盘检查矩阵

根任务拥有 Editor/Tools，可把下面检查写入 Editor 验证入口。每次仅使用 `Validation/RepositoryChecks/<GUID>/` 新建目录，向构造函数传该绝对路径。禁止传入、复制或写入 `Application.persistentDataPath`。每个场景独立子目录，结束后可保留结果与 JSON 便于复核，不需要删除目录。

| 场景 | 操作和必须验证的结果 |
| --- | --- |
| 空仓库 | 构造不创建目录；两个 Exists 均 false；Load 抛 FileNotFoundException |
| 双槽保存与读取 | 分别保存旧天下/幽州，检查文件名与 World 元数据；Load 返回不同实例，两个槽不互相覆盖 |
| 备份轮换 | 连续保存 A、B 两个不同有效进度，主文件读到 B，`.bak` 保存 A |
| 损坏主文件恢复 | 将测试主文件替换成无效 JSON；Load 返回 A、restoredBackup=true；Load 前后主/备份字节不变 |
| 恢复后的保存 | 对上述已恢复状态保存；主文件重新有效，原有效 `.bak` 的字节保留，不能变成损坏 JSON |
| 主文件缺失 | 仅保留 `.bak`，Exists=true，Load 可恢复且指示备份；再 Save 可建立主文件 |
| 元数据与错槽 | 将合法幽州 JSON 放入旧槽；或修改 v2 尺寸/修订；必须拒绝该候选，仅可回退到同槽合法备份 |
| 两者损坏 | 两个候选都不合法，必须抛 InvalidDataException，不能创建新游戏、静默覆盖或返回半有效对象 |
| 无效状态拒写 | 对无效 World 调用 Save，必须在修改任何已有主/备份前失败，文件字节保持不变 |
| 写入/替换失败 | 在隔离目录中用独占 FileStream 锁定目标文件模拟冲突；Save 应抛 I/O 异常，原有效存档仍可读，释放锁后可再次 Save |
| 临时文件无关 | 放入孤立 `.tmp`，Exists/Load 不将其当存档；正常 Save 后不遗留本次 `.tmp` |
| JsonUtility 往返 | 对两张地图空闲、出征、返城、已返城状态逐一 Save/Load；IsValid、orders 空/非空、地块和卡牌内容一致 |
| 会话与实盘组合 | 使用固定时钟的 CampaignSession 加真实仓库，只离线结算一次；Save 更新时间戳；只读会话 Save 不触碰文件字节或时间戳 |

实盘检查完成后，根任务应把实测通过项和证据路径写入整体架构验证报告。未运行的检查不能标为通过。
