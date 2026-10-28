using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class GameplayLevelsBuilder
{
    // Bump when Level 2-9 rows / scene wiring change. Missing scenes always rebuild.
    const int LayoutVersion = 19;

    static GameplayLevelsBuilder()
    {
        EditorApplication.delayCall += AutoBuild;
    }

    static void AutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += AutoBuild;
            return;
        }

        if (!MenuUiFactory.TryReadyFont())
        {
            MenuUiFactory.QueueTmpImport(AutoBuild);
            return;
        }

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
        var missing = !File.Exists(scenePath);
        if (!force && !missing && EditorPrefs.GetInt(pref, 0) == LayoutVersion)
            return;

        UnityEngine.Debug.Log("Diamond Rush building gameplay level " + level +
                              (missing ? " (scene missing)" : " (layout stale)") + "…");
        GameplayLevel1Builder.BuildSceneForSpec(spec, scenePath, pref, LayoutVersion);
    }
}
