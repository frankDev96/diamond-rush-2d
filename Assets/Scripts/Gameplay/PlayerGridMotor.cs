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
    [SerializeField] Sprite[] walkDown;
    [SerializeField] Sprite[] walkUp;
    [SerializeField] Sprite[] walkSide;
    [SerializeField] float walkFps = 10f;
    [SerializeField] float idleReturnDelay = 0.12f;
    [SerializeField] int idleFrame = 1;
    [SerializeField] float squashAmount = 0.04f;
    [SerializeField] float stepDuration = 0.17f;
    [SerializeField] float bumpCooldown = 0.35f;
    [SerializeField] float deadZone = 0.3f;

    Vector2Int gridPos;
    Vector2Int facing = Vector2Int.up;
    bool stepping;
    bool exiting;
    bool walking;
    float walkPhase;
    float lastStepEndTime = -10f;
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

        var feet = transform.position + Vector3.up * (visualBasePosition.y + 0.03f);
        var shadow = GameplaySprites.Spawn("Shadow", transform, GameplaySprites.SoftCircle, feet, 0.62f, 9);
        shadow.transform.localScale = new Vector3(shadow.transform.localScale.x, shadow.transform.localScale.x * 0.32f, 1f);
        shadow.color = new Color(0f, 0f, 0f, 0.45f);
    }

    public void Initialize(GridBoard gridBoard, Vector2Int spawn)
    {
        board = gridBoard;
        gridPos = spawn;
        transform.position = board.CellToWorld(spawn.x, spawn.y);
        Face(Vector2Int.up);
        ShowIdlePose();
    }

    void Update()
    {
        if (!stepping && !exiting)
        {
            if (walking && Time.time - lastStepEndTime >= idleReturnDelay)
                ShowIdlePose();
            AnimateIdle();
        }

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
            if (!walking)
                ShowIdlePose();
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
        if (!walking)
        {
            walking = true;
            walkPhase = 0f;
        }

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
            AdvanceWalk(Time.deltaTime);
            Squash(Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        transform.position = to;
        Squash(0f);
        gridPos = target;
        stepping = false;
        lastStepEndTime = Time.time;

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
            AdvanceWalk(Time.deltaTime);
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f - k);
            if (visual != null)
                visual.localScale = visualBaseScale * (1f - 0.3f * k);
            yield return null;
        }
    }

    Sprite[] FramesFor(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
            return walkUp;
        if (direction == Vector2Int.down)
            return walkDown;
        return walkSide;
    }

    Sprite StaticFor(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
            return upSprite;
        if (direction == Vector2Int.down)
            return downSprite;
        return sideSprite;
    }

    void Face(Vector2Int direction)
    {
        facing = direction;
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX = direction == Vector2Int.left;
        ApplyFrame(walking ? Mathf.FloorToInt(walkPhase) : -1);
    }

    void AdvanceWalk(float deltaTime)
    {
        walkPhase += deltaTime * walkFps;
        ApplyFrame(Mathf.FloorToInt(walkPhase));
    }

    void ShowIdlePose()
    {
        walking = false;
        walkPhase = 0f;
        ApplyFrame(-1);
    }

    // frame < 0 shows the standing pose for the current facing.
    void ApplyFrame(int frame)
    {
        if (spriteRenderer == null)
            return;

        var frames = FramesFor(facing);
        Sprite sprite;
        if (frames != null && frames.Length > 0)
            sprite = frames[(frame < 0 ? Mathf.Clamp(idleFrame, 0, frames.Length - 1) : frame) % frames.Length];
        else
            sprite = StaticFor(facing);

        if (sprite != null && spriteRenderer.sprite != sprite)
            spriteRenderer.sprite = sprite;
    }

    void Squash(float amount)
    {
        if (visual == null)
            return;

        visual.localScale = new Vector3(
            visualBaseScale.x * (1f + squashAmount * amount),
            visualBaseScale.y * (1f - squashAmount * amount),
            visualBaseScale.z);
        visual.localPosition = visualBasePosition + Vector3.up * (squashAmount * amount);
    }

    void AnimateIdle()
    {
        if (visual == null)
            return;

        var breathe = Mathf.Sin(Time.time * 3f);
        visual.localPosition = visualBasePosition;
        visual.localScale = new Vector3(visualBaseScale.x, visualBaseScale.y * (1f + breathe * 0.012f), visualBaseScale.z);
    }
}
