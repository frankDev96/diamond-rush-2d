using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class HomeScreenBuilder
{
    const string ScenePath = "Assets/Scenes/Home.unity";
    const string LayoutPref = "DiamondRush.HomeLayoutVersion";
    const int LayoutVersion = 3;

    static HomeScreenBuilder()
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

    [MenuItem("Diamond Rush/Build Home Screen")]
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

        var hero = MenuUiFactory.CreateSprite("Explorer", canvas, "hero_explorer.png", true);
        MenuUiFactory.Place(hero.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 0f), new Vector2(560f, 980f));

        var logo = MenuUiFactory.CreateSprite("Logo", canvas, "logo.png", true);
        MenuUiFactory.Place(logo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(80f, -8f), new Vector2(620f, 188f));

        var version = MenuUiFactory.CreateText("Version", canvas, "v1.2", 22, FontStyles.Bold, Color.white, TextAlignmentOptions.TopRight);
        MenuUiFactory.Place(version.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -16f), new Vector2(90f, 40f));
        MenuUiFactory.ApplyOutline(version, 0.16f);

        var gems = MenuUiFactory.CreateSprite("Gems", canvas, "gem_cluster.png", true);
        MenuUiFactory.Place(gems.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 12f), new Vector2(180f, 210f));
        gems.GetComponent<Image>().raycastTarget = false;

        var menuRoot = MenuUiFactory.CreateRect("Menu", canvas);
        MenuUiFactory.Place(menuRoot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, -24f), new Vector2(640f, 520f));
        var menu = menuRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        menu.spacing = 18f;
        menu.childAlignment = TextAnchor.MiddleCenter;
        menu.childControlWidth = true;
        menu.childControlHeight = true;
        menu.childForceExpandWidth = true;
        menu.childForceExpandHeight = false;
        menu.padding = new RectOffset(0, 0, 8, 8);

        var controllerGo = new GameObject("HomeScreen");
        var controller = controllerGo.AddComponent<HomeScreenController>();

        var continueButton = MenuUiFactory.CreateMenuButton(menuRoot, "button_continue.png", "CONTINUE", "Last Saved: Profile 1 - Angkor Wat Level 2", 148f, 88f, out var continueSubtitle);
        var newButton = MenuUiFactory.CreateMenuButton(menuRoot, "button_wood.png", "NEW GAME", "New Profile", 120f, 16f, out _);
        var settingsButton = MenuUiFactory.CreateMenuButton(menuRoot, "button_wood.png", "SETTINGS", null, 120f, 16f, out _);

        var settingsPanel = CreateSettingsModal(canvas, controller, out var sound, out var vibrate);
        var toast = MenuUiFactory.CreateToast(canvas, out var toastLabel);

        var so = new SerializedObject(controller);
        MenuUiFactory.SetRef(so, "continueSubtitle", continueSubtitle);
        MenuUiFactory.SetRef(so, "settingsPanel", settingsPanel);
        MenuUiFactory.SetRef(so, "soundOnImage", sound.onImage);
        MenuUiFactory.SetRef(so, "soundOffImage", sound.offImage);
        MenuUiFactory.SetRef(so, "vibrateOnImage", vibrate.onImage);
        MenuUiFactory.SetRef(so, "vibrateOffImage", vibrate.offImage);
        MenuUiFactory.SetRef(so, "soundOnLabel", sound.onLabel);
        MenuUiFactory.SetRef(so, "soundOffLabel", sound.offLabel);
        MenuUiFactory.SetRef(so, "vibrateOnLabel", vibrate.onLabel);
        MenuUiFactory.SetRef(so, "vibrateOffLabel", vibrate.offLabel);
        MenuUiFactory.SetRef(so, "toastRoot", toast);
        MenuUiFactory.SetRef(so, "toastLabel", toastLabel);
        MenuUiFactory.SetRef(so, "pillOn", MenuUiFactory.SpriteAt("pill_on.png"));
        MenuUiFactory.SetRef(so, "pillOff", MenuUiFactory.SpriteAt("pill_off.png"));
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(continueButton.onClick, controller.OnContinue);
        UnityEventTools.AddPersistentListener(newButton.onClick, controller.OnNewGame);
        UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.OnSettings);
        UnityEventTools.AddBoolPersistentListener(sound.onButton.onClick, controller.SetSound, true);
        UnityEventTools.AddBoolPersistentListener(sound.offButton.onClick, controller.SetSound, false);
        UnityEventTools.AddBoolPersistentListener(vibrate.onButton.onClick, controller.SetVibrate, true);
        UnityEventTools.AddBoolPersistentListener(vibrate.offButton.onClick, controller.SetVibrate, false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorPrefs.SetInt(LayoutPref, LayoutVersion);
        MenuUiFactory.SetMenuBuildScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Diamond Rush home screen saved to " + ScenePath);
    }

    static GameObject CreateSettingsModal(Transform canvas, HomeScreenController controller, out MenuUiFactory.PillPair sound, out MenuUiFactory.PillPair vibrate)
    {
        var panel = MenuUiFactory.CreateRect("SettingsModal", canvas).gameObject;
        MenuUiFactory.Stretch(panel.GetComponent<RectTransform>());

        var dim = MenuUiFactory.CreateRect("Dim", panel.transform);
        MenuUiFactory.Stretch(dim);
        var dimImage = dim.gameObject.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.62f);
        var dimButton = dim.gameObject.AddComponent<Button>();
        dimButton.targetGraphic = dimImage;
        dimButton.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(dimButton.onClick, controller.CloseSettings);

        var board = MenuUiFactory.CreateSprite("SettingsBoard", panel.transform, "stone_board.png", false);
        MenuUiFactory.Place(board.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 520f));

        var title = MenuUiFactory.CreateText("Title", board.transform, "SETTINGS", 40, FontStyles.Bold, MenuUiFactory.Ink, TextAlignmentOptions.Center);
        MenuUiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(420f, 56f));

        sound = MenuUiFactory.CreateToggleColumn(board.GetComponent<RectTransform>(), "Sound", "SOUND", new Vector2(-170f, 20f));
        vibrate = MenuUiFactory.CreateToggleColumn(board.GetComponent<RectTransform>(), "Vibrate", "VIBRATE", new Vector2(170f, 20f));

        var close = MenuUiFactory.CreateMenuButton(board.transform, "button_wood.png", "CLOSE", null, 96f, 16f, out _);
        var closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 56f);
        closeRect.sizeDelta = new Vector2(360f, 96f);
        UnityEventTools.AddPersistentListener(close.onClick, controller.CloseSettings);

        panel.SetActive(false);
        return panel;
    }

}
