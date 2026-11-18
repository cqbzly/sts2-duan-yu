# 段誉角色：本地环境与创意工坊发布条件检查

> 历史记录：本文保留只有设计稿时的检查结论。当前实现与验证状态请见[实现与测试记录](implementation-and-testing.md)及[兼容性与发布检查](compatibility-and-publishing.md)。

检查日期：2026-11-17。根据用户选择，本次只检查现有环境与发布条件，没有实现模组、启动游戏、安装工具、改变模组启用状态、修改存档或上传创意工坊。

## 结论

1. **当前段誉项目不能作为模组运行。** 仓库只有设计文档、卡牌JSON及文档校验脚本，没有角色程序、模组清单、DLL或角色资源包。游戏已经安装并不等于段誉模组已经可用。
2. **不能保证与其他模组无冲突。** 已检查安装清单与依赖，但段誉尚无代码，无法分析其实际补丁或进行联合运行测试。
3. **不能判断段誉模组没有Bug。** 当前能验证的是设计数据完整性；不存在可供执行的游戏实现。现有游戏历史日志还记录了其他模组的初始化异常。
4. **《杀戮尖塔2》已经正式支持Steam创意工坊。** 有官方上传工具；段誉须先实现并打包，才能作为可玩的角色模组发布。上传成功本身不能证明运行与兼容性合格。

## 本地游戏与开发环境

| 项目 | 本次观察 |
| --- | --- |
| Steam安装目录 | `D:\steam`。 |
| 游戏目录 | `D:\steam\steamapps\common\Slay the Spire 2`。 |
| Steam App ID | `2868840`，确认是二代。 |
| 游戏版本 | `v0.113.1`，release commit `d09d48e5`。 |
| Steam Build ID | `26660232`。 |
| Steam分支 | `public-beta`；不能据此声称正式分支兼容。 |
| 游戏运行框架 | `net9.0`，随游戏包含 `Microsoft.NETCore.App 9.0.7`。 |
| Godot运行环境 | 历史日志显示 `MegaDot v4.5.1.m.14.mono.custom_build`；`GodotSharp.dll`文件版本为4.5.1.0。 |
| Harmony | 游戏自带 `0Harmony.dll`，文件版本2.4.2.0。 |
| 当前dotnet命令 | 仅列出.NET 7.0.20运行时，没有发现SDK。 |
| Godot开发编辑器 | 当前PATH没有找到`godot`或`godot4`命令；未进行全盘搜索。 |
| 当前进程 | 检查时Steam正在运行，未发现《杀戮尖塔2》进程。 |
| 最近可见游戏日志 | `godot.log`最后写入2026-11-15 16:00:20，属于此前运行记录。 |

缺少SDK阻碍本机编译模组，**不表示玩家运行游戏需要另装.NET 9**：游戏已经携带自身运行时。后续开发需要配置能够构建目标框架的SDK，以及与目标游戏版本相匹配的资源工具链；不能把运行时DLL等同于完整开发环境。

证据文件：

- `D:\steam\steamapps\appmanifest_2868840.acf`
- `D:\steam\steamapps\common\Slay the Spire 2\release_info.json`
- `D:\steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.runtimeconfig.json`
- `%APPDATA%\SlayTheSpire2\logs\godot.log`
- 本次只读命令`dotnet --info`与进程枚举结果。

## 已安装模组与启用状态

工坊目录有**11个项目、12份模组清单**，原因是BaseLib项目还包含Compatibility Helper。当前保存的启用配置列出8个启用项，不能把所有已安装项目都描述成正在加载。

| 工坊项目ID | 模组ID / 名称 | 当前安装版本 | 最低游戏版本 | 当前启用列表 |
| --- | --- | --- | --- | --- |
| 3737322022 | QuickRestart | v2.0.0 | 0.107.0 | 已列入并启用。 |
| 3737335127 | BaseLib | v3.4.8 | 0.107.1 | 已列入并启用。 |
| 3737335127 | BaseLibCompatibilityHelper | v0.0.0 | 0.107.0 | 已列入并启用。 |
| 3747497501 | RegentFX / 万象辉星 | 0.5.2 | 0.103.0 | 未列入该启用列表。 |
| 3747530622 | LexNinja2 / 蕾忍 | v3.1.0-beta-0-g80bc145 | 0.113.0 | 未列入该启用列表。 |
| 3747532120 | SavesMerger | 1.0.0 | 未声明 | 已列入并启用。 |
| 3747557003 | ModConfig / 皮皮配置 | 0.2.4 | 未声明 | 已列入并启用。 |
| 3747557283 | DamageMeter / 皮皮统计 | 1.16.1 | 未声明 | 已列入并启用。 |
| 3747602295 | STS2-RitsuLib / RitsuLib | 0.6.14 | 0.107.1 | 已列入并启用。 |
| 3747656691 | jijia / 机甲战士 | 2.0 | 未声明 | 未列入该启用列表。 |
| 3748603697 | Mesugaki / Mesugaki_Regent | 0.2.0 | 未声明 | 未列入该启用列表。 |
| 3763265055 | AscensionUnlocker | 1.1.0 | 0.107.0 | 已列入并启用。 |

