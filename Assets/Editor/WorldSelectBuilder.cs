using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class WorldSelectBuilder
{
    const string ScenePath = "Assets/Scenes/WorldSelect.unity";
    const string LayoutPref = "DiamondRush.WorldSelectLayoutVersion";
    const int LayoutVersion = 1;

    static WorldSelectBuilder()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.delayCall += AutoBuild;
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += AutoBuild;
    }

    static void AutoBuild()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += AutoBuild;
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += AutoBuild;
            return;
        }

        if (File.Exists(ScenePath) && EditorPrefs.GetInt(LayoutPref, 0) == LayoutVersion)
            return;

        Build();
    }

    [MenuItem("Diamond Rush/Build World Select Screen")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += Build;
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Build;
            return;
        }

        if (!MenuUiFactory.TryReadyFont())
        {
            MenuUiFactory.QueueTmpImport(Build);
            return;
        }

        BuildScene();
    }

    static void BuildScene()
    {
        MenuUiFactory.ApplyPlayerDefaults();
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MenuUiFactory.CreateCamera();
        MenuUiFactory.CreateEventSystem();
        MenuUiFactory.CreateMenuMusic();

        var canvas = MenuUiFactory.CreateCanvas().transform;
        var background = MenuUiFactory.CreateSprite("Background", canvas, "bg_temple.png", false);
        MenuUiFactory.Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().raycastTarget = false;

        var title = MenuUiFactory.CreateText("Title", canvas, "SELECT WORLD", 48, FontStyles.Bold, MenuUiFactory.Title, TextAlignmentOptions.Center);
        MenuUiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(700f, 72f));
        MenuUiFactory.ApplyOutline(title, 0.22f);

        var controllerGo = new GameObject("WorldSelect");
        var controller = controllerGo.AddComponent<WorldSelectController>();

        var back = MenuUiFactory.CreateMenuButton(canvas, "button_wood.png", "BACK", null, 96f, 16f, out _);
        var backRect = back.GetComponent<RectTransform>();
        MenuUiFactory.Place(backRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(240f, 96f));
        UnityEventTools.AddPersistentListener(back.onClick, controller.OnBack);

        var row = MenuUiFactory.CreateRect("Worlds", canvas);
        MenuUiFactory.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1680f, 620f));
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 36f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.padding = new RectOffset(24, 24, 12, 12);

        CreateWorldCard(row, controller, 0, "ANGKOR WAT", "Unlocked", true);
        CreateWorldCard(row, controller, 1, "AMAZON", "Locked", false);
        CreateWorldCard(row, controller, 2, "PYRAMIDS", "Locked", false);

        var toast = MenuUiFactory.CreateToast(canvas, out var toastLabel);
        var so = new SerializedObject(controller);
        MenuUiFactory.SetRef(so, "toastRoot", toast);
        MenuUiFactory.SetRef(so, "toastLabel", toastLabel);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorPrefs.SetInt(LayoutPref, LayoutVersion);
        MenuUiFactory.SetMenuBuildScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Diamond Rush world select saved to " + ScenePath);
    }

    static void CreateWorldCard(Transform parent, WorldSelectController controller, int index, string name, string status, bool unlocked)
    {
        var card = MenuUiFactory.CreateSprite("World " + name, parent, "stone_board.png", false);
        var layout = card.AddComponent<LayoutElement>();
        layout.preferredWidth = 500f;
        layout.minHeight = 520f;

        var button = card.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        UnityEventTools.AddIntPersistentListener(button.onClick, controller.SelectWorld, index);

        var title = MenuUiFactory.CreateText("Name", card.transform, name, 32, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Center);
        MenuUiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 80f));

        var icon = MenuUiFactory.CreateSprite("Icon", card.transform, unlocked ? "icon_diamond.png" : "icon_dirt.png", true);
        MenuUiFactory.Place(icon.GetComponent<RectTransform>(), new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
        icon.GetComponent<Image>().raycastTarget = false;
        if (!unlocked)
            icon.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.45f);

        var label = MenuUiFactory.CreateText("Status", card.transform, status.ToUpperInvariant(), 22, FontStyles.Bold, unlocked ? new Color(0.15f, 0.45f, 0.18f) : new Color(0.35f, 0.25f, 0.18f), TextAlignmentOptions.Center);
        MenuUiFactory.Place(label.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 48f));
    }
}
