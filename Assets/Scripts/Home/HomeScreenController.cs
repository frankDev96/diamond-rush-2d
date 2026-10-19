using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeScreenController : MonoBehaviour
{
    [SerializeField] TMP_Text continueSubtitle;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] Image soundOnImage;
    [SerializeField] Image soundOffImage;
    [SerializeField] Image vibrateOnImage;
    [SerializeField] Image vibrateOffImage;
    [SerializeField] TMP_Text soundOnLabel;
    [SerializeField] TMP_Text soundOffLabel;
    [SerializeField] TMP_Text vibrateOnLabel;
    [SerializeField] TMP_Text vibrateOffLabel;
    [SerializeField] GameObject toastRoot;
    [SerializeField] TMP_Text toastLabel;
    [SerializeField] Sprite pillOn;
    [SerializeField] Sprite pillOff;

    Coroutine toastRoutine;

    void Awake()
    {
        ProfileSave.EnsureSeeded();
        GameSettings.Apply();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (toastRoot != null)
            toastRoot.SetActive(false);
        Refresh();
    }

    public void OnContinue()
    {
        var slot = ProfileSave.Active;
        if (slot == null || !slot.occupied)
        {
            ShowToast("No save to continue. Start a new game.");
            return;
        }

        SceneManager.LoadScene(GameScenes.WorldSelect);
    }

    public void OnNewGame()
    {
        if (!ProfileSave.TryNewGame(out var message))
        {
            ShowToast(message);
            return;
        }

        Refresh();
        SceneManager.LoadScene(GameScenes.WorldSelect);
    }

    public void OnSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void SetSound(bool on)
    {
        GameSettings.SetSound(on);
        Refresh();
    }

    public void SetVibrate(bool on)
    {
        GameSettings.SetVibrate(on);
        Refresh();
    }

    void Refresh()
    {
        if (continueSubtitle != null)
            continueSubtitle.text = ProfileSave.LastSavedLabel();

        PaintPill(soundOnImage, soundOnLabel, GameSettings.SoundOn);
        PaintPill(soundOffImage, soundOffLabel, !GameSettings.SoundOn);
        PaintPill(vibrateOnImage, vibrateOnLabel, GameSettings.VibrateOn);
        PaintPill(vibrateOffImage, vibrateOffLabel, !GameSettings.VibrateOn);
    }

    void PaintPill(Image image, TMP_Text label, bool selected)
    {
        if (image != null && pillOn != null && pillOff != null)
            image.sprite = selected ? pillOn : pillOff;
        if (label != null)
            label.color = selected ? Color.white : new Color(0.28f, 0.22f, 0.16f, 1f);
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
