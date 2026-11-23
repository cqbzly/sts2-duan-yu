using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Godot;
using File = System.IO.File;

namespace DuanYu.Assets;

public static class DuanYuAssets
{
    private const string CacheRoot = "user://cqbzly.duanyu/assets-v2";
    private static readonly object AssetLock = new();
    private static readonly Dictionary<string, string> Paths = new(StringComparer.Ordinal);
    private static bool _initialized;

    public static string CharacterTexture => ImagePath("character", DuanYuArt.Character());
    public static string RestTexture => ImagePath("rest", DuanYuArt.Character(true));
    public static string CharacterIcon => ImagePath("icon", DuanYuArt.Icon());
    public static string CharacterIconOutline => ImagePath("icon-outline", DuanYuArt.Icon(outline: true));
    public static string LockedIcon => ImagePath("icon-locked", DuanYuArt.Icon(locked: true));
    public static string BigEnergy => ImagePath("energy-big", DuanYuArt.Energy(128));
    public static string TextEnergy => ImagePath("energy-text", DuanYuArt.Energy(64));
    public static string CharacterScene => ScenePath("character", CombatScene());
    public static string RestScene => ScenePath("rest", SeatedScene());
    public static string MerchantScene => ScenePath("merchant", ShopScene());
    public static string IconScene => ScenePath("icon", UiIconScene());
    public static string CharacterSelectScene => ScenePath("select", SelectionScene());

    public static void Initialize()
    {
        lock (AssetLock)
        {
            if (_initialized) return;
            Directory.CreateDirectory(ProjectSettings.GlobalizePath(CacheRoot));
            _ = CharacterTexture;
            _ = RestTexture;
            _ = CharacterIcon;
            _ = CharacterIconOutline;
            _ = LockedIcon;
            _ = BigEnergy;
            _ = TextEnergy;
            _ = CharacterScene;
            _ = RestScene;
            _ = MerchantScene;
            _ = IconScene;
            _ = CharacterSelectScene;
            foreach ((string prefix, int count) in new[] { ("B", 5), ("C", 23), ("U", 38), ("R", 22), ("A", 1), ("T", 6), ("S", 2) })
            {
                for (int number = 1; number <= count; number++)
                {
                    _ = CardPortrait($"DY_{prefix}{number:00}");
                }
            }
            _initialized = true;
        }
    }

    public static string CardPortrait(string designId) => ImagePath("card-" + SafeKey(designId), DuanYuArt.Card(designId));
    public static string PowerIcon(string key) => ImagePath("power-" + SafeKey(key), DuanYuArt.Symbol(key, 128));
    public static string RelicIcon(string key) => ImagePath("relic-" + SafeKey(key), DuanYuArt.Symbol(key, 128));
    public static string EventPortrait(string key) => ImagePath("event-" + SafeKey(key), DuanYuArt.Event(key));

    public static Texture2D Texture(string path) => ResourceLoader.Load<CompressedTexture2D>(path)
        ?? throw new InvalidOperationException($"Duan Yu texture did not load: {path}");

