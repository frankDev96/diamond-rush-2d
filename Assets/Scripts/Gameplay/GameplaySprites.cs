using UnityEngine;

public static class GameplaySprites
{
    static Sprite softCircle;
    static Sprite ovalShadow;
    static Sprite whiteSquare;
    static Sprite shovel;
    static Texture2D dustSpeckTexture;
    static Texture2D pebbleTexture;
    static Texture2D starSparkleTexture;

    public static Sprite SoftCircle
    {
        get
        {
            if (softCircle != null)
                return softCircle;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                    var a = Mathf.Clamp01(1f - d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            softCircle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return softCircle;
        }
    }

    public static Sprite OvalShadow
    {
        get
        {
            if (ovalShadow != null)
                return ovalShadow;

            const int w = 64;
            const int h = 32;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[w * h];
            var cx = (w - 1) * 0.5f;
            var cy = (h - 1) * 0.5f;
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var dx = (x - cx) / cx;
                    var dy = (y - cy) / cy;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = Mathf.Clamp01(1f - d);
                    pixels[y * w + x] = new Color(1f, 1f, 1f, a * a);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            ovalShadow = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
            return ovalShadow;
        }
    }

    public static Sprite WhiteSquare
    {
        get
        {
            if (whiteSquare != null)
                return whiteSquare;

            var texture = Texture2D.whiteTexture;
            whiteSquare = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            return whiteSquare;
        }
    }

    public static Sprite Shovel
    {
        get
        {
            if (shovel != null)
                return shovel;

            const int s = 64;
            var texture = new Texture2D(s, s, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[s * s];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Wooden shaft from (10, 10) to (38, 38)
            for (var t = 8; t <= 38; t++)
            {
                for (var w = -1; w <= 1; w++)
                {
                    var px = t - w;
                    var py = t + w;
                    if (px >= 0 && px < s && py >= 0 && py < s)
                    {
                        var wood = (w == 0) ? new Color(0.70f, 0.45f, 0.22f, 1f) : new Color(0.50f, 0.30f, 0.15f, 1f);
                        pixels[py * s + px] = wood;
                    }
                }
            }

            // Grip at top (8, 8)
            for (var y = 4; y <= 12; y++)
            {
                for (var x = 4; x <= 12; x++)
                {
                    var dx = x - 8;
                    var dy = y - 8;
                    var distSq = dx * dx + dy * dy;
                    if (distSq >= 4 && distSq <= 16)
                        pixels[y * s + x] = new Color(0.55f, 0.35f, 0.18f, 1f);
                }
            }

            // Shovel spade blade from (36, 36) to (58, 58)
            for (var y = 30; y < 60; y++)
            {
                for (var x = 30; x < 60; x++)
                {
                    var u = ((x - 36) + (y - 36)) * 0.7071f;
                    var v = ((y - 36) - (x - 36)) * 0.7071f;
                    if (u >= 0 && u <= 26f && Mathf.Abs(v) <= (15f - u * 0.45f))
                    {
                        var metal = (v > 0) ? new Color(0.82f, 0.85f, 0.90f, 1f) : new Color(0.60f, 0.64f, 0.70f, 1f);
                        if (u > 23f)
                            metal = new Color(0.96f, 0.98f, 1f, 1f);
                        pixels[y * s + x] = metal;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            shovel = Sprite.Create(texture, new Rect(0, 0, s, s), new Vector2(0.25f, 0.25f), s);
            return shovel;
        }
    }

    public static Texture2D DustSpeckTexture
    {
        get
        {
            if (dustSpeckTexture != null)
                return dustSpeckTexture;

            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                    var a = Mathf.Clamp01(1f - d * d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            dustSpeckTexture = texture;
            return dustSpeckTexture;
        }
    }

    public static Texture2D PebbleTexture
    {
        get
        {
            if (pebbleTexture != null)
                return pebbleTexture;

            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    var angle = Mathf.Atan2(dy, dx);
                    var shape = 0.75f + 0.2f * Mathf.Sin(angle * 3f) + 0.1f * Mathf.Cos(angle * 5f);
                    var a = r <= shape ? 1f : 0f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            pebbleTexture = texture;
            return pebbleTexture;
        }
    }

    public static Texture2D StarSparkleTexture
    {
        get
        {
            if (starSparkleTexture != null)
                return starSparkleTexture;

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs(x - center) / center;
                    var dy = Mathf.Abs(y - center) / center;
                    var rayX = Mathf.Clamp01(1f - dx) * Mathf.Clamp01(1f - dy * 7f);
                    var rayY = Mathf.Clamp01(1f - dy) * Mathf.Clamp01(1f - dx * 7f);
                    var dDiag = Mathf.Abs(dx - dy);
                    var distDiag = Mathf.Sqrt(dx * dx + dy * dy);
                    var diagRay = Mathf.Clamp01(1f - distDiag * 1.5f) * Mathf.Clamp01(1f - dDiag * 4f) * 0.5f;
                    var core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 3f);
                    var intensity = Mathf.Clamp01(rayX + rayY + diagRay + core);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, intensity * intensity);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            starSparkleTexture = texture;
            return starSparkleTexture;
        }
    }

    public static SpriteRenderer Spawn(string name, Transform parent, Sprite sprite, Vector3 position, float worldWidth, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        FitWidth(go.transform, sprite, worldWidth);
        return renderer;
    }

    public static SpriteRenderer SpawnOval(string name, Transform parent, Sprite sprite, Vector3 position, float worldWidth, float worldHeight, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        if (sprite != null)
        {
            var size = sprite.bounds.size;
            go.transform.localScale = new Vector3(worldWidth / size.x, worldHeight / size.y, 1f);
        }
        return renderer;
    }

    public static void FitWidth(Transform target, Sprite sprite, float worldWidth)
    {
        if (sprite == null)
            return;
        var scale = worldWidth / sprite.bounds.size.x;
        target.localScale = new Vector3(scale, scale, 1f);
    }

    public static void FitCell(Transform target, Sprite sprite, float cellSize)
    {
        if (sprite == null)
            return;
        var size = sprite.bounds.size;
        target.localScale = new Vector3(cellSize / size.x, cellSize / size.y, 1f);
    }
}
