using UnityEngine;

/// <summary>
/// Soft breeze on grass / exterior bushes. Exterior uses gentle mode (tilt only, no squash)
/// so long hedges do not flicker. Soft-clear grass can also Rustle on bump.
/// </summary>
[DisallowMultipleComponent]
public class BushSway : MonoBehaviour
{
    [SerializeField] float angleDegrees = 3.2f;
    [SerializeField] float stretch = 0.02f;
    [SerializeField] float speed = 1.05f;
    [SerializeField] float secondarySpeed = 1.7f;
    [SerializeField] float phase;
    [SerializeField] float secondaryPhase;
    [SerializeField] bool allowStretch = true;

    Vector3 baseScale;
    Quaternion baseRotation;
    bool cached;
    float rustle;
    float rustleAngle;

    void Awake()
    {
        CacheBaseline();
        if (Mathf.Approximately(phase, 0f))
            phase = Random.Range(0f, Mathf.PI * 2f);
        if (Mathf.Approximately(secondaryPhase, 0f))
            secondaryPhase = Random.Range(0f, Mathf.PI * 2f);
        speed *= Random.Range(0.92f, 1.08f);
        secondarySpeed *= Random.Range(0.92f, 1.08f);
    }

    void OnEnable()
    {
        CacheBaseline();
    }

    /// <summary>Quiet perimeter breeze — tilt only, slow, no scale flicker.</summary>
    public void ConfigureGentle()
    {
        angleDegrees = 1.6f;
        stretch = 0f;
        speed = 0.55f;
        secondarySpeed = 0.85f;
        allowStretch = false;
        CacheBaseline(force: true);
    }

    void CacheBaseline(bool force = false)
    {
        if (cached && !force)
            return;
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        cached = true;
    }

    public void Rustle(Vector2 worldPush)
    {
        if (!cached)
            CacheBaseline();
        var sign = worldPush.x < -0.01f || (Mathf.Abs(worldPush.x) < 0.01f && worldPush.y < 0f) ? -1f : 1f;
        rustle = 1f;
        rustleAngle = Random.Range(10f, 16f) * sign;
    }

    void LateUpdate()
    {
        if (!cached)
            CacheBaseline();

        var primary = Mathf.Sin(Time.time * speed + phase);
        var secondary = Mathf.Sin(Time.time * secondarySpeed + secondaryPhase);
        var wave = primary * 0.78f + secondary * 0.22f;

        if (rustle > 0f)
            rustle = Mathf.Max(0f, rustle - Time.deltaTime * 2.6f);

        var rustleWave = rustle > 0f
            ? Mathf.Sin((1f - rustle) * Mathf.PI * 4f) * rustle
            : 0f;

        var tilt = wave * angleDegrees + rustleWave * rustleAngle;
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, tilt);

        if (allowStretch)
        {
            var squash = 1f + wave * stretch + rustleWave * 0.05f;
            transform.localScale = new Vector3(
                baseScale.x * squash,
                baseScale.y * (2f - squash),
                baseScale.z);
        }
        else
        {
            transform.localScale = baseScale;
        }
    }

    void OnDisable()
    {
        if (!cached)
            return;
        transform.localRotation = baseRotation;
        transform.localScale = baseScale;
        rustle = 0f;
    }
}
