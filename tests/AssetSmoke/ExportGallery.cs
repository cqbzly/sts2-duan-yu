using System.Security.Cryptography;
using System.Text;
using DuanYu.Assets;
using Godot;
using File = System.IO.File;

internal static class ExportGallery
{
    internal static void RunIfRequested()
    {
        var arguments = OS.GetCmdlineUserArgs();
        var flagIndex = Array.IndexOf(arguments, "--export-art");
        if (flagIndex < 0) return;
        if (flagIndex + 1 >= arguments.Length) throw new ArgumentException("--export-art requires an output directory.");
        var outputDirectory = Path.GetFullPath(arguments[flagIndex + 1]);
        Directory.CreateDirectory(outputDirectory);
        var portraits = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (prefix, count) in new[] { ("B", 5), ("C", 20), ("U", 35), ("R", 20), ("T", 6) })
        {
            for (var number = 1; number <= count; number++)
            {
                var designId = $"DY_{prefix}{number:00}";
                var source = DuanYuArt.Card(designId);
                var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
                if (!portraits.Add(digest)) throw new InvalidOperationException("Duplicate card artwork: " + designId);
                File.WriteAllText(Path.Combine(outputDirectory, designId + ".svg"), source, new UTF8Encoding(false));
                using var image = DuanYuAssets.Texture(DuanYuAssets.CardPortrait(designId)).GetImage();
                if (image.GetWidth() != 1000 || image.GetHeight() != 760)
                    throw new InvalidOperationException("Incorrect card portrait dimensions: " + designId);
                if (image.SavePng(Path.Combine(outputDirectory, designId + ".png")) != Error.Ok)
                    throw new IOException("Could not export card artwork: " + designId);
            }
        }
        GD.Print($"ART_EXPORT_OK: {portraits.Count} distinct portraits exported to {outputDirectory}");
    }
}
