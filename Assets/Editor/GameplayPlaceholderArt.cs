using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameplayPlaceholderArt
{
    public const string Folder = "Assets/Art/Gameplay/Generated/";

    public static void EnsureGenerated()
    {
        Directory.CreateDirectory(Folder);
        WriteSolid(Folder + "tile_stone.png", new Color(0.45f, 0.47f, 0.5f, 1f));
        WriteSolid(Folder + "tile_floor.png", new Color(0.62f, 0.58f, 0.52f, 1f));
        WriteSolid(Folder + "tile_dirt.png", new Color(0.45f, 0.3f, 0.18f, 1f));
        WriteSolid(Folder + "tile_gem.png", new Color(0.92f, 0.15f, 0.2f, 1f));
        WriteSolid(Folder + "tile_exit.png", new Color(0.35f, 0.28f, 0.22f, 1f));
        WriteSolid(Folder + "tile_exit_open.png", new Color(0.55f, 0.75f, 0.35f, 1f));
        WriteSolid(Folder + "player.png", new Color(0.82f, 0.62f, 0.38f, 1f));
        AssetDatabase.Refresh();
        ConfigureSprites();
    }

    static void WriteSolid(string path, Color color)
    {
        if (File.Exists(path))
            return;

        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color[64 * 64];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void ConfigureSprites()
    {
        foreach (var file in Directory.GetFiles(Folder, "*.png"))
            MenuUiFactory.SpriteAt(Path.GetFileName(file), Folder);
    }

    public static Sprite Load(string file)
    {
        return MenuUiFactory.SpriteAt(file, Folder);
    }
}
