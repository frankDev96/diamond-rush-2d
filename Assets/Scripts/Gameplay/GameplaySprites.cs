using UnityEngine;

public static class GameplaySprites
{
    static Sprite softCircle;
    static Sprite whiteSquare;

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