检查结果：

- 未发现重复模组ID。
- 清单中的4条显式依赖条件均满足：QuickRestart需要BaseLib≥3.4.5；BaseLib需要CompatibilityHelper≥0.0.0；LexNinja2需要BaseLib≥3.4.8和RitsuLib≥0.6.11。
- 游戏版本不低于各清单声明的最低版本；这只是声明检查，不是对实际API兼容性的证明。没有写最低版本或最高版本也不能推导为全版本兼容。
- `mods\AscensionUnlocker`只有历史日志，没有第二份清单或DLL，因此此次未发现本地与工坊重复载荷。
- 历史日志加载的是RitsuLib0.6.13；当前文件已更新为0.6.14。不能把11月15日的运行结果直接当成今天全部安装文件的运行结果。

证据范围：`D:\steam\steamapps\workshop\appworkshop_2868840.acf`、`content\2868840`各项目清单，以及游戏设置中单独读取的`mods_enabled`、`mod_settings`字段。报告不包含完整存档或账号标识。

## 已发现的现有问题

### SavesMerger初始化异常

11月15日的`godot.log`第99行开始记录：

```text
Exception thrown when calling mod initializer of type UnifiedSavePath.UnifiedSavePathMod
HarmonyLib.HarmonyException: Ambiguous match for HarmonyMethod[
  class=MegaCrit.Sts2.Core.Saves.UserDataPathProvider,
  methodname=GetProfileDir, type=Normal, args=undefined
]
System.Reflection.AmbiguousMatchException
```

堆栈明确指向SavesMerger对`GetProfileDir`的Harmony匹配出现歧义，补丁没有提供可唯一匹配的参数签名。之后虽有通用的“Finished mod initialization”日志，也不能据此否认前面的初始化异常。

这发生在段誉代码不存在的情况下，不能归因于段誉。后续实测前，应先取得适配当前版本的SavesMerger，或在隔离测试配置中停用它，建立可比较的基线。本次没有执行这些变更，也没有复现今天的启动结果。

### 机甲战士包声明与文件不一致

`content\2868840\3747656691\jijia.json`声明`has_dll=true`，但目录中未找到`jijia.dll`，只见JSON、PCK与占位README。

这是需要复核的打包异常候选；本次未启用或运行它，不能声称已经观察到加载失败，也不把它列为段誉引发的冲突。

### 其他历史错误

同一份日志还出现`Invalid Task ID`、多人消息没有注册处理器、断开连接时发送消息，以及退出时资源未释放等错误。日志末尾存在正常退出与Steamworks关闭记录。

这些记录值得在今后的运行基线中留意，但本次没有做无模组对照、逐个启用或复现，无法可靠归因于游戏本体、某个模组或某个模组组合。不能仅凭一条退出资源警告断言存档损坏或游戏不能启动。

## 段誉实现后必须验证的交互

| 组合 / 范围 | 重点 |
| --- | --- |
| BaseLib、CompatibilityHelper、RitsuLib | 选择并固定所需依赖，验证内容注册、回合钩子、补丁顺序、序列化与版本分支。不要因为它们已安装就无条件全部作为依赖。 |
| QuickRestart | 重载后真气、凌波、剑式集合、每回合次数与生成牌准确恢复，不能通过重载重复领取一次性收益。 |
| SavesMerger | 先消除已知基线异常，再测自定义角色、卡牌与进度的保存、读回和卸载后的行为。 |
| DamageMeter | 六剑分别统计、多段与耗气附加伤害归因、凌波在格挡前减伤、支援格挡归属。区分统计错误与实际战斗结算错误。 |
| AscensionUnlocker | 新角色进度与进阶解锁不能覆盖或污染其他角色数据。 |
| ModConfig | 配置键、热键与设置存储使用独立命名空间。 |
| LexNinja2等其他角色模组 | 一起启用时验证角色注册、卡池隔离、资源路径和联机归属。当前未启用状态不能证明联合兼容。 |
| RegentFX、jijia、Mesugaki等表现相关模组 | 如需共同启用，验证补丁和资源覆盖范围；作者描述或`affects_gameplay=false`不能作为无冲突认证。 |
| 正式分支与public-beta | 分别构建或明确支持范围，记录具体游戏与依赖版本。 |

