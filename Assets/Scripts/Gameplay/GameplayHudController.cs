using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayHudController : MonoBehaviour
{
    [SerializeField] TMP_Text worldLine;
    [SerializeField] TMP_Text stageLine;
    [SerializeField] TMP_Text requiredRedsLabel;
    [SerializeField] TMP_Text optionalRedsLabel;
    [SerializeField] TMP_Text purpleLabel;
    [SerializeField] TMP_Text livesLabel;
    [SerializeField] RectTransform requiredChip;
    [SerializeField] RectTransform requiredIcon;
    [SerializeField] Image requiredChipImage;
    [SerializeField] Button pauseButton;
    [SerializeField] Button inventoryButton;
    [SerializeField] Button restartButton;

    Coroutine punchRoutine;

    public Button PauseButton => pauseButton;
    public Button InventoryButton => inventoryButton;
    public Button RestartButton => restartButton;

    public Vector2 RequiredChipScreenPoint
    {
        get
        {
            var target = requiredIcon != null ? requiredIcon : requiredChip;
            if (target == null)
                return new Vector2(Screen.width - 200f, Screen.height - 60f);
            return RectTransformUtility.WorldToScreenPoint(null, target.position);
        }
    }

    public void SetStage(int levelIndex, string levelName)
    {
        if (worldLine != null)
            worldLine.text = "WORLD 1: ANGKOR WAT - LEVEL " + levelIndex;
        if (stageLine != null)
            stageLine.text = levelName;
    }

    public void SetCounters(int requiredCollected, int requiredTotal, int extraCollected, int extraTotal, int purple, int lives)
    {
        if (requiredRedsLabel != null)
            requiredRedsLabel.text = requiredCollected + "/" + requiredTotal;
        if (optionalRedsLabel != null)
            optionalRedsLabel.text = extraCollected + "/" + extraTotal;
        if (purpleLabel != null)
            purpleLabel.text = purple.ToString();
        if (livesLabel != null)
            livesLabel.text = "x" + lives;
    }

    public void PunchRequired(bool complete)
    {
        if (requiredChip == null)
            return;
        if (punchRoutine != null)
            StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(Punch(complete));
    }

    IEnumerator Punch(bool complete)
    {
        var flash = complete ? new Color(1f, 0.92f, 0.40f) : new Color(1f, 0.65f, 0.65f);
        var baseTextColor = requiredRedsLabel != null ? requiredRedsLabel.color : Color.white;
        var t = 0f;
        const float duration = 0.32f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(t / duration);
            // Elastic bounce pulse
            var k = Mathf.Sin(progress * Mathf.PI);
            var scalePulse = 1f + 0.28f * k;

            if (requiredChip != null)
                requiredChip.localScale = Vector3.one * scalePulse;
            if (requiredIcon != null)
                requiredIcon.localScale = Vector3.one * (1f + 0.35f * k);
            if (requiredChipImage != null)
                requiredChipImage.color = Color.Lerp(Color.white, flash, k);
            if (requiredRedsLabel != null)
            {
                requiredRedsLabel.transform.localScale = Vector3.one * (1f + 0.26f * k);
                requiredRedsLabel.color = Color.Lerp(baseTextColor, flash, k);
            }
            yield return null;
        }

        if (requiredChip != null)
            requiredChip.localScale = Vector3.one;
        if (requiredIcon != null)
            requiredIcon.localScale = Vector3.one;
        if (requiredChipImage != null)
            requiredChipImage.color = Color.white;
        if (requiredRedsLabel != null)
        {
            requiredRedsLabel.transform.localScale = Vector3.one;
            requiredRedsLabel.color = baseTextColor;
        }
        punchRoutine = null;
    }
}
