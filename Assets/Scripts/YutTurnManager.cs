using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

// Day 3 prototype: one piece per side, alternating moves, no capture or bonus rolls.
// Requires the previously supplied YutPieceMovement and CameraDirector scripts.
public class YutTurnManager : MonoBehaviour
{
    public enum TurnState
    {
        Preparing, PlayerInput, PlayerMoving, OpponentThinking,
        OpponentMoving, GameOver, SetupError, ChoosingRoute
    }

    [Header("Exactly four DIFFERENT pieces per team")]
    [SerializeField] private YutPieceMovement[] playerPieces = new YutPieceMovement[4];
    [SerializeField] private YutPieceMovement[] opponentPieces = new YutPieceMovement[4];
    [Header("UI")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private Button[] moveButtons;
    [SerializeField] private Button[] pieceButtons = new Button[4];
    [SerializeField] private Button outerRouteButton;
    [SerializeField] private Button shortcutRouteButton;
    [Header("Camera")]
    [SerializeField] private CameraDirector cameraDirector;
    [SerializeField] private CinemachineBrain cameraBrain;
    [Header("Timing")]
    [SerializeField, Min(0f)] private float opponentThinkSeconds = 0.8f;
    [SerializeField, Min(0f)] private float resultHoldSeconds = 0.6f;
    [Header("Testing only")]
    [SerializeField] private bool skipOpponentForRouteTests;
    [SerializeField] private TurnState currentState = TurnState.Preparing;

    private bool ready;
    private int selectedIndex = -1;
    private int pendingSteps;
    private string lastOutcome = "";
    private readonly System.Random opponentRandom = new System.Random();

    private IEnumerator Start()
    {
        SetState(TurnState.Preparing, "Preparing...");
        yield return null; // Let all piece Start methods initialize.
        ready = ValidateSetup();
        if (!ready) { Fail("Check 8 pieces, 4 piece buttons, 5 move buttons, 2 route buttons, text and cameras."); yield break; }
        RestartMatch();
    }

    private bool ValidateSetup()
    {
        if (turnText == null || cameraDirector == null || cameraBrain == null
            || playerPieces == null || playerPieces.Length != 4
            || opponentPieces == null || opponentPieces.Length != 4
            || moveButtons == null || moveButtons.Length != 5
            || pieceButtons == null || pieceButtons.Length != 4) return false;
        var pieces = new HashSet<YutPieceMovement>();
        foreach (var team in new[] { playerPieces, opponentPieces })
            foreach (var piece in team)
                if (piece == null || !piece.IsReady || !piece.isActiveAndEnabled || !pieces.Add(piece)) return false;
        var buttons = new HashSet<Button>();
        foreach (var set in new[] { moveButtons, pieceButtons, new[] { outerRouteButton, shortcutRouteButton } })
            foreach (var button in set)
                if (button == null || !buttons.Add(button)) return false;
        return true;
    }

    // Inspector integer argument is 0..3, corresponding to P1..P4.
    public void SelectPlayerPiece(int index)
    {
        if (!CanAcceptInput() || index < 0 || index >= 4 || playerPieces[index].IsFinished) return;
        selectedIndex = index;
        int count = YutGroupRules.Members(Nodes(playerPieces), index).Count;
        SetState(TurnState.PlayerInput,
            $"P{index + 1} [{playerPieces[index].CurrentNodeName}], group {count}: choose 1-5. {Score()}");
    }

    public void PlayerMove(int steps)
    {
        if (!CanAcceptInput() || selectedIndex < 0 || steps < 1 || steps > 5) return;
        pendingSteps = steps;
        var piece = playerPieces[selectedIndex];
        if (piece.HasRouteChoice)
        {
            SetState(TurnState.ChoosingRoute,
                $"{steps} steps - Outer: {piece.PreviewDestination(steps, false)} / Shortcut: {piece.PreviewDestination(steps, true)}");
            return;
        }
        StartPlayerMove(false);
    }
    private bool CanAcceptInput() => ready && isActiveAndEnabled && currentState == TurnState.PlayerInput;
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
        SetState(TurnState.PlayerMoving, "Player group moving...");
        StartCoroutine(PlayPlayerAction(selectedIndex, steps, shortcut));
    }

