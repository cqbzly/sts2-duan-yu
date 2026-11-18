# 完整 A10 流程验证

本文记录段誉 v0.3.0 开发预览版的完整 A10 自动测试方法与结果，并保留v0.2.1历史记录。版本下载见 [GitHub v0.3.0](https://github.com/cqbzly/sts2-duan-yu/releases/tag/v0.3.0)。2026-11-18，种子 `DUANYUFULLA10-048` 完整通关，随后在新进程从头复现成功；两次均完成三幕四首领与Architect原生结尾，最后首领战后72/72生命。同种子复现不能视为两个独立种子的胜率样本。

## v0.3.0 当前结果

| 项目 | 确认结果 |
| --- | --- |
| 环境与难度 | 段誉v0.3.0；游戏0.113.1 public-beta、BaseLib3.4.8；Standard A10。 |
| 批次与种子 | `v030-aeonglass`、`v030-aeonglass-restart`；`DUANYUFULLA10-048`。 |
| 完整流程 | 两次新进程从Neow开局，保留原生地图、难度、奖励、遗物随机与连续资源，完成三幕四首领。 |
| 原生胜利 | 两次均 `nativeEnded=true`、`winTime=7`、`remainingVictoryOptions=0`。 |
| 首次报告 | [v030-aeonglass-full-run.json](../artifacts/v030-aeonglass-full-run.json)。 |
| 重启报告 | [v030-aeonglass-restart-full-run.json](../artifacts/v030-aeonglass-restart-full-run.json)。 |
| 最终引擎回归 | 283/283；`artifacts/v030-regression-final-engine-tests.json`。 |

| 首领 | 幕 / 层 | 战后生命 |
| --- | --- | --- |
| The Kin | 1 / 17 | 67 |
| The Insatiable | 2 / 33 | 64 |
| Queen | 3 / 48 | 72 |
| Aeonglass | 3 / 49 | 72 |

最终首领战后 `lastCombatHp=72`，最大生命72；终章后的 `state.hp=0` 是原生结尾行为，不是战斗死亡。此次验证明确覆盖此前失败记录中的Aeonglass路线，未把局部对局或固定构筑战斗当成完整通关。

两次日志仍保留Godot RID、ObjectDB与resources退出告警，并出现非致命 `Invalid Task ID`；两次均无用例异常且取得原生胜利。报告支持完整流程可完成，不支持“日志无告警”“没有资源泄漏”或“所有模组兼容”的结论。

复现命令：

```powershell
.\scripts\test-engine.ps1 -FullRun -Attempts 1 -Seed DUANYUFULLA10-048 -TimeoutSeconds 180 -OutputName v030-aeonglass-restart
```

## 测试范围

测试在本机安装的《杀戮尖塔2》v0.113.1 public-beta 与 BaseLib 3.4.8 环境中运行，使用真实游戏模型、卡牌效果和战斗规则。自动策略负责选择路线、卡牌、目标和房间选项，目标是从新建 A10 游戏开始，连续走完一局并由原生胜利流程结束。

| 环节 | 验证方法 |
| --- | --- |
| 开局与难度 | 使用段誉角色与 A10 配置，从原生 Neow 开局开始。 |
| 地图 | 使用原生生成地图，只沿当前节点允许的连接选择下一节点，记录实际路线。 |
| 连续状态 | 生命、金币、牌组、遗物与药水随同一局连续推进；房间间不人工恢复生命、替换构筑或补发资源。 |
| 战斗 | 经由游戏行动队列打牌、支付费用、处理目标和结算效果；正常结束玩家回合，执行敌方回合，保留真实伤害与死亡结果。 |
| 非战斗房间 | 处理战后奖励、商店、营地和事件，资源变化继续带入后续房间。 |
| 幕与首领 | 覆盖三幕及 A10 路线中的四场首领战；每场首领胜利均需记录。 |
| 结尾 | 完成 Architect 结尾对话，通过原生 final option 调用 `RunManager.WinRun`，并检查最终胜利状态。 |

每次运行应保留版本、种子、难度、角色、路线、房间结果、资源变化与异常日志。角色死亡属于败局；程序异常、未处理的选择界面或运行被终止属于中断，不能视为正常完成的胜局或败局。

## Headless 适配范围

自动测试不加载完整交互界面。以下适配用于让原生规则在该环境下继续执行，必须与模组玩法改动分开检查。

### 水晶球

`FullRunCrystalSphere` 仅基于小游戏公开的隐藏格状态作选择，不读取隐藏奖励来挑选结果。揭格通过原生 `CellClicked` 执行，结束通过原生 `CompleteMinigame` 完成；点击代价、随机结果及奖励由原生流程处理。

### SoulNexus 与 Trial

`FullRunHeadlessVisuals` 跳过 SoulNexus 的视觉动画，保留原有事件退订行为，避免无界面环境中的视觉等待阻断后续流程。

Trial 仅替换三处依赖 UI 的判断；原生 RNG、选项生成和选择结果保留。不得通过固定事件分支绕过原本需要随机决定的内容。

### 原生规则分支

`FullRunNativeRules` 恢复 CallingBell、Cauldron 和药水价格计算的普通分支，避免 headless 环境进入与正常游戏不同的特殊处理。相关随机生成、资源代价和奖励仍按原生规则执行。

## 已作废的批次

`v020-fullrun5` 曾固定 Trial 分支，未保留该处原生随机流程。该批次已终止作废，其日志、路线或战斗结果不得用作完整 A10 通关证据，也不得与修正后的批次合并统计。修正后的适配器必须从新的完整运行开始验证。

## 批量搜索运行

在仓库根目录使用 PowerShell 执行：

```powershell
.\scripts\test-engine.ps1 -FullRun -Attempts 100 -StartSeed 1 -TimeoutSeconds 1800 -OutputName v030-fullrun
```

此命令从第1号种子开始，最多尝试100局，在首次完整胜利后停止，超时参数为1800秒，不需要额外环境变量。脚本自动构建 Debug 版本；Release 构建排除所有测试适配器。该停止规则只验证是否存在可完成的路线，不产生无偏胜率。

脚本输出文件：

- `artifacts/v030-fullrun-full-run.json`
- `artifacts/v030-fullrun-engine-tests.json`
- `artifacts/v030-fullrun-stdout.log`
- `artifacts/v030-fullrun-stderr.log`

测试用户数据位于 `%APPDATA%/cqbzly-duanyu-engine-tests`。运行期间不要读取该目录中的活动报告；等待运行完成后检查脚本导出的上述文件，避免把正在更新的内容当作最终结果。需要保留不同批次时，使用不同的 `-OutputName`。

复现应从新建游戏开始，并使用同一游戏版本、模组构建和适配器版本；不能用作废批次的中途状态继续运行后作为有效证据。

## 胜利判据

只有同时满足以下条件，才能在本文件记录一次有效的完整 A10 通关：

1. 运行记录确认段誉角色、A10 难度和可复现种子，且由原生 Neow 开局开始。
2. 所有跨房间生命、金币、牌组、遗物和药水变化连续；路线符合原生地图连接。
3. 战斗通过原生行动队列和敌方回合完成，没有跳过死亡、人工补血或直接修改胜利状态。
4. 三幕和四场首领战全部完成，奖励、事件、商店、营地的决策与资源变化有记录。
5. Architect 结尾经过原生 final option，实际调用 `RunManager.WinRun`，最终胜利断言通过。
6. 无未处理异常、测试中断或会改变玩法结果的临时绕过；日志能够对应上述完整流程。

## v0.2.1 历史结果

下表与Debug哈希保留v0.2.1当时的记录，不是v0.3.0当前构建或测试结果。

| 项目 | 结果 |
| --- | --- |
| 有效完整 A10 胜利 | 新版首个尝试即胜；新进程同种子从头复现再次胜利。 |
| 最终运行批次与种子 | `v021-fullrun`、`v021-fullrun-restart`；`DUANYUFULLA10-001`。 |
| 模组构建及适配器版本 | v0.2.1 Debug，含剑势与三张随机牌；游戏 v0.113.1 public-beta、BaseLib 3.4.8。 |
| 三幕与四首领记录 | 第17层 Vantom、33层 Kaiser Crab、48层 Queen、49层 Test Subject，全部原生胜利。 |
| Architect 与 `RunManager.WinRun` 断言 | 两次 `nativeEnded=true`、`winTime=8`、`remainingVictoryOptions=0`。 |
| 完整日志与结果文件 | `artifacts/v021-fullrun-full-run.json`、`artifacts/v021-fullrun-restart-full-run.json`；同名前缀的 stdout/stderr 与 engine-tests 报告。 |
| 重启一致性 | 四首领记录、最终状态（牌组/遗物/生命/金币/药水）和所有路线房间决策完全一致。 |

| 首领 | 幕 / 层 | 战后生命 |
| --- | --- | --- |
| Vantom | 1 / 17 | 68 |
| Kaiser Crab | 2 / 33 | 80 |
| Queen | 3 / 48 | 77 |
| Test Subject | 3 / 49 | 77 |

本轮测试 DLL 的 SHA256：`B7A5B41F6049EA865458ABB19D6D9BAC11D06A8C76E8A73B898925F55732B544`。此为包含测试执行器的 Debug DLL，不是安装用 Release 文件。引擎日志仍有 Godot 退出时的 RID/ObjectDB/resource 回收提示；两次完整流程均无未处理异常、进程退出码为0。

复现命令：

```powershell
.\scripts\test-engine.ps1 -FullRun -Attempts 1 -Seed DUANYUFULLA10-001 -TimeoutSeconds 180 -OutputName v021-fullrun-restart
```

v0.2.0 的 `v020-fullrun6` 是已完成的100局败局；`v020-fullrun7` 为版本切换时中断的批次。v0.2.1 的搜索在首次胜利后停止，没有重新跑满100局，也不能从这一次胜利推算普通玩家的通关率。

## 结论边界

本方法验证的是 headless 自动策略驱动的完整游戏流程，不是人工操作图形界面的试玩记录。即使取得一次有效胜利，也只能证明该版本、种子、策略与测试条件下存在一条完成路线；不能据此给出角色胜率、整体平衡结论、联机同步保证或全部第三方模组兼容性结论。

图形界面交互、手动操作体验、继续存档、远程多人和 Steam 工坊订阅回装需要各自的验证记录；这里已验证的是独立headless新进程从头复现。当前其他检查见[运行验证记录](runtime-verification.md)与[v0.3.0候选说明](releases/v0.3.0.md)。
