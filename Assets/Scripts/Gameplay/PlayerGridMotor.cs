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
    [SerializeField] float stepDuration = 0.22f;
    [Tooltip("1 = design speed. Applied to grid step time.")]
    [SerializeField] float walkSpeedScale = 0.85f;
    [Tooltip("Fallback Hz when not mid-step. During a grid step, legs lock to one full cycle per cell so feet match the floor.")]
    [SerializeField] float strideCyclesPerSecond = 2.2f;
    [Tooltip("How many walk cycles complete across one cell step (1 = one full stride per tile).")]
    [SerializeField] float strideCyclesPerStep = 1f;
    [SerializeField] float deadZone = 0.3f;
    [SerializeField] float cornerAssist = 0.35f;

    [Header("Motion curves")]
    [SerializeField] AnimationCurve easeInFromRest = DefaultEaseIn();
    [SerializeField] AnimationCurve easeOutToRest = DefaultEaseOut();
    [SerializeField] AnimationCurve easeChain = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] float settleDuration = 0.02f;

    [Header("Visual walk feel")]
    [SerializeField] float walkBobAmplitude = 0.07f;
    [SerializeField] float strideSkew = 0.024f;
    [SerializeField] float leanAngle = 6.5f;
    [SerializeField] float leanSmooth = 16f;
    [SerializeField] float pivotSquashDuration = 0.07f;
    [SerializeField] Vector2 pivotSquashScale = new Vector2(1.08f, 0.92f);
    [SerializeField] float contactPlantScale = 1.04f;
    [SerializeField] float breatheSpeed = 2.0f;
    [SerializeField] float breatheAmount = 0.01f;
    [SerializeField] float horizontalPivotSeconds = 0.04f;

    [Header("Interaction timing")]
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
    SpriteRenderer shadowRenderer;
    Vector3 shadowBaseScale;
    bool visualBaselineCached;
    float cornerSlideVisual;
    Vector2Int cornerSlideDir;
    float stridePhaseRadians;
    float wallIgnoreUntil;
    Vector2Int wallIgnoreDir;
    float grassReach;

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
        shadowRenderer = GameplaySprites.SpawnOval("Shadow", transform, GameplaySprites.OvalShadow, feet, 0.44f, 0.18f, 9);
        shadowTransform = shadowRenderer.transform;
        shadowBaseScale = shadowTransform.localScale;
        shadowRenderer.color = new Color(0f, 0f, 0f, 0.38f);
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
        if (direction == Vector2Int.zero)
            return;

        // Held into a stone wall: stay put — no bounce spam / visual flicker.
        if (Time.time < wallIgnoreUntil && direction == wallIgnoreDir && IsStoneClip(direction))
        {
            Face(direction);
            ShowIdlePose();
            return;
        }

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
        if (CanStep(primary) || !IsStoneClip(primary))
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
        cornerSlideVisual = Mathf.MoveTowards(cornerSlideVisual, cellSlideAmount(raw, primary, slide), Time.deltaTime * 3.5f);
        visual.localPosition = visualBaseLocalPosition + (Vector3)(Vector2)slide * cornerSlideVisual;
    }

    float cellSlideAmount(Vector2 raw, Vector2Int blocked, Vector2Int slide)
    {
        var norm = raw.normalized;
        var slideVec = new Vector2(slide.x, slide.y);
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
        var cell = board.GetCell(target.x, target.y);
        return cell == GridCell.Stone || cell == GridCell.Void;
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

        if (CornerRayFavorsSlide(raw, blockedPrimary, a, b, out var slideOption))
            return slideOption;

        return blockedPrimary;
    }

    bool CornerRayFavorsSlide(Vector2 raw, Vector2Int blockedPrimary, Vector2Int optionA, Vector2Int optionB, out Vector2Int bestOption)
    {
        bestOption = blockedPrimary;
        if (board == null)
            return false;

        var norm = raw.normalized;
        var dotA = Vector2.Dot(norm, new Vector2(optionA.x, optionA.y));
        var dotB = Vector2.Dot(norm, new Vector2(optionB.x, optionB.y));

        var chosen = dotA >= dotB ? optionA : optionB;
        var lateralMag = Mathf.Max(dotA, dotB);

        // Joystick within ~35% of the perpendicular axis → slide along the open corridor.
        var blockedMag = Mathf.Abs(blockedPrimary.x != 0 ? norm.y : norm.x);
        if (blockedMag >= cornerAssist && lateralMag >= cornerAssist * 0.65f && CanStep(chosen))
        {
            bestOption = chosen;
            return true;
        }

        if (lateralMag >= cornerAssist && CanStep(chosen))
        {
            bestOption = chosen;
            return true;
        }

        var alt = dotA >= dotB ? optionB : optionA;
        var altMag = Mathf.Min(dotA, dotB);
        if (altMag >= cornerAssist && CanStep(alt))
        {
            bestOption = alt;
            return true;
        }

        return false;
    }

    IEnumerator TryStepRoutine(Vector2Int direction, GameplaySessionController session)
    {
        if (stepping)
            yield break;

        stepping = true;
        walking = true;
        chainingSteps = true;
        var fromIdle = true;
        lastPlantedFrame = -1;

        while (session != null && session.AcceptsInput)
        {
            if (direction == Vector2Int.zero)
            {
                direction = ReadDirection();
                if (direction == Vector2Int.zero)
                    break;
            }

            var previousFacing = facing;
            // Snap to the true facing sprite immediately (down / up / side+flipX).
            // Do not route left↔right reverses through a fake "down" pose — that only
            // made sense when all facings shared one identical sheet.
            Face(direction);
            if (fromIdle && previousFacing != direction && previousFacing != Vector2Int.zero)
            {
                TriggerPivotSquash();
                if (NeedsFacingTurnBeat(previousFacing, direction))
                    yield return FacingTurnBeat();
            }

            var moveDir = direction;
            direction = Vector2Int.zero;
            var target = gridPos + moveDir;

            if (board.HasBoulder(target.x, target.y))
            {
                if (board.CanPushBoulder(target, moveDir))
                    yield return PushStepTo(target, moveDir, session);
                else
                    yield return WallContactRoutine(moveDir);
                break;
            }

            if (board.IsBlocked(target.x, target.y))
            {
                if (board.IsExit(target.x, target.y) && !board.ExitUnlocked)
                    session.OnBlockedExit();
                yield return WallContactRoutine(moveDir);
                break;
            }

            yield return AnimateStepTo(target, board.GetCell(target.x, target.y), session, false, fromIdle);
            fromIdle = false;
        }

        chainingSteps = false;
        walking = false;
        stepping = false;
        stridePhaseRadians = 0f;
        ShowIdlePose();
        yield return SettleVisual();
        PushAnimatorState(false, false, false);
    }

    bool NeedsFacingTurnBeat(Vector2Int from, Vector2Int to)
    {
        if (from == Vector2Int.zero || to == Vector2Int.zero || from == to)
            return false;
        // Any 90°/180° facing change gets a short plant beat so the new sheet reads.
        return from != to;
    }

    IEnumerator FacingTurnBeat()
    {
        // Brief planted beat on the new facing sprite (side mirrored for left).
        PushAnimatorState(false, false, false);
        ShowIdlePose();
        ResetVisualImmediate();
        yield return new WaitForSeconds(horizontalPivotSeconds);
    }

    IEnumerator WallContactRoutine(Vector2Int direction)
    {
        // Hard stop: face the wall and ignore that input briefly. No bounce/squash flicker.
        Face(direction);
        ShowIdlePose();
        ResetVisualImmediate();
        PushAnimatorState(false, false, false);
        wallIgnoreDir = direction;
        wallIgnoreUntil = Time.time + 0.2f;
        yield break;
    }

    IEnumerator PushStepTo(Vector2Int target, Vector2Int direction, GameplaySessionController session)
    {
        walking = true;
        PushAnimatorState(true, false, true);
        TriggerPivotSquash();

        var brace = 0f;
        while (brace < pushBraceDelay)
        {
            var dt = Time.deltaTime;
            brace += dt;
            AdvanceStride(dt, true);
            ApplyWalkVisuals(WalkFrameFromStride(), true, pushBobMultiplier);
            yield return null;
        }

        if (!board.TryPushBoulder(target, direction, out var landing))
        {
            PushAnimatorState(false, false, false);
            yield break;
        }

        session.OnBoulderPushed(board.CellToWorld(landing.x, landing.y));
        yield return AnimateStepTo(target, GridCell.Floor, session, true, false);
    }

    void AdvanceStride(float deltaTime, bool pushing)
    {
        var rate = strideCyclesPerSecond * (pushing ? 0.85f : 1f);
        stridePhaseRadians += deltaTime * rate * Mathf.PI * 2f;
    }

    int WalkFrameFromStride()
    {
        var quarter = Mathf.PI * 0.5f;
        var frame = Mathf.FloorToInt(stridePhaseRadians / quarter);
        return ((frame % 4) + 4) % 4;
    }

    IEnumerator AnimateStepTo(Vector2Int target, GridCell entering, GameplaySessionController session, bool pushing, bool fromIdle)
    {
        var duration = StepDurationSeconds(pushing);
        var heavyBob = pushing ? pushBobMultiplier : 1f;
        var isBush = entering == GridCell.Dirt;

        PushAnimatorState(true, isBush, pushing);
        if (fromIdle)
            TriggerPivotSquash();

        var from = transform.position;
        var to = board.CellToWorld(target.x, target.y);

        // Soft grass: part by hand mid-step (rustle + SFX), then clear — no dig halt.
        var push = (Vector2)(to - from);
        var grassCleared = false;
        if (isBush)
        {
            board.BeginDirtCrack(target.x, target.y, push);
            GameSfx.Instance?.PlayGrassRustle();
            PushAnimatorState(true, true, pushing);
        }

        var strideStart = stridePhaseRadians;
        var cycles = Mathf.Max(0.25f, strideCyclesPerStep) * (pushing ? 0.85f : 1f);
        lastPlantedFrame = -1;
        grassReach = 0f;

        var t = 0f;
        while (t < duration)
        {
            var dt = Time.deltaTime;
            t += dt;
            var k = Mathf.Clamp01(t / duration);
            var eased = fromIdle ? easeInFromRest.Evaluate(k) : easeChain.Evaluate(k);
            transform.position = Vector3.Lerp(from, to, eased);

            // Reach into the grass with the hands/body, then clear mid-step.
            if (isBush)
                grassReach = Mathf.Sin(Mathf.Clamp01(k / 0.5f) * Mathf.PI) * 0.055f;
            else
                grassReach = 0f;

            if (isBush && !grassCleared && k >= 0.38f)
            {
                grassCleared = true;
                grassReach = 0f;
                if (board.TryDig(target.x, target.y, push))
                    session.OnGrassParted(to);
                entering = GridCell.Floor;
                PushAnimatorState(true, false, pushing);
            }

            // Lock legs to floor travel: one stride cycle maps across the eased cell step.
            stridePhaseRadians = strideStart + eased * cycles * Mathf.PI * 2f;
            var frame = WalkFrameFromStride();
            ApplyWalkVisuals(frame, true, heavyBob);
            TryFootPlant(session, frame);

            yield return null;
        }

        grassReach = 0f;
        if (isBush && !grassCleared)
        {
            if (board.TryDig(target.x, target.y, push))
                session.OnGrassParted(to);
            entering = GridCell.Floor;
        }

        transform.position = to;
        gridPos = target;
        // Land on a contact frame so the next chained step starts cleanly.
        stridePhaseRadians = Mathf.Round(stridePhaseRadians / (Mathf.PI * 0.5f)) * (Mathf.PI * 0.5f);

        session.OnEnteredCell(target, entering);

        if (entering == GridCell.RedGem)
            session.OnGemCell(target);
        else if (entering == GridCell.Exit)
            session.OnReachedExit(target);

        PushAnimatorState(true, false, pushing);
    }

    void TryFootPlant(GameplaySessionController session, int frame)
    {
        if (frame != 0 && frame != 2)
            return;
        if (lastPlantedFrame == frame)
            return;

        lastPlantedFrame = frame;

        var feet = FeetWorld();
        var lateralSign = (frame == 0) ? 1f : -1f;
        Vector3 bootPos;
        if (facing.x != 0)
            bootPos = feet + new Vector3(-facing.x * 0.04f, (lateralSign * 0.02f) - 0.02f, 0f);
        else
            bootPos = feet + new Vector3(lateralSign * 0.06f, -0.02f, 0f);

        session?.OnFootPlant(bootPos);
    }

    Vector3 FeetWorld()
    {
        return transform.position + Vector3.up * (visualBaseLocalPosition.y + 0.03f);
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

    bool HasWalkFrames(Vector2Int direction)
    {
        var frames = FramesFor(direction);
        return frames != null && frames.Length >= 4;
    }

    void ApplyWalkVisuals(int frame, bool moving, float bobMultiplier, float settleBlend = 0f)
    {
        walkFrameIndex = frame;
        var hasFrames = HasWalkFrames(facing);
        if (hasFrames)
            ApplyFrame(frame);
        else
            ApplyFrame(-1);

        if (visual == null)
            return;

        var strideAngle = moving ? stridePhaseRadians : 0f;

        // Contact→flight→contact bob locked to stride phase (prompt-style abs(sin) bounce).
        var bob = moving ? Mathf.Abs(Mathf.Sin(strideAngle)) * walkBobAmplitude * bobMultiplier : 0f;
        var strideSkewX = 0f;
        var targetLean = 0f;
        var plant = 0f;

        if (moving)
        {
            var swing = Mathf.Sin(strideAngle);
            // Contact frames (0/2) plant slightly; passing frames lift.
            plant = Mathf.Clamp01(1f - Mathf.Abs(swing)) * 0.55f;
            if (facing.x != 0)
            {
                strideSkewX = swing * strideSkew * facing.x;
                targetLean = -facing.x * leanAngle * 0.45f;
            }
            else
            {
                strideSkewX = swing * (strideSkew * 0.55f);
                targetLean = swing * leanAngle * 0.2f;
            }
        }

        // Procedural two-part limb/stride oscillation fallback if sprite sheets are not assigned
        if (!hasFrames && moving)
        {
            var stepCyclePhase = (stridePhaseRadians / (Mathf.PI * 2f)) % 1f;
            var stepHalf = Mathf.FloorToInt(stepCyclePhase * 2f) % 2;
            var altSign = stepHalf == 0 ? 1f : -1f;

            targetLean += altSign * 5.5f;
            bob += Mathf.Abs(Mathf.Sin(strideAngle)) * (walkBobAmplitude * 0.8f);
            strideSkewX += altSign * (strideSkew * 1.2f);
        }

        var lean = moving
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
        else if (moving && plant > 0.01f)
        {
            squash = new Vector3(
                Mathf.Lerp(1f, contactPlantScale, plant),
                Mathf.Lerp(1f, 2f - contactPlantScale, plant),
                1f);
        }

        var reach = new Vector3(facing.x, facing.y, 0f) * grassReach;
        visual.localPosition = visualBaseLocalPosition + Vector3.up * bob + Vector3.right * strideSkewX + reach;
        visual.localScale = Vector3.Scale(visualBaseLocalScale, squash);
        visual.localRotation = Quaternion.Euler(0f, 0f, lean + facing.x * -6f * (grassReach / 0.055f));

        // Keep explorer above floors/walls. Dynamic -y sort went negative on the
        // north side of the board and buried the player under FloorOrder 0.
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = 10;

        ApplyShadowFromBob(moving ? Mathf.Max(0f, bob) : 0f);

        if (settleBlend > 0f)
            visual.localPosition += Vector3.up * (-bob * settleBlend);
    }

    float currentLeanZ()
    {
        return visual != null ? visual.localRotation.eulerAngles.z : 0f;
    }

    void ApplyShadowFromBob(float bounce)
    {
        if (shadowTransform == null)
            return;

        // Inverse scaling: ground shadow shrinks and softens when character bounces up,
        // and expands to full size at the foot-plant nadir.
        var factor = 1f / (1f + Mathf.Max(0f, bounce) * 3.5f);
        shadowTransform.localScale = new Vector3(
            shadowBaseScale.x * factor,
            shadowBaseScale.y * factor,
            shadowBaseScale.z);
        if (shadowRenderer != null)
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.38f * factor);
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

        // Left uses the right-facing side sheet mirrored.
        spriteRenderer.flipX = facing == Vector2Int.left;

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
