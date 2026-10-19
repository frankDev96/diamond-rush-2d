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
    const int LayoutVersion = 2;
    const string Map = MenuUiFactory.MapArtFolder;

    static readonly string[] WorldTitles =
    {
        "WORLD 1: ANGKOR WAT",
        "WORLD 2: BAVARIA",
        "WORLD 3: TIBET",
        "WORLD 4: GIZA",
        "WORLD 5: THIRUVITHAMKOOR"
    };

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
        var bg = MenuUiFactory.CreateSprite("Background", canvas, "map_frame.png", false, Map);
        MenuUiFactory.Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().raycastTarget = false;

        var banner = MenuUiFactory.CreateSprite("TitleBanner", canvas, "title_banner.png", true, Map);
        MenuUiFactory.Place(banner.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(720f, 110f));
        var title = MenuUiFactory.CreateText("Title", banner.transform, "SELECT WORLD", 36, FontStyles.Bold, MenuUiFactory.Title, TextAlignmentOptions.Center);
        MenuUiFactory.Stretch(title.rectTransform, 40f, 18f, 40f, 18f);
        MenuUiFactory.ApplyOutline(title, 0.2f);

        var controllerGo = new GameObject("WorldSelect");
        var controller = controllerGo.AddComponent<WorldSelectController>();

        var back = MenuUiFactory.CreateMenuButton(canvas, "button_wood.png", "BACK", null, 88f, 8f, out _);
        MenuUiFactory.Place(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(220f, 88f));
        UnityEventTools.AddPersistentListener(back.onClick, controller.OnBack);

        var hud = MenuUiFactory.CreateSprite("Hud", canvas, "hud_chip.png", false, Map);
        MenuUiFactory.Place(hud.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -18f), new Vector2(260f, 72f));
        for (var i = 0; i < 3; i++)
        {
            var heart = MenuUiFactory.CreateSprite("Heart" + i, hud.transform, "heart.png", true, Map);
            MenuUiFactory.Place(heart.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f + i * 42f, 0f), new Vector2(38f, 38f));
        }

        var gem = MenuUiFactory.CreateSprite("HudGem", hud.transform, "icon_diamond.png", true, MenuUiFactory.ArtFolder);
        MenuUiFactory.Place(gem.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70f, 0f), new Vector2(36f, 36f));
        var gemLabel = MenuUiFactory.CreateText("GemCount", hud.transform, "18", 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Right);
        MenuUiFactory.Place(gemLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(56f, 40f));

        var list = MenuUiFactory.CreateRect("WorldList", canvas);
        MenuUiFactory.Place(list, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(36f, -20f), new Vector2(560f, 820f));
        var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 12f;
        listLayout.childAlignment = TextAnchor.UpperCenter;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        listLayout.padding = new RectOffset(0, 0, 8, 8);

        var worldImages = new Image[WorldTitles.Length];
        for (var i = 0; i < WorldTitles.Length; i++)
        {
            var unlocked = i == 0;
            var row = MenuUiFactory.CreateSprite("World" + i, list, unlocked ? "world_row_active.png" : "world_row_locked.png", false, Map);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 118f;
            le.minHeight = 118f;
            var button = row.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            UnityEventTools.AddIntPersistentListener(button.onClick, controller.SelectWorld, i);
            worldImages[i] = row.GetComponent<Image>();

            var label = MenuUiFactory.CreateText("Name", row.transform, WorldTitles[i], 22, FontStyles.Bold, unlocked ? Color.white : new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Left);
            MenuUiFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 10f), new Vector2(360f, 40f));
            MenuUiFactory.ApplyOutline(label, 0.14f);

            var status = MenuUiFactory.CreateText("Status", row.transform, unlocked ? "12 Stages     ACTIVE" : "LOCKED", 16, FontStyles.Bold, unlocked ? new Color(0.45f, 1f, 0.45f) : new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.Left);
            MenuUiFactory.Place(status.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, -22f), new Vector2(360f, 32f));
        }

        var parchment = MenuUiFactory.CreateSprite("Parchment", canvas, "map_parchment.png", false, Map);
        MenuUiFactory.Place(parchment.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-36f, -16f), new Vector2(1240f, 860f));

        var mapHeader = MenuUiFactory.CreateText("MapHeader", parchment.transform, "WORLD 1: ANGKOR WAT - THE JUNGLES", 26, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Center);
        MenuUiFactory.Place(mapHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(900f, 40f));

        var grid = MenuUiFactory.CreateRect("Nodes", parchment.transform);
        MenuUiFactory.Place(grid, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(1080f, 430f));
        var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(168f, 168f);
        gridLayout.spacing = new Vector2(36f, 12f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 5;
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        var nodeImages = new Image[12];
        var nodeLabels = new TMP_Text[12];
        var nodeNotes = new TMP_Text[12];
        for (var i = 0; i < 12; i++)
        {
            var node = MenuUiFactory.CreateSprite("Node" + (i + 1), grid, "node_locked.png", true, Map);
            var button = node.AddComponent<Button>();
            button.targetGraphic = node.GetComponent<Image>();
            UnityEventTools.AddIntPersistentListener(button.onClick, controller.SelectLevel, i + 1);
            nodeImages[i] = node.GetComponent<Image>();

            var label = MenuUiFactory.CreateText("Label", node.transform, "LEVEL\n" + (i + 1), 16, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            MenuUiFactory.Stretch(label.rectTransform, 10f, 36f, 10f, 28f);
            MenuUiFactory.ApplyOutline(label, 0.16f);
            nodeLabels[i] = label;

            var note = MenuUiFactory.CreateText("Note", node.transform, string.Empty, 12, FontStyles.Bold, new Color(0.2f, 0.55f, 0.2f), TextAlignmentOptions.Center);
            MenuUiFactory.Place(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -6f), new Vector2(150f, 32f));
            nodeNotes[i] = note;
        }

        var preview = MenuUiFactory.CreateSprite("Preview", parchment.transform, "preview_card.png", false, Map);
        MenuUiFactory.Place(preview.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(1120f, 220f));

        var thumb = MenuUiFactory.CreateSprite("Thumb", preview.transform, "courtyard_thumb.png", true, Map);
        MenuUiFactory.Place(thumb.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(280f, 170f));

        var previewTitle = MenuUiFactory.CreateText("PreviewTitle", preview.transform, "LEVEL 2: \"THE COURTYARD\"", 24, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Left);
        MenuUiFactory.Place(previewTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(330f, -16f), new Vector2(520f, 40f));

        var previewTarget = MenuUiFactory.CreateText("PreviewTarget", preview.transform, "Target:  4", 22, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Left);
        MenuUiFactory.Place(previewTarget.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(330f, -62f), new Vector2(280f, 36f));

        var targetGem = MenuUiFactory.CreateSprite("TargetGem", preview.transform, "icon_diamond.png", true, MenuUiFactory.ArtFolder);
        MenuUiFactory.Place(targetGem.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(470f, -60f), new Vector2(32f, 32f));

        var previewReward = MenuUiFactory.CreateText("PreviewReward", preview.transform, "Rewards:  18", 22, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Left);
        MenuUiFactory.Place(previewReward.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(330f, -102f), new Vector2(280f, 36f));

        var play = MenuUiFactory.CreateSprite("Play", preview.transform, "play_button.png", false, Map);
        MenuUiFactory.Place(play.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(280f, 88f));
        var playButton = play.AddComponent<Button>();
        playButton.targetGraphic = play.GetComponent<Image>();
        var playLabel = MenuUiFactory.CreateText("PlayLabel", play.transform, "PLAY", 32, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        MenuUiFactory.Stretch(playLabel.rectTransform, 16f, 8f, 48f, 8f);
        MenuUiFactory.ApplyOutline(playLabel, 0.18f);
        UnityEventTools.AddPersistentListener(playButton.onClick, controller.OnPlay);

        var toast = MenuUiFactory.CreateToast(canvas, out var toastLabel);

        var so = new SerializedObject(controller);
        MenuUiFactory.SetRef(so, "angkorMusic", AssetDatabase.LoadAssetAtPath<AudioClip>(MenuUiFactory.AngkorMusicPath));
        MenuUiFactory.SetRef(so, "worldActive", MenuUiFactory.SpriteAt("world_row_active.png", Map));
        MenuUiFactory.SetRef(so, "worldLocked", MenuUiFactory.SpriteAt("world_row_locked.png", Map));
        MenuUiFactory.SetRef(so, "nodeCleared", MenuUiFactory.SpriteAt("node_cleared.png", Map));
        MenuUiFactory.SetRef(so, "nodeCurrent", MenuUiFactory.SpriteAt("node_current.png", Map));
        MenuUiFactory.SetRef(so, "nodeLocked", MenuUiFactory.SpriteAt("node_locked.png", Map));
        MenuUiFactory.SetRef(so, "previewTitle", previewTitle);
        MenuUiFactory.SetRef(so, "previewTarget", previewTarget);
        MenuUiFactory.SetRef(so, "previewReward", previewReward);
        MenuUiFactory.SetRef(so, "gemLabel", gemLabel);
        MenuUiFactory.SetRef(so, "toastRoot", toast);
        MenuUiFactory.SetRef(so, "toastLabel", toastLabel);
        SetArray(so, "worldImages", worldImages);
        SetArray(so, "nodeImages", nodeImages);
        SetArray(so, "nodeLabels", nodeLabels);
        SetArray(so, "nodeNotes", nodeNotes);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorPrefs.SetInt(LayoutPref, LayoutVersion);
        MenuUiFactory.SetMenuBuildScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Diamond Rush world select saved to " + ScenePath);
    }

    static void SetArray<T>(SerializedObject so, string propertyName, T[] values) where T : Object
    {
        var property = so.FindProperty(propertyName);
        property.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
