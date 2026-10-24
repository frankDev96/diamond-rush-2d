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
        var shovel = GameplaySprites.Spawn("DigShovel", transform, GameplaySprites.WhiteSquare, dirtFace, 0.22f, FxOrder + 1);
        shovel.color = new Color(0.55f, 0.42f, 0.28f, 1f);
        var pivot = explorerVisual != null ? explorerVisual.position + Vector3.up * 0.08f : dirtFace;
        var t = 0f;
        const float duration = 0.12f;
        while (t < duration)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            var arc = Mathf.Sin(k * Mathf.PI);
            var offset = new Vector3(facing.x, facing.y, 0f) * (0.18f * arc);
            var spin = Mathf.Lerp(-55f, 35f, k) * (facing.x < 0 ? -1f : 1f);
            shovel.transform.position = pivot + offset;
            shovel.transform.rotation = Quaternion.Euler(0f, 0f, spin);
            shovel.transform.localScale = Vector3.one * (0.22f / shovel.sprite.bounds.size.x) * (0.85f + arc * 0.25f);
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
        var startScale = flyTransform.localScale;
        var startPos = flyTransform.position;
        GameplayFxParticles.EmitBurst(gemSparkle, startPos, Random.Range(6, 9));

        var t = 0f;
        const float lift = 0.08f;
        while (t < lift)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / lift);
            flyTransform.position = startPos + Vector3.up * (0.12f * k);
            flyTransform.localScale = startScale * (1f + 0.04f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        var from = flyTransform.position;
        var spin = 0f;
        var cam = worldCamera != null ? worldCamera : Camera.main;
        t = 0f;
        const float fly = 0.42f;
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

            var eased = k * k * (3f - 2f * k);
            var arc = Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.35f);
            flyTransform.position = Vector3.Lerp(from, to, eased) + arc;
            spin += Time.deltaTime * 720f;
            flyTransform.rotation = Quaternion.Euler(0f, 0f, spin);
            flyTransform.localScale = Vector3.Lerp(startScale, startScale * 0.22f, eased);
            yield return null;
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
