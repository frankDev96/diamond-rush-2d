using System.Collections.Generic;
using System.IO;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[InitializeOnLoad]
public static class HomeScreenBuilder
{
    const string ScenePath = "Assets/Scenes/Home.unity";
    const string ArtFolder = "Assets/Art/Home/";
    const string LayoutPref = "DiamondRush.HomeLayoutVersion";
    const int LayoutVersion = 2;

    static bool importQueued;
    static TMP_FontAsset font;

    static readonly Color Ink = new Color(0.27f, 0.16f, 0.08f, 1f);
    static readonly Color Title = new Color(1f, 0.97f, 0.9f, 1f);
    static readonly Color Subtitle = new Color(1f, 0.9f, 0.55f, 1f);
    static readonly Color OutlineColor = new Color(0.18f, 0.09f, 0.03f, 0.95f);

    static HomeScreenBuilder()
    {
        EditorApplication.delayCall += AutoBuild;
    }

    static void AutoBuild()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += AutoBuild;
            return;
        }

        if (File.Exists(ScenePath) && EditorPrefs.GetInt(LayoutPref, 0) == LayoutVersion)
            return;

        Build();
    }

    [MenuItem("Diamond Rush/Build Home Screen")]
    public static void Build()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Build;
            return;
        }

        font = FindFont();
        if (font == null)
        {
            QueueTmpImport();
            return;
        }

        BuildScene();
    }

    static void QueueTmpImport()
    {
        if (importQueued)
            return;

        var package = TMP_EditorUtility.packageFullPath + "/Package Resources/TMP Essential Resources.unitypackage";
        if (!File.Exists(package))
        {
            Debug.LogError("Diamond Rush could not find TMP Essential Resources at " + package);
            return;
        }

        importQueued = true;
        AssetDatabase.importPackageCompleted += OnTmpImported;
        AssetDatabase.ImportPackage(package, false);
        Debug.Log("Diamond Rush is importing TextMesh Pro essentials before building the home screen.");
    }

    static void OnTmpImported(string packageName)
    {
        AssetDatabase.importPackageCompleted -= OnTmpImported;
        importQueued = false;
        EditorApplication.delayCall += Build;
    }

    static TMP_FontAsset FindFont()
    {
        foreach (var guid in AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null)
                return asset;
        }

        return null;
    }

    static void BuildScene()
    {
        ApplyPlayerDefaults();
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        CreateEventSystem();

        var canvasGo = CreateCanvas();
        var canvas = canvasGo.transform;
        var background = CreateSprite("Background", canvas, "bg_temple.png", false);
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().raycastTarget = false;

        var hero = CreateSprite("Explorer", canvas, "hero_explorer.png", true);
        Place(hero.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 0f), new Vector2(500f, 860f));

        var logo = CreateSprite("Logo", canvas, "logo.png", true);
        Place(logo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -4f), new Vector2(760f, 230f));

        var leftBoard = CreateSprite("LeftBoard", canvas, "stone_board.png", false);
        Place(leftBoard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200f, -30f), new Vector2(540f, 740f));
        var leftContent = CreateRect("Menu", leftBoard.transform);
        Stretch(leftContent, 72f, 78f, 72f, 64f);
        var menu = leftContent.gameObject.AddComponent<VerticalLayoutGroup>();
        menu.spacing = 8f;
        menu.childAlignment = TextAnchor.UpperCenter;
        menu.childControlWidth = true;
        menu.childControlHeight = true;
        menu.childForceExpandWidth = true;
        menu.childForceExpandHeight = false;

        var rightBoard = CreateSprite("RightBoard", canvas, "stone_board.png", false);
        var rightRect = rightBoard.GetComponent<RectTransform>();
        Place(rightRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(330f, -20f), new Vector2(720f, 760f));

        var profile = CreateSprite("Profile", canvas, "profile_chip.png", true);
        Place(profile.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -16f), new Vector2(96f, 96f));
        var version = CreateText("Version", canvas, "v1.2", 22, FontStyles.Bold, Color.white, TextAlignmentOptions.TopRight);
        Place(version.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -114f), new Vector2(90f, 40f));
        ApplyOutline(version, 0.16f);

        var gems = CreateSprite("Gems", canvas, "gem_cluster.png", true);
        Place(gems.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 4f), new Vector2(220f, 260f));
        gems.GetComponent<Image>().raycastTarget = false;

        var controllerGo = new GameObject("HomeScreen");
        var controller = controllerGo.AddComponent<HomeScreenController>();

        var continueButton = CreateMenuButton(leftContent, "button_continue.png", "CONTINUE", "Last Saved: Profile 1 - Angkor Wat Level 2", 118f, 72f, out var continueSubtitle);
        var newButton = CreateMenuButton(leftContent, "button_wood.png", "NEW GAME", "New Profile", 96f, 12f, out _);
        var loadButton = CreateMenuButton(leftContent, "button_wood.png", "LOAD GAME", "From Profiles", 96f, 12f, out _);
        var settingsButton = CreateMenuButton(leftContent, "button_wood.png", "SETTINGS", null, 90f, 12f, out _);
        var helpButton = CreateMenuButton(leftContent, "button_scroll.png", "HELP", null, 96f, 64f, out _);

        var settingsBlock = CreateSettings(rightBoard.transform, out var sound, out var vibrate);
        var helpBlock = CreateHelp(rightBoard.transform);
        var loadPanel = CreateLoadPanel(canvas, controller, out var slotLabels, out var slotImages);
        var toast = CreateToast(canvas, out var toastLabel);
        var focus = CreateSprite("FocusRing", canvas, "focus_ring.png", false);
        var focusRect = focus.GetComponent<RectTransform>();
        focusRect.anchorMin = focusRect.anchorMax = new Vector2(0.5f, 0.5f);
        focusRect.pivot = new Vector2(0.5f, 0.5f);
        focusRect.sizeDelta = new Vector2(200f, 120f);
        var focusImage = focus.GetComponent<Image>();
        focusImage.raycastTarget = false;
        focusImage.type = Image.Type.Simple;
        focus.SetActive(false);

        background.transform.SetAsFirstSibling();
        leftBoard.transform.SetSiblingIndex(1);
        rightBoard.transform.SetSiblingIndex(2);
        hero.transform.SetSiblingIndex(3);
        logo.transform.SetSiblingIndex(4);
        profile.transform.SetSiblingIndex(5);
        version.transform.SetSiblingIndex(6);
        gems.transform.SetSiblingIndex(7);
        loadPanel.transform.SetAsLastSibling();
        toast.transform.SetAsLastSibling();
        focus.transform.SetAsLastSibling();

        var so = new SerializedObject(controller);
        SetRef(so, "continueSubtitle", continueSubtitle);
        SetRef(so, "soundOnImage", sound.onImage);
        SetRef(so, "soundOffImage", sound.offImage);
        SetRef(so, "vibrateOnImage", vibrate.onImage);
        SetRef(so, "vibrateOffImage", vibrate.offImage);
        SetRef(so, "soundOnLabel", sound.onLabel);
        SetRef(so, "soundOffLabel", sound.offLabel);
        SetRef(so, "vibrateOnLabel", vibrate.onLabel);
        SetRef(so, "vibrateOffLabel", vibrate.offLabel);
        SetRef(so, "loadPanel", loadPanel);
        SetRef(so, "toastRoot", toast);
        SetRef(so, "toastLabel", toastLabel);
        SetRef(so, "focusRing", focusRect);
        SetRef(so, "settingsBlock", settingsBlock);
        SetRef(so, "helpBlock", helpBlock);
        SetRef(so, "pillOn", SpriteAt("pill_on.png"));
        SetRef(so, "pillOff", SpriteAt("pill_off.png"));
        SetArray(so, "slotLabels", slotLabels);
        SetArray(so, "slotImages", slotImages);
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(continueButton.onClick, controller.OnContinue);
        UnityEventTools.AddPersistentListener(newButton.onClick, controller.OnNewGame);
        UnityEventTools.AddPersistentListener(loadButton.onClick, controller.OnLoadGame);
        UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.OnSettings);
        UnityEventTools.AddPersistentListener(helpButton.onClick, controller.OnHelp);
        UnityEventTools.AddBoolPersistentListener(sound.onButton.onClick, controller.SetSound, true);
        UnityEventTools.AddBoolPersistentListener(sound.offButton.onClick, controller.SetSound, false);
        UnityEventTools.AddBoolPersistentListener(vibrate.onButton.onClick, controller.SetVibrate, true);
        UnityEventTools.AddBoolPersistentListener(vibrate.offButton.onClick, controller.SetVibrate, false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorPrefs.SetInt(LayoutPref, LayoutVersion);
        SetBuildScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Diamond Rush home screen saved to " + ScenePath);
    }

    static void ApplyPlayerDefaults()
    {
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
    }

    static void SetBuildScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (existing.path != ScenePath)
                scenes.Add(existing);
        }

        if (!scenes.Exists(scene => scene.path == "Assets/Scenes/SampleScene.unity"))
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void CreateCamera()
    {
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.1f, 0.16f, 0.09f);
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 100f;
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        cameraGo.AddComponent<AudioListener>();

        var urpType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpType != null && cameraGo.GetComponent(urpType) == null)
            cameraGo.AddComponent(urpType);
    }

    static void CreateEventSystem()
    {
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        var module = eventSystem.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    static GameObject CreateCanvas()
    {
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvasGo;
    }

    static Button CreateMenuButton(Transform parent, string sprite, string title, string subtitle, float height, float leftInset, out TMP_Text subtitleText)
    {
        var go = CreateSprite("Button " + title, parent, sprite, false);
        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;
        var image = go.GetComponent<Image>();
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.8f, 1f);
        colors.pressedColor = new Color(0.78f, 0.64f, 0.4f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        var hasSubtitle = !string.IsNullOrEmpty(subtitle);
        var titleText = CreateText("Title", go.transform, title, hasSubtitle ? 32 : 30, FontStyles.Bold, Title, TextAlignmentOptions.Center);
        ApplyOutline(titleText, 0.2f);
        var titleRect = titleText.rectTransform;
        if (hasSubtitle)
        {
            titleRect.anchorMin = new Vector2(0f, 0.38f);
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(leftInset, 0f);
            titleRect.offsetMax = new Vector2(-16f, -8f);
            subtitleText = CreateText("Subtitle", go.transform, subtitle, 16, FontStyles.Bold, Subtitle, TextAlignmentOptions.Center);
            ApplyOutline(subtitleText, 0.15f);
            var subtitleRect = subtitleText.rectTransform;
            subtitleRect.anchorMin = Vector2.zero;
            subtitleRect.anchorMax = new Vector2(1f, 0.46f);
            subtitleRect.offsetMin = new Vector2(leftInset, 8f);
            subtitleRect.offsetMax = new Vector2(-16f, 0f);
        }
        else
        {
            subtitleText = null;
            Stretch(titleRect, leftInset, 8f, 16f, 8f);
        }

        return button;
    }

    struct PillPair
    {
        public Button onButton;
        public Button offButton;
        public Image onImage;
        public Image offImage;
        public TMP_Text onLabel;
        public TMP_Text offLabel;
    }

    static RectTransform CreateSettings(Transform board, out PillPair sound, out PillPair vibrate)
    {
        var header = CreateText("SettingsHeader", board, "SETTINGS", 36, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(420f, 52f));

        var block = CreateRect("SettingsBlock", board);
        Place(block, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(620f, 160f));

        sound = CreateToggleColumn(block, "Sound", "SOUND", new Vector2(-155f, -18f));
        vibrate = CreateToggleColumn(block, "Vibrate", "VIBRATE", new Vector2(155f, -18f));
        return block;
    }

    static PillPair CreateToggleColumn(RectTransform parent, string name, string caption, Vector2 position)
    {
        var column = CreateRect(name, parent);
        Place(column, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(280f, 150f));
        var label = CreateText("Caption", column, caption, 26, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(260f, 40f));

        var pair = new PillPair();
        pair.onButton = CreatePill(column, "On", "ON", new Vector2(-62f, -28f), out pair.onImage, out pair.onLabel);
        pair.offButton = CreatePill(column, "Off", "OFF", new Vector2(62f, -28f), out pair.offImage, out pair.offLabel);
        return pair;
    }

    static Button CreatePill(RectTransform parent, string name, string caption, Vector2 position, out Image image, out TMP_Text label)
    {
        var go = CreateSprite(name, parent, "pill_on.png", false);
        var rect = go.GetComponent<RectTransform>();
        Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(112f, 52f));
        image = go.GetComponent<Image>();
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        button.colors = colors;
        label = CreateText("Label", go.transform, caption, 20, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return button;
    }

    static RectTransform CreateHelp(Transform board)
    {
        var block = CreateRect("HelpBlock", board);
        Place(block, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(640f, 390f));
        var header = CreateText("HelpHeader", block, "HELP (HOW TO PLAY)", 28, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(560f, 42f));

        CreateTip(block, "icon_joystick.png", "DRAG JOYSTICK\nTO MOVE!", new Vector2(-160f, -20f));
        CreateTip(block, "icon_dirt.png", "DIG AND BREAK\nDIRT!", new Vector2(160f, -20f));
        CreateTip(block, "icon_diamond.png", "COLLECT RED\nDIAMONDS TO\nUNLOCK THE EXIT!", new Vector2(-160f, -150f));
        CreateTip(block, "icon_snake.png", "FLEE DANGERS,\nENEMIES, & TRAPS!", new Vector2(160f, -150f));
        return block;
    }

    static void CreateTip(RectTransform parent, string icon, string caption, Vector2 position)
    {
        var tip = CreateRect(caption, parent);
        Place(tip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(300f, 120f));
        var image = CreateSprite("Icon", tip, icon, true);
        Place(image.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(78f, 78f));
        image.GetComponent<Image>().raycastTarget = false;
        var label = CreateText("Caption", tip, caption, 16, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
        label.textWrappingMode = TextWrappingModes.Normal;
        Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, 0f), new Vector2(200f, 110f));
    }

    static GameObject CreateLoadPanel(Transform canvas, HomeScreenController controller, out TMP_Text[] labels, out Image[] images)
    {
        var panel = CreateRect("LoadPanel", canvas).gameObject;
        Stretch(panel.GetComponent<RectTransform>());

        var dim = CreateRect("Dim", panel.transform);
        Stretch(dim);
        var dimImage = dim.gameObject.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.62f);
        var dimButton = dim.gameObject.AddComponent<Button>();
        dimButton.targetGraphic = dimImage;
        dimButton.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(dimButton.onClick, controller.CloseLoad);

        var board = CreateSprite("LoadBoard", panel.transform, "stone_board.png", false);
        Place(board.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 620f));
        var title = CreateText("Title", board.transform, "LOAD GAME", 36, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(420f, 56f));

        labels = new TMP_Text[3];
        images = new Image[3];
        for (var i = 0; i < 3; i++)
        {
            var button = CreateMenuButton(board.transform, "button_wood.png", "Profile " + (i + 1), null, 96f, 12f, out _);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -140f - i * 112f);
            rect.sizeDelta = new Vector2(460f, 100f);
            labels[i] = button.GetComponentInChildren<TMP_Text>();
            images[i] = button.GetComponent<Image>();
            var index = i;
            UnityEventTools.AddIntPersistentListener(button.onClick, controller.SelectSlot, index);
        }

        panel.SetActive(false);
        return panel;
    }

    static GameObject CreateToast(Transform canvas, out TMP_Text label)
    {
        var toast = CreateRect("Toast", canvas).gameObject;
        var rect = toast.GetComponent<RectTransform>();
        Place(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(820f, 64f));
        var image = toast.AddComponent<Image>();
        image.color = new Color(0.12f, 0.08f, 0.05f, 0.9f);
        image.raycastTarget = false;
        label = CreateText("Message", toast.transform, string.Empty, 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 16f, 8f, 16f, 8f);
        toast.SetActive(false);
        return toast;
    }

    static GameObject CreateSprite(string name, Transform parent, string file, bool preserveAspect)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = SpriteAt(file);
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        return go;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static TMP_Text CreateText(string name, Transform parent, string text, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }

    static void ApplyOutline(TMP_Text label, float width)
    {
        label.outlineWidth = width;
        label.outlineColor = OutlineColor;
    }

    static Sprite SpriteAt(string file)
    {
        var path = ArtFolder + file;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Missing home screen art: " + path);
            return null;
        }

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        var changed = importer.textureType != TextureImporterType.Sprite
            || importer.mipmapEnabled
            || !importer.alphaIsTransparency
            || settings.spriteMeshType != SpriteMeshType.FullRect;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.alphaIsTransparency = true;
        importer.SetTextureSettings(settings);
        if (changed)
            importer.SaveAndReimport();

        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        return null;
    }

    static void Stretch(RectTransform rect)
    {
        Stretch(rect, 0f, 0f, 0f, 0f);
    }

    static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    static void SetRef(SerializedObject so, string propertyName, Object value)
    {
        var property = so.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError("HomeScreenController is missing " + propertyName);
            return;
        }

        property.objectReferenceValue = value;
    }

    static void SetArray<T>(SerializedObject so, string propertyName, T[] values) where T : Object
    {
        var property = so.FindProperty(propertyName);
        property.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
