using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameplayArtImporter
{
    public const string SourceFolder = "ArtSource/Gameplay/Angkor/";
    public const string OutputFolder = "Assets/Art/Gameplay/Angkor/";
    public const int PixelsPerUnit = 128;
    public const int FrameWidth = 160;
    public const int FrameHeight = 256;
    public static readonly Vector2 FeetPivot = new Vector2(0.5f, 0.04f);

    enum Mode
    {
        Full,
        Key,
        KeyLeftHalf,
        KeyRightHalf,
        KeyCenter,
        Grid2x2
    }

    class Entry
    {
        public string source;
        public string output;
        public Mode mode;
        public int size;
        public bool rotate90;
        public float clearCenter;
        public float inset;
        public float cropTop;
        public float cropBottom;
        public float seamless;
        public bool tiled;
        public bool feetPivot;

        public Entry(string source, string output, Mode mode, int size)
        {
            this.source = source;
            this.output = output;
            this.mode = mode;
            this.size = size;
        }

        public IEnumerable<string> Outputs()
        {
            if (mode != Mode.Grid2x2)
            {
                yield return output;
                yield break;
            }

            var stem = Path.GetFileNameWithoutExtension(output);
            for (var i = 0; i < 4; i++)
                yield return stem + "_" + i + ".png";
        }
    }

    // Keep in sync with ArtSource/Gameplay/process_art.mjs.
    static readonly Entry[] Entries =
    {
        new Entry("angkor_floor_tile.png", "floor_stone.png", Mode.Full, 128) { inset = 0.07f, seamless = 0.12f },
        new Entry("angkor_floor_tile.png", "floor_stone_alt.png", Mode.Full, 128) { inset = 0.07f, seamless = 0.12f, rotate90 = true },
        new Entry("angkor_floor_tile.png", "floor_moss.png", Mode.Full, 128) { inset = 0.03f, seamless = 0.1f },
        new Entry("dirt_soft.jpg", "dirt_soft.png", Mode.Full, 128),
        new Entry("angkor_wall_front.jpg", "wall_block.png", Mode.Full, 128) { inset = 0.03f },
        new Entry("angkor_wall_front.jpg", "wall_top.png", Mode.Full, 128) { inset = 0.04f, cropTop = 0.3f, seamless = 0.16f },
        new Entry("angkor_wall_front.jpg", "jungle_fill.png", Mode.Full, 256) { inset = 0.03f, cropTop = 0.28f, cropBottom = 0.22f, seamless = 0.18f, tiled = true },
        new Entry("red_diamond.jpg", "red_diamond.png", Mode.Key, 128),
        new Entry("exit_door_closed.jpg", "exit_door_closed.png", Mode.Key, 384),
        new Entry("exit_door_open.jpg", "exit_door_open.png", Mode.Key, 384),
        new Entry("explorer_down.jpg", "explorer_down.png", Mode.Key, 256) { feetPivot = true },
        new Entry("explorer_up.jpg", "explorer_up.png", Mode.Key, 256) { feetPivot = true },
        new Entry("explorer_side.jpg", "explorer_side.png", Mode.Key, 256) { feetPivot = true },
        new Entry("explorer_walk_down.jpg", "explorer_walk_down.png", Mode.Grid2x2, FrameHeight) { feetPivot = true },
        new Entry("explorer_walk_up.jpg", "explorer_walk_up.png", Mode.Grid2x2, FrameHeight) { feetPivot = true },
        new Entry("explorer_walk_side.jpg", "explorer_walk_side.png", Mode.Grid2x2, FrameHeight) { feetPivot = true },
        new Entry("jungle_border_16x9.jpg", "jungle_border_16x9.png", Mode.Full, 1920),
        new Entry("jungle_vignette_16x9.jpg", "jungle_vignette_16x9.png", Mode.KeyCenter, 1920),
        new Entry("joystick_base.jpg", "joystick_base.png", Mode.Key, 320) { clearCenter = 0.165f },
        new Entry("joystick_knob.jpg", "joystick_knob.png", Mode.Key, 160),
        new Entry("btn_restart.jpg", "btn_restart.png", Mode.Key, 192),
        new Entry("btn_inventory.jpg", "btn_inventory.png", Mode.Key, 192),
        new Entry("btn_pause.jpg", "btn_pause.png", Mode.Key, 192),
        new Entry("red_diamond.jpg", "icon_red.png", Mode.Key, 96),
        new Entry("icon_purple.jpg", "icon_purple.png", Mode.Key, 96),
        new Entry("icon_lives_head.jpg", "icon_lives_head.png", Mode.Key, 96),
        new Entry("exit_arrow.jpg", "exit_arrow.png", Mode.Key, 96),
        new Entry("fx_dust_dirt.jpg", "dust_puff.png", Mode.KeyLeftHalf, 64),
        new Entry("fx_dust_dirt.jpg", "dirt_chunk.png", Mode.KeyRightHalf, 64),
    };

    struct Image
    {
        public Color[] px;
        public int width;
        public int height;

        public Image(Color[] px, int width, int height)
        {
            this.px = px;
            this.width = width;
            this.height = height;
        }
    }

    [MenuItem("Diamond Rush/Process Gameplay Art")]
    public static void ProcessAll()
    {
        Directory.CreateDirectory(OutputFolder);
        foreach (var entry in Entries)
            Process(entry);

        AssetDatabase.Refresh();
        foreach (var entry in Entries)
        {
            foreach (var output in entry.Outputs())
                ConfigureImporter(OutputFolder + output, entry);
        }

        Debug.Log("Diamond Rush gameplay art processed into " + OutputFolder);
    }

    public static bool HasProcessedArt()
    {
        foreach (var entry in Entries)
        {
            foreach (var output in entry.Outputs())
            {
                if (!File.Exists(OutputFolder + output))
                    return false;
            }
        }

        return true;
    }

    public static Sprite Load(string file)
    {
        var path = OutputFolder + file;
        if (!File.Exists(path))
            return null;
        if (AssetImporter.GetAtPath(path) == null)
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        ConfigureImporter(path, FindEntry(file));
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static Sprite[] LoadFrames(string stem)
    {
        var frames = new List<Sprite>();
        for (var i = 0; i < 4; i++)
        {
            var sprite = Load(stem + "_" + i + ".png");
            if (sprite != null)
                frames.Add(sprite);
        }

        return frames.Count == 4 ? frames.ToArray() : new Sprite[0];
    }

    static Entry FindEntry(string file)
    {
        foreach (var entry in Entries)
        {
            foreach (var output in entry.Outputs())
            {
                if (output == file)
                    return entry;
            }
        }

        return null;
    }

    static void Process(Entry entry)
    {
        var sourcePath = SourceFolder + entry.source;
        if (!File.Exists(sourcePath))
        {
            Debug.LogError("Missing gameplay art source: " + sourcePath);
            return;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(sourcePath));
        var image = new Image(texture.GetPixels(), texture.width, texture.height);
        Object.DestroyImmediate(texture);

        if (entry.mode == Mode.Grid2x2)
        {
            ProcessSheet(entry, image);
            return;
        }

        if (entry.mode == Mode.KeyLeftHalf || entry.mode == Mode.KeyRightHalf)
        {
            var half = image.width / 2;
            image = Crop(image, entry.mode == Mode.KeyLeftHalf ? 0 : half, 0, half, image.height);
        }

        if (entry.inset > 0f)
        {
            var dx = Mathf.RoundToInt(image.width * entry.inset);
            var dy = Mathf.RoundToInt(image.height * entry.inset);
            image = Crop(image, dx, dy, image.width - dx * 2, image.height - dy * 2);
        }

        if (entry.cropTop > 0f && entry.cropTop < 1f)
        {
            var keepH = Mathf.Max(1, Mathf.RoundToInt(image.height * entry.cropTop));
            var y0 = image.height - keepH;
            image = Crop(image, 0, y0, image.width, keepH);
        }

        if (entry.cropBottom > 0f && entry.cropBottom < 1f)
        {
            var keepH = Mathf.Max(1, Mathf.RoundToInt(image.height * entry.cropBottom));
            image = Crop(image, 0, 0, image.width, keepH);
        }

        if (entry.mode != Mode.Full)
        {
            var key = entry.mode == Mode.KeyCenter
                ? image.px[(image.height / 2) * image.width + image.width / 2]
                : image.px[0];
            KeyImage(image, key);
            if (entry.clearCenter > 0f)
                ClearCenter(image, entry.clearCenter);
            if (entry.mode != Mode.KeyCenter)
                image = Trim(image);
        }

        if (entry.seamless > 0f)
            image = MakeSeamless(image, entry.seamless);

        if (entry.rotate90)
            image = Rotate90(image);

        int outWidth;
        int outHeight;
        if (entry.seamless > 0f)
        {
            outWidth = outHeight = entry.size;
        }
        else if (entry.mode == Mode.Full || entry.mode == Mode.KeyCenter)
        {
            outWidth = entry.size;
            outHeight = Mathf.RoundToInt(entry.size * image.height / (float)image.width);
        }
        else
        {
            var scale = entry.size / (float)Mathf.Max(image.width, image.height);
            outWidth = Mathf.Max(1, Mathf.RoundToInt(image.width * scale));
            outHeight = Mathf.Max(1, Mathf.RoundToInt(image.height * scale));
        }

        Write(OutputFolder + entry.output, Resample(image, outWidth, outHeight));
    }

    static void ProcessSheet(Entry entry, Image sheet)
    {
        KeyImage(sheet, sheet.px[0]);
        var qw = sheet.width / 2;
        var qh = sheet.height / 2;

        // Unity pixel rows start at the bottom: frame 0 is the top-left quadrant.
        var origins = new[]
        {
            new Vector2Int(0, qh),
            new Vector2Int(qw, qh),
            new Vector2Int(0, 0),
            new Vector2Int(qw, 0),
        };

        int minX = qw, minY = qh, maxX = -1, maxY = -1;
        foreach (var origin in origins)
        {
            for (var y = 0; y < qh; y++)
            {
                for (var x = 0; x < qw; x++)
                {
                    if (sheet.px[(origin.y + y) * sheet.width + origin.x + x].a < 0.05f)
                        continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }

        if (maxX < 0)
        {
            Debug.LogError("Empty sprite sheet: " + entry.source);
            return;
        }

        minX = Mathf.Max(0, minX - 2);
        minY = Mathf.Max(0, minY - 2);
        maxX = Mathf.Min(qw - 1, maxX + 2);
        maxY = Mathf.Min(qh - 1, maxY + 2);
        var boxWidth = maxX - minX + 1;
        var boxHeight = maxY - minY + 1;
        var scale = Mathf.Min(FrameWidth / (float)boxWidth, (FrameHeight - 8) / (float)boxHeight);
        var fitWidth = Mathf.Max(1, Mathf.RoundToInt(boxWidth * scale));
        var fitHeight = Mathf.Max(1, Mathf.RoundToInt(boxHeight * scale));

        var stem = Path.GetFileNameWithoutExtension(entry.output);
        for (var i = 0; i < origins.Length; i++)
        {
            var frame = Crop(sheet, origins[i].x + minX, origins[i].y + minY, boxWidth, boxHeight);
            var fitted = Resample(frame, fitWidth, fitHeight);
            var canvas = new Color[FrameWidth * FrameHeight];
            var offsetX = (FrameWidth - fitWidth) / 2;
            const int offsetY = 8;
            for (var y = 0; y < fitHeight; y++)
            {
                for (var x = 0; x < fitWidth; x++)
                    canvas[(y + offsetY) * FrameWidth + x + offsetX] = fitted.px[y * fitWidth + x];
            }

            Write(OutputFolder + stem + "_" + i + ".png", new Image(canvas, FrameWidth, FrameHeight));
        }
    }

    static void Write(string path, Image image)
    {
        var result = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        result.SetPixels(image.px);
        result.Apply();
        File.WriteAllBytes(path, result.EncodeToPNG());
        Object.DestroyImmediate(result);
    }

    static void KeyImage(Image image, Color key)
    {
        for (var i = 0; i < image.px.Length; i++)
            image.px[i] = KeyPixel(image.px[i], key);
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

    static void ClearCenter(Image image, float radiusFraction)
    {
        var cx = image.width * 0.5f;
        var cy = image.height * 0.5f;
        var radius = image.width * radiusFraction;
        var feather = image.width * 0.01f;
        for (var y = 0; y < image.height; y++)
        {
            for (var x = 0; x < image.width; x++)
            {
                var d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d > radius + feather)
                    continue;
                image.px[y * image.width + x].a *= Mathf.Clamp01((d - radius) / feather);
            }
        }
    }

    // Cross-fades a band from the far edge into the near edge on each axis so the result wraps.
    static Image MakeSeamless(Image image, float bandFraction)
    {
        var band = Mathf.Max(1, Mathf.RoundToInt(image.width * bandFraction));
        var tileWidth = image.width - band;
        var horizontal = new Color[tileWidth * image.height];
        for (var y = 0; y < image.height; y++)
        {
            for (var x = 0; x < tileWidth; x++)
            {
                var c = image.px[y * image.width + x];
                if (x < band)
                    c = Color.Lerp(image.px[y * image.width + x + tileWidth], c, x / (float)band);
                horizontal[y * tileWidth + x] = c;
            }
        }

        var bandY = Mathf.Max(1, Mathf.RoundToInt(image.height * bandFraction));
        var tileHeight = image.height - bandY;
        var result = new Color[tileWidth * tileHeight];
        for (var y = 0; y < tileHeight; y++)
        {
            for (var x = 0; x < tileWidth; x++)
            {
                var c = horizontal[y * tileWidth + x];
                if (y < bandY)
                    c = Color.Lerp(horizontal[(y + tileHeight) * tileWidth + x], c, y / (float)bandY);
                result[y * tileWidth + x] = c;
            }
        }

        return new Image(result, tileWidth, tileHeight);
    }

    static Image Crop(Image image, int x0, int y0, int width, int height)
    {
        var cropped = new Color[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                cropped[y * width + x] = image.px[(y + y0) * image.width + x + x0];
        }

        return new Image(cropped, width, height);
    }

    static Image Trim(Image image)
    {
        int minX = image.width, minY = image.height, maxX = -1, maxY = -1;
        for (var y = 0; y < image.height; y++)
        {
            for (var x = 0; x < image.width; x++)
            {
                if (image.px[y * image.width + x].a < 0.05f)
                    continue;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < 0)
            return image;

        const int pad = 2;
        minX = Mathf.Max(0, minX - pad);
        minY = Mathf.Max(0, minY - pad);
        maxX = Mathf.Min(image.width - 1, maxX + pad);
        maxY = Mathf.Min(image.height - 1, maxY + pad);
        return Crop(image, minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    static Image Rotate90(Image image)
    {
        var rotated = new Color[image.px.Length];
        for (var y = 0; y < image.height; y++)
        {
            for (var x = 0; x < image.width; x++)
                rotated[x * image.height + (image.height - 1 - y)] = image.px[y * image.width + x];
        }

        return new Image(rotated, image.height, image.width);
    }

    static Image Resample(Image image, int outWidth, int outHeight)
    {
        var result = new Color[outWidth * outHeight];
        var samples = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(image.width / (float)outWidth, image.height / (float)outHeight)));
        for (var y = 0; y < outHeight; y++)
        {
            for (var x = 0; x < outWidth; x++)
            {
                float r = 0f, g = 0f, b = 0f, a = 0f;
                for (var sy = 0; sy < samples; sy++)
                {
                    for (var sx = 0; sx < samples; sx++)
                    {
                        var u = (x + (sx + 0.5f) / samples) / outWidth * image.width - 0.5f;
                        var v = (y + (sy + 0.5f) / samples) / outHeight * image.height - 0.5f;
                        var c = Bilinear(image, u, v);
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

        return new Image(result, outWidth, outHeight);
    }

    static Color Bilinear(Image image, float u, float v)
    {
        var x0 = Mathf.Clamp(Mathf.FloorToInt(u), 0, image.width - 1);
        var y0 = Mathf.Clamp(Mathf.FloorToInt(v), 0, image.height - 1);
        var x1 = Mathf.Min(x0 + 1, image.width - 1);
        var y1 = Mathf.Min(y0 + 1, image.height - 1);
        var tx = Mathf.Clamp01(u - x0);
        var ty = Mathf.Clamp01(v - y0);
        var top = Color.Lerp(image.px[y0 * image.width + x0], image.px[y0 * image.width + x1], tx);
        var bottom = Color.Lerp(image.px[y1 * image.width + x0], image.px[y1 * image.width + x1], tx);
        return Color.Lerp(top, bottom, ty);
    }

    static void ConfigureImporter(string path, Entry entry)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        var pivot = entry != null && entry.feetPivot ? FeetPivot : new Vector2(0.5f, 0.5f);
        var wrap = entry != null && entry.tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        var changed = importer.textureType != TextureImporterType.Sprite
            || importer.spritePixelsPerUnit != PixelsPerUnit
            || importer.mipmapEnabled
            || importer.filterMode != FilterMode.Bilinear
            || importer.wrapMode != wrap
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || settings.spriteMeshType != SpriteMeshType.FullRect
            || settings.spriteAlignment != (int)SpriteAlignment.Custom
            || settings.spritePivot != pivot;
        if (!changed)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = wrap;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
}
