using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameplayFeedback : MonoBehaviour
{
    const int FxOrder = 9;

    [SerializeField] GameplayCameraFit cameraFit;
    [SerializeField] Camera worldCamera;
    [SerializeField] GameplayHudController hud;
    [SerializeField] Sprite dirtChunkSprite;

    ParticleSystem footDust;
    ParticleSystem digDebris;
    ParticleSystem gemSparkle;
    Coroutine shakeRoutine;

    void Awake()
    {
        footDust = GameplayFxParticles.CreateFootDust(transform);
        digDebris = GameplayFxParticles.CreateDigDebris(transform);
        gemSparkle = GameplayFxParticles.CreateGemSparkle(transform);
    }

    public void SpawnFootstep(Vector3 feetWorld)
    {
        GameplayFxParticles.EmitBurst(footDust, feetWorld + Vector3.down * 0.04f, Random.Range(3, 5));
    }

    public void DigBurst(Vector3 position)
    {
        GameplayFxParticles.EmitBurst(digDebris, position + Vector3.up * 0.08f, Random.Range(8, 13));
        for (var i = 0; i < 4; i++)
            StartCoroutine(ChunkRoutine(position, true));
        Shake(0.12f, 0.06f, Vector2.zero);
    }

    public void PlayDigLunge(Vector3 dirtFace, Vector2Int facing, Transform explorerVisual)
    {
        StartCoroutine(DigShovelRoutine(dirtFace, facing, explorerVisual));
    }

    public void CollectGem(GameObject gemRoot, Action onArrived)
    {
        if (gemRoot == null)
        {
            onArrived?.Invoke();
            return;
        }

        StartCoroutine(GemRoutine(gemRoot, onArrived));
    }

    public void DoorUnlocked(Vector3 doorPosition)
    {
        Shake(0.3f, 0.1f, Vector2.zero);
        StartCoroutine(SparkleBurst(doorPosition, new Color(1f, 0.85f, 0.4f, 0.8f), 6));
    }

    public void Bump(Vector2Int direction)
    {
        Shake(0.14f, 0.08f, new Vector2(direction.x, direction.y));
        if (GameSettings.VibrateOn)
            GameSettings.TriggerVibrate();
    }

    void Shake(float duration, float amplitude, Vector2 direction)
    {
        if (cameraFit == null)
            return;
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(duration, amplitude, direction));
    }

    IEnumerator ShakeRoutine(float duration, float amplitude, Vector2 direction)
    {
        var t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            var falloff = 1f - Mathf.Clamp01(t / duration);
            Vector2 offset = direction == Vector2.zero
                ? Random.insideUnitCircle * amplitude * falloff
                : direction * (Mathf.Sin(t / duration * Mathf.PI) * amplitude);
            cameraFit.ShakeOffset = offset;
            yield return null;
        }

        cameraFit.ShakeOffset = Vector3.zero;
        shakeRoutine = null;
    }

    IEnumerator DigShovelRoutine(Vector3 dirtFace, Vector2Int facing, Transform explorerVisual)
    {
        var shovelSprite = GameplaySprites.Shovel;
        var shovel = GameplaySprites.Spawn("DigShovel", transform, shovelSprite, dirtFace, 0.28f, FxOrder + 2);
        shovel.color = Color.white;
        var pivot = explorerVisual != null ? explorerVisual.position + Vector3.up * 0.04f : dirtFace;
        var t = 0f;
        const float duration = 0.12f;
        while (t < duration)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            var arc = Mathf.Sin(k * Mathf.PI);
            var offset = new Vector3(facing.x, facing.y, 0f) * (0.22f * arc);
            var spin = Mathf.Lerp(-65f, 45f, k) * (facing.x < 0 ? -1f : 1f);
            if (facing == Vector2Int.down)
                spin = Mathf.Lerp(-30f, 60f, k);
            else if (facing == Vector2Int.up)
                spin = Mathf.Lerp(45f, -45f, k);
            shovel.transform.position = pivot + offset;
            shovel.transform.rotation = Quaternion.Euler(0f, 0f, spin);
            shovel.transform.localScale = Vector3.one * (0.28f / shovel.sprite.bounds.size.x) * (0.9f + arc * 0.3f);
            yield return null;
        }

        if (shovel != null)
            Destroy(shovel.gameObject);
    }

    IEnumerator ChunkRoutine(Vector3 origin, bool dig = false)
    {
        var sprite = dirtChunkSprite != null ? dirtChunkSprite : GameplaySprites.WhiteSquare;
        var size = dig ? Random.Range(0.06f, 0.11f) : Random.Range(0.14f, 0.24f);
        var chunk = GameplaySprites.Spawn("DirtChunk", transform, sprite, origin, size, FxOrder);
        if (dig)
            chunk.color = new Color(0.45f, 0.32f, 0.22f, 1f);
        var velocity = new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(1.2f, 2.6f), 0f);
        var spin = Random.Range(-540f, 540f);
        var t = 0f;
        const float duration = 0.45f;
        while (t < duration)
        {
            t += Time.deltaTime;
            velocity.y -= 9f * Time.deltaTime;
            chunk.transform.position += velocity * Time.deltaTime;
            chunk.transform.Rotate(0f, 0f, spin * Time.deltaTime);
            chunk.color = new Color(chunk.color.r, chunk.color.g, chunk.color.b, 1f - Mathf.Clamp01(t / duration));
            yield return null;
        }

        Destroy(chunk.gameObject);
    }

    IEnumerator GemRoutine(GameObject gemRoot, Action onArrived)
    {
        var gemRenderer = gemRoot.transform.Find("Gem")?.GetComponent<SpriteRenderer>();
        var glow = gemRoot.transform.Find("Glow")?.GetComponent<SpriteRenderer>();
        if (gemRenderer == null)
            gemRenderer = gemRoot.GetComponentInChildren<SpriteRenderer>();

        if (glow != null)
        {
            var c = glow.color;
            c.a = 0f;
            glow.color = c;
        }

        var flyTransform = gemRenderer != null ? gemRenderer.transform : gemRoot.transform;
        var startPos = flyTransform.position;
        const float pickupWorldWidth = 0.36f;
        if (gemRenderer != null && gemRenderer.sprite != null)
        {
            var fit = pickupWorldWidth / gemRenderer.sprite.bounds.size.x;
            flyTransform.localScale = new Vector3(fit, fit, 1f);
        }

        var startScale = flyTransform.localScale;

        // 1. Quick star-sparkle burst at pickup location
        GameplayFxParticles.EmitBurst(gemSparkle, startPos, Random.Range(8, 12));

        // 2. Upward float & initial spin
        var t = 0f;
        const float lift = 0.10f;
        while (t < lift)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / lift);
            var easeLift = Mathf.Sin(k * Mathf.PI * 0.5f);
            flyTransform.position = startPos + Vector3.up * (0.16f * easeLift);
            flyTransform.rotation = Quaternion.Euler(0f, 0f, k * 45f);
            yield return null;
        }

        var from = flyTransform.position;
        var cam = worldCamera != null ? worldCamera : Camera.main;
        t = 0f;
        const float fly = 0.40f;
        var spin = 45f;

        // 3. Smooth Bezier curve flight to HUD counter with ease-in scale down
        while (t < fly)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / fly);
            var to = from;
            if (hud != null && cam != null)
            {
                var screen = hud.RequiredChipScreenPoint;
                to = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                to.z = 0f;
            }

            var control = Vector3.Lerp(from, to, 0.4f) + Vector3.up * 1.1f + Vector3.right * 0.3f;
            var u = 1f - k;
            var bezierPos = u * u * from + 2f * u * k * control + k * k * to;
            flyTransform.position = bezierPos;

            spin += Time.deltaTime * 720f;
            flyTransform.rotation = Quaternion.Euler(0f, 0f, spin);

            // Ease-in scale down: smoothly shrink down to match HUD diamond icon size
            var scaleK = k * k;
            flyTransform.localScale = Vector3.Lerp(startScale, startScale * 0.20f, scaleK);

            yield return null;
        }

        // 4. Sparkle flash on HUD arrival
        if (hud != null && cam != null)
        {
            var screen = hud.RequiredChipScreenPoint;
            var arrivalWorld = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            arrivalWorld.z = 0f;
            GameplayFxParticles.EmitBurst(gemSparkle, arrivalWorld, Random.Range(6, 10));
        }

        Destroy(gemRoot);
        onArrived?.Invoke();
    }

    IEnumerator SparkleBurst(Vector3 position, Color tint, int count)
    {
        GameplayFxParticles.EmitBurst(gemSparkle, position, count);
        yield break;
    }
}
