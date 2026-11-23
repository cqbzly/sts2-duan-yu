# 段誉卡面设计

v0.5.30 重画 96 张卡图，先古牌「六脉真传」使用古碑插画。卡图为 1000 × 760 像素，使用 500 × 380 设计坐标。游戏运行时将内置 SVG 绘制成缓存纹理。

## 视觉规则

- 少商剑气劈岩，商阳剑气破甲，中冲剑气扫过整片场地，关冲剑气折面挡矢。
- 凌波初步从靴尖点水起势，掠影步穿过竹林，倒踩七星沿屋脊留下落点。
- 逆脉强攻用逆行裂电表现强攻的代价，内力震荡则让护甲从内部炸开。

每种卡牌都有独立构图，升级版沿用该牌插画。插画由 `src/DuanYu/Assets/DuanYuCardArt.cs` 按卡牌编号加载。

## 导出与验证

```powershell
.\scripts\test-assets.ps1 -ExportArtPath artifacts/card-art
.\scripts\render-promo.ps1
```

第一条命令加载纹理和场景，并导出 PNG 与 SVG，验证尺寸及源图唯一性。第二条命令使用导出的游戏卡图生成精选画册 `assets/card-gallery.png`，并检查文字区域溢出。

![卡面精选](../assets/card-gallery.png)

画册使用项目绘制的游戏卡图和宣传卡框，游戏内使用原生卡框。README 横幅和工坊方形封面取景自南宋马麟的《Landscape with Flying Geese》，采用书法题字和朱印排版。两张封面独立维护。[原作与 CC0 授权](https://www.clevelandart.org/art/1952.285)
