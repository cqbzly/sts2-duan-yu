# 兼容性与 Steam 创意工坊发布检查

本文记录段誉模组在本机环境中的实际证据、兼容边界和发布前条件。它不把“能编译”或“能启动”扩大解释为所有第三方模组组合都兼容。

## 当前版本入口

v0.3.0 开发预览版已通过23项规则、283组最终引擎回归、118项资源检查与88张卡牌数据校验，Debug/Release均0警告0错误。完整A10种子 `DUANYUFULLA10-048` 两次新进程从头胜利，击败Kin、Insatiable、Queen与Aeonglass，最后首领战后72/72生命；两次原生结束标志与胜利选项完成情况均确认。详见[当前验证](runtime-verification.md)与[完整 A10 证据](full-run-validation.md)。

主动消耗、状态牌与外门借招的回归不扩大为多人或任意第三方组合兼容。日志仍保留Godot RID/ObjectDB/resources退出告警及非致命 `Invalid Task ID`；283组用例无失败、完整流程两次胜利，不代表日志没有告警或已排除资源泄漏。

版本下载见 [GitHub v0.3.0](https://github.com/cqbzly/sts2-duan-yu/releases/tag/v0.3.0)；[Steam工坊条目](https://steamcommunity.com/sharedfiles/filedetails/?id=3841781260)沿用 private 可见性。v0.3.0本地安装、备份及包哈希以[v0.3.0版本说明](releases/v0.3.0.md)为准，本地验证不替代工坊订阅回装验证。下文保留 v0.1.1 环境和发布时的历史记录，其中旧版测试数量、未完成完整通关的陈述和旧安装包路径只适用于当时版本。

## 已验证环境

目标环境为 Windows Steam 版《杀戮尖塔2》`v0.113.1 public-beta`，release commit `d09d48e5`，Steam App ID `2868840`。段誉清单要求 BaseLib `3.4.8`，模组 ID 为 `CQBZLY_DUAN_YU`，程序集文件名必须是 `CQBZLY_DUAN_YU.dll`。

以下检查已经完成：

| 检查 | 实际结果 | 范围和限制 |
| --- | --- | --- |
| `node scripts/validate.mjs` | 通过 | 86 张卡牌、稀有度分布、起始牌组、衍生牌引用和图鉴一致性。 |
| `scripts/test-assets.ps1` | 通过，115 个纹理/场景 | 使用 Godot 4.5.1 Mono 独立进程加载程序化生成的 `.ctex` 与 `.tscn`。 |
| Release 构建 | 通过，0 警告、0 错误 | 生成 `CQBZLY_DUAN_YU.dll`；不等于 Steam 上传成功。 |
| 隔离游戏启动 | 通过 | 只加载 BaseLib、Compatibility Helper 和段誉；模组初始化、角色注册、主菜单加载成功。 |
| 引擎内 Debug 自测 | 186 通过、0 失败 | 86 张卡牌基础/升级执行、关键资源机制和事件边界；测试报告位于独立用户目录。 |
| 人工 UI 冒烟 | 通过 | 在隔离副本中成功选择段誉并完成首战，随后获得段誉卡牌；没有覆盖完整通关。 |

引擎自测日志是 `artifacts/game-selftest/selftest.stdout.log` 和 `artifacts/game-selftest/selftest.stderr.log`，报告是 `%APPDATA%/cqbzly-duanyu-engine-tests/duanyu-engine-tests.json`。测试副本只加载 3 个模组，输出中的 `DUANYU_SELF_TEST_RESULT passed=186 failed=0` 及对应JSON报告为自动测试结果依据。退出时出现Godot RID与资源回收提示，尚未完成泄漏归因，不能据此判定段誉卡牌断言失败，也不能声称日志无异常。

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

`dist/DuanYu-0.1.1.zip` 内含：

```text
DuanYu/CQBZLY_DUAN_YU.dll
DuanYu/DuanYu.json
README.md
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

`workshop/workshop.json`已提供中文说明、private可见性与BaseLib工坊项目数字ID；脚本同步`DuanYu.json`中的版本信息。封面为项目生成的`assets/workshop-preview.png`，小于1MB。上传器来源、执行命令和条目更新方式见[上传说明](workshop-upload.md)，当次上传结果见[版本记录](releases/v0.1.1.md)。

发布流程使用官方 `ModUploader.exe upload -w <workshop-workspace>` 创建 `private` 条目，再从 Steam Workshop 安装回一个干净测试用户目录，确认清单、DLL名称、BaseLib依赖、角色选择、首战、继续游戏和卸载提示；之后再决定是否改变可见性。上传器使用当前Steam客户端登录账号，GitHub的`cqbzly`账号不能代替Steam发布权限。

## 发布判定

`v0.1.1`可供私有条目测试：Release构建与独立游戏自测通过，程序化资源可加载，清单和DLL名称匹配，工坊工作区与封面已准备。当前证据仍不能证明与本机所有模组无冲突；Steam订阅回装、远程联机和第三方组合测试未完成。

公开发布前至少应保存一份包含游戏版本、BaseLib 版本、启用模组列表、工坊项目版本和自测报告的测试记录，并在更新游戏或 BaseLib 后重新执行构建、资源 smoke、隔离引擎自测和最小联合测试。
