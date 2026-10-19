using UnityEngine;

public class GemIdle : MonoBehaviour
{
    [SerializeField] SpriteRenderer glow;

    Vector3 basePosition;
    Vector3 baseScale;
    float phase;

    public void SetGlow(SpriteRenderer renderer)
    {
        glow = renderer;
    }

    void Start()
    {
        basePosition = transform.localPosition;
        baseScale = transform.localScale;
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        var t = Time.time * 2.4f + phase;
        transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(t) * 0.06f);
        transform.localScale = baseScale * (1f + Mathf.Sin(t * 2f) * 0.04f);
        if (glow != null)
        {
            var c = glow.color;
            c.a = 0.35f + Mathf.Sin(t * 1.5f) * 0.15f;
            glow.color = c;
        }
    }
}
