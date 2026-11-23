using DuanYu.Assets;
using Godot;

public partial class SmokeTest : Node
{
    public override void _Ready()
    {
        try
        {
            DuanYuAssets.Initialize();
            int loaded = 0;
            foreach ((string prefix, int count) in new[] { ("B", 5), ("C", 23), ("U", 38), ("R", 22), ("A", 1), ("T", 6), ("S", 2) })
            {
                for (int number = 1; number <= count; number++)
                {
                    var texture = DuanYuAssets.Texture(DuanYuAssets.CardPortrait($"DY_{prefix}{number:00}"));
                    if (texture.GetWidth() <= 0 || texture.GetHeight() <= 0) throw new InvalidOperationException("Empty card portrait.");
                    loaded++;
                }
            }
            foreach (string path in new[] { DuanYuAssets.CharacterTexture, DuanYuAssets.RestTexture, DuanYuAssets.CharacterIcon, DuanYuAssets.CharacterIconOutline, DuanYuAssets.LockedIcon, DuanYuAssets.BigEnergy, DuanYuAssets.TextEnergy, DuanYuAssets.RelicIcon("test"), DuanYuAssets.PowerIcon("test"), DuanYuAssets.EventPortrait("test") })
            {
                var texture = DuanYuAssets.Texture(path);
                if (texture.GetWidth() <= 0 || texture.GetHeight() <= 0) throw new InvalidOperationException("Empty texture: " + path);
                loaded++;
            }
            foreach (string path in Enumerable.Range(1, 9).Select(number => DuanYuAssets.RelicIcon($"CQBZLY_DY_RELIC{number:00}"))
                .Concat(new[] { "CQBZLY_DY_QI", "CQBZLY_DY_LINGBO", "CQBZLY_DY_QI_BLADE", "CQBZLY_DY_SWORD_MOMENTUM", "CQBZLY_DY_VERMILION_WARD", "CQBZLY_DY_POWER_R22" }.Select(DuanYuAssets.PowerIcon))
                .Concat(new[] { "CQBZLY_DY_EVENT01", "CQBZLY_DY_EVENT02" }.Select(DuanYuAssets.EventPortrait)))
            {
                var texture = DuanYuAssets.Texture(path);
                if (texture.GetWidth() <= 0 || texture.GetHeight() <= 0) throw new InvalidOperationException("Empty themed texture: " + path);
                loaded++;
            }
            foreach (string path in new[] { DuanYuAssets.CharacterScene, DuanYuAssets.RestScene, DuanYuAssets.MerchantScene, DuanYuAssets.IconScene, DuanYuAssets.CharacterSelectScene })
            {
                var scene = ResourceLoader.Load<PackedScene>(path) ?? throw new InvalidOperationException("Could not load scene: " + path);
                var node = scene.Instantiate();
                if (path == DuanYuAssets.CharacterScene)
                {
                    foreach (string name in new[] { "%Visuals", "%Bounds", "%IntentPos", "%CenterPos", "%OrbPos", "%TalkPos", "%FormVfx" })
                        if (!node.HasNode(name)) throw new InvalidOperationException("Missing node: " + name);
                }
                node.Free();
                loaded++;
            }
            GD.Print($"ASSET_SMOKE_OK: {loaded} textures and scenes loaded.");
            ExportGallery.RunIfRequested();
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PrintErr("ASSET_SMOKE_FAIL: " + error);
            GetTree().Quit(1);
        }
    }
}
