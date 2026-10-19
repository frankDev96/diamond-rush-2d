using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelCompletePanel : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] RectTransform board;
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text stageLabel;
    [SerializeField] TMP_Text redsValue;
    [SerializeField] Button continueButton;
    [SerializeField] Button replayButton;

    void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(() => GameplaySessionController.Instance?.LeaveToWorldSelect());
        if (replayButton != null)
            replayButton.onClick.AddListener(() => GameplaySessionController.Instance?.Restart());
        if (root != null)
            root.SetActive(false);
    }

    public void Show(string stageName, int reds, int redsRequired)
    {
        if (stageLabel != null)
            stageLabel.text = stageName;
        if (redsValue != null)
            redsValue.text = reds + " / " + redsRequired;

        root.SetActive(true);
        root.transform.SetAsLastSibling();
        StartCoroutine(ModalPop.In(board, group, 0.32f));
    }
}
