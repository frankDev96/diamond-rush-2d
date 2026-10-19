using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] Canvas canvas;
    [SerializeField] RectTransform background;
    [SerializeField] RectTransform handle;
    [SerializeField] CanvasGroup visualGroup;
    [SerializeField] GameObject hint;
    [SerializeField] float maxRadius = 96f;
    [SerializeField] float restAlpha = 0.45f;

    Vector2 restPosition;
    bool dragging;
    int pointerId = int.MinValue;

    public Vector2 Direction { get; private set; }

    void Awake()
    {
        if (background != null)
            restPosition = background.anchoredPosition;
        SetAlpha(restAlpha);
    }

    void OnDisable()
    {
        Release();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (dragging)
            return;

        dragging = true;
        pointerId = eventData.pointerId;
        if (background != null)
            background.position = eventData.position;
        SetAlpha(1f);
        if (hint != null)
            hint.SetActive(false);
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || eventData.pointerId != pointerId || background == null || handle == null)
            return;

        var scale = canvas != null ? canvas.scaleFactor : 1f;
        var delta = (eventData.position - (Vector2)background.position) / Mathf.Max(0.01f, scale);
        var clamped = Vector2.ClampMagnitude(delta, maxRadius);
        handle.anchoredPosition = clamped;
        Direction = clamped / maxRadius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId)
            Release();
    }

    public void Release()
    {
        dragging = false;
        pointerId = int.MinValue;
        Direction = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
        if (background != null)
            background.anchoredPosition = restPosition;
        SetAlpha(restAlpha);
    }

    void SetAlpha(float alpha)
    {
        if (visualGroup != null)
            visualGroup.alpha = alpha;
    }
}
