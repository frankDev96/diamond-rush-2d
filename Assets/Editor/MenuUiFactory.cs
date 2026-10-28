using System.IO;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class MenuUiFactory
{
    public const string ArtFolder = "Assets/Art/Home/";
    public const string MapArtFolder = "Assets/Art/Map/";
    public const string MusicPath = "Assets/Audio/The_Gilded_Labyrinth.wav";
    public const string AngkorMusicPath = "Assets/Audio/ankor-wat-bgm.wav";
    public const string LevelEntryMusicPath = "Assets/Audio/Ankorwat-level entry-music.wav";
    public const string SfxFolder = "Assets/Audio/Sfx/";

    public static readonly Color Ink = new Color(0.27f, 0.16f, 0.08f, 1f);
    public static readonly Color Title = new Color(1f, 0.97f, 0.9f, 1f);
    public static readonly Color Subtitle = new Color(1f, 0.9f, 0.55f, 1f);
    public static readonly Color OutlineColor = new Color(0.18f, 0.09f, 0.03f, 0.95f);

    public static TMP_FontAsset Font { get; private set; }

    public struct PillPair
    {
        public Button onButton;
        public Button offButton;
        public Image onImage;
        public Image offImage;
        public TMP_Text onLabel;
        public TMP_Text offLabel;
    }

    public static bool TryReadyFont()
    {
        Font = FindFont();
        return Font != null;
    }

    public static void QueueTmpImport(System.Action onReady)
    {
        var package = TMP_EditorUtility.packageFullPath + "/Package Resources/TMP Essential Resources.unitypackage";
        if (!File.Exists(package))
        {
            Debug.LogError("Diamond Rush could not find TMP Essential Resources at " + package);
            return;
        }

        AssetDatabase.importPackageCompleted += OnImported;
        UnityEditor.AssetPackage.Package.Import(package, false);

        void OnImported(string _)
        {
            AssetDatabase.importPackageCompleted -= OnImported;
            EditorApplication.delayCall += () => onReady();
        }
    }

    public static TMP_FontAsset FindFont()
    {
        foreach (var guid in AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null)
                return asset;
        }

        return null;
    }

    public static void ApplyPlayerDefaults()
    {
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
    }

    public static void CreateCamera()
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

    public static void CreateEventSystem()
    {
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        var module = eventSystem.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    public static GameObject CreateCanvas()
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

    public static MenuMusicPlayer CreateMenuMusic()
    {
        ConfigureMusicClip(MusicPath);
        ConfigureMusicClip(AngkorMusicPath);
        var go = new GameObject("MenuMusic");
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        var player = go.AddComponent<MenuMusicPlayer>();
        var so = new SerializedObject(player);
        so.FindProperty("track").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        so.ApplyModifiedPropertiesWithoutUndo();
        return player;
    }

    public static void ConfigureMusicClip(string path = MusicPath)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null)
            return;

        var settings = importer.defaultSampleSettings;
        var changed = settings.loadType != AudioClipLoadType.Streaming;
        settings.loadType = AudioClipLoadType.Streaming;
        importer.defaultSampleSettings = settings;
        if (changed)
            importer.SaveAndReimport();
    }

    public static Button CreateMenuButton(Transform parent, string sprite, string title, string subtitle, float height, float leftInset, out TMP_Text subtitleText)
    {
        var label = string.IsNullOrEmpty(title) ? Path.GetFileNameWithoutExtension(sprite) : title;
        var go = CreateSprite("Button " + label, parent, sprite, false);
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
        var hasTitle = !string.IsNullOrEmpty(title);
        if (hasTitle)
        {
            var titleText = CreateText("Title", go.transform, title, hasSubtitle ? 34 : 32, FontStyles.Bold, Title, TextAlignmentOptions.Center);
            ApplyOutline(titleText, 0.2f);
            var titleRect = titleText.rectTransform;
            if (hasSubtitle)
            {
                titleRect.anchorMin = new Vector2(0f, 0.38f);
                titleRect.anchorMax = Vector2.one;
                titleRect.offsetMin = new Vector2(leftInset, 0f);
                titleRect.offsetMax = new Vector2(-16f, -8f);
            }
            else
            {
                Stretch(titleRect, leftInset, 8f, 16f, 8f);
            }
        }

        if (hasSubtitle)
        {
            subtitleText = CreateText("Subtitle", go.transform, subtitle, 16, FontStyles.Bold, Subtitle, TextAlignmentOptions.Center);
            subtitleText.textWrappingMode = TextWrappingModes.NoWrap;
            ApplyOutline(subtitleText, 0.15f);
            var subtitleRect = subtitleText.rectTransform;
            subtitleRect.anchorMin = Vector2.zero;
            subtitleRect.anchorMax = new Vector2(1f, hasTitle ? 0.46f : 0.34f);
            subtitleRect.offsetMin = new Vector2(leftInset, 8f);
            subtitleRect.offsetMax = new Vector2(-16f, 0f);
        }
        else
        {
            subtitleText = null;
        }

        return button;
    }

    public static PillPair CreateToggleColumn(RectTransform parent, string name, string caption, Vector2 position)
    {
        var column = CreateRect(name, parent);
        Place(column, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(300f, 160f));
        var label = CreateText("Caption", column, caption, 26, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(280f, 40f));

        var pair = new PillPair();
        pair.onButton = CreatePill(column, "On", "ON", new Vector2(-68f, -28f), out pair.onImage, out pair.onLabel);
        pair.offButton = CreatePill(column, "Off", "OFF", new Vector2(68f, -28f), out pair.offImage, out pair.offLabel);
        return pair;
    }

    public static Button CreatePill(RectTransform parent, string name, string caption, Vector2 position, out Image image, out TMP_Text label)
    {
        var go = CreateSprite(name, parent, "pill_on.png", false);
        var rect = go.GetComponent<RectTransform>();
        Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(124f, 56f));
        image = go.GetComponent<Image>();
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        label = CreateText("Label", go.transform, caption, 22, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return button;
    }

    public static GameObject CreateToast(Transform canvas, out TMP_Text label)
    {
        var toast = CreateRect("Toast", canvas).gameObject;
        var rect = toast.GetComponent<RectTransform>();
        Place(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(900f, 72f));
        var image = toast.AddComponent<Image>();
        image.color = new Color(0.12f, 0.08f, 0.05f, 0.9f);
        image.raycastTarget = false;
        label = CreateText("Message", toast.transform, string.Empty, 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 16f, 8f, 16f, 8f);
        toast.SetActive(false);
        return toast;
    }

    public static GameObject CreateSprite(string name, Transform parent, string file, bool preserveAspect, string folder = ArtFolder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = SpriteAt(file, folder);
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        return go;
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static TMP_Text CreateText(string name, Transform parent, string text, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = Font;
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

    public static void ApplyOutline(TMP_Text label, float width)
    {
        label.outlineWidth = width;
        label.outlineColor = OutlineColor;
    }

    public static Sprite SpriteAt(string file, string folder = ArtFolder)
    {
        var path = folder + file;
        var sprite = EnsureSingleSprite(path);
        if (sprite != null)
            return sprite;

        Debug.LogError("No Sprite sub-asset after import: " + path);
        return null;
    }

    /// <summary>
    /// Multiple-mode leftovers (named _0 slices / out-of-bounds rects / nameFileIdTable /
    /// internalIDToNameTable) survive PNG swaps. Empty Multiple sheets yield zero sprites.
    /// Always normalize to Single and verify a Sprite sub-asset exists.
    /// </summary>
    public static Sprite EnsureSingleSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Missing menu art: " + path);
            return null;
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            ClearSpriteSheetLeftovers(importer);

            var needsReimport = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || settings.spriteMode != (int)SpriteImportMode.Single
                || importer.mipmapEnabled
                || !importer.alphaIsTransparency
                || settings.spriteMeshType != SpriteMeshType.FullRect
                || AssetDatabase.LoadAssetAtPath<Sprite>(path) == null;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.alphaIsTransparency = true;
            importer.SetTextureSettings(settings);

            if (needsReimport || attempt > 0)
            {
                importer.SaveAndReimport();
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite s)
                    return s;
            }
        }

        return null;
    }

    static void ClearSpriteSheetLeftovers(TextureImporter importer)
    {
        var so = new SerializedObject(importer);
        var dirty = false;
        foreach (var propName in new[]
                 {
                     "m_SpriteSheet.m_Sprites",
                     "m_SpriteSheet.m_NameFileIdTable",
                     "m_NameFileIdTable",
                     "m_InternalIDToNameTable"
                 })
        {
            var prop = so.FindProperty(propName);
            if (prop != null && prop.isArray && prop.arraySize > 0)
            {
                prop.arraySize = 0;
                dirty = true;
            }
        }

        if (dirty)
            so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void EnsureAllMenuSprites()
    {
        foreach (var folder in new[] { ArtFolder, MapArtFolder })
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    EnsureSingleSprite(path);
            }
        }
    }

    public static void Stretch(RectTransform rect)
    {
        Stretch(rect, 0f, 0f, 0f, 0f);
    }

    public static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    public static void SetRef(SerializedObject so, string propertyName, Object value)
    {
        var property = so.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError("Missing serialized field " + propertyName);
            return;
        }

        property.objectReferenceValue = value;
    }

    public static void SetMenuBuildScenes()
    {
        var ordered = new[]
        {
            "Assets/Scenes/Home.unity",
            "Assets/Scenes/WorldSelect.unity",
            "Assets/Scenes/GameplayLevel1.unity",
            "Assets/Scenes/GameplayLevel2.unity",
            "Assets/Scenes/GameplayLevel3.unity",
            "Assets/Scenes/GameplayLevel4.unity",
            "Assets/Scenes/GameplayLevel5.unity",
            "Assets/Scenes/GameplayLevel6.unity",
            "Assets/Scenes/GameplayLevel7.unity",
            "Assets/Scenes/GameplayLevel8.unity",
            "Assets/Scenes/GameplayLevel9.unity",
            "Assets/Scenes/SampleScene.unity"
        };
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var path in ordered)
        {
            if (File.Exists(path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (!scenes.Exists(scene => scene.path == existing.path))
                scenes.Add(existing);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