    private IEnumerator PlayPlayerAction(int index, int steps, bool shortcut)
    {
        var group = StartGroupMove(playerPieces, index, steps, shortcut);
        if (group == null) yield break;
        while (AnyMoving(group)) yield return null;
        int captured = ResolveLanding(playerPieces, opponentPieces, index, "Player");
        if (CheckVictory(playerPieces, true)) yield break;
        yield return new WaitForSeconds(resultHoldSeconds);
        if (captured > 0 || skipOpponentForRouteTests)
        {
            yield return BeginPlayerTurn();
            yield break;
        }
        yield return OpponentTurn();
    }

    private IEnumerator OpponentTurn()
    {
        while (true)
        {
            selectedIndex = -1;
            SetState(TurnState.OpponentThinking, "Opponent thinking... " + Score());
            cameraDirector.ShowDefault();
            yield return WaitForCamera();
            yield return new WaitForSeconds(opponentThinkSeconds);
            List<int> candidates = GroupRepresentatives(opponentPieces);
            int index = candidates[opponentRandom.Next(candidates.Count)];
            int steps = opponentRandom.Next(1, 6); // Test input, not real yut probabilities.
            bool shortcut = opponentPieces[index].HasRouteChoice;
            SetState(TurnState.OpponentMoving, $"Opponent P{index + 1}: {steps} steps.");
            var group = StartGroupMove(opponentPieces, index, steps, shortcut);
            if (group == null) yield break;
            while (AnyMoving(group)) yield return null;
            int captured = ResolveLanding(opponentPieces, playerPieces, index, "Opponent");
            if (CheckVictory(opponentPieces, false)) yield break;
            yield return new WaitForSeconds(resultHoldSeconds);
            if (captured == 0) break; // One further action per capturing move, not per captured piece.
        }
        yield return BeginPlayerTurn();
    }

    private List<YutPieceMovement> StartGroupMove(YutPieceMovement[] team, int index, int steps, bool shortcut)
    {
        var group = new List<YutPieceMovement>();
        foreach (int member in YutGroupRules.Members(Nodes(team), index)) group.Add(team[member]);
        if (group.Count == 0) { Fail("Finished pieces cannot move."); return null; }
        // Validate the entire group before committing any member.
        foreach (var piece in group)
            if (!piece.IsReady || !piece.isActiveAndEnabled || piece.IsMoving || piece.IsFinished
                || steps < 1 || steps > 5 || (shortcut && !piece.HasRouteChoice))
            { Fail("Group move is invalid. Check all group members."); return null; }
        foreach (var piece in group)
            if (!piece.TryMoveBySteps(steps, shortcut))
            { Fail("Unexpected group move failure."); return null; }
        return group;
    }

    private int ResolveLanding(YutPieceMovement[] allies, YutPieceMovement[] enemies, int selected, string actor)
    {
        int destination = allies[selected].CurrentNodeId;
        List<int> captured = YutGroupRules.Captured(Nodes(enemies), destination);
        foreach (int index in captured) enemies[index].ResetPiece();
        RefreshStacks();
        int groupSize = YutGroupRules.Members(Nodes(allies), selected).Count;
        lastOutcome = destination == YutRouteRules.Finished
            ? actor + " finished a group."
            : actor + " arrived at " + allies[selected].CurrentNodeName + $" (group {groupSize}).";
        if (captured.Count > 0) lastOutcome += $" Captured {captured.Count}! Extra action.";
        SetState(currentState, lastOutcome + " " + Score());
        Debug.Log(lastOutcome, this);
        return captured.Count;
    }

    private bool CheckVictory(YutPieceMovement[] team, bool player)
    {
        if (!YutGroupRules.AllFinished(Nodes(team))) return false;
        SetState(TurnState.GameOver, (player ? "You win! " : "Opponent wins! ") + Score() + " Press Reset.");
        return true;
    }

    private IEnumerator BeginPlayerTurn()
    {
        selectedIndex = -1;
        pendingSteps = 0;
        SetState(TurnState.Preparing, "Your turn - camera moving...");
        cameraDirector.ShowBoard();
        yield return WaitForCamera();
        SetState(TurnState.PlayerInput, "Select P1-P4. " + Score() + "\n" + lastOutcome);
    }
    private IEnumerator WaitForCamera()
    {
        yield return null;
        while (cameraBrain.IsBlending) yield return null;
    }

