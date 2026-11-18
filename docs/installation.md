# 段誉模组 v0.1.1 安装说明

这是开发预览版，目标环境为 Windows《杀戮尖塔2》v0.113.1 public-beta、BaseLib 3.4.8。其他游戏版本和全部第三方模组组合尚未验证。

## 玩家安装

1. 在 Steam 创意工坊安装并启用 BaseLib 及其随包的 Compatibility Helper。
2. 关闭游戏，将安装包中的整个 `DuanYu` 文件夹放到游戏的 `mods` 目录。例如 `D:\steam\steamapps\common\Slay the Spire 2\mods\DuanYu`。
3. 确认目录中直接包含 `CQBZLY_DUAN_YU.dll` 与 `DuanYu.json`，避免多套一层文件夹。
4. 启动游戏，在模组列表中启用“大理世子·段誉”，按游戏提示重启。新建游戏时选择段誉。

玩家不需要单独安装 .NET SDK 或 Godot 编辑器。图片、图标与场景由DLL在本机生成，缓存在游戏用户目录 `cqbzly.duanyu` 下；不会从互联网下载图片。

多人游戏的所有参加者需要相同游戏分支、相同版本的段誉与依赖，以及一致的影响玩法的模组列表。玩家状态隔离已实现，但完整远程多人会话尚需验证。

## 从源码构建

安装 .NET 9 SDK，运行：

```powershell
.\scripts\build.ps1 -Sts2Path 'D:\steam\steamapps\common\Slay the Spire 2'
.\scripts\package.ps1 -Sts2Path 'D:\steam\steamapps\common\Slay the Spire 2'
```

输出：`dist/DuanYu-0.1.1.zip`。构建与打包不会改动游戏目录。

若需要直接安装本地构建：

```powershell
.\scripts\install-local.ps1 -Sts2Path 'D:\steam\steamapps\common\Slay the Spire 2'
```

安装脚本只写 `mods/DuanYu`，覆盖同名旧文件前备份至仓库 `artifacts/install-backups`。不要同时安装多个段誉副本。

## 存档与故障排查

- 普通继续游戏沿用宿主的房间起点重载：战斗开始重新建立真气、凌波和战斗能力；事件访问与曾取得遗物的记录随玩家存档保存。
- 不承诺兼容第三方“战斗中途快照”工具。卸载前请结束含段誉的未完成游戏，否则存档引用的模型将缺失。
- 本机历史记录中 SavesMerger 有独立于本模组的初始化异常。遇到加载错误时，先只启用 BaseLib、Compatibility Helper、段誉进行比较。
- 日志通常位于 `%APPDATA%\SlayTheSpire2\logs\godot.log`。报告问题时提供游戏版本、段誉版本、模组列表、复现操作和相关错误片段。

## 发布状态

安装包可以本地加载；GitHub安装包见[Releases](https://github.com/cqbzly/sts2-duan-yu/releases)。Steam首发使用私有测试条目，条目状态见[发布记录](releases/v0.1.1.md)。完整多人、第三方模组组合和订阅回装验证仍需完成。角色为《天龙八部》同人改编，与金庸权利人、Mega Crit无官方关联。
