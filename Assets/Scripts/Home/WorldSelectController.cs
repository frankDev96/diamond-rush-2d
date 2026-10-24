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

    int selectedLevel = 2;
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
        var current = CurrentLevel();
        if (level > current)
        {
            ShowToast("Level locked.");
            return;
        }

        selectedLevel = level;
        RefreshPreview();
    }

    public void OnPlay()
    {
        if (selectedLevel < 1 || selectedLevel > 9)
        {
            ShowToast("Level " + selectedLevel + " is coming soon.");
            return;
        }

        LevelLaunchContext.Set(0, selectedLevel);
        SceneManager.LoadScene(SceneForLevel(selectedLevel));
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

        var current = CurrentLevel();
        if (nodeImages != null)
        {
            for (var i = 0; i < nodeImages.Length; i++)
            {
                var level = i + 1;
                if (nodeImages[i] != null)
                {
                    if (level < current)
                        nodeImages[i].sprite = nodeCleared;
                    else if (level == current)
                        nodeImages[i].sprite = nodeCurrent;
                    else
                        nodeImages[i].sprite = nodeLocked;
                }

                if (nodeLabels != null && i < nodeLabels.Length && nodeLabels[i] != null)
                    nodeLabels[i].text = "LEVEL\n" + level;

                if (nodeNotes != null && i < nodeNotes.Length && nodeNotes[i] != null)
                {
                    if (level < current)
                        nodeNotes[i].text = "CLEARED\n3/3";
                    else if (level == current)
                        nodeNotes[i].text = "NEW";
                    else
                        nodeNotes[i].text = string.Empty;
                }
            }
        }

        if (gemLabel != null)
            gemLabel.text = "18";

        RefreshPreview();
    }

    void RefreshPreview()
    {
        var name = LevelNames[Mathf.Clamp(selectedLevel - 1, 0, LevelNames.Length - 1)];
        if (previewTitle != null)
            previewTitle.text = "LEVEL " + selectedLevel + ": \"" + name + "\"";
        if (previewTarget != null)
            previewTarget.text = "Target:  4";
        if (previewReward != null)
            previewReward.text = "Rewards:  18";
    }

    int CurrentLevel()
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
