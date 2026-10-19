using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGridMotor : MonoBehaviour
{
    [SerializeField] GridBoard board;
    [SerializeField] FloatingJoystick joystick;
    [SerializeField] Transform visual;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite downSprite;
    [SerializeField] Sprite upSprite;
    [SerializeField] Sprite sideSprite;
    [SerializeField] float stepDuration = 0.17f;
    [SerializeField] float bumpCooldown = 0.35f;
    [SerializeField] float deadZone = 0.3f;

    Vector2Int gridPos;
    bool stepping;
    bool exiting;
    float lastBumpTime = -10f;
    Vector3 visualBasePosition;
    Vector3 visualBaseScale;

    public Vector2Int GridPosition => gridPos;

    void Awake()
    {
        if (visual != null)
        {
            visualBasePosition = visual.localPosition;
            visualBaseScale = visual.localScale;
        }

        var shadow = GameplaySprites.Spawn("Shadow", transform, GameplaySprites.SoftCircle, transform.position + Vector3.down * 0.36f, 0.62f, 9);
        shadow.transform.localScale = new Vector3(shadow.transform.localScale.x, shadow.transform.localScale.x * 0.32f, 1f);
        shadow.color = new Color(0f, 0f, 0f, 0.45f);
    }

    public void Initialize(GridBoard gridBoard, Vector2Int spawn)
    {
        board = gridBoard;
        gridPos = spawn;
        transform.position = board.CellToWorld(spawn.x, spawn.y);
        Face(Vector2Int.up);
    }

    void Update()
    {
        if (!stepping && !exiting)
            AnimateIdle();

        var session = GameplaySessionController.Instance;
        if (board == null || stepping || exiting || session == null || !session.AcceptsInput)
            return;

        var direction = ReadDirection();
        if (direction != Vector2Int.zero)
            TryStep(direction, session);
    }

    Vector2Int ReadDirection()
    {
        var input = joystick != null ? joystick.Direction : Vector2.zero;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
                input = Vector2.up;
            else if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
                input = Vector2.down;
            else if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
                input = Vector2.left;
            else if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
                input = Vector2.right;
        }

        if (input.sqrMagnitude < deadZone * deadZone)
            return Vector2Int.zero;
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return input.x > 0f ? Vector2Int.right : Vector2Int.left;
        return input.y > 0f ? Vector2Int.up : Vector2Int.down;
    }

    void TryStep(Vector2Int direction, GameplaySessionController session)
    {
        Face(direction);
        var target = gridPos + direction;
        if (board.IsBlocked(target.x, target.y))
        {
            if (Time.time - lastBumpTime >= bumpCooldown)
            {
                lastBumpTime = Time.time;
                session.OnBlockedMove(target, direction);
            }

            return;
        }

        StartCoroutine(StepTo(target, board.GetCell(target.x, target.y), session));
    }

    IEnumerator StepTo(Vector2Int target, GridCell entering, GameplaySessionController session)
    {
        stepping = true;
        var from = transform.position;
        var to = board.CellToWorld(target.x, target.y);
        session.OnStepStarted(from);
        if (entering == GridCell.Dirt && board.TryDig(target.x, target.y))
            session.OnDug(to);

        var t = 0f;
        while (t < stepDuration)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / stepDuration);
            transform.position = Vector3.Lerp(from, to, k * k * (3f - 2f * k));
            Squash(Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        transform.position = to;
        Squash(0f);
        gridPos = target;
        stepping = false;

        if (entering == GridCell.RedGem)
            session.OnGemCell(target);
        else if (entering == GridCell.Exit)
            session.OnReachedExit(target);
    }

    public void PlayExitAnimation()
    {
        exiting = true;
        StartCoroutine(ExitRoutine());
    }

    IEnumerator ExitRoutine()
    {
        Face(Vector2Int.up);
        var from = transform.position;
        var to = from + Vector3.up * (board.CellSize * 0.6f);
        var t = 0f;
        const float duration = 0.5f;
        while (t < duration)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            transform.position = Vector3.Lerp(from, to, k);
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f - k);
            if (visual != null)
                visual.localScale = visualBaseScale * (1f - 0.3f * k);
            yield return null;
        }
    }

    void Face(Vector2Int direction)
    {
        if (spriteRenderer == null)
            return;

        if (direction == Vector2Int.up && upSprite != null)
        {
            spriteRenderer.sprite = upSprite;
            spriteRenderer.flipX = false;
        }
        else if (direction == Vector2Int.down && downSprite != null)
        {
            spriteRenderer.sprite = downSprite;
            spriteRenderer.flipX = false;
        }
        else if (sideSprite != null && (direction == Vector2Int.left || direction == Vector2Int.right))
        {
            spriteRenderer.sprite = sideSprite;
            spriteRenderer.flipX = direction == Vector2Int.left;
        }
    }

    void Squash(float amount)
    {
        if (visual == null)
            return;

        visual.localScale = new Vector3(
            visualBaseScale.x * (1f + 0.07f * amount),
            visualBaseScale.y * (1f - 0.07f * amount),
            visualBaseScale.z);
        visual.localPosition = visualBasePosition + Vector3.up * (0.08f * amount);
    }

    void AnimateIdle()
    {
        if (visual == null)
            return;

        var breathe = Mathf.Sin(Time.time * 3f);
        visual.localPosition = visualBasePosition + Vector3.up * (breathe * 0.015f);
        visual.localScale = new Vector3(visualBaseScale.x, visualBaseScale.y * (1f + breathe * 0.012f), visualBaseScale.z);
    }
}