    private static string ImagePath(string key, string svg)
    {
        string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(svg)))[..12].ToLowerInvariant();
        string filename = key + "-" + digest + ".ctex";
        lock (AssetLock)
        {
            if (Paths.TryGetValue(filename, out string? existing)) return existing;
            string path = CacheRoot + "/" + filename;
            string absolutePath = ProjectSettings.GlobalizePath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            if (!File.Exists(absolutePath))
            {
                using var image = new Image();
                Error result = image.LoadSvgFromString(svg);
                if (result != Error.Ok) throw new InvalidOperationException($"Unable to render {key}: {result}");
                image.Convert(Image.Format.Rgba8);
                WriteCompressedTexture(absolutePath, image);
            }
            Paths[filename] = path;
            return path;
        }
    }

    private static void WriteCompressedTexture(string path, Image image)
    {
        byte[] png = image.SavePngToBuffer();
        using var output = new MemoryStream();
        using (var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("GST2"));
            writer.Write(1u);
            writer.Write((uint)image.GetWidth());
            writer.Write((uint)image.GetHeight());
            writer.Write(0u);
            writer.Write(uint.MaxValue);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(1u);
            writer.Write((ushort)image.GetWidth());
            writer.Write((ushort)image.GetHeight());
            writer.Write(0u);
            writer.Write((uint)Image.Format.Rgba8);
            writer.Write((uint)png.Length + 4u);
            writer.Write(Encoding.ASCII.GetBytes("PNG "));
            writer.Write(png);
        }
        File.WriteAllBytes(path, output.ToArray());
    }

    private static string ScenePath(string key, string content)
    {
        string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)))[..12].ToLowerInvariant();
        string filename = key + "-" + digest + ".tscn";
        lock (AssetLock)
        {
            if (Paths.TryGetValue(filename, out string? existing)) return existing;
            string path = CacheRoot + "/" + filename;
            string absolutePath = ProjectSettings.GlobalizePath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            if (!File.Exists(absolutePath)) File.WriteAllText(absolutePath, content, new UTF8Encoding(false));
            Paths[filename] = path;
            return path;
        }
    }

    private static string CombatScene() => $$"""
        [gd_scene load_steps=8 format=3]

        [ext_resource type="CompressedTexture2D" path="{{CharacterTexture}}" id="1"]

        [sub_resource type="Animation" id="Idle"]
        resource_name = "idle"
        length = 2.4
        loop_mode = 1
        tracks/0/type = "value"
        tracks/0/path = NodePath("Visuals:position")
        tracks/0/keys = {"times": PackedFloat32Array(0, 1.2, 2.4), "transitions": PackedFloat32Array(1, 1, 1), "update": 0, "values": [Vector2(0, -160), Vector2(0, -164), Vector2(0, -160)]}

        [sub_resource type="Animation" id="Attack"]
        resource_name = "attack"
        length = 0.38
        tracks/0/type = "value"
        tracks/0/path = NodePath("Visuals:position")
        tracks/0/keys = {"times": PackedFloat32Array(0, 0.14, 0.38), "transitions": PackedFloat32Array(1, 1, 1), "update": 0, "values": [Vector2(0, -160), Vector2(28, -166), Vector2(0, -160)]}

        [sub_resource type="Animation" id="Cast"]
        resource_name = "cast"
        length = 0.5
        tracks/0/type = "value"
        tracks/0/path = NodePath("Visuals:modulate")
        tracks/0/keys = {"times": PackedFloat32Array(0, 0.25, 0.5), "transitions": PackedFloat32Array(1, 1, 1), "update": 0, "values": [Color(1, 1, 1, 1), Color(0.65, 1.3, 1.05, 1), Color(1, 1, 1, 1)]}

        [sub_resource type="Animation" id="Hurt"]
        resource_name = "hurt"
        length = 0.3
        tracks/0/type = "value"
        tracks/0/path = NodePath("Visuals:position")
        tracks/0/keys = {"times": PackedFloat32Array(0, 0.1, 0.3), "transitions": PackedFloat32Array(1, 1, 1), "update": 0, "values": [Vector2(0, -160), Vector2(-13, -157), Vector2(0, -160)]}

        [sub_resource type="Animation" id="Dead"]
        resource_name = "die"
        length = 0.7
        tracks/0/type = "value"
        tracks/0/path = NodePath("Visuals:rotation")
        tracks/0/keys = {"times": PackedFloat32Array(0, 0.7), "transitions": PackedFloat32Array(1, 1), "update": 0, "values": [0.0, -1.3]}
        tracks/1/type = "value"
        tracks/1/path = NodePath("Visuals:position")
        tracks/1/keys = {"times": PackedFloat32Array(0, 0.7), "transitions": PackedFloat32Array(1, 1), "update": 0, "values": [Vector2(0, -160), Vector2(-20, -65)]}

        [sub_resource type="AnimationLibrary" id="Library"]
        _data = {"idle": SubResource("Idle"), "attack": SubResource("Attack"), "cast": SubResource("Cast"), "hurt": SubResource("Hurt"), "die": SubResource("Dead"), "relaxed_loop": SubResource("Idle")}

        [node name="DuanYuVisuals" type="Node2D"]

        [node name="Visuals" type="Sprite2D" parent="."]
        unique_name_in_owner = true
        position = Vector2(0, -160)
        texture = ExtResource("1")

        [node name="Bounds" type="Control" parent="."]
        unique_name_in_owner = true
        offset_left = -100.0
        offset_top = -300.0
        offset_right = 100.0
        mouse_filter = 2

        [node name="IntentPos" type="Marker2D" parent="."]
        unique_name_in_owner = true
        position = Vector2(0, -360)

        [node name="CenterPos" type="Marker2D" parent="."]
        unique_name_in_owner = true
        position = Vector2(0, -160)

        [node name="OrbPos" type="Marker2D" parent="."]
        unique_name_in_owner = true
        position = Vector2(0, -350)

        [node name="TalkPos" type="Marker2D" parent="."]
        unique_name_in_owner = true
        position = Vector2(20, -295)

        [node name="FormVfx" type="Control" parent="."]
        unique_name_in_owner = true
        mouse_filter = 2

        [node name="AnimationPlayer" type="AnimationPlayer" parent="."]
        libraries = {"": SubResource("Library")}
        autoplay = "idle"
        """;

    private static string SeatedScene() => $$"""
        [gd_scene load_steps=2 format=3]
        [ext_resource type="CompressedTexture2D" path="{{RestTexture}}" id="1"]
        [node name="DuanYuRest" type="Sprite2D"]
        texture = ExtResource("1")
        scale = Vector2(0.8, 0.8)
        """;

    private static string ShopScene() => $$"""
        [gd_scene load_steps=2 format=3]
        [ext_resource type="CompressedTexture2D" path="{{CharacterTexture}}" id="1"]
        [node name="DuanYuMerchant" type="Node2D"]
        [node name="Visuals" type="Sprite2D" parent="."]
        unique_name_in_owner = true
        texture = ExtResource("1")
        position = Vector2(0, -160)
        """;

    private static string UiIconScene() => $$"""
        [gd_scene load_steps=2 format=3]
        [ext_resource type="CompressedTexture2D" path="{{CharacterIcon}}" id="1"]
        [node name="DuanYuIcon" type="TextureRect"]
        offset_right = 128.0
        offset_bottom = 128.0
        texture = ExtResource("1")
        expand_mode = 1
        stretch_mode = 5
        mouse_filter = 2
        """;

    private static string SelectionScene() => $$"""
        [gd_scene load_steps=3 format=3]
        [ext_resource type="CompressedTexture2D" path="{{ImagePath("selection-background", DuanYuArt.SelectBackground())}}" id="1"]
        [ext_resource type="CompressedTexture2D" path="{{CharacterTexture}}" id="2"]
        [node name="DuanYuSelection" type="Control"]
        layout_mode = 3
        anchors_preset = 15
        anchor_right = 1.0
        anchor_bottom = 1.0
        grow_horizontal = 2
        grow_vertical = 2
        mouse_filter = 2
        [node name="Landscape" type="TextureRect" parent="."]
        layout_mode = 1
        anchors_preset = 15
        anchor_right = 1.0
        anchor_bottom = 1.0
        grow_horizontal = 2
        grow_vertical = 2
        texture = ExtResource("1")
        expand_mode = 1
        stretch_mode = 6
        mouse_filter = 2
        [node name="Portrait" type="TextureRect" parent="."]
        layout_mode = 1
        anchor_left = 0.54
        anchor_top = 0.12
        anchor_right = 0.87
        anchor_bottom = 0.88
        texture = ExtResource("2")
        expand_mode = 1
        stretch_mode = 5
        mouse_filter = 2
        """;

    private static string SafeKey(string key) => string.Concat(key.Select(character => char.IsAsciiLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-'));
}
