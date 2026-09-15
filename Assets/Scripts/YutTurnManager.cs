using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

// Day 3 prototype: one piece per side, alternating moves, no capture or bonus rolls.
// Requires the previously supplied YutPieceMovement and CameraDirector scripts.
public class YutTurnManager : MonoBehaviour
{
    public enum TurnState
    {
        Preparing, PlayerInput, PlayerMoving, OpponentThinking,
        OpponentMoving, GameOver, SetupError, ChoosingRoute
    }

    [Header("Pieces")]
    [SerializeField] private YutPieceMovement playerPiece;
    [SerializeField] private YutPieceMovement opponentPiece;

    [Header("UI")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private Button[] moveButtons;
    [SerializeField] private Button outerRouteButton;
    [SerializeField] private Button shortcutRouteButton;

    [Header("Camera")]
    [SerializeField] private CameraDirector cameraDirector;
    [SerializeField] private CinemachineBrain cameraBrain;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float opponentThinkSeconds = 0.8f;
    [SerializeField, Min(0f)] private float resultHoldSeconds = 0.4f;

    [Header("Testing only: disable for normal alternating turns")]
    [SerializeField] private bool skipOpponentForRouteTests;

    [SerializeField] private TurnState currentState = TurnState.Preparing;
    private bool ready;
    private int pendingSteps;
    private readonly System.Random opponentRandom = new System.Random();

    private IEnumerator Start()
    {
        SetState(TurnState.Preparing, "Preparing...");
        // Let both movement components complete their Start initialization first.
        yield return null;

        ready = playerPiece != null && opponentPiece != null
            && playerPiece != opponentPiece && turnText != null
            && cameraDirector != null && cameraBrain != null
            && moveButtons != null && moveButtons.Length == 5
            && outerRouteButton != null && shortcutRouteButton != null
            && outerRouteButton != shortcutRouteButton
            && playerPiece.IsReady && opponentPiece.IsReady;

        if (ready)
        {
            for (int i = 0; i < moveButtons.Length; i++)
            {
                if (moveButtons[i] == null) ready = false;
                if (moveButtons[i] == outerRouteButton || moveButtons[i] == shortcutRouteButton) ready = false;
                for (int j = 0; j < i; j++)
                    if (moveButtons[i] == moveButtons[j]) ready = false;
            }
        }

        if (!ready)
        {
            Fail("Check both pieces' route setup, five move buttons, two route buttons, text, director and brain. Fix in Edit mode then restart Play.");
            yield break;
        }

        RestartMatch();
    }

    // Connect the five existing UI buttons here, with integer arguments 1..5.
    public void PlayerMove(int steps)
    {
        if (!ready || !isActiveAndEnabled || currentState != TurnState.PlayerInput) return;
        if (steps < 1 || steps > 5) return;
        pendingSteps = steps;
        if (playerPiece.HasRouteChoice)
        {
            SetState(TurnState.ChoosingRoute,
                $"{steps} steps - Outer: {playerPiece.PreviewDestination(steps, false)} / Shortcut: {playerPiece.PreviewDestination(steps, true)}");
            return;
        }
        StartPlayerMove(false);
    }

    public void ChooseOuterRoute() { ChooseRoute(false); }
    public void ChooseShortcutRoute() { ChooseRoute(true); }

    private void ChooseRoute(bool shortcut)
    {
        if (!ready || !isActiveAndEnabled || currentState != TurnState.ChoosingRoute) return;
        StartPlayerMove(shortcut);
    }

    private void StartPlayerMove(bool shortcut)
    {
        int steps = pendingSteps;
        pendingSteps = 0;
        SetState(TurnState.PlayerMoving, $"Player moving: {steps}");
        StartCoroutine(PlayTurnPair(steps, shortcut));
    }

    private IEnumerator PlayTurnPair(int playerSteps, bool useShortcut)
    {
        if (!BeginMove(playerPiece, playerSteps, useShortcut)) yield break;
        while (playerPiece.IsMoving) yield return null;

        if (playerPiece.IsFinished)
        {
            SetState(TurnState.GameOver, "You win! Press Reset.");
            yield break;
        }

        yield return new WaitForSeconds(resultHoldSeconds);
        if (skipOpponentForRouteTests)
        {
            yield return BeginPlayerTurn();
            yield break;
        }
        SetState(TurnState.OpponentThinking, "Opponent thinking...");
        cameraDirector.ShowDefault();
        yield return WaitForCamera();
        yield return new WaitForSeconds(opponentThinkSeconds);

        // Uniform 1..5 is ONLY a turn-test input, not the final yut probability model.
        int opponentSteps = opponentRandom.Next(1, 6);
        SetState(TurnState.OpponentMoving, $"Opponent moving: {opponentSteps}");
        // Temporary policy: choose the shortcut whenever standing at a branch.
        if (!BeginMove(opponentPiece, opponentSteps, opponentPiece.HasRouteChoice)) yield break;
        while (opponentPiece.IsMoving) yield return null;

        if (opponentPiece.IsFinished)
        {
            SetState(TurnState.GameOver, "Opponent wins! Press Reset.");
            yield break;
        }

        yield return new WaitForSeconds(resultHoldSeconds);
        yield return BeginPlayerTurn();
    }

    private bool BeginMove(YutPieceMovement piece, int steps, bool useShortcut)
    {
        if (!piece.isActiveAndEnabled || piece.IsMoving || piece.IsFinished)
        {
            Fail("Piece must be active, idle and unfinished before moving.");
            return false;
        }

        if (!piece.TryMoveBySteps(steps, useShortcut))
        {
            Fail("Move rejected. Check piece setup and route choice.");
            return false;
        }
        return true;
    }

    private IEnumerator BeginPlayerTurn()
    {
        SetState(TurnState.Preparing, "Your turn - camera moving...");
        cameraDirector.ShowBoard();
        yield return WaitForCamera();
        SetState(TurnState.PlayerInput,
            $"Your turn [{playerPiece.CurrentNodeName}]: choose 1-5."
            + (skipOpponentForRouteTests ? " (Route test)" : ""));
    }

    private IEnumerator WaitForCamera()
    {
        // The Brain resolves the new camera during its frame update.
        yield return null;
        while (cameraBrain.IsBlending) yield return null;
    }

    public void RestartMatch()
    {
        if (!ready || !isActiveAndEnabled) return;
        // Cancel AI delays as well as movement; neither old turn may resume later.
        StopAllCoroutines();
        pendingSteps = 0;
        playerPiece.ResetPiece();
        opponentPiece.ResetPiece();
        StartCoroutine(BeginPlayerTurn());
    }

    private void SetState(TurnState state, string message)
    {
        currentState = state;
        if (turnText != null) turnText.text = message;
        SetButtons(state == TurnState.PlayerInput);
        bool choosing = state == TurnState.ChoosingRoute;
        if (outerRouteButton != null) outerRouteButton.interactable = choosing;
        if (shortcutRouteButton != null) shortcutRouteButton.interactable = choosing;
    }

    private void SetButtons(bool interactable)
    {
        if (moveButtons == null) return;
        foreach (Button button in moveButtons)
            if (button != null) button.interactable = interactable;
    }

    private void Fail(string reason)
    {
        SetState(TurnState.SetupError, "Setup error - check Console, then Reset.");
        Debug.LogError("YutTurnManager: " + reason, this);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        SetState(TurnState.Preparing, "Paused - press Reset after enabling.");
        if (!ready) return;
        playerPiece.ResetPiece();
        opponentPiece.ResetPiece();
    }
}
