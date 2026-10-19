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
    [SerializeField] Sprite dustSprite;
    [SerializeField] Sprite dirtChunkSprite;

    Coroutine shakeRoutine;

    public void SpawnDust(Vector3 position)
    {
        StartCoroutine(DustRoutine(position + Vector3.down * 0.32f));
    }

    public void DigBurst(Vector3 position)
    {
        for (var i = 0; i < 6; i++)
            StartCoroutine(ChunkRoutine(position));
        Shake(0.08f, 0.04f, Vector2.zero);
    }

    public void CollectGem(GameObject gem, Action onArrived)
    {
        if (gem == null)
        {
            onArrived?.Invoke();
            return;
        }

        StartCoroutine(GemRoutine(gem, onArrived));
    }

    public void DoorUnlocked(Vector3 doorPosition)
    {
        Shake(0.3f, 0.1f, Vector2.zero);
        StartCoroutine(BurstRoutine(doorPosition, new Color(1f, 0.85f, 0.4f, 0.8f), 3.5f, 0.6f));
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

    IEnumerator DustRoutine(Vector3 position)
    {
        var dust = GameplaySprites.Spawn("Dust", transform, dustSprite != null ? dustSprite : GameplaySprites.SoftCircle, position, 0.3f, FxOrder);
        var baseScale = dust.transform.localScale;
        var t = 0f;
        const float duration = 0.35f;
        while (t < duration)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            dust.transform.localScale = baseScale * Mathf.Lerp(1f, 2f, k);
            dust.transform.position = position + Vector3.up * (0.1f * k);
            dust.color = new Color(1f, 1f, 1f, 0.75f * (1f - k));
            yield return null;
        }

        Destroy(dust.gameObject);
    }

    IEnumerator ChunkRoutine(Vector3 origin)
    {
        var chunk = GameplaySprites.Spawn("DirtChunk", transform, dirtChunkSprite != null ? dirtChunkSprite : GameplaySprites.SoftCircle, origin, Random.Range(0.14f, 0.24f), FxOrder);
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
            chunk.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(t / duration));
            yield return null;
        }

        Destroy(chunk.gameObject);
    }

    IEnumerator GemRoutine(GameObject gem, Action onArrived)
    {
        var root = gem.transform;
        var startScale = root.localScale;
        StartCoroutine(BurstRoutine(root.position, new Color(1f, 0.4f, 0.45f, 0.8f), 1.6f, 0.35f));

        var t = 0f;
        const float pop = 0.12f;
        while (t < pop)
        {
            t += Time.deltaTime;
            root.localScale = startScale * Mathf.Lerp(1f, 1.4f, Mathf.Clamp01(t / pop));
            yield return null;
        }

        var from = root.position;
        var cam = worldCamera != null ? worldCamera : Camera.main;
        t = 0f;
        const float fly = 0.45f;
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

            var arc = Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.8f);
            root.position = Vector3.Lerp(from, to, k * k) + arc;
            root.localScale = startScale * Mathf.Lerp(1.4f, 0.5f, k);
            yield return null;
        }

        Destroy(gem);
        onArrived?.Invoke();
    }

    IEnumerator BurstRoutine(Vector3 position, Color color, float size, float duration)
    {
        var burst = GameplaySprites.Spawn("Burst", transform, GameplaySprites.SoftCircle, position, size * 0.3f, FxOrder);
        var baseScale = burst.transform.localScale;
        var t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            burst.transform.localScale = baseScale * Mathf.Lerp(1f, 3.3f, k);
            burst.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
            yield return null;
        }

        Destroy(burst.gameObject);
    }
}
