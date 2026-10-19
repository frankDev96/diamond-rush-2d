using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldSelectController : MonoBehaviour
{
    [SerializeField] GameObject toastRoot;
    [SerializeField] TMP_Text toastLabel;

    Coroutine toastRoutine;

    void Awake()
    {
        GameSettings.Apply();
        if (toastRoot != null)
            toastRoot.SetActive(false);
    }

    public void OnBack()
    {
        SceneManager.LoadScene(GameScenes.Home);
    }

    public void SelectWorld(int index)
    {
        if (index == 0)
        {
            ShowToast("Angkor Wat is coming soon.");
            return;
        }

        ShowToast("World locked.");
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
