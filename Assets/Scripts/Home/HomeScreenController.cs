using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeScreenController : MonoBehaviour
{
    [SerializeField] TMP_Text continueSubtitle;
    [SerializeField] Image soundOnImage;
    [SerializeField] Image soundOffImage;
    [SerializeField] Image vibrateOnImage;
    [SerializeField] Image vibrateOffImage;
    [SerializeField] TMP_Text soundOnLabel;
    [SerializeField] TMP_Text soundOffLabel;
    [SerializeField] TMP_Text vibrateOnLabel;
    [SerializeField] TMP_Text vibrateOffLabel;
    [SerializeField] GameObject loadPanel;
    [SerializeField] TMP_Text[] slotLabels;
    [SerializeField] Image[] slotImages;
    [SerializeField] GameObject toastRoot;
    [SerializeField] TMP_Text toastLabel;
    [SerializeField] RectTransform focusRing;
    [SerializeField] RectTransform settingsBlock;
    [SerializeField] RectTransform helpBlock;
    [SerializeField] Sprite pillOn;
    [SerializeField] Sprite pillOff;

    Coroutine toastRoutine;
    Coroutine focusRoutine;

    void Awake()
    {
        ProfileSave.EnsureSeeded();
        GameSettings.Apply();
        if (loadPanel != null)
            loadPanel.SetActive(false);
        if (toastRoot != null)
            toastRoot.SetActive(false);
        if (focusRing != null)
            focusRing.gameObject.SetActive(false);
        Refresh();
    }

    public void OnContinue()
    {
        var slot = ProfileSave.Active;
        if (slot == null || !slot.occupied)
        {
            ShowToast("No save to continue. Start a new profile.");
            return;
        }

        ShowToast("Continuing " + slot.Subtitle);
    }

    public void OnNewGame()
    {
        ProfileSave.TryNewGame(out var message);
        Refresh();
        ShowToast(message);
    }

    public void OnLoadGame()
    {
        if (loadPanel == null)
            return;

        Refresh();
        loadPanel.SetActive(true);
    }

    public void CloseLoad()
    {
        if (loadPanel != null)
            loadPanel.SetActive(false);
    }

    public void SelectSlot(int index)
    {
        var selected = ProfileSave.TrySelect(index, out var message);
        Refresh();
        if (selected)
            CloseLoad();
        ShowToast(message);
    }

    public void OnSettings()
    {
        Focus(settingsBlock);
    }

    public void OnHelp()
    {
        Focus(helpBlock);
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

        if (slotLabels == null)
            return;

        for (var i = 0; i < slotLabels.Length; i++)
        {
            var slot = ProfileSave.Get(i);
            if (slotLabels[i] != null)
            {
                slotLabels[i].text = slot.occupied
                    ? slot.profileName + "\n" + slot.locationName + " Level " + slot.level
                    : "Empty";
            }

            if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                slotImages[i].color = slot.occupied ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }
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

    void Focus(RectTransform target)
    {
        if (focusRing == null || target == null)
            return;

        focusRing.gameObject.SetActive(true);
        focusRing.position = target.position;
        focusRing.sizeDelta = target.rect.size + new Vector2(24f, 24f);
        if (focusRoutine != null)
            StopCoroutine(focusRoutine);
        focusRoutine = StartCoroutine(PulseFocus());
    }

    IEnumerator PulseFocus()
    {
        var image = focusRing.GetComponent<Image>();
        var elapsed = 0f;
        while (elapsed < 1.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (image != null)
            {
                var color = image.color;
                color.a = 0.45f + Mathf.PingPong(elapsed * 3f, 0.55f);
                image.color = color;
            }

            yield return null;
        }

        if (focusRing != null)
            focusRing.gameObject.SetActive(false);
    }
}
