using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    static readonly Color UnselectedLabel = new Color(0.28f, 0.22f, 0.16f, 1f);

    [SerializeField] GameObject root;
    [SerializeField] RectTransform board;
    [SerializeField] CanvasGroup group;
    [SerializeField] Button resumeButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button quitButton;
    [SerializeField] Button soundOnButton;
    [SerializeField] Button soundOffButton;
    [SerializeField] Button vibrateOnButton;
    [SerializeField] Button vibrateOffButton;
    [SerializeField] Image soundOnImage;
    [SerializeField] Image soundOffImage;
    [SerializeField] Image vibrateOnImage;
    [SerializeField] Image vibrateOffImage;
    [SerializeField] TMP_Text soundOnLabel;
    [SerializeField] TMP_Text soundOffLabel;
    [SerializeField] TMP_Text vibrateOnLabel;
    [SerializeField] TMP_Text vibrateOffLabel;
    [SerializeField] Sprite pillOn;
    [SerializeField] Sprite pillOff;
    [SerializeField] ConfirmDialog confirm;

    public bool IsOpen => root != null && root.activeSelf;

    void Awake()
    {
        Bind(resumeButton, Hide);
        Bind(restartButton, OnRestart);
        Bind(quitButton, OnQuit);
        Bind(soundOnButton, () => SetSound(true));
        Bind(soundOffButton, () => SetSound(false));
        Bind(vibrateOnButton, () => SetVibrate(true));
        Bind(vibrateOffButton, () => SetVibrate(false));
        if (root != null)
            root.SetActive(false);
    }

    static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    public void Show()
    {
        if (IsOpen)
            return;

        root.SetActive(true);
        root.transform.SetAsLastSibling();
        Paint();
        GameplaySessionController.Instance?.OpenModal(true);
        StartCoroutine(ModalPop.In(board, group));
    }

    public void Hide()
    {
        if (!IsOpen)
            return;

        if (confirm != null && confirm.IsOpen)
            confirm.Hide();
        root.SetActive(false);
        GameplaySessionController.Instance?.CloseModal(true);
    }

    void OnRestart()
    {
        GameplaySessionController.Instance?.Restart();
    }

    void OnQuit()
    {
        if (confirm == null)
        {
            GameplaySessionController.Instance?.LeaveToWorldSelect();
            return;
        }

        confirm.Show("Quit to the map?\nThis level's progress will be lost.", () => GameplaySessionController.Instance?.LeaveToWorldSelect());
    }

    void SetSound(bool on)
    {
        GameSettings.SetSound(on);
        Paint();
    }

    void SetVibrate(bool on)
    {
        GameSettings.SetVibrate(on);
        Paint();
    }

    void Paint()
    {
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
            label.color = selected ? Color.white : UnselectedLabel;
    }
}
