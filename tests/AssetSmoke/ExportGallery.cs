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
        int embeddedCount = 0;
        foreach (var (prefix, count) in new[] { ("B", 5), ("C", 23), ("U", 38), ("R", 22), ("A", 1), ("T", 6), ("S", 2) })
        {
            for (var number = 1; number <= count; number++)
            {
                var designId = $"DY_{prefix}{number:00}";
                using var image = DuanYuAssets.Texture(DuanYuAssets.CardPortrait(designId)).GetImage();
                if (image.GetWidth() != 1000 || image.GetHeight() != 760)
                    throw new InvalidOperationException("Incorrect card portrait dimensions: " + designId);
                byte[] pixels = image.GetData();
                if (!portraits.Add(Convert.ToHexString(SHA256.HashData(pixels))))
                    throw new InvalidOperationException("Duplicate card artwork: " + designId);

                string svgPath = Path.Combine(outputDirectory, designId + ".svg");
                using Stream? embedded = DuanYuAssets.OpenEmbeddedCardPortrait(designId);
                if (embedded is null)
                {
                    File.WriteAllText(svgPath, DuanYuArt.Card(designId), new UTF8Encoding(false));
                }
                else
                {
                    using var buffer = new MemoryStream();
                    embedded.CopyTo(buffer);
                    using var source = new Image();
                    if (source.LoadPngFromBuffer(buffer.ToArray()) != Error.Ok)
                        throw new InvalidOperationException("Invalid embedded card portrait: " + designId);
                    source.Convert(Image.Format.Rgba8);
                    if (!pixels.AsSpan().SequenceEqual(source.GetData()))
                        throw new InvalidOperationException("Loaded texture differs from embedded portrait: " + designId);
                    File.Delete(svgPath);
                    embeddedCount++;
                }

                if (image.SavePng(Path.Combine(outputDirectory, designId + ".png")) != Error.Ok)
                    throw new IOException("Could not export card artwork: " + designId);
            }
        }
        GD.Print($"ART_EXPORT_OK: {portraits.Count} distinct portraits ({embeddedCount} embedded PNG) exported to {outputDirectory}");
    }
}
