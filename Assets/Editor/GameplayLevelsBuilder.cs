using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class GameplayLevelsBuilder
{
    const int LayoutVersion = 14;

    static GameplayLevelsBuilder()
    {
        EditorApplication.delayCall += AutoBuild;
    }

    static void AutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        for (var level = 2; level <= 9; level++)
            BuildIfStale(level);
    }

    [MenuItem("Diamond Rush/Build Angkor Gameplay Levels 2-9")]
    public static void BuildAll()
    {
        for (var level = 2; level <= 9; level++)
            BuildIfStale(level, true);
    }

    static void BuildIfStale(int level, bool force = false)
    {
        var spec = GameplayLevelCatalog.Get(level);
        var scenePath = "Assets/Scenes/GameplayLevel" + level + ".unity";
        var pref = "DiamondRush.GameplayLevel" + level + "LayoutVersion";
        if (!force && File.Exists(scenePath) && EditorPrefs.GetInt(pref, 0) == LayoutVersion)
            return;

        GameplayLevel1Builder.BuildSceneForSpec(spec, scenePath, pref, LayoutVersion);
    }
}
