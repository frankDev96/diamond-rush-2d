using UnityEngine;

[RequireComponent(typeof(Camera))]
public class GameplayCameraFit : MonoBehaviour
{
    [SerializeField] GridBoard board;
    [SerializeField] SpriteRenderer background;
    [SerializeField] Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] float marginTop = 150f;
    [SerializeField] float marginBottom = 40f;
    [SerializeField] float marginLeft = 250f;
    [SerializeField] float marginRight = 260f;

    Camera cam;
    int lastWidth;
    int lastHeight;
    bool fitted;
    Vector3 basePosition;

    public Vector3 ShakeOffset { get; set; }

    void Awake()
    {
        cam = GetComponent<Camera>();
        basePosition = transform.position;
    }

    void LateUpdate()
    {
        if (!fitted || Screen.width != lastWidth || Screen.height != lastHeight)
            Fit();
        transform.position = basePosition + ShakeOffset;
    }

    public void Fit()
    {
        if (board == null || board.Width == 0 || Screen.width == 0 || Screen.height == 0)
            return;

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        // Same scale the 1920x1080, match 0.5 CanvasScaler applies to the HUD.
        var logWidth = Mathf.Log(Screen.width / referenceResolution.x, 2f);
        var logHeight = Mathf.Log(Screen.height / referenceResolution.y, 2f);
        var uiScale = Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, 0.5f));

        var left = marginLeft * uiScale;
        var right = marginRight * uiScale;
        var top = marginTop * uiScale;
        var bottom = marginBottom * uiScale;
        var availableWidth = Mathf.Max(1f, Screen.width - left - right);
        var availableHeight = Mathf.Max(1f, Screen.height - top - bottom);

        var size = board.WorldSize;
        var pixelsPerUnit = Mathf.Min(availableWidth / size.x, availableHeight / size.y);
        cam.orthographicSize = Screen.height / (2f * pixelsPerUnit);

        var areaCenterX = left + availableWidth * 0.5f;
        var areaCenterY = bottom + availableHeight * 0.5f;
        var center = board.WorldCenter;
        basePosition = new Vector3(
            center.x - (areaCenterX - Screen.width * 0.5f) / pixelsPerUnit,
            center.y - (areaCenterY - Screen.height * 0.5f) / pixelsPerUnit,
            transform.position.z);

        FitBackground();
        fitted = true;
    }

    void FitBackground()
    {
        if (background == null || background.sprite == null)
            return;

        var viewHeight = cam.orthographicSize * 2f;
        var viewWidth = viewHeight * cam.aspect;
        var spriteSize = background.sprite.bounds.size;
        var scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y) * 1.02f;
        background.transform.localScale = new Vector3(scale, scale, 1f);
        background.transform.position = new Vector3(basePosition.x, basePosition.y, 0f);
    }
}
