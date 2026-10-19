using System.Collections;
using UnityEngine;

public static class ModalPop
{
    public static IEnumerator In(RectTransform board, CanvasGroup group, float duration = 0.22f)
    {
        var t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            var k = Mathf.Clamp01(t / duration);
            var overshoot = 1f + Mathf.Sin(k * Mathf.PI) * 0.06f;
            if (board != null)
                board.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, k) * overshoot;
            if (group != null)
                group.alpha = k;
            yield return null;
        }

        if (board != null)
            board.localScale = Vector3.one;
        if (group != null)
            group.alpha = 1f;
    }
}
