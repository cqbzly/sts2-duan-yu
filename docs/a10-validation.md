# 段誉：A10专项测试与固定构筑模拟

本轮在 Windows《杀戮尖塔2》v0.113.1 public-beta（d09d48e5）、BaseLib 3.4.8及兼容辅助组件的独立游戏副本中完成。段誉内容版本为v0.1.1；本轮新增的是Debug测试代码，没有调整角色数值或更新已发布安装包。

## 验证结果

| 检查 | 实际结果 |
| --- | --- |
| 生产规则单元测试 | 13/13通过；覆盖真气、凌波、回合边界、玩家隔离、历史序列化。 |
| 设计数据校验 | 86种卡牌、升级、初始牌组、文档一致性通过。 |
| Debug / Release构建 | 均0警告、0错误。 |
| 游戏引擎回归 | 187/187组通过，退出码0；原186组加A10专项组。 |
| 固定构筑战斗模拟 | 192场完成，145胜、47负、0异常、0回合上限中断。 |
| 进程重启后复现 | 首个种子的16场逐项一致；随后校正房间进入钩子顺序，再跑全部192场，完整报告SHA256保持一致。 |

## A10规则检查

二代标准模式A10为`DoubleBoss`，累计生效A1至A9。专项组验证全部10级开启、初始牌组11张且恰有一张`AscendersBane`、诅咒不可打出／虚无／不可移除、药水槽3→2、地图精英数量5→8、移除价格75→100及递增25→50、普通战斗金币10–20→7–15、普通稀有牌概率0.03→0.0149、Wriggler生命范围17–21→18–22，以及最后一幕生成两个不同Boss而前两幕不生成第二Boss。

起始牌组模拟另调用原生Neow治疗入口：A0起始72生命，A10起始57生命，符合当前宿主的治疗与取整。未选择Neow奖励。

## 模拟方法

- 8种“牌组×遭遇”组合，每种分别跑A0和A10，各12个固定种子：`DUANYUA10-001`至`DUANYUA10-012`，共192场。
- 起始牌组对战`NibbitsWeak`和`BowlbugsWeak`；北冥、凌波、六脉三种构筑分别对战`ByrdonisElite`及`CeremonialBeastBoss`。
- 三种构筑采用[构筑指南](builds-and-balance.md)的18张示例牌组，各指定3张升级。A10保留额外的进阶诅咒，因此为19张。每场独立从72生命开始，仅带初始玉佩，不使用药水。
- 使用宿主的遭遇生成、随机洗牌、敌人AI、回合开始／结束、状态牌、胜负处理及段誉实际卡牌效果。每回合断言3能量；每张牌先检查可打出与目标，再由`SpendResources`扣费，执行`OnPlayWrapper(isAutoPlay:false)`。
- 每次出牌检查能量非负、真气0–9、凌波0–3。每回合上限100次出牌、每战60回合，超过上限视为测试问题，不能记作胜利。
- 决策器只根据当前可见手牌、敌人意图、生命、格挡与资源做贪心评分；优先低生命目标，选牌按候选顺序取合法项。它不会搜索完整战术，也不预测抽牌顺序。评分是近似启发式，实际效果始终交给宿主结算。
- 这是无界面战斗模型测试；手动驱动原生回合方法，未经过玩家输入、完整行动队列、房间场景加载和奖励路线。未使用原版AutoSlay，其作弊增益不适用于难度评估。

## 对局结果

每格分母12；平均剩余生命包含败局的0生命。A0与A10使用相同种子，但新增诅咒会改变牌序，不能理解为完全相同的手牌实验。

| 牌组 | 遭遇 | A0胜场 | A10胜场 | A0平均剩余生命 | A10平均剩余生命 |
| --- | --- | --- | --- | --- | --- |
| 初始 | NibbitsWeak | 12/12 | 12/12 | 68.33 | 50.58 |
| 初始 | BowlbugsWeak | 8/12 | 1/12 | 17.00 | 0.25 |
| 北冥 | ByrdonisElite | 12/12 | 12/12 | 52.75 | 45.58 |
| 北冥 | CeremonialBeastBoss | 11/12 | 9/12 | 21.83 | 5.83 |
| 凌波 | ByrdonisElite | 12/12 | 12/12 | 62.92 | 60.50 |
| 凌波 | CeremonialBeastBoss | 11/12 | 4/12 | 28.92 | 6.17 |
| 六脉 | ByrdonisElite | 12/12 | 11/12 | 38.83 | 33.92 |
| 六脉 | CeremonialBeastBoss | 4/12 | 2/12 | 3.25 | 1.25 |

A0合计82胜14负；A10合计63胜33负。单批192场共执行1756个玩家回合、5345次正常支付资源的出牌。此计数不包括额外16场复现检查及第二批192场复验。

初始双敌对局、凌波及六脉的Boss战值得优先人工验证。起始生命下降、诅咒占抽牌位与原生敌人强化均实际生效；贪心决策器未专门利用敌人机制，也未优化六脉选剑，因此不能仅凭这些结果认定角色过弱或自动调整数值。

这些结果不代表A10整局通关率。没有模拟地图路线、奖励与商店构筑、连续战斗损耗、后两幕敌人或最终双Boss连战；双Boss目前只有生成规则断言。也没有验证联机或其他第三方模组组合。

## 测试环境问题与日志

首次探测中，宿主洗牌新手教程访问不存在的战斗界面导致一次空引用。执行器现在先在独立测试档标记该教程完成；此后192场和重启复现均无该异常。该修正只位于Debug测试代码。

正常退出仍有Godot RID／ObjectDB／resources回收警告，完整stderr已保留。它们不计入卡牌断言失败，但也不能据此宣称完全无Bug或无资源泄漏。

## 复现命令与证据

运行脚本要求事先存在合法本地游戏的独立副本`artifacts/game-selftest/`，以及其中的BaseLib、兼容辅助组件和段誉依赖。`override.cfg`必须使用独立用户目录`cqbzly-duanyu-engine-tests`。脚本检查隔离配置，重建Debug DLL，启动隐藏的无界面进程，检查退出码及新报告，超时只终止该测试进程。

```powershell
.\.tools\dotnet\dotnet.exe run --project tests/DuanYu.Rules.Tests.csproj --configuration Release
node scripts/validate.mjs
.\scripts\test-engine.ps1 -OutputName a10-regression
.\scripts\test-engine.ps1 -Simulation -Samples 12 -OutputName a10-full
.\scripts\test-engine.ps1 -Simulation -Samples 1 -OutputName a10-replay
.\scripts\test-engine.ps1 -Simulation -Samples 12 -OutputName a10-final
.\scripts\build.ps1 -Configuration Release
```

- 引擎断言报告：`artifacts/a10-regression-engine-tests.json`。
- 全量逐回合报告：`artifacts/a10-final-simulation.json`；与首次完整批次`artifacts/a10-full-simulation.json`的SHA256均为`F1F7BC3F22D963E6C1FCF8F597823286D557A7686F451F0E5858AAD352A736E9`。
- 重跑逐回合报告：`artifacts/a10-replay-simulation.json`。
- 每次运行的stdout／stderr：`artifacts/<OutputName>-stdout.log`及`artifacts/<OutputName>-stderr.log`。
- 测试源码：`src/DuanYu/DebugTests/AscensionSelfTests.cs`、`src/DuanYu/DebugTests/A10CombatSimulation.cs`；均由`#if DEBUG`排除在Release之外。

游戏副本、原生程序集、完整本地报告和用户存档不纳入仓库或发行包。独立测试档可能由宿主写入胜场／图鉴进度，不改动日常Steam游戏存档。
