using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialOverlay : MonoBehaviour
{
    [SerializeField] GridBoard board;
    [SerializeField] GameplayCameraFit cameraFit;
    [SerializeField] CanvasGroup introCard;
    [SerializeField] TMP_Text introLevel;
    [SerializeField] TMP_Text introStage;
    [SerializeField] CanvasGroup hint;
    [SerializeField] TMP_Text hintMessage;
    [SerializeField] float introSeconds = 2.2f;
    [SerializeField] float hintSeconds = 6f;
    [SerializeField] float goalPanHold = 1.2f;

    bool dug;
    bool hintVisible;

    public void Begin(int levelIndex, string stageName, string tutorialHint)
    {
        if (introLevel != null)
            introLevel.text = "LEVEL " + levelIndex;
        if (introStage != null)
            introStage.text = stageName;
        if (hintMessage != null)
            hintMessage.text = string.IsNullOrEmpty(tutorialHint) ? "Move to Dig!" : tutorialHint;

        SetVisible(hint, false);
        StartCoroutine(Run());
    }

    public void NotifyDug()
    {
        dug = true;
        if (hintVisible)
            StartCoroutine(Fade(hint, 1f, 0f, 0.25f));
        hintVisible = false;
    }

    IEnumerator Run()
    {
        if (introCard != null)
        {
            SetVisible(introCard, true);
            yield return Fade(introCard, 0f, 1f, 0.25f);
            yield return new WaitForSeconds(introSeconds);
            yield return Fade(introCard, 1f, 0f, 0.3f);
            SetVisible(introCard, false);
        }

        if (cameraFit != null && board != null)
        {
            cameraFit.PanTo(board.DoorCameraAnchor, goalPanHold);
            while (cameraFit.IsPanning)
                yield return null;
        }

        if (dug || hint == null)
            yield break;

        SetVisible(hint, true);
        hintVisible = true;
        yield return Fade(hint, 0f, 1f, 0.25f);

        var t = 0f;
        while (t < hintSeconds && !dug)
        {
            t += Time.deltaTime;
            if (hint != null && hintVisible)
                hint.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 4f) * 0.03f);
            yield return null;
        }

        if (hintVisible)
        {
            hintVisible = false;
            yield return Fade(hint, 1f, 0f, 0.3f);
        }
    }

    static IEnumerator Fade(CanvasGroup target, float from, float to, float duration)
    {
        if (target == null)
            yield break;

        var t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            target.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        target.alpha = to;
        if (to <= 0f)
            target.gameObject.SetActive(false);
    }

    static void SetVisible(CanvasGroup target, bool visible)
    {
        if (target == null)
            return;
        target.gameObject.SetActive(visible);
        target.alpha = visible ? target.alpha : 0f;
        target.blocksRaycasts = false;
        target.interactable = false;
    }
}