建议段誉使用独立模组ID，例如`cqbzly.sts2.duanyu`，同时隔离类命名空间、资源路径、本地化键和存档字段。优先使用宿主或选定框架提供的扩展点，只有确有需要才补丁全局逻辑。以上是后续实现要求，本次没有声称这些隔离措施已经实现。

## Steam创意工坊支持与发布要求

官方在2026-06-19的**Major Update #2 - v0.107.1**中明确宣布二代已加入Steam Workshop支持，并链接官方上传器。2026-06-20的官方六月Neowsletter再次说明游戏有内置模组加载器，工坊用于浏览和安装模组。

- [二代官方工坊](https://steamcommunity.com/app/2868840/workshop/)
- [官方Major Update #2公告](https://steamstore-a.akamaihd.net/news/externalpost/steam_community_announcements/1835871199305790)
- [官方六月Neowsletter](https://steamstore-a.akamaihd.net/news/externalpost/steam_community_announcements/1835871199312310)
- [官方上传器megacrit/sts2-mod-uploader](https://github.com/megacrit/sts2-mod-uploader)
- [上传器工作目录模板](https://github.com/megacrit/sts2-mod-uploader/blob/main/template/README.md)
- [上传器v0.2.0发布页](https://github.com/megacrit/sts2-mod-uploader/releases/tag/v0.2.0)

官方模板规定的准备结构：

```text
workshop-workspace/
  content/
    <实际模组清单与构建产物>
  workshop.json
  image.png
```

- `content/`至少需要有效模组JSON；可玩的自定义角色还需要实际角色实现和资源，通常以DLL/PCK提供。本仓库的`data/cards.json`不是可供游戏加载的模组清单。
- `workshop.json`包含标题、描述、可见性、标签、更新说明与依赖。
- `image.png`为工坊封面，按官方模板要求小于1MB。
- 可见性支持`private`、`public`、`unlisted`、`friends_only`，官方模板默认`private`，适合先做测试条目。
- `dependencies`填写工坊数字项目ID，不是代码里的模组ID；还要与模组自身依赖声明保持一致。
- 官方模板提到`minBranch`、`maxBranch`的API行为存在问题，建议在网页编辑支持分支。当前只检查到public-beta环境，不能声称正式分支也经过验证。

准备完成后的官方命令为：

```powershell
ModUploader.exe upload -w <workspace-folder>
```

新建条目后保存`mod_id.txt`，后续用相同工作目录更新原条目。上传器使用**当前Steam客户端登录账号**；GitHub账号`cqbzly`及其令牌不代表Steam上传账号或发布权限。Steam可能要求接受工坊协议，上传器对此有检测与提示。

本次没有下载或运行上传器，因此没有验证当前Steam账号的实际上传权限、协议状态或一次真实上传。平台支持已确认，段誉构建尚未准备好，账号发布验收也尚未执行。

## 当前验证记录与下一步

本次运行`node scripts/validate.mjs`通过：86种卡牌、稀有度分布、10张初始牌组、衍生引用和生成文档一致。该脚本不运行游戏、不执行卡牌效果，也不验证联机、存档或Harmony补丁。

后续实施顺序建议：

1. 固定目标游戏分支、SDK与依赖版本，实现可加载的角色最小版本。
2. 在隔离的测试配置中验证加载、选角、开局、战斗、保存与继续游戏。
3. 实现并验收真气、凌波、六剑及完整内容，尤其测试多段伤害、耗气失败、满手、复制与重启。
4. 先解决或隔离既有环境异常，再逐组加入本机现有模组，测试单人及多人。
5. 形成明确的已测游戏版本、依赖版本和已知问题清单，准备完整工坊包，再进行私有条目上传与订阅回装验证。

本报告只保存在本地工作区；没有把个人环境、存档或日志上传到GitHub或Steam。
