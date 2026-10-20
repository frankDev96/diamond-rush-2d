using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryModal : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] RectTransform board;
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text redsValue;
    [SerializeField] TMP_Text purpleValue;
    [SerializeField] TMP_Text livesValue;
    [SerializeField] Button closeButton;
    [SerializeField] Button dimButton;

    public bool IsOpen => root != null && root.activeSelf;

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        if (dimButton != null)
            dimButton.onClick.AddListener(Hide);
        if (root != null)
            root.SetActive(false);
    }

    public void Show(int required, int requiredTotal, int extra, int extraTotal, int purple, int lives)
    {
        if (IsOpen)
            return;

        if (redsValue != null)
            redsValue.text = required + "/" + requiredTotal + "  +" + extra + "/" + extraTotal;
        if (purpleValue != null)
            purpleValue.text = purple.ToString();
        if (livesValue != null)
            livesValue.text = "x" + lives;

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
        GameplaySessionController.Instance?.CloseModal(false);
    }
}
