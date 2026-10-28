using UnityEngine;

/// <summary>
/// Live breeze on soft-clear grass tiles, plus a short rustle when the player bumps in.
/// Rotation/scale only — the tile root owns world position.
/// </summary>
[DisallowMultipleComponent]
public class BushSway : MonoBehaviour
{
    [SerializeField] float angleDegrees = 5.5f;
    [SerializeField] float stretch = 0.045f;
    [SerializeField] float speed = 1.85f;
    [SerializeField] float secondarySpeed = 3.1f;
    [SerializeField] float phase;
    [SerializeField] float secondaryPhase;

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
        // Slight per-tile speed variance so a row of grass doesn't sway in lockstep.
        speed *= Random.Range(0.88f, 1.14f);
        secondarySpeed *= Random.Range(0.9f, 1.12f);
    }

    void OnEnable()
    {
        CacheBaseline();
    }

    void CacheBaseline()
    {
        if (cached)
            return;
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        cached = true;
    }

    /// <summary>Quick parting flutter when the player steps into this tile.</summary>
    public void Rustle(Vector2 worldPush)
    {
        if (!cached)
            CacheBaseline();
        var sign = worldPush.x < -0.01f || (Mathf.Abs(worldPush.x) < 0.01f && worldPush.y < 0f) ? -1f : 1f;
        rustle = 1f;
        rustleAngle = Random.Range(14f, 22f) * sign;
    }

    void LateUpdate()
    {
        if (!cached)
            CacheBaseline();

        var primary = Mathf.Sin(Time.time * speed + phase);
        var secondary = Mathf.Sin(Time.time * secondarySpeed + secondaryPhase);
        var wave = primary * 0.72f + secondary * 0.28f;

        if (rustle > 0f)
            rustle = Mathf.Max(0f, rustle - Time.deltaTime * 3.4f);

        var rustleWave = rustle > 0f
            ? Mathf.Sin((1f - rustle) * Mathf.PI * 5.5f) * rustle
            : 0f;

        var tilt = wave * angleDegrees + rustleWave * rustleAngle;
        var squash = 1f + wave * stretch + rustleWave * 0.08f;

        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, tilt);
        transform.localScale = new Vector3(
            baseScale.x * squash,
            baseScale.y * (2f - squash),
            baseScale.z);
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
