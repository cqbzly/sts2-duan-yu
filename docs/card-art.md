# 段誉卡面设计

段誉的卡图参考《杀戮尖塔 2》的手牌可读性，以旧版段誉卡面的墨青底色、平涂色面和硬边阴影为画风参考。每张插画突出一个动作或物件，将发力点放在画面中下部，避开游戏标题带。

## 视觉规则

- 青衫、宽袖和束发保留书生轮廓。指劲、衣袖和水纹承担动作表现。
- 少商剑气用火色弧光劈岩，商阳剑气用细束击中圆靶，中冲剑气以横向光浪表现群攻，关冲剑气以翠绿菱形气劲挡矢。
- 凌波牌用足尖、落点和衣摆表现步法。北冥牌通过回流、凝聚和向外迸发的气流表现不同效果。

每种卡牌都有独立构图，升级版沿用同一张插画。Z-Image Turbo 根据逐张撰写的构图描述和旧版「凌波初步」卡面的风格参考生成插画。选图和裁切后的 1000 × 760 像素 PNG 保存在 `assets/card-portraits/`，随 DLL 一同加载。缺少对应 PNG 时，游戏读取 `src/DuanYu/Assets/DuanYuCardArt.cs` 中的 SVG。

## 导出与验证

```powershell
.\scripts\test-assets.ps1 -GodotPath 'D:\tools\godot\Godot_v4.5.1-stable_mono_win64_console.exe' -ExportArtPath 'D:\duanyu-card-export'
.\scripts\render-promo.ps1 -ArtPath 'D:\duanyu-card-export'
```

将 `-GodotPath` 指向本机的 Godot 4.5.1 .NET 控制台程序。第一条命令加载纹理和场景，验证实际卡图的尺寸、像素是否重复以及内嵌 PNG 与游戏纹理是否一致。所有卡图都会导出为 PNG，使用 SVG 的卡图还会导出源图。第二条命令使用同一目录下的游戏卡图生成精选画册 `assets/card-gallery.png`，并检查文字区域溢出。

![卡面精选](../assets/card-gallery.png)

画册使用游戏卡图和宣传卡框，游戏内使用原生卡框。README 横幅和工坊方形封面以段誉的青衫背影为主体，使用同一模型图生图，分别排版。
