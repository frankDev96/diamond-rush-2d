using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldSelectController : MonoBehaviour
{
    static readonly string[] LevelNames =
    {
        "THE GATES",
        "THE COURTYARD",
        "THE JUNGLE",
        "THE RUINS",
        "THE RIVER",
        "THE MARKET",
        "THE TOMB",
        "THE SPIRE",
        "THE GROTTO",
        "THE BRIDGE",
        "THE SANCTUM",
        "THE INNER COURT"
    };

    static readonly Color ClearedTint = Color.white;
    static readonly Color CurrentTint = Color.white;
    static readonly Color LockedTint = new Color(0.72f, 0.72f, 0.72f, 1f);
    static readonly Color SelectedTint = new Color(1f, 0.94f, 0.72f, 1f);
    const float SelectedScale = 1.08f;
    const float NormalScale = 1f;

    [SerializeField] AudioClip angkorMusic;
    [SerializeField] Image[] worldImages;
    [SerializeField] Sprite worldActive;
    [SerializeField] Sprite worldLocked;
    [SerializeField] Image[] nodeImages;
    [SerializeField] TMP_Text[] nodeLabels;
    [SerializeField] TMP_Text[] nodeNotes;
    [SerializeField] Sprite nodeCleared;
    [SerializeField] Sprite nodeCurrent;
    [SerializeField] Sprite nodeLocked;
    [SerializeField] TMP_Text previewTitle;
    [SerializeField] TMP_Text previewTarget;
    [SerializeField] TMP_Text previewReward;
    [SerializeField] TMP_Text gemLabel;
    [SerializeField] GameObject toastRoot;
    [SerializeField] TMP_Text toastLabel;

    int selectedLevel = 1;
    Coroutine toastRoutine;

    void Awake()
    {
        ProfileSave.EnsureSeeded();
        GameSettings.Apply();
        if (MenuMusicPlayer.Instance != null && angkorMusic != null)
            MenuMusicPlayer.Instance.PlayClip(angkorMusic);
        if (toastRoot != null)
            toastRoot.SetActive(false);
        selectedLevel = Mathf.Clamp(ProfileSave.Active != null ? ProfileSave.Active.level : 1, 1, 12);
        Refresh();
    }

    public void OnBack()
    {
        SceneManager.LoadScene(GameScenes.Home);
    }

    public void SelectWorld(int index)
    {
        if (index != 0)
        {
            ShowToast("World locked.");
            return;
        }

        Refresh();
    }

    public void SelectLevel(int level)
    {
        var progress = ProgressLevel();
        if (level > progress)
        {
            ShowToast("Level locked.");
            return;
        }

        // Cleared and current levels stay playable; never lock a completed stage.
        selectedLevel = level;
        RefreshNodes();
        RefreshPreview();
    }

    public void OnPlay()
    {
        var progress = ProgressLevel();
        if (selectedLevel < 1 || selectedLevel > progress)
        {
            ShowToast("Level locked.");
            return;
        }

        if (selectedLevel > 9)
        {
            ShowToast("Level " + selectedLevel + " is coming soon.");
            return;
        }

        var sceneName = SceneForLevel(selectedLevel);
        var buildPath = "Assets/Scenes/" + sceneName + ".unity";
        if (SceneUtility.GetBuildIndexByScenePath(buildPath) < 0)
        {
            ShowToast("Level " + selectedLevel + " scene missing. Rebuild in Unity.");
            return;
        }

        LevelLaunchContext.Set(0, selectedLevel);
        SceneManager.LoadScene(sceneName);
    }

    static string SceneForLevel(int level)
    {
        switch (level)
        {
            case 2: return GameScenes.GameplayLevel2;
            case 3: return GameScenes.GameplayLevel3;
            case 4: return GameScenes.GameplayLevel4;
            case 5: return GameScenes.GameplayLevel5;
            case 6: return GameScenes.GameplayLevel6;
            case 7: return GameScenes.GameplayLevel7;
            case 8: return GameScenes.GameplayLevel8;
            case 9: return GameScenes.GameplayLevel9;
            default: return GameScenes.GameplayLevel1;
        }
    }

    void Refresh()
    {
        if (worldImages != null)
        {
            for (var i = 0; i < worldImages.Length; i++)
            {
                if (worldImages[i] == null)
                    continue;
                worldImages[i].sprite = i == 0 ? worldActive : worldLocked;
            }
        }

        var progress = ProgressLevel();
        if (selectedLevel < 1 || selectedLevel > progress)
            selectedLevel = progress;

        RefreshNodes();

        if (gemLabel != null)
            gemLabel.text = "18";

        RefreshPreview();
    }

    void RefreshNodes()
    {
        var progress = ProgressLevel();
        if (nodeImages == null)
            return;

        for (var i = 0; i < nodeImages.Length; i++)
        {
            var level = i + 1;
            var selected = level == selectedLevel;
            var image = nodeImages[i];

            if (image != null)
            {
                if (level < progress)
                    image.sprite = selected ? nodeCurrent : nodeCleared;
                else if (level == progress)
                    image.sprite = nodeCurrent;
                else
                    image.sprite = nodeLocked;

                image.color = level > progress
                    ? LockedTint
                    : selected
                        ? SelectedTint
                        : level == progress
                            ? CurrentTint
                            : ClearedTint;

                image.rectTransform.localScale = Vector3.one * (selected && level <= progress ? SelectedScale : NormalScale);
            }

            if (nodeLabels != null && i < nodeLabels.Length && nodeLabels[i] != null)
                nodeLabels[i].text = "LEVEL\n" + level;

            if (nodeNotes != null && i < nodeNotes.Length && nodeNotes[i] != null)
            {
                if (level < progress)
                    nodeNotes[i].text = selected ? "CLEARED\nSELECTED" : "CLEARED";
                else if (level == progress)
                    nodeNotes[i].text = selected ? "CURRENT\nSELECTED" : "CURRENT";
                else
                    nodeNotes[i].text = string.Empty;
            }
        }
    }

    void RefreshPreview()
    {
        var spec = GameplayLevelCatalog.Get(selectedLevel);
        var name = !string.IsNullOrEmpty(spec.StageName)
            ? spec.StageName
            : LevelNames[Mathf.Clamp(selectedLevel - 1, 0, LevelNames.Length - 1)];

        if (previewTitle != null)
            previewTitle.text = "LEVEL " + selectedLevel + ": \"" + name + "\"";
        if (previewTarget != null)
            previewTarget.text = "Target:  " + spec.RequiredReds;
        if (previewReward != null)
            previewReward.text = "Rewards:  " + PurpleRewardFor(spec);
    }

    static int PurpleRewardFor(GameplayLevelSpec spec)
    {
        // Simple wallet tease until store economy is wired: base + extras.
        return 8 + Mathf.Max(0, spec.ExtraReds) * 4 + Mathf.Max(0, spec.RequiredReds) * 2;
    }

    int ProgressLevel()
    {
        var slot = ProfileSave.Active;
        if (slot == null || !slot.occupied)
            return 1;
        return Mathf.Clamp(slot.level, 1, 12);
    }

    void ShowToast(string message)
    {
        if (toastRoot == null || toastLabel == null)
            return;

        toastLabel.text = message;
        toastRoot.SetActive(true);
        if (toastRoutine != null)
            StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(HideToast());
    }

    IEnumerator HideToast()
    {
        yield return new WaitForSecondsRealtime(2.2f);
        if (toastRoot != null)
            toastRoot.SetActive(false);
    }
}
