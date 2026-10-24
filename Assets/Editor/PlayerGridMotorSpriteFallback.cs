#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fills missing walk sprites on PlayerGridMotor when playing in the Editor (runtime scripts cannot reference GameplayArtImporter).
/// </summary>
[InitializeOnLoad]
static class PlayerGridMotorSpriteFallback
{
    static PlayerGridMotorSpriteFallback()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;

        foreach (var motor in Object.FindObjectsByType<PlayerGridMotor>(FindObjectsInactive.Include))
            ApplyIfMissing(motor);
    }

    static void ApplyIfMissing(PlayerGridMotor motor)
    {
        var so = new SerializedObject(motor);
        if (NeedsWalkArray(so, "walkDown"))
            SetWalkArray(so, "walkDown", GameplayArtImporter.LoadFrames("explorer_walk_down"));
        if (NeedsWalkArray(so, "walkUp"))
            SetWalkArray(so, "walkUp", GameplayArtImporter.LoadFrames("explorer_walk_up"));
        if (NeedsWalkArray(so, "walkSide"))
            SetWalkArray(so, "walkSide", GameplayArtImporter.LoadFrames("explorer_walk_side"));
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static bool NeedsWalkArray(SerializedObject so, string property)
    {
        var prop = so.FindProperty(property);
        return prop == null || prop.arraySize < 4;
    }

    static void SetWalkArray(SerializedObject so, string property, Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
            return;
        var prop = so.FindProperty(property);
        prop.arraySize = sprites.Length;
        for (var i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
#endif
