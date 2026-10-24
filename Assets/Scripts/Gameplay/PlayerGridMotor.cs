using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
public class PlayerGridMotor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GridBoard board;
    [SerializeField] FloatingJoystick joystick;
    [SerializeField] Transform visual;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Animator animator;

    [Header("Sprites")]
    [SerializeField] Sprite downSprite;
    [SerializeField] Sprite upSprite;
    [SerializeField] Sprite sideSprite;
    [SerializeField] Sprite[] walkDown;
    [SerializeField] Sprite[] walkUp;
    [SerializeField] Sprite[] walkSide;

    [Header("Grid timing")]
    [SerializeField] float stepDuration = 0.18f;
    [Tooltip("1 = design speed. 0.5 = half speed (testing). Applied to grid step time.")]
    [SerializeField] float walkSpeedScale = 0.5f;
    [SerializeField] float deadZone = 0.3f;
    [SerializeField] float cornerAssist = 0.35f;

    [Header("Motion curves")]
    [SerializeField] AnimationCurve easeInFromRest = DefaultEaseIn();
    [SerializeField] AnimationCurve easeOutToRest = DefaultEaseOut();
    [SerializeField] AnimationCurve easeChain = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] float settleDuration = 0.05f;

    [Header("Visual walk feel")]
    [SerializeField] float walkBobAmplitude = 0.028f;
    [SerializeField] float strideSkew = 0.012f;
    [SerializeField] float leanAngle = 4f;
    [SerializeField] float leanSmooth = 16f;
    [SerializeField] float pivotSquashDuration = 0.08f;
    [SerializeField] Vector2 pivotSquashScale = new Vector2(1.07f, 0.93f);
    [SerializeField] float breatheSpeed = 2.2f;
    [SerializeField] float breatheAmount = 0.012f;
    [SerializeField] float horizontalPivotSeconds = 0.05f;

    [Header("Interaction timing")]
    [SerializeField] float digAnticipation = 0.12f;
    [SerializeField] float digLeanDegrees = 12f;
    [SerializeField] float pushBraceDelay = 0.12f;
    [SerializeField] float pushStepMultiplier = 2f;
    [SerializeField] float pushBobMultiplier = 1.35f;

    static readonly int MoveXId = Animator.StringToHash("MoveX");
    static readonly int MoveYId = Animator.StringToHash("MoveY");
    static readonly int IsMovingId = Animator.StringToHash("IsMoving");
    static readonly int IsDiggingId = Animator.StringToHash("IsDigging");
    static readonly int IsPushingId = Animator.StringToHash("IsPushing");

    Vector2Int gridPos;
    Vector2Int facing = Vector2Int.up;
    bool stepping;
    bool exiting;
    bool walking;
    bool chainingSteps;
    bool settling;
    int walkFrameIndex;
    int lastPlantedFrame = -1;
    float squashTimer;
    Vector3 visualBaseLocalPosition;
    Vector3 visualBaseLocalScale;
    Transform shadowTransform;
    Vector3 shadowBaseScale;
    bool visualBaselineCached;
    float cornerSlideVisual;
    Vector2Int cornerSlideDir;

    public Vector2Int GridPosition => gridPos;
    public Vector2 FacingVector => new Vector2(facing.x, facing.y);

    float StepDurationSeconds(bool pushing)
    {
        var scale = Mathf.Max(0.05f, walkSpeedScale);
        return stepDuration / scale * (pushing ? pushStepMultiplier : 1f);
    }

    static AnimationCurve DefaultEaseIn()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.22f, 0.04f, 0.35f, 0.35f),
            new Keyframe(1f, 1f, 2.2f, 0f));
    }

    static AnimationCurve DefaultEaseOut()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(0.78f, 0.96f, 0.4f, 0.4f),
            new Keyframe(1f, 1f, 0f, 0f));
    }

    void Awake()
    {
        CacheVisualBaseline();
        var feet = transform.position + Vector3.up * (visualBaseLocalPosition.y + 0.03f);
        var shadow = GameplaySprites.Spawn("Shadow", transform, GameplaySprites.SoftCircle, feet, 0.52f, 9);
        shadowTransform = shadow.transform;
        shadowBaseScale = shadowTransform.localScale;
        shadow.color = new Color(0f, 0f, 0f, 0.42f);
    }

    public void Initialize(GridBoard gridBoard, Vector2Int spawn)
    {
        board = gridBoard;
        CacheVisualBaseline();
        gridPos = spawn;
        transform.position = board.CellToWorld(spawn.x, spawn.y);
        chainingSteps = false;
        walking = false;
        walkFrameIndex = 1;
        Face(Vector2Int.up);
        ShowIdlePose();
        ResetVisualImmediate();
        PushAnimatorState(false, false, false);
    }

    void CacheVisualBaseline()
    {
        if (visual == null || visualBaselineCached)
            return;

        visualBaseLocalPosition = visual.localPosition;
        visualBaseLocalScale = visual.localScale;
        if (visualBaseLocalScale.sqrMagnitude < 0.001f)
            visualBaseLocalScale = Vector3.one * 0.52f;
        visualBaselineCached = true;
    }

    void Update()
    {
        if (!stepping && !exiting && !settling && !walking)
            ApplyBreathingIdle();

        UpdateCornerSlideVisual();

        var session = GameplaySessionController.Instance;
        if (board == null || stepping || exiting || settling || session == null || !session.AcceptsInput)
            return;

        var direction = ReadDirection();
        if (direction != Vector2Int.zero)
            StartCoroutine(TryStepRoutine(direction, session));
    }

    void UpdateCornerSlideVisual()
    {
        if (visual == null || stepping || walking)
        {
            cornerSlideVisual = Mathf.MoveTowards(cornerSlideVisual, 0f, Time.deltaTime * 6f);
            return;
        }

        var raw = GetRawInput();
        if (raw.sqrMagnitude < deadZone * deadZone)
        {
            cornerSlideVisual = Mathf.MoveTowards(cornerSlideVisual, 0f, Time.deltaTime * 6f);
            return;
        }

        var primary = CardinalFromRaw(raw);
        if (CanStep(primary))
        {
            cornerSlideVisual = Mathf.MoveTowards(cornerSlideVisual, 0f, Time.deltaTime * 6f);
            return;
        }

        if (!IsStoneClip(primary))
        {
            cornerSlideVisual = Mathf.MoveTowards(cornerSlideVisual, 0f, Time.deltaTime * 6f);
            return;
        }

        var slide = TryCornerNudge(raw, primary);
        if (slide == primary || !CanStep(slide))
        {
            cornerSlideVisual = Mathf.MoveTowards(cornerSlideVisual, 0f, Time.deltaTime * 6f);
            return;
        }

        cornerSlideDir = slide;
        cornerSlideVisual = Mathf.MoveTowards(cornerSlideVisual, cellSlideAmount(raw, primary, slide), Time.deltaTime * 2.2f);
        visual.localPosition = visualBaseLocalPosition + (Vector3)(Vector2)slide * cornerSlideVisual;
    }

    float cellSlideAmount(Vector2 raw, Vector2Int blocked, Vector2Int slide)
    {
        var norm = raw.normalized;
        var slideVec = new Vector2(slide.x, slide.y);
        var blockedVec = new Vector2(blocked.x, blocked.y);
        var blend = Mathf.Clamp01((Vector2.Dot(norm, slideVec) - cornerAssist * 0.35f) / (1f - cornerAssist));
        return Mathf.Lerp(0.04f, cellSize * cornerAssist * 0.45f, blend);
    }

    float cellSize => board != null ? board.CellSize : 1f;

    void LateUpdate()
    {
        if (shadowTransform == null)
            return;

        var feet = transform.position + Vector3.up * (visualBaseLocalPosition.y + 0.03f);
        shadowTransform.position = feet;
    }

    Vector2 GetRawInput()
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

        return input;
    }

    Vector2Int ReadDirection()
    {
        var raw = GetRawInput();
        if (raw.sqrMagnitude < deadZone * deadZone)
            return Vector2Int.zero;

        var primary = CardinalFromRaw(raw);
        if (CanStep(primary))
            return primary;

        if (IsStoneClip(primary))
            return TryCornerNudge(raw, primary);

        return primary;
    }

    static Vector2Int CardinalFromRaw(Vector2 raw)
    {
        if (Mathf.Abs(raw.x) > Mathf.Abs(raw.y))
            return raw.x > 0f ? Vector2Int.right : Vector2Int.left;
        return raw.y > 0f ? Vector2Int.up : Vector2Int.down;
    }

    bool CanStep(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
            return false;

        var target = gridPos + direction;
        if (board.HasBoulder(target.x, target.y))
            return board.CanPushBoulder(target, direction);
        return !board.IsBlocked(target.x, target.y);
    }

    bool IsStoneClip(Vector2Int direction)
    {
        var target = gridPos + direction;
        if (board.HasBoulder(target.x, target.y))
            return false;
        if (!board.IsBlocked(target.x, target.y))
            return false;
        return board.GetCell(target.x, target.y) == GridCell.Stone;
    }

    Vector2Int TryCornerNudge(Vector2 raw, Vector2Int blockedPrimary)
    {
        Vector2Int a;
        Vector2Int b;
        if (blockedPrimary.x != 0)
        {
            a = Vector2Int.up;
            b = Vector2Int.down;
        }
        else
        {
            a = Vector2Int.right;
            b = Vector2Int.left;
        }

        var norm = raw.normalized;
        var scoreA = Vector2.Dot(norm, new Vector2(a.x, a.y));
        var scoreB = Vector2.Dot(norm, new Vector2(b.x, b.y));
        var first = scoreA >= scoreB ? a : b;
        var second = scoreA >= scoreB ? b : a;

        var nearCorner = CornerRayFavorsSlide(raw, blockedPrimary, first, second);
        if (nearCorner || Mathf.Abs(scoreA) >= cornerAssist || Mathf.Abs(scoreB) >= cornerAssist)
        {
            if (CanStep(first))
                return first;
            if (CanStep(second))
                return second;
        }

        if (CanStep(first))
            return first;
        if (CanStep(second))
            return second;
        return blockedPrimary;
    }

    bool CornerRayFavorsSlide(Vector2 raw, Vector2Int blockedPrimary, Vector2Int optionA, Vector2Int optionB)
    {
        if (board == null)
            return false;

        var origin = transform.position;
        var target = origin + new Vector3(blockedPrimary.x, blockedPrimary.y, 0f) * cellSize;
        var hitDistance = Vector2.Distance(origin, target);
        var clip = hitDistance - cellSize * (1f - cornerAssist);
        if (clip > cellSize * cornerAssist)
            return false;

        var norm = raw.normalized;
        var lateral = Mathf.Max(Mathf.Abs(Vector2.Dot(norm, new Vector2(optionA.x, optionA.y))),
            Mathf.Abs(Vector2.Dot(norm, new Vector2(optionB.x, optionB.y))));
        return lateral >= cornerAssist * 0.5f;
    }

    IEnumerator TryStepRoutine(Vector2Int direction, GameplaySessionController session)
    {
        if (stepping)
            yield break;

        if (NeedsHorizontalPivot(direction))
            yield return HorizontalPivotRoutine();

        var previousFacing = facing;
        Face(direction);
        if (previousFacing != direction && previousFacing != Vector2Int.zero)
            TriggerPivotSquash();

        var target = gridPos + direction;
        if (board.HasBoulder(target.x, target.y))
        {
            if (board.CanPushBoulder(target, direction))
                yield return PushStepTo(target, direction, session);
            else
                yield return WallContactRoutine(direction);
            yield break;
        }

        if (board.IsBlocked(target.x, target.y))
        {
            if (board.IsExit(target.x, target.y) && !board.ExitUnlocked)
                session.OnBlockedExit();
            yield return WallContactRoutine(direction);
            yield break;
        }

        yield return StepTo(target, board.GetCell(target.x, target.y), session, false);
    }

    bool NeedsHorizontalPivot(Vector2Int newDirection)
    {
        if (newDirection.x == 0 || facing.x == 0)
            return false;
        return newDirection.x != facing.x;
    }

    IEnumerator HorizontalPivotRoutine()
    {
        stepping = true;
        facing = Vector2Int.down;
        if (spriteRenderer != null)
            spriteRenderer.flipX = false;
        ShowIdlePose();
        ApplyProceduralVisualLayer(false, 1f);
        yield return new WaitForSeconds(horizontalPivotSeconds);
        stepping = false;
    }

    IEnumerator WallContactRoutine(Vector2Int direction)
    {
        stepping = true;
        TriggerPivotSquash();
        var t = 0f;
        const float duration = 0.07f;
        while (t < duration)
        {
            t += Time.deltaTime;
            var k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            if (visual != null)
            {
                var push = new Vector3(direction.x, direction.y, 0f) * (0.04f * k);
                visual.localPosition = visualBaseLocalPosition + push;
            }

            yield return null;
        }

        ResetVisualImmediate();
        stepping = false;
    }

    IEnumerator PushStepTo(Vector2Int target, Vector2Int direction, GameplaySessionController session)
    {
        stepping = true;
        walking = true;
        PushAnimatorState(true, false, true);
        TriggerPivotSquash();

        var brace = 0f;
        while (brace < pushBraceDelay)
        {
            brace += Time.deltaTime;
            ApplyWalkVisuals(0, true, pushBobMultiplier);
            yield return null;
        }

        if (!board.TryPushBoulder(target, direction, out var landing))
        {
            stepping = false;
            walking = false;
            PushAnimatorState(false, false, false);
            yield break;
        }

        session.OnBoulderPushed(board.CellToWorld(landing.x, landing.y));
        yield return StepTo(target, GridCell.Floor, session, true);
    }

    IEnumerator StepTo(Vector2Int target, GridCell entering, GameplaySessionController session, bool pushing)
    {
        stepping = true;
        walking = true;
        var fromIdle = !chainingSteps;
        chainingSteps = true;
        lastPlantedFrame = -1;

        var duration = StepDurationSeconds(pushing);
        var heavyBob = pushing ? pushBobMultiplier : 1f;
        var isDig = entering == GridCell.Dirt;

        PushAnimatorState(true, isDig, pushing);
        if (fromIdle)
            TriggerPivotSquash();

        var from = transform.position;
        var to = board.CellToWorld(target.x, target.y);

        if (isDig)
        {
            PushAnimatorState(true, true, false);
            yield return DigAnticipation(session, target, to);
            from = transform.position;
            entering = GridCell.Floor;
        }

        var t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            var eased = fromIdle ? easeInFromRest.Evaluate(k) : easeChain.Evaluate(k);
            transform.position = Vector3.Lerp(from, to, eased);

            var frame = WalkFrameFromProgress(eased);
            ApplyWalkVisuals(frame, true, heavyBob, 0f, eased);
            TryFootPlant(session, frame);

            yield return null;
        }

        transform.position = to;
        walkFrameIndex = 3;
        ApplyWalkVisuals(3, true, heavyBob);
        gridPos = target;
        stepping = false;

        session.OnEnteredCell(target, entering);

        if (entering == GridCell.RedGem)
            session.OnGemCell(target);
        else if (entering == GridCell.Exit)
            session.OnReachedExit(target);

        var continueMove = ReadDirection() != Vector2Int.zero;
        if (!continueMove)
        {
            chainingSteps = false;
            walking = false;
            ShowIdlePose();
            yield return SettleVisual();
        }

        PushAnimatorState(continueMove, false, false);
    }

    void TryFootPlant(GameplaySessionController session, int frame)
    {
        if (frame != 0 && frame != 2)
            return;
        if (lastPlantedFrame == frame)
            return;

        lastPlantedFrame = frame;
        session?.OnFootPlant(FeetWorld());
    }

    Vector3 FeetWorld()
    {
        return transform.position + Vector3.up * (visualBaseLocalPosition.y + 0.03f);
    }

    IEnumerator DigAnticipation(GameplaySessionController session, Vector2Int dirtCell, Vector3 digWorld)
    {
        stepping = true;
        board.BeginDirtCrack(dirtCell.x, dirtCell.y);
        session.OnDigLungeStart(digWorld, facing, visual);

        var t = 0f;
        var lungeDir = (digWorld - transform.position).normalized;
        ShowIdlePose();
        PushAnimatorState(false, true, false);

        while (t < digAnticipation)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / digAnticipation);
            var squashK = Mathf.Sin(k * Mathf.PI);
            if (visual != null)
            {
                visual.localScale = new Vector3(
                    visualBaseLocalScale.x * Mathf.Lerp(1f, pivotSquashScale.x, squashK),
                    visualBaseLocalScale.y * Mathf.Lerp(1f, pivotSquashScale.y, squashK),
                    visualBaseLocalScale.z);
                visual.localPosition = visualBaseLocalPosition + (Vector3)lungeDir * (0.12f * squashK);
                var lean = digLeanDegrees * squashK;
                if (facing == Vector2Int.left)
                    lean = -lean;
                if (facing == Vector2Int.up)
                    lean *= 0.35f;
                visual.localRotation = Quaternion.Euler(0f, 0f, lean);
            }

            ApplyShadowFromBob(Mathf.Sin(k * Mathf.PI * 2f) * 0.12f);
            yield return null;
        }

        if (board.TryDig(dirtCell.x, dirtCell.y))
            session.OnDug(digWorld);

        if (visual != null)
            visual.localRotation = Quaternion.identity;
        stepping = false;
    }

    static int WalkFrameFromProgress(float eased)
    {
        return Mathf.Clamp(Mathf.FloorToInt(eased * 4f), 0, 3);
    }

    IEnumerator SettleVisual()
    {
        settling = true;
        var t = 0f;
        while (t < settleDuration)
        {
            t += Time.deltaTime;
            var k = easeOutToRest.Evaluate(Mathf.Clamp01(t / settleDuration));
            ApplyWalkVisuals(walkFrameIndex, false, 1f, 1f - k);
            yield return null;
        }

        settling = false;
        ResetVisualImmediate();
    }

    void ApplyWalkVisuals(int frame, bool moving, float bobMultiplier, float settleBlend = 0f, float stepProgress = -1f)
    {
        walkFrameIndex = frame;
        ApplyFrame(frame);

        if (visual == null)
            return;

        var verticalWalk = moving && facing.y != 0 && facing.x == 0;
        var stridePhase = stepProgress >= 0f
            ? stepProgress * Mathf.PI * 2f
            : (frame + 0.5f) / 4f * Mathf.PI * 2f;
        var bob = 0f;
        var strideSkewX = 0f;
        var targetLean = 0f;
        if (!verticalWalk && moving)
        {
            bob = Mathf.Sin(stridePhase) * walkBobAmplitude * bobMultiplier;
            if (facing.x != 0)
            {
                strideSkewX = Mathf.Sin(stridePhase) * strideSkew * facing.x;
                targetLean = -facing.x * leanAngle;
            }
        }

        var lean = moving && !verticalWalk
            ? targetLean
            : Mathf.LerpAngle(currentLeanZ(), 0f, Time.deltaTime * leanSmooth);

        var squash = Vector3.one;
        if (squashTimer > 0f)
        {
            var squashK = squashTimer / pivotSquashDuration;
            squash = new Vector3(
                Mathf.Lerp(pivotSquashScale.x, 1f, squashK),
                Mathf.Lerp(pivotSquashScale.y, 1f, squashK),
                1f);
            squashTimer = Mathf.Max(0f, squashTimer - Time.deltaTime);
        }

        visual.localPosition = visualBaseLocalPosition + Vector3.up * bob + Vector3.right * strideSkewX;
        visual.localScale = Vector3.Scale(visualBaseLocalScale, squash);
        visual.localRotation = Quaternion.Euler(0f, 0f, lean);

        ApplyShadowFromBob(!verticalWalk && moving ? Mathf.Abs(Mathf.Sin(stridePhase)) * 0.2f * bobMultiplier : 0f);

        if (settleBlend > 0f)
            visual.localPosition += Vector3.up * (-bob * settleBlend);
    }

    float currentLeanZ()
    {
        return visual != null ? visual.localRotation.eulerAngles.z : 0f;
    }

    void ApplyShadowFromBob(float intensity)
    {
        if (shadowTransform == null)
            return;

        var spread = 1f + intensity;
        shadowTransform.localScale = new Vector3(
            shadowBaseScale.x * spread,
            shadowBaseScale.y / Mathf.Max(0.5f, spread),
            shadowBaseScale.z);
    }

    void ApplyProceduralVisualLayer(bool moving, float bobMultiplier)
    {
        ApplyWalkVisuals(walkFrameIndex, moving, bobMultiplier);
    }

    void ApplyBreathingIdle()
    {
        if (visual == null)
            return;

        var breathe = Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;
        visual.localScale = new Vector3(
            visualBaseLocalScale.x,
            visualBaseLocalScale.y * (1f + breathe),
            visualBaseLocalScale.z);
        visual.localPosition = visualBaseLocalPosition;
        visual.localRotation = Quaternion.identity;
        ApplyShadowFromBob(0f);
    }

    void ResetVisualImmediate()
    {
        if (visual == null)
            return;

        visual.localPosition = visualBaseLocalPosition;
        visual.localScale = visualBaseLocalScale;
        visual.localRotation = Quaternion.identity;
        if (shadowTransform != null)
            shadowTransform.localScale = shadowBaseScale;
    }

    void TriggerPivotSquash()
    {
        squashTimer = pivotSquashDuration;
    }

    void PushAnimatorState(bool moving, bool digging, bool pushing)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.SetFloat(MoveXId, facing.x);
        animator.SetFloat(MoveYId, facing.y);
        animator.SetBool(IsMovingId, moving);
        animator.SetBool(IsDiggingId, digging);
        animator.SetBool(IsPushingId, pushing);
    }

    public void PlayExitAnimation()
    {
        exiting = true;
        PushAnimatorState(true, false, false);
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
            var frame = Mathf.FloorToInt(k * 4f) % 4;
            ApplyWalkVisuals(frame, true, 1f);
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f - k);
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
        ApplyFrame(walking ? walkFrameIndex : -1);
    }

    void ShowIdlePose()
    {
        ApplyFrame(-1);
    }

    void ApplyFrame(int frame)
    {
        if (spriteRenderer == null)
            return;

        var frames = FramesFor(facing);
        Sprite sprite;
        if (frames != null && frames.Length >= 4)
        {
            var index = frame < 0 ? 1 : Mathf.Clamp(frame, 0, frames.Length - 1);
            sprite = frames[index];
        }
        else if (frames != null && frames.Length > 0)
        {
            var index = frame < 0 ? 1 : frame % frames.Length;
            sprite = frames[index];
        }
        else
            sprite = StaticFor(facing);

        if (sprite != null && spriteRenderer.sprite != sprite)
            spriteRenderer.sprite = sprite;
    }

}
