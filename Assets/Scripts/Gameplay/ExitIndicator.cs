using UnityEngine;
using UnityEngine.UI;

public class ExitIndicator : MonoBehaviour
{
    [SerializeField] GameplayCameraFit cameraFit;
    [SerializeField] RectTransform arrow;
    [SerializeField] Image arrowImage;
    [SerializeField] float edgeInset = 80f;
    [SerializeField] float pulseSpeed = 5f;
    [SerializeField] float pulseAmount = 0.12f;

    RectTransform area;
    Vector3 doorPosition;
    bool active;

    void Awake()
    {
        area = (RectTransform)transform;
        SetVisible(false);
    }

    public void Activate(Vector3 worldPosition)
    {
        doorPosition = worldPosition;
        active = true;
    }

    public void Deactivate()
    {
        active = false;
        SetVisible(false);
    }

    void LateUpdate()
    {
        if (!active || cameraFit == null || arrow == null || cameraFit.IsPanning)
        {
            SetVisible(false);
            return;
        }

        var safe = cameraFit.SafeScreenRect;
        var screen = (Vector2)cameraFit.Camera.WorldToScreenPoint(doorPosition);
        if (safe.Contains(screen))
        {
            SetVisible(false);
            return;
        }

        var inset = edgeInset * cameraFit.UiScale;
        var center = safe.center;
        var halfX = Mathf.Max(1f, safe.width * 0.5f - inset);
        var halfY = Mathf.Max(1f, safe.height * 0.5f - inset);
        var direction = screen - center;
        var t = Mathf.Min(
            Mathf.Abs(direction.x) > 0.001f ? halfX / Mathf.Abs(direction.x) : float.MaxValue,
            Mathf.Abs(direction.y) > 0.001f ? halfY / Mathf.Abs(direction.y) : float.MaxValue);
        var point = center + direction * Mathf.Min(1f, t);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, point, null, out var local))
            return;

        SetVisible(true);
        arrow.anchoredPosition = local;
        arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        var pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        arrow.localScale = new Vector3(pulse, pulse, 1f);
    }

    void SetVisible(bool visible)
    {
        if (arrowImage != null && arrowImage.enabled != visible)
            arrowImage.enabled = visible;
    }
}
