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
    const int LayoutVersion = 8;
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
        MenuUiFactory.EnsureAllMenuSprites();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MenuUiFactory.CreateCamera();
        MenuUiFactory.CreateEventSystem();
        MenuUiFactory.CreateMenuMusic();

        var canvas = MenuUiFactory.CreateCanvas().transform;
        var bg = MenuUiFactory.CreateSprite("Background", canvas, "map_frame.png", false, Map);
        MenuUiFactory.Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().raycastTarget = false;
        // Soft left scrim so world-row labels stay readable on busy jungle.
        var scrim = new GameObject("LeftScrim", typeof(RectTransform), typeof(Image));
        scrim.transform.SetParent(canvas, false);
        var scrimRt = scrim.GetComponent<RectTransform>();
        MenuUiFactory.Place(scrimRt, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(620f, 0f));
        scrimRt.offsetMin = new Vector2(0f, 0f);
        scrimRt.offsetMax = new Vector2(620f, 0f);
        var scrimImg = scrim.GetComponent<Image>();
        scrimImg.sprite = null;
        scrimImg.color = new Color(0.05f, 0.08f, 0.05f, 0.42f);
        scrimImg.raycastTarget = false;

        var banner = MenuUiFactory.CreateSprite("TitleBanner", canvas, "title_banner.png", true, Map);
        MenuUiFactory.Place(banner.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(760f, 108f));
        var title = MenuUiFactory.CreateText("Title", banner.transform, "SELECT WORLD", 38, FontStyles.Bold, new Color(1f, 0.92f, 0.55f, 1f), TextAlignmentOptions.Center);
        MenuUiFactory.Stretch(title.rectTransform, 48f, 20f, 48f, 20f);
        MenuUiFactory.ApplyOutline(title, 0.28f);

        var controllerGo = new GameObject("WorldSelect");
        var controller = controllerGo.AddComponent<WorldSelectController>();

        var back = MenuUiFactory.CreateMenuButton(canvas, "button_wood.png", "BACK", null, 88f, 8f, out _);
        MenuUiFactory.Place(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(220f, 88f));
        UnityEventTools.AddPersistentListener(back.onClick, controller.OnBack);

        var hud = MenuUiFactory.CreateSprite("Hud", canvas, "hud_chip.png", false, Map);
        MenuUiFactory.Place(hud.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -18f), new Vector2(300f, 80f));
        for (var i = 0; i < 3; i++)
        {
            var heart = MenuUiFactory.CreateSprite("Heart" + i, hud.transform, "heart.png", true, Map);
            MenuUiFactory.Place(heart.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f + i * 44f, 0f), new Vector2(40f, 40f));
        }

        var gem = MenuUiFactory.CreateSprite("HudGem", hud.transform, "icon_purple.png", true, MenuUiFactory.GameplayArtFolder);
        MenuUiFactory.Place(gem.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-78f, 0f), new Vector2(40f, 40f));
        var gemLabel = MenuUiFactory.CreateText("GemCount", hud.transform, "18", 26, FontStyles.Bold, Color.white, TextAlignmentOptions.Right);
        MenuUiFactory.Place(gemLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(60f, 44f));
        MenuUiFactory.ApplyOutline(gemLabel, 0.16f);

        var list = MenuUiFactory.CreateRect("WorldList", canvas);
        MenuUiFactory.Place(list, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, -28f), new Vector2(560f, 800f));
        var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 14f;
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
            le.preferredHeight = 124f;
            le.minHeight = 124f;
            var button = row.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            UnityEventTools.AddIntPersistentListener(button.onClick, controller.SelectWorld, i);
            worldImages[i] = row.GetComponent<Image>();

            var label = MenuUiFactory.CreateText("Name", row.transform, WorldTitles[i], 22, FontStyles.Bold, unlocked ? Color.white : new Color(0.9f, 0.9f, 0.9f), TextAlignmentOptions.Left);
            MenuUiFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 14f), new Vector2(300f, 40f));
            MenuUiFactory.ApplyOutline(label, 0.18f);

            var status = MenuUiFactory.CreateText("Status", row.transform, unlocked ? "12 Stages  ·  ACTIVE" : "LOCKED", 16, FontStyles.Bold, unlocked ? new Color(0.55f, 1f, 0.55f) : new Color(0.85f, 0.78f, 0.55f), TextAlignmentOptions.Left);
            MenuUiFactory.Place(status.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, -20f), new Vector2(300f, 32f));
            MenuUiFactory.ApplyOutline(status, 0.14f);
        }

        var parchment = MenuUiFactory.CreateSprite("Parchment", canvas, "map_parchment.png", false, Map);
        MenuUiFactory.Place(parchment.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, -20f), new Vector2(1240f, 880f));

        var mapHeader = MenuUiFactory.CreateText("MapHeader", parchment.transform, "WORLD 1: ANGKOR WAT — THE JUNGLES", 26, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Center);
        MenuUiFactory.Place(mapHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(980f, 42f));

        var grid = MenuUiFactory.CreateRect("Nodes", parchment.transform);
        MenuUiFactory.Place(grid, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(1100f, 460f));
        var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(160f, 160f);
        gridLayout.spacing = new Vector2(28f, 18f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
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

            var label = MenuUiFactory.CreateText("Label", node.transform, "LEVEL\n" + (i + 1), 17, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            MenuUiFactory.Stretch(label.rectTransform, 10f, 36f, 10f, 28f);
            MenuUiFactory.ApplyOutline(label, 0.18f);
            nodeLabels[i] = label;

            var note = MenuUiFactory.CreateText("Note", node.transform, string.Empty, 13, FontStyles.Bold, new Color(0.18f, 0.5f, 0.18f), TextAlignmentOptions.Center);
            MenuUiFactory.Place(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -4f), new Vector2(150f, 32f));
            nodeNotes[i] = note;
        }

        var preview = MenuUiFactory.CreateSprite("Preview", parchment.transform, "preview_card.png", false, Map);
        MenuUiFactory.Place(preview.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1120f, 220f));

        var thumb = MenuUiFactory.CreateSprite("Thumb", preview.transform, "courtyard_thumb.png", true, Map);
        MenuUiFactory.Place(thumb.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(260f, 168f));

        var previewTitle = MenuUiFactory.CreateText("PreviewTitle", preview.transform, "LEVEL 2: \"THE COURTYARD\"", 24, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Left);
        MenuUiFactory.Place(previewTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(318f, -18f), new Vector2(480f, 40f));

        var previewTarget = MenuUiFactory.CreateText("PreviewTarget", preview.transform, "Target:  4", 22, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Left);
        MenuUiFactory.Place(previewTarget.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(318f, -64f), new Vector2(220f, 36f));

        // PRD: required reds unlock the exit — show red diamond, not a heart.
        var targetGem = MenuUiFactory.CreateSprite("TargetGem", preview.transform, "icon_red.png", true, MenuUiFactory.GameplayArtFolder);
        MenuUiFactory.Place(targetGem.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(458f, -62f), new Vector2(34f, 34f));

        var previewReward = MenuUiFactory.CreateText("PreviewReward", preview.transform, "Rewards:  18", 22, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Left);
        MenuUiFactory.Place(previewReward.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(318f, -104f), new Vector2(220f, 36f));

        var rewardGem = MenuUiFactory.CreateSprite("RewardGem", preview.transform, "icon_purple.png", true, MenuUiFactory.GameplayArtFolder);
        MenuUiFactory.Place(rewardGem.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(478f, -102f), new Vector2(34f, 34f));

        var play = MenuUiFactory.CreateSprite("Play", preview.transform, "play_button.png", false, Map);
        MenuUiFactory.Place(play.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 28f), new Vector2(300f, 96f));
        var playButton = play.AddComponent<Button>();
        playButton.targetGraphic = play.GetComponent<Image>();
        var playLabel = MenuUiFactory.CreateText("PlayLabel", play.transform, "PLAY", 34, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        MenuUiFactory.Stretch(playLabel.rectTransform, 16f, 10f, 16f, 10f);
        MenuUiFactory.ApplyOutline(playLabel, 0.2f);
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
