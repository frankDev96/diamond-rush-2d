using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameplayArtImporter
{
    public const string SourceFolder = "ArtSource/Gameplay/Angkor/";
    public const string OutputFolder = "Assets/Art/Gameplay/Angkor/";
    public const int PixelsPerUnit = 128;

    enum Mode
    {
        Full,
        Key,
        KeyLeftHalf,
        KeyRightHalf
    }

    struct Entry
    {
        public string source;
        public string output;
        public Mode mode;
        public int size;
        public bool rotate90;
        public float clearCenter;

        public Entry(string source, string output, Mode mode, int size, bool rotate90 = false, float clearCenter = 0f)
        {
            this.source = source;
            this.output = output;
            this.mode = mode;
            this.size = size;
            this.rotate90 = rotate90;
            this.clearCenter = clearCenter;
        }
    }

    // Keep in sync with ArtSource/Gameplay/process_art.mjs.
    static readonly Entry[] Entries =
    {
        new Entry("floor_stone_single.jpg", "floor_stone.png", Mode.Full, 128),
        new Entry("floor_stone_single.jpg", "floor_stone_alt.png", Mode.Full, 128, true),
        new Entry("dirt_soft.jpg", "dirt_soft.png", Mode.Full, 128),
        new Entry("wall_block_full.jpg", "wall_block.png", Mode.Full, 128),
        new Entry("red_diamond.jpg", "red_diamond.png", Mode.Key, 128),
        new Entry("exit_door_closed.jpg", "exit_door_closed.png", Mode.Key, 384),
        new Entry("exit_door_open.jpg", "exit_door_open.png", Mode.Key, 384),
        new Entry("explorer_down.jpg", "explorer_down.png", Mode.Key, 256),
        new Entry("explorer_up.jpg", "explorer_up.png", Mode.Key, 256),
        new Entry("explorer_side.jpg", "explorer_side.png", Mode.Key, 256),
        new Entry("jungle_border_16x9.jpg", "jungle_border_16x9.png", Mode.Full, 1920),
        new Entry("joystick_base.jpg", "joystick_base.png", Mode.Key, 320, false, 0.165f),
        new Entry("joystick_knob.jpg", "joystick_knob.png", Mode.Key, 160),
        new Entry("btn_restart.jpg", "btn_restart.png", Mode.Key, 192),
        new Entry("btn_inventory.jpg", "btn_inventory.png", Mode.Key, 192),
        new Entry("btn_pause.jpg", "btn_pause.png", Mode.Key, 192),
        new Entry("red_diamond.jpg", "icon_red.png", Mode.Key, 96),
        new Entry("icon_purple.jpg", "icon_purple.png", Mode.Key, 96),
        new Entry("icon_lives_head.jpg", "icon_lives_head.png", Mode.Key, 96),
        new Entry("fx_dust_dirt.jpg", "dust_puff.png", Mode.KeyLeftHalf, 64),
        new Entry("fx_dust_dirt.jpg", "dirt_chunk.png", Mode.KeyRightHalf, 64),
    };

    [MenuItem("Diamond Rush/Process Gameplay Art")]
    public static void ProcessAll()
    {
        Directory.CreateDirectory(OutputFolder);
        foreach (var entry in Entries)
            Process(entry);

        AssetDatabase.Refresh();
        foreach (var entry in Entries)
            ConfigureImporter(OutputFolder + entry.output);
        Debug.Log("Diamond Rush gameplay art processed into " + OutputFolder);
    }

    public static bool HasProcessedArt()
    {
        foreach (var entry in Entries)
        {
            if (!File.Exists(OutputFolder + entry.output))
                return false;
        }

        return true;
    }

    public static Sprite Load(string file)
    {
        var path = OutputFolder + file;
        if (AssetImporter.GetAtPath(path) == null && File.Exists(path))
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        ConfigureImporter(path);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void Process(Entry entry)
    {
        var sourcePath = SourceFolder + entry.source;
        if (!File.Exists(sourcePath))
        {
            Debug.LogError("Missing gameplay art source: " + sourcePath);
            return;
        }

        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(sourcePath));
        var width = source.width;
        var height = source.height;
        var pixels = source.GetPixels();
        Object.DestroyImmediate(source);

        if (entry.mode == Mode.KeyLeftHalf || entry.mode == Mode.KeyRightHalf)
        {
            var half = width / 2;
            var offset = entry.mode == Mode.KeyLeftHalf ? 0 : half;
            var cropped = new Color[half * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < half; x++)
                    cropped[y * half + x] = pixels[y * width + offset + x];
            }

            pixels = cropped;
            width = half;
        }

        if (entry.mode != Mode.Full)
        {
            var key = pixels[0];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = KeyPixel(pixels[i], key);

            if (entry.clearCenter > 0f)
                ClearCenter(pixels, width, height, entry.clearCenter);

            pixels = Trim(pixels, ref width, ref height);
        }

        if (entry.rotate90)
            pixels = Rotate90(pixels, ref width, ref height);

        int outWidth;
        int outHeight;
        if (entry.mode == Mode.Full && width != height)
        {
            outWidth = entry.size;
            outHeight = Mathf.RoundToInt(entry.size * height / (float)width);
        }
        else
        {
            var scale = entry.size / (float)Mathf.Max(width, height);
            outWidth = Mathf.Max(1, Mathf.RoundToInt(width * scale));
            outHeight = Mathf.Max(1, Mathf.RoundToInt(height * scale));
        }

        var resized = Resample(pixels, width, height, outWidth, outHeight);
        var result = new Texture2D(outWidth, outHeight, TextureFormat.RGBA32, false);
        result.SetPixels(resized);
        result.Apply();
        File.WriteAllBytes(OutputFolder + entry.output, result.EncodeToPNG());
        Object.DestroyImmediate(result);
    }

    static Color KeyPixel(Color pixel, Color key)
    {
        const float low = 50f / 255f;
        const float high = 110f / 255f;
        var dr = pixel.r - key.r;
        var dg = pixel.g - key.g;
        var db = pixel.b - key.b;
        var distance = Mathf.Sqrt(dr * dr + dg * dg + db * db);
        var alpha = Mathf.Clamp01((distance - low) / (high - low));
        if (alpha <= 0f)
            return new Color(0f, 0f, 0f, 0f);

        var r = pixel.r;
        var g = pixel.g;
        var b = pixel.b;
        if (alpha < 1f)
        {
            // Un-blend the key colour out of semi-transparent edge pixels.
            r = Mathf.Clamp01((r - key.r * (1f - alpha)) / alpha);
            g = Mathf.Clamp01((g - key.g * (1f - alpha)) / alpha);
            b = Mathf.Clamp01((b - key.b * (1f - alpha)) / alpha);
        }

        // Glows blended over the key keep a tint; strip it and fade those pixels.
        var spill = 0f;
        if (key.r > 0.5f && key.b > 0.4f && key.g < 0.3f)
        {
            spill = Mathf.Max(0f, Mathf.Min(r, b) - g);
            r -= spill;
            b -= spill;
        }
        else if (key.g > 0.5f && key.r < 0.5f && key.b < 0.3f)
        {
            spill = Mathf.Max(0f, g - Mathf.Max(r, b));
            g -= spill;
        }

        alpha *= Mathf.Clamp01(1f - spill * 1.6f);
        return new Color(r, g, b, alpha);
    }

    static void ClearCenter(Color[] pixels, int width, int height, float radiusFraction)
    {
        var cx = width * 0.5f;
        var cy = height * 0.5f;
        var radius = width * radiusFraction;
        var feather = width * 0.01f;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d > radius + feather)
                    continue;
                var keep = Mathf.Clamp01((d - radius) / feather);
                var i = y * width + x;
                pixels[i].a *= keep;
            }
        }
    }

    static Color[] Trim(Color[] pixels, ref int width, ref int height)
    {
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a < 0.05f)
                    continue;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < 0)
            return pixels;

        const int pad = 2;
        minX = Mathf.Max(0, minX - pad);
        minY = Mathf.Max(0, minY - pad);
        maxX = Mathf.Min(width - 1, maxX + pad);
        maxY = Mathf.Min(height - 1, maxY + pad);
        var newWidth = maxX - minX + 1;
        var newHeight = maxY - minY + 1;
        var trimmed = new Color[newWidth * newHeight];
        for (var y = 0; y < newHeight; y++)
        {
            for (var x = 0; x < newWidth; x++)
                trimmed[y * newWidth + x] = pixels[(y + minY) * width + x + minX];
        }

        width = newWidth;
        height = newHeight;
        return trimmed;
    }

    static Color[] Rotate90(Color[] pixels, ref int width, ref int height)
    {
        var rotated = new Color[pixels.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                rotated[x * height + (height - 1 - y)] = pixels[y * width + x];
        }

        (width, height) = (height, width);
        return rotated;
    }

    static Color[] Resample(Color[] pixels, int width, int height, int outWidth, int outHeight)
    {
        var result = new Color[outWidth * outHeight];
        var samples = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(width / (float)outWidth, height / (float)outHeight)));
        for (var y = 0; y < outHeight; y++)
        {
            for (var x = 0; x < outWidth; x++)
            {
                float r = 0f, g = 0f, b = 0f, a = 0f;
                for (var sy = 0; sy < samples; sy++)
                {
                    for (var sx = 0; sx < samples; sx++)
                    {
                        var u = (x + (sx + 0.5f) / samples) / outWidth * width - 0.5f;
                        var v = (y + (sy + 0.5f) / samples) / outHeight * height - 0.5f;
                        var c = Bilinear(pixels, width, height, u, v);
                        r += c.r * c.a;
                        g += c.g * c.a;
                        b += c.b * c.a;
                        a += c.a;
                    }
                }

                var count = samples * samples;
                result[y * outWidth + x] = a > 0f
                    ? new Color(r / a, g / a, b / a, a / count)
                    : new Color(0f, 0f, 0f, 0f);
            }
        }

        return result;
    }

    static Color Bilinear(Color[] pixels, int width, int height, float u, float v)
    {
        var x0 = Mathf.Clamp(Mathf.FloorToInt(u), 0, width - 1);
        var y0 = Mathf.Clamp(Mathf.FloorToInt(v), 0, height - 1);
        var x1 = Mathf.Min(x0 + 1, width - 1);
        var y1 = Mathf.Min(y0 + 1, height - 1);
        var tx = Mathf.Clamp01(u - x0);
        var ty = Mathf.Clamp01(v - y0);
        var top = Color.Lerp(pixels[y0 * width + x0], pixels[y0 * width + x1], tx);
        var bottom = Color.Lerp(pixels[y1 * width + x0], pixels[y1 * width + x1], tx);
        return Color.Lerp(top, bottom, ty);
    }

    static void ConfigureImporter(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        var changed = importer.textureType != TextureImporterType.Sprite
            || importer.spritePixelsPerUnit != PixelsPerUnit
            || importer.mipmapEnabled
            || importer.filterMode != FilterMode.Bilinear
            || importer.textureCompression != TextureImporterCompression.Uncompressed;
        if (!changed)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
