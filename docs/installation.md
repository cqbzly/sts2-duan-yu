# 段誉模组 v0.5.8 安装说明

这是 v0.5.8 的安装与构建说明。目标环境为 Windows《杀戮尖塔2》v0.113.1 public-beta、BaseLib 3.4.8；其他游戏版本和全部第三方模组组合尚未验证。

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

输出：`dist/DuanYu-0.5.8.zip`。构建与打包不会改动游戏目录。

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

## 发布状态

当前版本为 v0.5.8 借招候选修正版，下载见 [GitHub v0.5.8](https://github.com/cqbzly/sts2-duan-yu/releases/tag/v0.5.8) 或公开的 [Steam 创意工坊条目](https://steamcommunity.com/sharedfiles/filedetails/?id=3841781260)。借力、吐纳、踏虚而行、双脉并进和一气贯之分别改为抽弃、供气过牌、减伤过牌、剑气回收和输出续手，七张纯伤害牌恢复v0.5.5数值，借招改为其他已注册角色完整卡池中的非基础稀有度攻击牌。玉佩继续写明回合结束失去1真气。本轮规则23/23、真实游戏引擎440/440，以及96张卡牌、10张初始牌组与图鉴一致性校验通过；Debug/Release构建均0警告、0错误。127项资源检查沿用v0.5.1，未重跑。详情见[本版更新记录](releases/v0.5.8.md)和[运行时校验说明](runtime-verification.md)。

本地工坊目录曾安装旧版；安装 v0.5.8 前应关闭游戏并更新同一目录。使用工坊版本时，不要再安装第二份到 `mods`。角色为《天龙八部》同人改编，与金庸权利人、Mega Crit 无官方关联。

历史v0.2.1安装DLL的SHA256为 `6C5390A760B4FAB96E1F270C9BB4D89DFA0E7FF4E502DC80F840FCA1D43BF37A`，其替换前备份为 `artifacts/install-backups/20261118-161142-before-v021`。该哈希仅用于旧版追溯，不能用于核验 v0.5.8。
