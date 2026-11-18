# 段誉模组 v0.3.0 开发预览版安装说明

这是 v0.3.0 开发预览版的安装与构建说明，版本下载见 [GitHub v0.3.0](https://github.com/cqbzly/sts2-duan-yu/releases/tag/v0.3.0)。目标环境为 Windows《杀戮尖塔2》v0.113.1 public-beta、BaseLib 3.4.8。新版增加两种状态牌、跨角色借招与主动消耗，完整A10已通关并复现，Release已安装到本机现有工坊目录；其他游戏版本和全部第三方模组组合尚未验证。

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

输出：`dist/DuanYu-0.3.0.zip`。构建与打包不会改动游戏目录。

若需要直接安装本地构建：

```powershell
.\scripts\install-local.ps1 -Sts2Path 'D:\steam\steamapps\common\Slay the Spire 2'
```

安装脚本只写 `mods/DuanYu`，覆盖同名旧文件前备份至仓库 `artifacts/install-backups`。不要同时安装多个段誉副本。

## 存档与故障排查

- 游戏内卡牌图鉴请选择段誉头像筛选。当前版本启用段誉卡池默认可见，包含全部88种牌、衍生剑气与状态；两种状态不可升级。原v0.1.1会按宿主“是否见过”隐藏未发现牌并禁止打开其详情。更新DLL并重启游戏后，再次打开图鉴即可生效，无需重建存档。
- 普通继续游戏沿用宿主的房间起点重载：战斗开始重新建立真气、剑势、凌波和战斗能力；事件访问与曾取得遗物的记录随玩家存档保存。剑势只在一场战斗内跨回合保留，战斗结束清空。
- 不承诺兼容第三方“战斗中途快照”工具。卸载前请结束含段誉的未完成游戏，否则存档引用的模型将缺失。
- 本机历史记录中 SavesMerger 有独立于本模组的初始化异常。遇到加载错误时，先只启用 BaseLib、Compatibility Helper、段誉进行比较。
- 日志通常位于 `%APPDATA%\SlayTheSpire2\logs\godot.log`。报告问题时提供游戏版本、段誉版本、模组列表、复现操作和相关错误片段。

## 发布状态

版本下载见 [GitHub v0.3.0](https://github.com/cqbzly/sts2-duan-yu/releases/tag/v0.3.0)，测试与安装证据见[运行验证记录](runtime-verification.md)。[Steam工坊条目](https://steamcommunity.com/sharedfiles/filedetails/?id=3841781260)沿用 private 可见性。v0.3.0的 `DUANYUFULLA10-048` 已完成headless原生A10沙漏路线及同种子新进程复现，最后首领战后72/72生命，见[完整 A10 验证](full-run-validation.md)。多人、第三方模组组合和订阅回装验证仍需完成。角色为《天龙八部》同人改编，与金庸权利人、Mega Crit 无官方关联。

本机已确认游戏进程退出，并将Release v0.3.0替换到 `D:\steam\steamapps\workshop\content\2868840\3841781260`；替换前备份为 `artifacts/install-backups/20261118-164444-before-v030`。DLL SHA256为 `7F371489CE5DD93A92C84EA69EB5386AA87E4ECE12B5073A433424697AF67B72`，IL检查确认没有 `DebugTests`，包含气滞和异种真气模型。不要再安装第二份到 `mods`；本地替换不等于重新上传Steam。

历史v0.2.1安装DLL的SHA256为 `6C5390A760B4FAB96E1F270C9BB4D89DFA0E7FF4E502DC80F840FCA1D43BF37A`，其替换前备份为 `artifacts/install-backups/20261118-161142-before-v021`；当前安装以v0.3.0段落的版本和哈希为准。
