using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameplaySessionController : MonoBehaviour
{
    enum SessionState
    {
        Playing,
        Won,
    }

    public static GameplaySessionController Instance { get; private set; }

    [SerializeField] GridBoard board;
    [SerializeField] PlayerGridMotor player;
    [SerializeField] GameplayHudController hud;
    [SerializeField] GameplayFeedback feedback;
    [SerializeField] GameplayCameraFit cameraFit;
    [SerializeField] ExitIndicator exitIndicator;
    [SerializeField] FloatingJoystick joystick;
    [SerializeField] TutorialOverlay tutorial;
    [SerializeField] InteractionHintOverlay interactionHint;
    [SerializeField] GridWorldController world;
    [SerializeField] PauseMenuController pauseMenu;
    [SerializeField] InventoryModal inventory;
    [SerializeField] ConfirmDialog confirm;
    [SerializeField] LevelCompletePanel completePanel;
    [SerializeField] float completeDelay = 0.6f;
    [SerializeField] float doorRevealHold = 1.2f;

    GameplayLevelSpec levelSpec;
    int RedsRequired => levelSpec.RequiredReds;
    int ExtraReds => levelSpec.ExtraReds;

    int redsCollected;
    int purpleWallet;
    int lives = 3;
    Vector2Int respawnCell;
    int modalCount;
    int audioPauseCount;
    SessionState state = SessionState.Playing;

    public bool AcceptsInput => state == SessionState.Playing && modalCount == 0 && (cameraFit == null || !cameraFit.IsPanning);

    int LevelIndex => LevelLaunchContext.LevelIndex > 0 ? LevelLaunchContext.LevelIndex : 1;
    int RequiredCollected => Mathf.Min(redsCollected, RedsRequired);
    int ExtraCollected => Mathf.Max(0, redsCollected - RedsRequired);

    void Awake()
    {
        Instance = this;
        ResetGlobalPause();
        ProfileSave.EnsureSeeded();
        GameSettings.Apply();

        levelSpec = GameplayLevelCatalog.Get(LevelIndex);
        board.BuildFromSpec(levelSpec);
        respawnCell = board.Spawn;
        if (world == null)
            world = board.GetComponent<GridWorldController>();
        player.Initialize(board, board.Spawn);
        if (cameraFit != null)
            cameraFit.SnapToTarget();

        if (hud != null)
        {
            hud.SetStage(LevelIndex, levelSpec.StageName);
            if (hud.PauseButton != null)
                hud.PauseButton.onClick.AddListener(OpenPause);
            if (hud.InventoryButton != null)
                hud.InventoryButton.onClick.AddListener(OpenInventory);
            if (hud.RestartButton != null)
                hud.RestartButton.onClick.AddListener(AskRestart);
            RefreshHud();
        }
    }

    void Start()
    {
        if (tutorial != null)
            tutorial.Begin(LevelIndex, levelSpec.StageName, levelSpec.TutorialHint);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame || state != SessionState.Playing)
            return;

        if (pauseMenu != null && pauseMenu.IsOpen)
            pauseMenu.Hide();
        else if (modalCount == 0)
            OpenPause();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && AcceptsInput)
            OpenPause();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        ResetGlobalPause();
    }

    public void OpenModal(bool pauseAudio)
    {
        modalCount++;
        if (pauseAudio)
        {
            audioPauseCount++;
            AudioListener.pause = true;
        }

        Time.timeScale = 0f;
        if (joystick != null)
            joystick.Release();
    }

    public void CloseModal(bool pauseAudio)
    {
        modalCount = Mathf.Max(0, modalCount - 1);
        if (pauseAudio)
            audioPauseCount = Mathf.Max(0, audioPauseCount - 1);
        if (audioPauseCount == 0)
            AudioListener.pause = false;
        if (modalCount == 0)
            Time.timeScale = 1f;
    }

    void OpenPause()
    {
        if (state == SessionState.Playing && pauseMenu != null)
            pauseMenu.Show();
    }

    void OpenInventory()
    {
        if (AcceptsInput && inventory != null)
            inventory.Show(RequiredCollected, RedsRequired, ExtraCollected, ExtraReds, purpleWallet, lives);
    }

    void AskRestart()
    {
        if (!AcceptsInput)
            return;
        if (confirm == null)
        {
            Restart();
            return;
        }

        confirm.Show("Restart this level?", Restart);
    }

    public void OnFootPlant(Vector3 feetWorld)
    {
        if (feedback != null)
            feedback.SpawnFootstep(feetWorld);
    }

    // Locked exit only — stone walls stop silently with no SFX or camera bump.
    public void OnBlockedExit()
    {
        if (board != null && !board.ExitUnlocked)
            board.FlashSlots();
    }

    public void OnDigLungeStart(Vector3 dirtFace, Vector2Int facing, Transform explorerVisual)
    {
        if (feedback != null)
            feedback.PlayDigLunge(dirtFace, facing, explorerVisual);
    }

    public void OnDug(Vector3 position)
    {
        GameSfx.Instance?.PlayDig();
        if (feedback != null)
            feedback.DigBurst(position);
        if (tutorial != null)
            tutorial.NotifyDug();
        if (interactionHint != null)
            interactionHint.NotifyDug();
    }

    public void OnBoulderPushed(Vector3 at)
    {
        if (feedback != null)
            feedback.SpawnFootstep(at);
    }

    public void OnEnteredCell(Vector2Int cell, GridCell type)
    {
        if (type == GridCell.PurpleGem && board.TryCollectPurple(cell.x, cell.y, out var gem))
        {
            purpleWallet += 3;
            RefreshHud();
            GameSfx.Instance?.PlayGemCollect();
            if (feedback != null)
                feedback.CollectGem(gem, () => { if (gem != null) Destroy(gem); });
        }
        else if (type == GridCell.Chest || type == GridCell.SecretChest)
        {
            if (board.TryOpenChest(cell.x, cell.y, out var amount, out _))
            {
                purpleWallet += amount;
                RefreshHud();
                GameSfx.Instance?.PlayGemCollect();
            }
        }
        else if (type == GridCell.Checkpoint)
        {
            respawnCell = cell;
            if (world != null)
                world.SetCheckpoint(cell);
        }

        if (world == null)
            return;
        if (world.IsSnakeAt(cell) || world.IsSpearDanger(cell) || world.IsFireAt(cell))
            OnPlayerHit();
    }

    void OnPlayerHit()
    {
        lives = Mathf.Max(0, lives - 1);
        RefreshHud();
        GameSfx.Instance?.PlayHit();
        if (feedback != null)
            feedback.Bump(Vector2Int.zero);
        if (lives <= 0)
        {
            AskRestart();
            return;
        }

        player.Initialize(board, respawnCell);
        if (cameraFit != null)
            cameraFit.SnapToTarget();
    }

    public void OnGemCell(Vector2Int cell)
    {
        if (!board.TryCollectGem(cell.x, cell.y, out var gem))
            return;

        redsCollected++;
        var count = redsCollected;
        GameSfx.Instance?.PlayGemCollect();

        if (feedback != null)
            feedback.CollectGem(gem, () => OnGemArrived(count));
        else
        {
            if (gem != null)
                Destroy(gem);
            OnGemArrived(count);
        }
    }

    void OnGemArrived(int count)
    {
        RefreshHud();
        if (hud != null)
            hud.PunchRequired(count >= RedsRequired);
        if (count <= RedsRequired)
            board.LightNextSlot();
        if (count >= RedsRequired && !board.ExitUnlocked)
            UnlockExit();
    }

    void UnlockExit()
    {
        board.SetExitUnlocked();
        GameSfx.Instance?.PlayDoorUnlock();
        if (feedback != null)
            feedback.DoorUnlocked(board.DoorWorldPosition);
        if (joystick != null)
            joystick.Release();
        if (cameraFit != null)
            cameraFit.PanTo(board.DoorWorldPosition, doorRevealHold);
        if (exitIndicator != null)
            exitIndicator.Activate(board.DoorWorldPosition);
    }

    public void OnReachedExit(Vector2Int cell)
    {
        if (state != SessionState.Playing || !board.IsExit(cell.x, cell.y) || !board.ExitUnlocked)
            return;

        state = SessionState.Won;
        if (joystick != null)
            joystick.Release();
        if (exitIndicator != null)
            exitIndicator.Deactivate();
        ProfileSave.CompleteLevel(LevelIndex);
        player.PlayExitAnimation();
        StartCoroutine(ShowComplete());
    }

    IEnumerator ShowComplete()
    {
        yield return new WaitForSeconds(completeDelay);
        if (completePanel != null)
            completePanel.Show(levelSpec.StageName, RequiredCollected, RedsRequired, ExtraCollected, ExtraReds);
        else
            LeaveToWorldSelect();
    }

    void RefreshHud()
    {
        if (hud != null)
            hud.SetCounters(RequiredCollected, RedsRequired, ExtraCollected, ExtraReds, purpleWallet, lives);
    }

    public void ReturnToCheckpoint()
    {
        if (respawnCell.x < 0 || player == null || board == null)
            return;
        player.Initialize(board, respawnCell);
        if (cameraFit != null)
            cameraFit.SnapToTarget();
    }

    public void Restart()
    {
        ResetGlobalPause();
        GameplayAudioController.SkipEntryOnce = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LeaveToWorldSelect()
    {
        ResetGlobalPause();
        if (MenuMusicPlayer.Instance != null)
            MenuMusicPlayer.Instance.ResumeFromGameplay();
        SceneManager.LoadScene(GameScenes.WorldSelect);
    }

    static void ResetGlobalPause()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
