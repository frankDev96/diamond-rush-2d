using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmDialog : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] RectTransform board;
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text message;
    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;
    [SerializeField] Button dimButton;

    Action pendingYes;

    public bool IsOpen => root != null && root.activeSelf;

    void Awake()
    {
        if (yesButton != null)
            yesButton.onClick.AddListener(OnYes);
        if (noButton != null)
            noButton.onClick.AddListener(Hide);
        if (dimButton != null)
            dimButton.onClick.AddListener(Hide);
        if (root != null)
            root.SetActive(false);
    }

    public void Show(string text, Action onYes)
    {
        if (IsOpen)
            return;

        pendingYes = onYes;
        if (message != null)
            message.text = text;
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        GameplaySessionController.Instance?.OpenModal(false);
        StartCoroutine(ModalPop.In(board, group));
    }

    public void Hide()
    {
        if (!IsOpen)
            return;

        root.SetActive(false);
        pendingYes = null;
        GameplaySessionController.Instance?.CloseModal(false);
    }

    void OnYes()
    {
        var action = pendingYes;
        Hide();
        action?.Invoke();
    }
}
