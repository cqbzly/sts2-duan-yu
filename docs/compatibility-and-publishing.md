# 兼容性与 Steam 创意工坊发布检查

本文记录段誉模组在本机环境中的实际证据、兼容边界和发布前条件。它不把“能编译”或“能启动”扩大解释为所有第三方模组组合都兼容。

## 当前版本入口

v0.5.9 本轮96张卡牌数据与图鉴校验、24/24项规则测试、442/442项真实游戏引擎回归通过；Debug与Release构建均0警告、0错误。127项资源加载通过结果沿用 v0.5.1，本轮未重跑。详见[当前验证](runtime-verification.md)。本版未以自动策略采样推断胜率，历史完整通关记录不外推当前胜率。

主动消耗、状态牌与外门借招的回归不扩大为远程多人或任意第三方组合兼容。日志仍保留Godot RID/ObjectDB/resources退出回收告警；本轮442/442项引擎回归通过，不代表日志没有告警或已排除资源泄漏。

版本源码见 [GitHub 仓库](https://github.com/cqbzly/sts2-duan-yu)；当前 v0.5.9 使用已有的公开 [Steam工坊条目](https://steamcommunity.com/sharedfiles/filedetails/?id=3841781260)。订阅回装和任意第三方模组组合仍需单独验证。下文保留旧版本环境和发布时的历史记录，其中旧版测试数量、未完成完整通关的陈述和旧安装包路径只适用于当时版本。

## 已验证环境

目标环境为 Windows Steam 版《杀戮尖塔2》`v0.113.1 public-beta`，release commit `d09d48e5`，Steam App ID `2868840`。段誉清单要求 BaseLib `3.4.8`，模组 ID 为 `CQBZLY_DUAN_YU`，程序集文件名必须是 `CQBZLY_DUAN_YU.dll`。

以下列出本轮检查及明确标注的历史结果：

| 检查 | 实际结果 | 范围和限制 |
| --- | --- | --- |
| `node scripts/validate.mjs` | 通过 | 96 张卡牌、稀有度分布、起始牌组、衍生牌引用和图鉴一致性。 |
| `scripts/test-assets.ps1` | v0.5.1通过，127 个纹理/场景；本轮未重跑 | 使用 Godot 4.5.1 Mono 独立进程加载程序化生成的 `.ctex` 与 `.tscn`。 |
| 规则单元测试 | 本轮24/24通过 | 真气、凌波、状态和关键触发规则；不替代宿主引擎回归。 |
| Release 构建 | 本轮通过，0警告、0错误 | 生成 `CQBZLY_DUAN_YU.dll`；不等于 Steam 上传成功。 |
| 隔离游戏启动 | 历史通过 | 只加载 BaseLib、Compatibility Helper 和段誉；模组初始化、角色注册、主菜单加载成功。 |
| 引擎内 Debug 自测 | 本轮442/442通过，0失败 | 96张卡牌基础/升级执行、关键资源机制、图鉴、奖励过滤、事件边界、抽弃牌与剑气回收；报告见当前验证。 |
| 人工 UI 冒烟 | 历史通过 | 在隔离副本中成功选择段誉并完成首战，随后获得段誉卡牌；没有覆盖完整通关。 |

本轮报告见[v0.5.9 最终引擎报告](https://github.com/cqbzly/sts2-duan-yu/releases/download/v0.5.9/v059-balance-engine-tests.json)。测试副本只加载BaseLib、Compatibility Helper和段誉。退出时出现Godot RID与资源回收提示，尚未完成泄漏归因，不能据此判定段誉卡牌断言失败，也不能声称日志无异常。资源检查127项沿用 v0.5.1，本轮未重跑。

## 本机已安装模组

工坊目录当前有 11 个项目和 12 份清单；BaseLib 项目同时带有 Compatibility Helper。游戏设置的已启用列表包含：BaseLib、Compatibility Helper、QuickRestart、SavesMerger、ModConfig、DamageMeter、STS2-RitsuLib 和 AscensionUnlocker。RegentFX、LexNinja2、jijia、Mesugaki 已安装但不在这份启用列表中。

实际的联合测试范围只有“BaseLib + Compatibility Helper + 段誉”。没有执行以下组合，因此不能声称它们兼容：

- 段誉 + QuickRestart；
- 段誉 + SavesMerger；
- 段誉 + DamageMeter；
- 段誉 + ModConfig、STS2-RitsuLib 或 AscensionUnlocker；
- 段誉 + LexNinja2、RegentFX、jijia 或 Mesugaki；
- 两台客户端的在线合作、不同游戏分支，或完整高进阶通关。

## 冲突风险边界

段誉通过 BaseLib 注册自定义角色、卡牌、遗物、能力、事件和两个自定义资源；稳定 ID 使用 `CQBZLY_` 前缀，运行时资源缓存使用 `user://cqbzly.duanyu/assets-v2`，历史存档扩展键使用 `cqbzly.sts2.duanyu.history.v1`。这些命名空间降低了与其他内容模组直接重名的概率，但不能排除第三方模组主动修改同一宿主流程的影响。

段誉的 Harmony 补丁涉及以下宿主钩子：战斗开始/结束、玩家回合开始/结束、出牌前/后、伤害修正以及受伤前/后。DamageMeter 或其他战斗统计模组若也修补伤害生命周期，可能只产生统计差异，也可能改变修正顺序；需要联合实测才能区分。QuickRestart、SavesMerger 或其他存档/重载模组可能影响段誉战斗状态清理和“已获得遗物/已访问事件”历史；本地自动测试没有替代这些模组的存档实现。

本机历史日志中有一条与段誉无关的 SavesMerger 基线异常：它对 `UserDataPathProvider.GetProfileDir` 的 Harmony 匹配出现 `AmbiguousMatchException`。在解决该异常或停用 SavesMerger 前，不能把段誉的存档结果归因给段誉。`jijia.json` 声明需要 DLL，但其安装目录未发现对应 `jijia.dll`，这是安装包一致性候选问题，不是段誉冲突证据。

游戏会按模组 ID 和版本处理本地 `mods` 与 Steam Workshop 的重复安装。发布测试时不要同时留下本地段誉目录和同 ID 的工坊版本；否则游戏可能禁用其中一份，导致测试的实际程序集与预期不一致。

## 发布前准备

当前仓库已经能生成可加载的游戏模组包：

```powershell
.\scripts\build.ps1 -Configuration Release
.\scripts\package.ps1 -Configuration Release -SkipBuild
```

`dist/DuanYu-0.5.9.zip` 内含：

```text
DuanYu/CQBZLY_DUAN_YU.dll
DuanYu/DuanYu.json
README.md
CHEAT-SHEET.md
```

安装包另由`scripts/prepare-workshop.ps1`生成 Steam Workshop 工作区：

```text
workshop-workspace/
  content/
    CQBZLY_DUAN_YU.dll
    DuanYu.json
  workshop.json
  image.png
```

`workshop/workshop.json`已提供中文说明、public可见性与BaseLib工坊项目数字ID；脚本同步`DuanYu.json`中的版本信息。封面为项目生成的`assets/workshop-preview.png`，小于1MB。上传器来源、执行命令和条目更新方式见[上传说明](workshop-upload.md)，本版内容和验证结果见[版本记录](releases/v0.5.9.md)。

发布流程使用官方 `ModUploader.exe upload -w <workshop-workspace>` 更新现有公开条目 `3841781260`，工作区保留原条目ID。上传后查询远程版本和公开状态；订阅回装、角色选择、首战、继续游戏和卸载提示仍需各自验证。上传器使用当前Steam客户端登录账号，GitHub的`cqbzly`账号不能代替Steam发布权限。

## 发布判定

`v0.5.9`延续现有公开发布方式：本轮Release构建0警告、0错误，独立游戏引擎回归442/442通过；清单和DLL名称沿用既有约定，程序化资源加载检查沿用v0.5.1结果。当前证据仍不能证明与本机所有模组无冲突；Steam订阅回装、远程联机和第三方组合测试未完成。

每次发布保存包含游戏版本、BaseLib版本、测试模组列表、发布版本和自测报告的记录；更新游戏或BaseLib后重新执行构建、资源检查、隔离引擎自测和最小联合测试。
