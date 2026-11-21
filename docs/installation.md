# 段誉模组安装说明

已发布版本为 v0.5.21。目标环境为 Windows《杀戮尖塔2》v0.113.1 public-beta，依赖 BaseLib 3.4.8。发布包与源码分支的进度分别见 [GitHub Releases](https://github.com/cqbzly/sts2-duan-yu/releases/latest) 和[卡牌图鉴](cards.md)。

## 玩家安装

1. 在 Steam 创意工坊订阅 [BaseLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127)，在游戏中启用 BaseLib 及其随包的 Compatibility Helper。
2. 订阅[大理世子·段誉](https://steamcommunity.com/sharedfiles/filedetails/?id=3841781260)。手动安装时，关闭游戏，从 [Release](https://github.com/cqbzly/sts2-duan-yu/releases/latest) 下载 ZIP，将其中的 `DuanYu` 文件夹解压到游戏的 `mods` 目录。该文件夹应直接包含 `CQBZLY_DUAN_YU.dll` 和 `DuanYu.json`。
3. 启动游戏，在模组列表中启用“大理世子·段誉”，按提示重启，再新开一局选择段誉。

玩家不需要单独安装 .NET SDK 或 Godot 编辑器。图片、图标与场景由DLL在本机生成，缓存在游戏用户目录 `cqbzly.duanyu` 下；不会从互联网下载图片。

多人游戏的所有参加者需要相同游戏分支、相同版本的段誉与依赖，以及一致的影响玩法的模组列表。玩家状态隔离已实现，但完整远程多人会话尚需验证。

## 从源码构建

安装 .NET 9 SDK，运行：

```powershell
.\scripts\build.ps1 -Sts2Path 'D:\steam\steamapps\common\Slay the Spire 2'
.\scripts\package.ps1 -Sts2Path 'D:\steam\steamapps\common\Slay the Spire 2'
```

安装包生成在 `dist/DuanYu-<版本号>.zip`，版本号取自 `DuanYu.json`。构建与打包不会改动游戏目录。

若需要直接安装本地构建：

```powershell
.\scripts\install-local.ps1 -Sts2Path 'D:\steam\steamapps\common\Slay the Spire 2'
```

安装脚本只写 `mods/DuanYu`，覆盖同名旧文件前备份至仓库 `artifacts/install-backups`。不要同时安装多个段誉副本。

## 存档与故障排查

- 游戏内卡牌图鉴请选择段誉头像筛选。当前版本启用段誉卡池默认可见，包含全部96种牌、衍生剑气与状态；两种状态不可升级。原v0.1.1会按宿主“是否见过”隐藏未发现牌并禁止打开其详情。更新DLL并重启游戏后，再次打开图鉴即可生效，无需重建存档。
- 普通继续游戏沿用宿主的房间起点重载：战斗开始重新建立真气、剑势、凌波和战斗能力；事件访问与曾取得遗物的记录随玩家存档保存。剑势只在当前回合保留，玩家下回合开始时清零，战斗结束也清空。
- 不承诺兼容第三方“战斗中途快照”工具。卸载前请结束含段誉的未完成游戏，否则存档引用的模型将缺失。
- 本机历史记录中 SavesMerger 有独立于本模组的初始化异常。遇到加载错误时，先只启用 BaseLib、Compatibility Helper、段誉进行比较。
- 日志通常位于 `%APPDATA%\SlayTheSpire2\logs\godot.log`。报告问题时提供游戏版本、段誉版本、模组列表、复现操作和相关错误片段。

## v0.5.21 发布记录

六脉齐发每支付 1 点真气，就对同一目标打出一段伤害，各段独立应用增伤和减伤。该版通过 24 项规则测试、464 项游戏引擎回归与 128 项资源检查。[更新记录](releases/v0.5.21.md) · [引擎报告](https://github.com/cqbzly/sts2-duan-yu/releases/download/v0.5.21/v0521-final-engine-tests.json)

手动更新前先关闭游戏。Steam 订阅和手动安装任选一种，避免游戏同时加载同 ID 的两份模组。角色为《天龙八部》同人改编，与金庸权利人、Mega Crit 无官方关联。
