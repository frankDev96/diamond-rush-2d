using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GameplayLevel1Builder
{
    const string ScenePath = "Assets/Scenes/GameplayLevel1.unity";
    const string LayoutPref = "DiamondRush.GameplayLevel1LayoutVersion";
    const int LayoutVersion = 8;
    const string Home = MenuUiFactory.ArtFolder;
    const string Map = MenuUiFactory.MapArtFolder;
    const string Angkor = GameplayArtImporter.OutputFolder;

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.62f);

    struct Modal
    {
        public GameObject root;
        public RectTransform board;
        public CanvasGroup group;
        public Button dim;
    }

    static GameplayLevel1Builder()
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
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += AutoBuild;
            return;
        }

        if (File.Exists(ScenePath) && EditorPrefs.GetInt(LayoutPref, 0) == LayoutVersion)
            return;

        Build();
    }

    [MenuItem("Diamond Rush/Build Angkor Level 1 Gameplay")]
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

        if (!File.Exists(MenuUiFactory.SfxFolder + "dig.wav"))
            GenerateGameplaySfx.Generate();

        if (!GameplayArtImporter.HasProcessedArt())
            GameplayArtImporter.ProcessAll();

        BuildSceneForSpec(Level1Layout.Spec, ScenePath, LayoutPref, LayoutVersion);
    }

    public static void BuildSceneForSpec(GameplayLevelSpec spec, string scenePath, string layoutPref, int layoutVersion)
    {
        var layoutErrors = LevelLayoutValidator.Validate(spec.Rows, spec.Width, spec.Height, spec.TotalReds);
        if (layoutErrors.Count > 0)
        {
            foreach (var error in layoutErrors)
                Debug.LogError("Level1Layout: " + error);
            Debug.LogError("Diamond Rush gameplay level 1 not rebuilt: fix Level1Layout first.");
            return;
        }

        MenuUiFactory.ApplyPlayerDefaults();
        MenuUiFactory.ConfigureMusicClip(MenuUiFactory.LevelEntryMusicPath);
        MenuUiFactory.ConfigureMusicClip(MenuUiFactory.AngkorMusicPath);
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = CreateGameplayCamera();
        MenuUiFactory.CreateEventSystem();

        var sessionGo = new GameObject("GameplaySession");
        var session = sessionGo.AddComponent<GameplaySessionController>();

        var board = CreateBoard();
        var player = CreatePlayer(board);
        CreateAudio(sessionGo.transform);

        var fit = camera.gameObject.AddComponent<GameplayCameraFit>();
        var fitSo = new SerializedObject(fit);
        MenuUiFactory.SetRef(fitSo, "board", board);
        MenuUiFactory.SetRef(fitSo, "target", player.transform);
        fitSo.ApplyModifiedPropertiesWithoutUndo();

        var canvasGo = MenuUiFactory.CreateCanvas();
        var canvas = canvasGo.transform;
        var exitIndicator = BuildExitIndicator(canvas, fit);
        var joystick = BuildJoystick(canvasGo.GetComponent<Canvas>());
        var hud = BuildHud(canvas, spec);
        var tutorial = BuildTutorial(canvas, spec);
        var tutorialSo = new SerializedObject(tutorial);
        MenuUiFactory.SetRef(tutorialSo, "board", board);
        MenuUiFactory.SetRef(tutorialSo, "cameraFit", fit);
        tutorialSo.ApplyModifiedPropertiesWithoutUndo();
        var interactionHint = BuildInteractionHint(canvas, fit, board, player);

        var modalsGo = MenuUiFactory.CreateRect("Modals", canvas);
        MenuUiFactory.Stretch(modalsGo);
        var confirm = BuildConfirm(modalsGo);
        var pause = BuildPause(modalsGo, confirm);
        var inventory = BuildInventory(modalsGo);
        var complete = BuildComplete(modalsGo);

        var feedback = sessionGo.AddComponent<GameplayFeedback>();
        var feedbackSo = new SerializedObject(feedback);
        MenuUiFactory.SetRef(feedbackSo, "cameraFit", fit);
        MenuUiFactory.SetRef(feedbackSo, "worldCamera", camera);
        MenuUiFactory.SetRef(feedbackSo, "hud", hud);
        MenuUiFactory.SetRef(feedbackSo, "dirtChunkSprite", Art("dirt_chunk.png"));
        feedbackSo.ApplyModifiedPropertiesWithoutUndo();

        var playerSo = new SerializedObject(player);
        MenuUiFactory.SetRef(playerSo, "joystick", joystick);
        playerSo.ApplyModifiedPropertiesWithoutUndo();

        var so = new SerializedObject(session);
        MenuUiFactory.SetRef(so, "board", board);
        MenuUiFactory.SetRef(so, "player", player);
        MenuUiFactory.SetRef(so, "hud", hud);
        MenuUiFactory.SetRef(so, "feedback", feedback);
        MenuUiFactory.SetRef(so, "cameraFit", fit);
        MenuUiFactory.SetRef(so, "exitIndicator", exitIndicator);
        MenuUiFactory.SetRef(so, "joystick", joystick);
        MenuUiFactory.SetRef(so, "tutorial", tutorial);
        MenuUiFactory.SetRef(so, "interactionHint", interactionHint);
        MenuUiFactory.SetRef(so, "world", board.GetComponent<GridWorldController>());
        MenuUiFactory.SetRef(so, "pauseMenu", pause);
        MenuUiFactory.SetRef(so, "inventory", inventory);
        MenuUiFactory.SetRef(so, "confirm", confirm);
        MenuUiFactory.SetRef(so, "completePanel", complete);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorPrefs.SetInt(layoutPref, layoutVersion);
        MenuUiFactory.SetMenuBuildScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Diamond Rush gameplay level " + spec.LevelIndex + " (v" + layoutVersion + ") saved to " + scenePath);
    }

    static Sprite Art(string file)
    {
        return GameplayArtImporter.Load(file);
    }

    static Camera CreateGameplayCamera()
    {
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.09f, 0.05f);
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 100f;
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        cameraGo.AddComponent<AudioListener>();

        var urpType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpType != null && cameraGo.GetComponent(urpType) == null)
            cameraGo.AddComponent(urpType);
        return camera;
    }

    static void SetSprites(SerializedObject so, string field, Sprite[] sprites)
    {
        var property = so.FindProperty(field);
        property.arraySize = sprites.Length;
        for (var i = 0; i < sprites.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    static GridBoard CreateBoard()
    {
        var gridGo = new GameObject("Board");
        var tiles = new GameObject("Tiles").transform;
        tiles.SetParent(gridGo.transform, false);
        var entities = new GameObject("Entities").transform;
        entities.SetParent(gridGo.transform, false);

        var board = gridGo.AddComponent<GridBoard>();
        var world = gridGo.AddComponent<GridWorldController>();
        var so = new SerializedObject(board);
        MenuUiFactory.SetRef(so, "floorSprite", Art("floor_stone.png"));
        MenuUiFactory.SetRef(so, "floorAltSprite", Art("floor_stone_alt.png"));
        MenuUiFactory.SetRef(so, "floorMossSprite", Art("floor_moss.png"));
        MenuUiFactory.SetRef(so, "floorCrackedSprite", Art("floor_cracked.png"));
        MenuUiFactory.SetRef(so, "dirtSprite", Art("dirt_soft.png"));
        MenuUiFactory.SetRef(so, "wallSprite", Art("wall_block.png"));
        MenuUiFactory.SetRef(so, "wallTopSprite", Art("wall_top.png"));
        MenuUiFactory.SetRef(so, "jungleFillSprite", Art("jungle_fill.png"));
        MenuUiFactory.SetRef(so, "jungleEdgeSprite", Art("jungle_vignette_16x9.png"));
        MenuUiFactory.SetRef(so, "gemSprite", Art("red_diamond.png"));
        MenuUiFactory.SetRef(so, "purpleGemSprite", Art("icon_purple.png"));
        MenuUiFactory.SetRef(so, "boulderSprite", Art("boulder_face.png"));
        MenuUiFactory.SetRef(so, "chestSprite", Art("chest_wood.png"));
        MenuUiFactory.SetRef(so, "secretChestSprite", Art("chest_secret.png"));
        MenuUiFactory.SetRef(so, "fireSprite", Art("trap_fire.png"));
        MenuUiFactory.SetRef(so, "spearSprite", Art("trap_spear.png"));
        MenuUiFactory.SetRef(so, "checkpointSprite", Art("checkpoint_pad.png"));
        var worldSo = new SerializedObject(world);
        MenuUiFactory.SetRef(worldSo, "snakeSprite", Art("snake_green.png"));
        worldSo.ApplyModifiedPropertiesWithoutUndo();
        MenuUiFactory.SetRef(so, "doorClosedSprite", Art("exit_door_closed.png"));
        MenuUiFactory.SetRef(so, "doorOpenSprite", Art("exit_door_open.png"));
        MenuUiFactory.SetRef(so, "tilesRoot", tiles);
        MenuUiFactory.SetRef(so, "entitiesRoot", entities);
        so.FindProperty("cellSize").floatValue = 1f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return board;
    }

    static PlayerGridMotor CreatePlayer(GridBoard board)
    {
        var playerGo = new GameObject("Player");
        var visual = new GameObject("Visual");
        visual.transform.SetParent(playerGo.transform, false);
        // Sprites pivot at the feet, so this puts the boots near the bottom of the cell.
        visual.transform.localPosition = new Vector3(0f, -0.42f, 0f);
        visual.transform.localScale = Vector3.one * 0.52f;
        var renderer = visual.AddComponent<SpriteRenderer>();
        var walkUp = GameplayArtImporter.LoadFrames("explorer_walk_up");
        renderer.sprite = walkUp.Length > 1 ? walkUp[1] : Art("explorer_up.png");
        renderer.sortingOrder = 10;
        var animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = null;

        var motor = playerGo.AddComponent<PlayerGridMotor>();
        var so = new SerializedObject(motor);
        MenuUiFactory.SetRef(so, "board", board);
        MenuUiFactory.SetRef(so, "visual", visual.transform);
        MenuUiFactory.SetRef(so, "spriteRenderer", renderer);
        MenuUiFactory.SetRef(so, "animator", animator);
        MenuUiFactory.SetRef(so, "downSprite", Art("explorer_down.png"));
        MenuUiFactory.SetRef(so, "upSprite", Art("explorer_up.png"));
        MenuUiFactory.SetRef(so, "sideSprite", Art("explorer_side.png"));
        SetSprites(so, "walkDown", GameplayArtImporter.LoadFrames("explorer_walk_down"));
        SetSprites(so, "walkUp", walkUp);
        SetSprites(so, "walkSide", GameplayArtImporter.LoadFrames("explorer_walk_side"));
        so.FindProperty("stepDuration").floatValue = 0.18f;
        so.FindProperty("walkSpeedScale").floatValue = 0.5f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return motor;
    }

    static void CreateAudio(Transform parent)
    {
        var root = new GameObject("GameplayAudio");
        root.transform.SetParent(parent, false);

        var entry = root.AddComponent<AudioSource>();
        var bgm = root.AddComponent<AudioSource>();
        var music = root.AddComponent<GameplayAudioController>();
        var sfx = root.AddComponent<GameSfx>();
        var sfxA = root.AddComponent<AudioSource>();
        var sfxB = root.AddComponent<AudioSource>();

        var audioSo = new SerializedObject(music);
        MenuUiFactory.SetRef(audioSo, "entryMusic", AssetDatabase.LoadAssetAtPath<AudioClip>(MenuUiFactory.LevelEntryMusicPath));
        MenuUiFactory.SetRef(audioSo, "levelBgm", AssetDatabase.LoadAssetAtPath<AudioClip>(MenuUiFactory.AngkorMusicPath));
        MenuUiFactory.SetRef(audioSo, "entrySource", entry);
        MenuUiFactory.SetRef(audioSo, "bgmSource", bgm);
        audioSo.ApplyModifiedPropertiesWithoutUndo();

        var sfxSo = new SerializedObject(sfx);
        MenuUiFactory.SetRef(sfxSo, "digClip", AssetDatabase.LoadAssetAtPath<AudioClip>(MenuUiFactory.SfxFolder + "dig.wav"));
        MenuUiFactory.SetRef(sfxSo, "gemCollectClip", AssetDatabase.LoadAssetAtPath<AudioClip>(MenuUiFactory.SfxFolder + "gem_collect.wav"));
        MenuUiFactory.SetRef(sfxSo, "hitClip", AssetDatabase.LoadAssetAtPath<AudioClip>(MenuUiFactory.SfxFolder + "hit.wav"));
        MenuUiFactory.SetRef(sfxSo, "doorUnlockClip", AssetDatabase.LoadAssetAtPath<AudioClip>(MenuUiFactory.SfxFolder + "door_unlock.wav"));
        MenuUiFactory.SetRef(sfxSo, "sourceA", sfxA);
        MenuUiFactory.SetRef(sfxSo, "sourceB", sfxB);
        sfxSo.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Screen overlays ----------

    static ExitIndicator BuildExitIndicator(Transform canvas, GameplayCameraFit fit)
    {
        var area = MenuUiFactory.CreateRect("ExitIndicator", canvas);
        MenuUiFactory.Stretch(area);
        var arrow = Decor("Arrow", area, Art("exit_arrow.png"));
        arrow.preserveAspect = true;
        MenuUiFactory.Place(arrow.rectTransform, Center, Center, Vector2.zero, new Vector2(88f, 88f));

        var indicator = area.gameObject.AddComponent<ExitIndicator>();
        var so = new SerializedObject(indicator);
        MenuUiFactory.SetRef(so, "cameraFit", fit);
        MenuUiFactory.SetRef(so, "arrow", arrow.rectTransform);
        MenuUiFactory.SetRef(so, "arrowImage", arrow);
        so.ApplyModifiedPropertiesWithoutUndo();
        return indicator;
    }

    // ---------- HUD ----------

    static GameplayHudController BuildHud(Transform canvas, GameplayLevelSpec spec)
    {
        var hudRect = MenuUiFactory.CreateRect("GameplayHud", canvas);
        MenuUiFactory.Stretch(hudRect);

        var banner = SlicedPanel("TitleBanner", hudRect);
        MenuUiFactory.Place(banner.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(680f, 104f));
        var worldLine = MenuUiFactory.CreateText("WorldLine", banner.transform, "WORLD 1: ANGKOR WAT - LEVEL " + spec.LevelIndex, 20, FontStyles.Bold, MenuUiFactory.Title, TextAlignmentOptions.Left);
        MenuUiFactory.Place(worldLine.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -12f), new Vector2(620f, 28f));
        MenuUiFactory.ApplyOutline(worldLine, 0.15f);
        var stageLine = MenuUiFactory.CreateText("StageLine", banner.transform, spec.StageName, 36, FontStyles.Bold, MenuUiFactory.Subtitle, TextAlignmentOptions.Left);
        MenuUiFactory.Place(stageLine.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -44f), new Vector2(620f, 44f));
        MenuUiFactory.ApplyOutline(stageLine, 0.2f);

        var chipRow = MenuUiFactory.CreateRect("ChipRow", hudRect);
        MenuUiFactory.Place(chipRow, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(760f, 68f));
        var required = CreateHudChip(chipRow, "RequiredReds", "0/3", Art("icon_red.png"), Color.white, new Vector2(-570f, 0f));
        var optional = CreateHudChip(chipRow, "OptionalReds", "0/" + spec.ExtraReds, Art("icon_red.png"), new Color(1f, 1f, 1f, 0.55f), new Vector2(-380f, 0f));
        var purple = CreateHudChip(chipRow, "Purple", "0", Art("icon_purple.png"), Color.white, new Vector2(-190f, 0f));
        var lives = CreateHudChip(chipRow, "Lives", "x3", Art("icon_lives_head.png"), Color.white, Vector2.zero);

        var pause = CreateIconButton(hudRect, "Pause", Art("btn_pause.png"), new Vector2(1f, 1f), new Vector2(-24f, -100f), 104f);
        var inventory = CreateIconButton(hudRect, "Inventory", Art("btn_inventory.png"), new Vector2(1f, 0f), new Vector2(-40f, 40f), 150f);
        var restart = CreateIconButton(hudRect, "Restart", Art("btn_restart.png"), new Vector2(1f, 0f), new Vector2(-60f, 210f), 112f);

        var hud = hudRect.gameObject.AddComponent<GameplayHudController>();
        var so = new SerializedObject(hud);
        MenuUiFactory.SetRef(so, "worldLine", worldLine);
        MenuUiFactory.SetRef(so, "stageLine", stageLine);
        MenuUiFactory.SetRef(so, "requiredRedsLabel", required.label);
        MenuUiFactory.SetRef(so, "optionalRedsLabel", optional.label);
        MenuUiFactory.SetRef(so, "purpleLabel", purple.label);
        MenuUiFactory.SetRef(so, "livesLabel", lives.label);
        MenuUiFactory.SetRef(so, "requiredChip", required.chip.rectTransform);
        MenuUiFactory.SetRef(so, "requiredIcon", required.icon);
        MenuUiFactory.SetRef(so, "requiredChipImage", required.chip);
        MenuUiFactory.SetRef(so, "pauseButton", pause);
        MenuUiFactory.SetRef(so, "inventoryButton", inventory);
        MenuUiFactory.SetRef(so, "restartButton", restart);
        so.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

    struct HudChip
    {
        public Image chip;
        public RectTransform icon;
        public TMP_Text label;
    }

    static HudChip CreateHudChip(RectTransform parent, string name, string value, Sprite iconSprite, Color iconTint, Vector2 pos)
    {
        var chipSprite = File.Exists(Map + "hud_plaque_gold.png")
            ? MenuUiFactory.SpriteAt("hud_plaque_gold.png", Map)
            : MenuUiFactory.SpriteAt("hud_chip.png", Map);
        var chip = Decor(name, parent, chipSprite);
        MenuUiFactory.Place(chip.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), pos, new Vector2(172f, 60f));
        var icon = Decor("Icon", chip.transform, iconSprite);
        icon.preserveAspect = true;
        icon.color = iconTint;
        MenuUiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 2f), new Vector2(56f, 56f));
        var label = MenuUiFactory.CreateText("Value", chip.transform, value, 26, FontStyles.Bold, MenuUiFactory.Title, TextAlignmentOptions.Right);
        MenuUiFactory.ApplyOutline(label, 0.2f);
        MenuUiFactory.Place(label.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(100f, 36f));
        return new HudChip { chip = chip, icon = icon.rectTransform, label = label };
    }

    static Button CreateIconButton(RectTransform parent, string name, Sprite sprite, Vector2 corner, Vector2 pos, float size)
    {
        var image = Decor(name, parent, sprite);
        image.preserveAspect = true;
        image.raycastTarget = true;
        MenuUiFactory.Place(image.rectTransform, corner, corner, pos, new Vector2(size, size));
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.85f, 1f);
        colors.pressedColor = new Color(0.75f, 0.68f, 0.55f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        return button;
    }

    static Image SlicedPanel(string name, Transform parent)
    {
        var path = Map + "hud_chip.png";
        var sprite = MenuUiFactory.SpriteAt("hud_chip.png", Map);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        var border = new Vector4(14f, 14f, 14f, 14f);
        if (importer != null && importer.spriteBorder != border)
        {
            importer.spriteBorder = border;
            importer.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        var image = Decor(name, parent, sprite);
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.94f);
        return image;
    }

    static Image Decor(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    // ---------- Joystick ----------

    static FloatingJoystick BuildJoystick(Canvas canvas)
    {
        var touch = MenuUiFactory.CreateRect("JoystickTouchZone", canvas.transform);
        touch.anchorMin = Vector2.zero;
        touch.anchorMax = new Vector2(0.4f, 0.86f);
        touch.offsetMin = Vector2.zero;
        touch.offsetMax = Vector2.zero;
        var touchImage = touch.gameObject.AddComponent<Image>();
        touchImage.color = new Color(1f, 1f, 1f, 0f);
        touchImage.raycastTarget = true;

        var baseImage = Decor("JoystickBase", canvas.transform, Art("joystick_base.png"));
        baseImage.preserveAspect = true;
        MenuUiFactory.Place(baseImage.rectTransform, Vector2.zero, Center, new Vector2(140f, 190f), new Vector2(220f, 220f));
        var group = baseImage.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var knob = Decor("JoystickKnob", baseImage.transform, Art("joystick_knob.png"));
        knob.preserveAspect = true;
        MenuUiFactory.Place(knob.rectTransform, Center, Center, Vector2.zero, new Vector2(104f, 104f));

        var hint = MenuUiFactory.CreateText("JoystickHint", canvas.transform, "DRAG JOYSTICK\nTO MOVE!", 18, FontStyles.Bold, MenuUiFactory.Subtitle, TextAlignmentOptions.Center);
        MenuUiFactory.Place(hint.rectTransform, Vector2.zero, Center, new Vector2(140f, 50f), new Vector2(240f, 56f));
        MenuUiFactory.ApplyOutline(hint, 0.2f);

        var joystick = touch.gameObject.AddComponent<FloatingJoystick>();
        var so = new SerializedObject(joystick);
        MenuUiFactory.SetRef(so, "canvas", canvas);
        MenuUiFactory.SetRef(so, "background", baseImage.rectTransform);
        MenuUiFactory.SetRef(so, "handle", knob.rectTransform);
        MenuUiFactory.SetRef(so, "visualGroup", group);
        MenuUiFactory.SetRef(so, "hint", hint.gameObject);
        so.FindProperty("maxRadius").floatValue = 96f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return joystick;
    }

    // ---------- Intro + hint ----------

    static InteractionHintOverlay BuildInteractionHint(Transform canvas, GameplayCameraFit fit, GridBoard board, PlayerGridMotor player)
    {
        var area = MenuUiFactory.CreateRect("InteractionHint", canvas);
        MenuUiFactory.Stretch(area);
        var arrow = Decor("Arrow", area, Art("exit_arrow.png"));
        arrow.preserveAspect = true;
        arrow.color = new Color(1f, 1f, 1f, 0.85f);
        MenuUiFactory.Place(arrow.rectTransform, Center, Center, Vector2.zero, new Vector2(72f, 72f));

        var hint = area.gameObject.AddComponent<InteractionHintOverlay>();
        var so = new SerializedObject(hint);
        MenuUiFactory.SetRef(so, "board", board);
        MenuUiFactory.SetRef(so, "player", player);
        MenuUiFactory.SetRef(so, "cameraFit", fit);
        MenuUiFactory.SetRef(so, "arrow", arrow.rectTransform);
        MenuUiFactory.SetRef(so, "arrowImage", arrow);
        so.ApplyModifiedPropertiesWithoutUndo();
        return hint;
    }

    static TutorialOverlay BuildTutorial(Transform canvas, GameplayLevelSpec spec)
    {
        var root = MenuUiFactory.CreateRect("Tutorial", canvas);
        MenuUiFactory.Stretch(root);

        var card = SlicedPanel("IntroCard", root);
        MenuUiFactory.Place(card.rectTransform, Center, Center, new Vector2(0f, 60f), new Vector2(820f, 170f));
        var cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        var level = MenuUiFactory.CreateText("Level", card.transform, "LEVEL 1", 30, FontStyles.Bold, MenuUiFactory.Subtitle, TextAlignmentOptions.Center);
        MenuUiFactory.Place(level.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(760f, 40f));
        MenuUiFactory.ApplyOutline(level, 0.2f);
        var stage = MenuUiFactory.CreateText("Stage", card.transform, spec.StageName, 64, FontStyles.Bold, MenuUiFactory.Title, TextAlignmentOptions.Center);
        MenuUiFactory.Place(stage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(760f, 80f));
        MenuUiFactory.ApplyOutline(stage, 0.22f);

        var hintRect = MenuUiFactory.CreateRect("Hint", root);
        MenuUiFactory.Place(hintRect, new Vector2(0.5f, 0f), Center, new Vector2(0f, 84f), new Vector2(480f, 76f));
        var hintBg = hintRect.gameObject.AddComponent<Image>();
        hintBg.color = new Color(0.08f, 0.06f, 0.04f, 0.82f);
        hintBg.raycastTarget = false;
        var hintGroup = hintRect.gameObject.AddComponent<CanvasGroup>();
        var hintText = MenuUiFactory.CreateText("Message", hintRect, spec.TutorialHint, 40, FontStyles.Bold, MenuUiFactory.Subtitle, TextAlignmentOptions.Center);
        MenuUiFactory.ApplyOutline(hintText, 0.2f);
        MenuUiFactory.Stretch(hintText.rectTransform, 16f, 6f, 16f, 6f);

        var tutorial = root.gameObject.AddComponent<TutorialOverlay>();
        var so = new SerializedObject(tutorial);
        MenuUiFactory.SetRef(so, "introCard", cardGroup);
        MenuUiFactory.SetRef(so, "introLevel", level);
        MenuUiFactory.SetRef(so, "introStage", stage);
        MenuUiFactory.SetRef(so, "hint", hintGroup);
        MenuUiFactory.SetRef(so, "hintMessage", hintText);
        so.ApplyModifiedPropertiesWithoutUndo();
        return tutorial;
    }

    // ---------- Modals ----------

    static Modal CreateModal(RectTransform parent, string name, Vector2 boardSize, string title)
    {
        var modal = new Modal();
        var root = MenuUiFactory.CreateRect(name, parent);
        MenuUiFactory.Stretch(root);
        modal.root = root.gameObject;
        modal.group = modal.root.AddComponent<CanvasGroup>();

        var dim = MenuUiFactory.CreateRect("Dim", root);
        MenuUiFactory.Stretch(dim);
        var dimImage = dim.gameObject.AddComponent<Image>();
        dimImage.color = DimColor;
        modal.dim = dim.gameObject.AddComponent<Button>();
        modal.dim.targetGraphic = dimImage;
        modal.dim.transition = Selectable.Transition.None;

        var board = MenuUiFactory.CreateSprite("Board", root, "stone_board.png", false);
        modal.board = board.GetComponent<RectTransform>();
        MenuUiFactory.Place(modal.board, Center, Center, Vector2.zero, boardSize);

        if (!string.IsNullOrEmpty(title))
        {
            var titleText = MenuUiFactory.CreateText("Title", board.transform, title, 48, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Center);
            MenuUiFactory.Place(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(boardSize.x - 120f, 64f));
        }

        return modal;
    }

    static Button CreateBoardButton(RectTransform board, string sprite, string label, Vector2 pos, Vector2 size)
    {
        var button = MenuUiFactory.CreateMenuButton(board, sprite, label, null, size.y, 16f, out _);
        MenuUiFactory.Place(button.GetComponent<RectTransform>(), Center, Center, pos, size);
        return button;
    }

    static ConfirmDialog BuildConfirm(RectTransform modals)
    {
        var modal = CreateModal(modals, "ConfirmDialog", new Vector2(780f, 440f), null);
        var message = MenuUiFactory.CreateText("Message", modal.board, "Are you sure?", 36, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Center);
        message.textWrappingMode = TextWrappingModes.Normal;
        MenuUiFactory.Place(message.rectTransform, Center, Center, new Vector2(0f, 60f), new Vector2(640f, 180f));
        var no = CreateBoardButton(modal.board, "button_wood.png", "NO", new Vector2(-165f, -120f), new Vector2(290f, 100f));
        var yes = CreateBoardButton(modal.board, "button_continue.png", "YES", new Vector2(165f, -120f), new Vector2(290f, 100f));

        var dialog = modals.gameObject.AddComponent<ConfirmDialog>();
        var so = new SerializedObject(dialog);
        MenuUiFactory.SetRef(so, "root", modal.root);
        MenuUiFactory.SetRef(so, "board", modal.board);
        MenuUiFactory.SetRef(so, "group", modal.group);
        MenuUiFactory.SetRef(so, "message", message);
        MenuUiFactory.SetRef(so, "yesButton", yes);
        MenuUiFactory.SetRef(so, "noButton", no);
        MenuUiFactory.SetRef(so, "dimButton", modal.dim);
        so.ApplyModifiedPropertiesWithoutUndo();
        return dialog;
    }

    static PauseMenuController BuildPause(RectTransform modals, ConfirmDialog confirm)
    {
        var modal = CreateModal(modals, "PauseMenu", new Vector2(860f, 720f), "PAUSED");
        var sound = MenuUiFactory.CreateToggleColumn(modal.board, "Sound", "SOUND", new Vector2(-180f, 165f));
        var vibrate = MenuUiFactory.CreateToggleColumn(modal.board, "Vibrate", "VIBRATE", new Vector2(180f, 165f));
        EnlargePills(sound);
        EnlargePills(vibrate);

        var resume = CreateBoardButton(modal.board, "button_continue.png", "RESUME", new Vector2(0f, -20f), new Vector2(420f, 100f));
        var restart = CreateBoardButton(modal.board, "button_wood.png", "RESTART", new Vector2(0f, -135f), new Vector2(420f, 100f));
        var quit = CreateBoardButton(modal.board, "button_wood.png", "QUIT TO MAP", new Vector2(0f, -250f), new Vector2(420f, 100f));

        var pause = modals.gameObject.AddComponent<PauseMenuController>();
        var so = new SerializedObject(pause);
        MenuUiFactory.SetRef(so, "root", modal.root);
        MenuUiFactory.SetRef(so, "board", modal.board);
        MenuUiFactory.SetRef(so, "group", modal.group);
        MenuUiFactory.SetRef(so, "resumeButton", resume);
        MenuUiFactory.SetRef(so, "restartButton", restart);
        MenuUiFactory.SetRef(so, "quitButton", quit);
        MenuUiFactory.SetRef(so, "soundOnButton", sound.onButton);
        MenuUiFactory.SetRef(so, "soundOffButton", sound.offButton);
        MenuUiFactory.SetRef(so, "vibrateOnButton", vibrate.onButton);
        MenuUiFactory.SetRef(so, "vibrateOffButton", vibrate.offButton);
        MenuUiFactory.SetRef(so, "soundOnImage", sound.onImage);
        MenuUiFactory.SetRef(so, "soundOffImage", sound.offImage);
        MenuUiFactory.SetRef(so, "vibrateOnImage", vibrate.onImage);
        MenuUiFactory.SetRef(so, "vibrateOffImage", vibrate.offImage);
        MenuUiFactory.SetRef(so, "soundOnLabel", sound.onLabel);
        MenuUiFactory.SetRef(so, "soundOffLabel", sound.offLabel);
        MenuUiFactory.SetRef(so, "vibrateOnLabel", vibrate.onLabel);
        MenuUiFactory.SetRef(so, "vibrateOffLabel", vibrate.offLabel);
        MenuUiFactory.SetRef(so, "pillOn", MenuUiFactory.SpriteAt("pill_on.png", Home));
        MenuUiFactory.SetRef(so, "pillOff", MenuUiFactory.SpriteAt("pill_off.png", Home));
        MenuUiFactory.SetRef(so, "confirm", confirm);
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEditor.Events.UnityEventTools.AddPersistentListener(modal.dim.onClick, pause.Hide);
        return pause;
    }

    static void EnlargePills(MenuUiFactory.PillPair pair)
    {
        var on = pair.onButton.GetComponent<RectTransform>();
        var off = pair.offButton.GetComponent<RectTransform>();
        on.sizeDelta = off.sizeDelta = new Vector2(136f, 72f);
        on.anchoredPosition = new Vector2(-74f, -36f);
        off.anchoredPosition = new Vector2(74f, -36f);
    }

    static InventoryModal BuildInventory(RectTransform modals)
    {
        var modal = CreateModal(modals, "InventoryModal", new Vector2(840f, 640f), "BACKPACK");
        var reds = CreateInventoryRow(modal.board, "Reds", Art("icon_red.png"), "RED DIAMONDS", new Vector2(0f, 150f));
        var purple = CreateInventoryRow(modal.board, "Purple", Art("icon_purple.png"), "PURPLE DIAMONDS", new Vector2(0f, 65f));
        var lives = CreateInventoryRow(modal.board, "Lives", Art("icon_lives_head.png"), "LIVES", new Vector2(0f, -20f));

        var slot = MenuUiFactory.CreateRect("ToolSlot", modal.board);
        MenuUiFactory.Place(slot, Center, Center, new Vector2(0f, -120f), new Vector2(660f, 84f));
        var slotBg = slot.gameObject.AddComponent<Image>();
        slotBg.color = new Color(0.18f, 0.14f, 0.1f, 0.35f);
        slotBg.raycastTarget = false;
        var toolName = MenuUiFactory.CreateText("Tool", slot, "MYSTIC MALLET", 28, FontStyles.Bold, new Color(0.27f, 0.16f, 0.08f, 0.55f), TextAlignmentOptions.Left);
        MenuUiFactory.Place(toolName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(320f, 40f));
        var toolHint = MenuUiFactory.CreateText("Locked", slot, "Found in Level 10", 24, FontStyles.Italic, new Color(0.27f, 0.16f, 0.08f, 0.55f), TextAlignmentOptions.Right);
        MenuUiFactory.Place(toolHint.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(300f, 40f));

        var ret = CreateBoardButton(modal.board, "button_continue.png", "RETURN", new Vector2(0f, -140f), new Vector2(320f, 88f));
        var close = CreateBoardButton(modal.board, "button_wood.png", "CLOSE", new Vector2(0f, -232f), new Vector2(320f, 100f));

        var inventory = modals.gameObject.AddComponent<InventoryModal>();
        var so = new SerializedObject(inventory);
        MenuUiFactory.SetRef(so, "root", modal.root);
        MenuUiFactory.SetRef(so, "board", modal.board);
        MenuUiFactory.SetRef(so, "group", modal.group);
        MenuUiFactory.SetRef(so, "redsValue", reds);
        MenuUiFactory.SetRef(so, "purpleValue", purple);
        MenuUiFactory.SetRef(so, "livesValue", lives);
        MenuUiFactory.SetRef(so, "returnButton", ret);
        MenuUiFactory.SetRef(so, "closeButton", close);
        MenuUiFactory.SetRef(so, "dimButton", modal.dim);
        so.ApplyModifiedPropertiesWithoutUndo();
        return inventory;
    }

    static TMP_Text CreateInventoryRow(RectTransform board, string name, Sprite icon, string caption, Vector2 pos)
    {
        var row = MenuUiFactory.CreateRect(name, board);
        MenuUiFactory.Place(row, Center, Center, pos, new Vector2(660f, 76f));
        var bg = row.gameObject.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.14f, 0.1f, 0.22f);
        bg.raycastTarget = false;
        var iconImage = Decor("Icon", row, icon);
        iconImage.preserveAspect = true;
        MenuUiFactory.Place(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(48f, 0f), new Vector2(60f, 60f));
        var label = MenuUiFactory.CreateText("Caption", row, caption, 28, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Left);
        MenuUiFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(96f, 0f), new Vector2(360f, 40f));
        var value = MenuUiFactory.CreateText("Value", row, "0", 32, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Right);
        MenuUiFactory.Place(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(200f, 44f));
        return value;
    }

    static LevelCompletePanel BuildComplete(RectTransform modals)
    {
        var modal = CreateModal(modals, "LevelComplete", new Vector2(920f, 640f), null);
        modal.dim.interactable = false;

        var title = MenuUiFactory.CreateText("Title", modal.board, "LEVEL CLEAR!", 68, FontStyles.Bold, MenuUiFactory.Subtitle, TextAlignmentOptions.Center);
        MenuUiFactory.ApplyOutline(title, 0.25f);
        MenuUiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(800f, 84f));
        var stage = MenuUiFactory.CreateText("Stage", modal.board, Level1Layout.StageName, 36, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Center);
        MenuUiFactory.Place(stage.rectTransform, Center, Center, new Vector2(0f, 120f), new Vector2(700f, 48f));

        var gem = Decor("RedIcon", modal.board, Art("icon_red.png"));
        gem.preserveAspect = true;
        MenuUiFactory.Place(gem.rectTransform, Center, Center, new Vector2(-90f, 20f), new Vector2(96f, 96f));
        var reds = MenuUiFactory.CreateText("Reds", modal.board, "3 / 3", 52, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Left);
        MenuUiFactory.Place(reds.rectTransform, Center, new Vector2(0f, 0.5f), new Vector2(-20f, 20f), new Vector2(260f, 70f));
        var extra = MenuUiFactory.CreateText("ExtraReds", modal.board, "EXTRA REDS 0 / " + Level1Layout.ExtraReds, 30, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Center);
        MenuUiFactory.Place(extra.rectTransform, Center, Center, new Vector2(0f, -60f), new Vector2(600f, 44f));

        var replay = CreateBoardButton(modal.board, "button_wood.png", "REPLAY", new Vector2(-200f, -190f), new Vector2(340f, 104f));
        var next = CreateBoardButton(modal.board, "button_continue.png", "CONTINUE", new Vector2(200f, -190f), new Vector2(340f, 104f));

        var panel = modals.gameObject.AddComponent<LevelCompletePanel>();
        var so = new SerializedObject(panel);
        MenuUiFactory.SetRef(so, "root", modal.root);
        MenuUiFactory.SetRef(so, "board", modal.board);
        MenuUiFactory.SetRef(so, "group", modal.group);
        MenuUiFactory.SetRef(so, "stageLabel", stage);
        MenuUiFactory.SetRef(so, "redsValue", reds);
        MenuUiFactory.SetRef(so, "extraValue", extra);
        MenuUiFactory.SetRef(so, "continueButton", next);
        MenuUiFactory.SetRef(so, "replayButton", replay);
        so.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }
}