    public void RestartMatch()
    {
        if (!ready || !isActiveAndEnabled) return;
        StopAllCoroutines();
        ResetAllPieces();
        StartCoroutine(BeginPlayerTurn());
    }
    private void ResetAllPieces()
    {
        selectedIndex = -1;
        pendingSteps = 0;
        lastOutcome = "";
        foreach (var piece in playerPieces) piece.ResetPiece();
        foreach (var piece in opponentPieces) piece.ResetPiece();
        RefreshStacks();
    }
    private void RefreshStacks()
    {
        foreach (var team in new[] { playerPieces, opponentPieces })
        {
            var levels = new Dictionary<int, int>();
            foreach (var piece in team)
            {
                int node = piece.CurrentNodeId;
                if (!YutGroupRules.IsOnBoard(node)) { piece.SetStackLevel(0); continue; }
                int level;
                levels.TryGetValue(node, out level);
                piece.SetStackLevel(level);
                levels[node] = level + 1;
            }
        }
    }
    private static int[] Nodes(YutPieceMovement[] team)
    {
        var nodes = new int[team.Length];
        for (int i = 0; i < team.Length; i++) nodes[i] = team[i].CurrentNodeId;
        return nodes;
    }
    private static bool AnyMoving(List<YutPieceMovement> group)
    {
        foreach (var piece in group) if (piece.IsMoving) return true;
        return false;
    }
    private static List<int> GroupRepresentatives(YutPieceMovement[] team)
    {
        var result = new List<int>();
        var seen = new HashSet<int>();
        for (int i = 0; i < team.Length; i++)
        {
            int node = team[i].CurrentNodeId;
            if (node == YutRouteRules.Finished) continue;
            if (node == YutRouteRules.Waiting || seen.Add(node)) result.Add(i);
        }
        return result;
    }
    private string Score()
    {
        int p = 0, e = 0;
        foreach (var piece in playerPieces) if (piece.IsFinished) p++;
        foreach (var piece in opponentPieces) if (piece.IsFinished) e++;
        return $"Finished P:{p}/4 E:{e}/4";
    }

    private void SetState(TurnState state, string message)
    {
        currentState = state;
        if (turnText != null) turnText.text = message;
        bool input = state == TurnState.PlayerInput;
        if (moveButtons != null)
            foreach (var button in moveButtons) if (button != null) button.interactable = input && selectedIndex >= 0;
        if (pieceButtons != null)
            for (int i = 0; i < pieceButtons.Length; i++)
                if (pieceButtons[i] != null) pieceButtons[i].interactable = input && ready
                    && i < playerPieces.Length && playerPieces[i] != null && !playerPieces[i].IsFinished;
        bool route = state == TurnState.ChoosingRoute;
        if (outerRouteButton != null) outerRouteButton.interactable = route;
        if (shortcutRouteButton != null) shortcutRouteButton.interactable = route;
    }
    private void Fail(string reason)
    {
        SetState(TurnState.SetupError, "Setup error - check Console.");
        Debug.LogError("YutTurnManager: " + reason, this);
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        SetState(TurnState.Preparing, "Paused - enable and press Reset.");
        if (ready) ResetAllPieces();
    }

#if UNITY_EDITOR
    private bool PrepareTest(bool skipOpponent)
    {
        if (!Application.isPlaying || !ready || !isActiveAndEnabled)
        { Debug.LogWarning("Enter Play mode with all fields assigned first.", this); return false; }
        StopAllCoroutines();
        ResetAllPieces();
        skipOpponentForRouteTests = skipOpponent;
        return true;
    }
    [ContextMenu("Tests/Load Stack Test")]
    private void LoadStackTest()
    {
        if (!PrepareTest(true)) return;
        playerPieces[0].PlaceForTesting(3);
        playerPieces[1].PlaceForTesting(5);
        RefreshStacks();
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Load Capture Test")]
    private void LoadCaptureTest()
    {
        if (!PrepareTest(false)) return;
        playerPieces[0].PlaceForTesting(3);
        opponentPieces[0].PlaceForTesting(5);
        opponentPieces[1].PlaceForTesting(5);
        RefreshStacks();
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Load Finish Test")]
    private void LoadFinishTest()
    {
        if (!PrepareTest(true)) return;
        playerPieces[0].PlaceForTesting(30);
        playerPieces[1].PlaceForTesting(30);
        playerPieces[2].PlaceForTesting(20);
        playerPieces[3].PlaceForTesting(20);
        RefreshStacks();
        StartCoroutine(BeginPlayerTurn());
    }
#endif
}