# 大理世子 · 段誉

![段誉身穿青衫，背对观者望向湖山](assets/duanyu-ink-banner.png)

《杀戮尖塔2》的段誉同人角色模组。攒真气蓄势，以凌波卸去连击，再用六脉剑气打开局面。包含 **97 种卡牌、9 件遗物和 2 个专属事件**。

**公开发行版 v0.5.32** · 《杀戮尖塔2》v0.113.1 public-beta · BaseLib 3.4.8

[Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3841781260) · [GitHub 下载](https://github.com/cqbzly/sts2-duan-yu/releases/latest) · [安装说明](docs/installation.md)

## 怎么玩

- **北冥真气**　开战时，琅嬛玉佩给 3 点真气。真气无上限，跨回合保留。真气费用在出牌前支付，能量免费时也要付足真气。
- **凌波步法**　每层凌波让每段可格挡伤害减少 1 点，再结算格挡。凌波可以叠加，受击时保留，到自己下回合开始时清除。
- **六脉剑气**　六种剑气各司其职，能铺易伤、打群伤、补格挡、抽牌或施加虚弱。每打出一张剑气获得 1 层剑势，当回合打出第 3、6 种不同剑气时各额外获得 1 层。剑势上限为 8，每层为剑气和六脉齐发的每段伤害加 1。
- **借招与炼化**　北冥借招从其他角色的非基础攻击池生成外门招式。气滞和异种真气也可以作为主动消耗的素材，换取抽牌、格挡或真气。

| 起始生命 | 每回合能量 | 基础抽牌 | 初始牌组 |
| --- | --- | --- | --- |
| 72 | 3 | 5 | 4 张打击、4 张防御、北冥初窥、无意一指 |

[卡牌图鉴](docs/cards.md) · [遗物与事件](docs/relics-and-events.md) · [构筑指南](docs/builds-and-balance.md) · [实战小抄](docs/cheat-sheet.md)

## 版本

全套 97 张卡面重绘。GitHub 横幅和工坊封面换为段誉的青衫背影。[v0.5.32 更新记录](docs/releases/v0.5.32.md)

## 从源码构建

需要 .NET 9 SDK 和已安装的目标版本游戏。构建和打包命令如下。

```powershell
.\scripts\build.ps1 -Sts2Path 'D:\steam\steamapps\common\Slay the Spire 2'
.\scripts\package.ps1 -Sts2Path 'D:\steam\steamapps\common\Slay the Spire 2'
```

卡牌文本在 `data/cards.json`。用 Node.js 18 或更新版本运行 `node scripts/build-cards.mjs` 生成图鉴，再运行 `node scripts/validate.mjs` 检查数据及图鉴一致性。

[完整结算规则](docs/mechanics.md) · [运行时验证](docs/runtime-verification.md) · [工坊发布流程](docs/workshop-upload.md)

## 作品来源

角色与武学源于金庸《天龙八部》。本项目是玩家制作的同人模组。游戏卡图以 Z-Image Turbo 生成并逐张选图和裁切，封面取景自南宋马麟的《Landscape with Flying Geese》，由克利夫兰艺术博物馆按 CC0 开放。[卡面精选](docs/card-art.md) · [封面原作与授权](https://www.clevelandart.org/art/1952.285)
