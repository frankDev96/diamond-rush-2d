using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class GameplayCameraFit : MonoBehaviour
{
    [SerializeField] GridBoard board;
    [SerializeField] Transform target;
    [SerializeField] Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] float marginTop = 150f;
    [SerializeField] float marginBottom = 40f;
    [SerializeField] float marginLeft = 250f;
    [SerializeField] float marginRight = 260f;
    [SerializeField] float visibleColumns = 13f;
    [SerializeField] float visibleRows = 7f;
    [SerializeField] float followSmoothTime = 0.18f;
    [SerializeField] float panSmoothTime = 0.35f;
    [SerializeField] float edgePadding = 1f;
    [SerializeField] float topPadding = 1.6f;

    Camera cam;
    int lastWidth;
    int lastHeight;
    bool fitted;
    float pixelsPerUnit = 1f;
    Vector2 safeOffset;
    Vector2 safeHalfExtents;
    Vector3 basePosition;
    Vector3 velocity;
    Vector3 panTarget;
    Coroutine panRoutine;

    public Vector3 ShakeOffset { get; set; }
    public bool IsPanning { get; private set; }
    public Camera Camera => EnsureCamera();
    public Rect SafeScreenRect { get; private set; }
    public float UiScale { get; private set; } = 1f;

    void Awake()
    {
        EnsureCamera();
        cam.backgroundColor = new Color(0.04f, 0.1f, 0.05f);
        basePosition = transform.position;
    }

    Camera EnsureCamera()
    {
        if (cam == null)
            cam = GetComponent<Camera>();
        return cam;
    }

    void LateUpdate()
    {
        if (!fitted || Screen.width != lastWidth || Screen.height != lastHeight)
            Fit();
        if (!fitted)
            return;

        var desired = ClampToBoard(IsPanning ? panTarget : FocusPoint());
        var smooth = IsPanning ? panSmoothTime : followSmoothTime;
        basePosition = Vector3.SmoothDamp(basePosition, desired, ref velocity, smooth);
        transform.position = basePosition + ShakeOffset;
    }

    public void SnapToTarget()
    {
        Fit();
        if (!fitted)
            return;

        basePosition = ClampToBoard(FocusPoint());
        velocity = Vector3.zero;
        transform.position = basePosition + ShakeOffset;
    }

    public void PanTo(Vector3 worldPosition, float hold)
    {
        if (panRoutine != null)
            StopCoroutine(panRoutine);
        panRoutine = StartCoroutine(PanRoutine(worldPosition, hold));
    }

    IEnumerator PanRoutine(Vector3 worldPosition, float hold)
    {
        IsPanning = true;
        panTarget = worldPosition;
        var travel = 0f;
        while (travel < 1.2f && (ClampToBoard(panTarget) - basePosition).sqrMagnitude > 0.02f)
        {
            travel += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(hold);
        IsPanning = false;
        panRoutine = null;
    }

    public void Fit()
    {
        if (board == null || board.Width == 0 || Screen.width == 0 || Screen.height == 0)
            return;

        EnsureCamera();
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        // Same scale the 1920x1080, match 0.5 CanvasScaler applies to the HUD.
        var logWidth = Mathf.Log(Screen.width / referenceResolution.x, 2f);
        var logHeight = Mathf.Log(Screen.height / referenceResolution.y, 2f);
        UiScale = Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, 0.5f));

        var left = marginLeft * UiScale;
        var right = marginRight * UiScale;
        var top = marginTop * UiScale;
        var bottom = marginBottom * UiScale;
        var availableWidth = Mathf.Max(1f, Screen.width - left - right);
        var availableHeight = Mathf.Max(1f, Screen.height - top - bottom);
        SafeScreenRect = new Rect(left, bottom, availableWidth, availableHeight);

        var cell = board.CellSize;
        pixelsPerUnit = Mathf.Min(availableWidth / (visibleColumns * cell), availableHeight / (visibleRows * cell));
        cam.orthographicSize = Screen.height / (2f * pixelsPerUnit);

        // Camera position = focus point - safeOffset puts the focus at the safe-area centre.
        safeOffset = new Vector2(
            (left + availableWidth * 0.5f - Screen.width * 0.5f) / pixelsPerUnit,
            (bottom + availableHeight * 0.5f - Screen.height * 0.5f) / pixelsPerUnit);
        safeHalfExtents = new Vector2(availableWidth * 0.5f / pixelsPerUnit, availableHeight * 0.5f / pixelsPerUnit);
        fitted = true;
    }

    Vector3 FocusPoint()
    {
        return target != null ? target.position : board.WorldCenter;
    }

    Vector3 ClampToBoard(Vector3 focus)
    {
        var center = board.WorldCenter;
        var half = board.WorldSize * 0.5f;
        var minX = center.x - half.x - edgePadding + safeHalfExtents.x;
        var maxX = center.x + half.x + edgePadding - safeHalfExtents.x;
        var minY = center.y - half.y - edgePadding + safeHalfExtents.y;
        var maxY = center.y + half.y + topPadding - safeHalfExtents.y;

        var x = minX <= maxX ? Mathf.Clamp(focus.x, minX, maxX) : (minX + maxX) * 0.5f;
        var y = minY <= maxY ? Mathf.Clamp(focus.y, minY, maxY) : (minY + maxY) * 0.5f;
        return new Vector3(x - safeOffset.x, y - safeOffset.y, transform.position.z);
    }
}
