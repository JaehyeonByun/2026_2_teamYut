using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        OpponentMoving, GameOver, SetupError, ChoosingRoute, AwaitingRoll, Rolling
    }
    [Header("Exactly four DIFFERENT pieces per team")]
    [SerializeField] private YutPieceMovement[] playerPieces = new YutPieceMovement[4];
    [SerializeField] private YutPieceMovement[] opponentPieces = new YutPieceMovement[4];
    [Header("UI: old Move Buttons now select saved results")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private Button[] moveButtons;
    [SerializeField] private Button[] pieceButtons = new Button[4];
    [SerializeField] private Button outerRouteButton;
    [SerializeField] private Button shortcutRouteButton;
    [SerializeField] private Button rollButton;
    [SerializeField] private Button confirmMoveButton;
    [SerializeField] private TMP_Text resultText;
    [Header("Camera")]
    [SerializeField] private CameraDirector cameraDirector;
    [SerializeField] private CinemachineBrain cameraBrain;
    [Header("3D throw presentation")]
    [SerializeField] private YutThrowPresenter throwPresenter;
    [Header("Timing")]
    [SerializeField, Min(0f)] private float opponentThinkSeconds = 0.8f;
    [SerializeField, Min(0f)] private float resultHoldSeconds = 0.6f;
    [Header("Rule choice: default prevents a second bonus for Yut/Mo captures")]
    [SerializeField] private bool allowCaptureBonusAfterYutMo;
    [Header("Testing only")]
    [SerializeField] private bool skipOpponentForRouteTests;
    [SerializeField] private TurnState currentState = TurnState.Preparing;

    private readonly YutTurnPool playerPool = new YutTurnPool();
    private readonly YutTurnPool opponentPool = new YutTurnPool();
    // AI decision randomness cannot alter either side's throw sequence.
    private readonly System.Random playerRollRandom = new System.Random(System.Guid.NewGuid().GetHashCode());
    private readonly System.Random opponentRollRandom = new System.Random(System.Guid.NewGuid().GetHashCode());
    private readonly System.Random opponentDecisionRandom = new System.Random(System.Guid.NewGuid().GetHashCode());
    private readonly Queue<int> testPlayerRolls = new Queue<int>();
    private bool ready;
    private bool showingPlayerPool = true;
    private int selectedIndex = -1;
    private int selectedResultId = -1;
    private string lastOutcome = "";
    private readonly TMP_Text[] resultLabels = new TMP_Text[5];

    private IEnumerator Start()
    {
        SetState(TurnState.Preparing, "Preparing...");
        yield return null;
        ready = ValidateSetup();
        if (!ready) { Fail("Check pieces/UI/cameras and Throw Presenter (including its four sticks and landing points)."); yield break; }
        for (int i = 0; i < 5; i++) resultLabels[i] = moveButtons[i].GetComponentInChildren<TMP_Text>(true);
        RestartMatch();
    }
    private bool ValidateSetup()
    {
        if (turnText == null || resultText == null || turnText == resultText
            || cameraDirector == null || cameraBrain == null
            || throwPresenter == null || !throwPresenter.IsReady || !throwPresenter.isActiveAndEnabled
            || playerPieces == null || playerPieces.Length != 4
            || opponentPieces == null || opponentPieces.Length != 4
            || moveButtons == null || moveButtons.Length != 5
            || pieceButtons == null || pieceButtons.Length != 4) return false;
        var pieces = new HashSet<YutPieceMovement>();
        foreach (var team in new[] { playerPieces, opponentPieces })
            foreach (var piece in team)
                if (piece == null || !piece.IsReady || !piece.isActiveAndEnabled || !pieces.Add(piece)) return false;
        var buttons = new HashSet<Button>();
        foreach (var set in new[] {moveButtons, pieceButtons,
            new[] {outerRouteButton, shortcutRouteButton, rollButton, confirmMoveButton}})
            foreach (var button in set)
                if (button == null || !buttons.Add(button)) return false;
        return true;
    }
    private bool CanSelect() => ready && isActiveAndEnabled
        && currentState == TurnState.PlayerInput && playerPool.CanMove;

    public void PlayerRoll()
    {
        if (!ready || !isActiveAndEnabled || currentState != TurnState.AwaitingRoll || !playerPool.CanRoll) return;
        SetState(TurnState.Rolling, "Rolling..."); // Lock synchronously before starting the coroutine.
        StartCoroutine(RollForPlayer());
    }
    private IEnumerator RollForPlayer()
    {
        bool forced = testPlayerRolls.Count > 0;
        YutThrow generated;
        if (forced)
        {
            int forcedSteps = testPlayerRolls.Dequeue();
            generated = YutThrow.FromMask(forcedSteps == 5 ? 0 : (1 << forcedSteps) - 1);
        }
        else generated = YutThrow.Roll(playerRollRandom);
        int steps = generated.Steps;
        yield return throwPresenter.PlayThrow(generated.FaceMask, false);
        if (throwPresenter.LastCompletedMask != generated.FaceMask)
        { Fail("Throw animation was interrupted. Reset after checking the presenter."); yield break; }
        if (!playerPool.RecordRoll(steps)) { Fail("Roll had no matching roll credit."); yield break; }
        lastOutcome = (forced ? "[TEST] " : "") + "Player rolled " + YutThrow.Name(steps) + ".";
        if (!forced) Debug.Log($"Player face mask: {generated.FaceMask}, result: {steps}", this);
        SetState(TurnState.Rolling, lastOutcome);
        yield return new WaitForSeconds(resultHoldSeconds);
        yield return PreparePlayerPhase();
    }
    public void SelectPlayerPiece(int index)
    {
        if (!CanSelect() || index < 0 || index >= 4 || playerPieces[index].IsFinished) return;
        selectedIndex = index;
        ShowSelection();
    }
    // Keeps existing On Click bindings. This now SELECTS a result; it cannot manufacture one.
    public void PlayerMove(int steps) { SelectResult(steps); }
    public void SelectResult(int steps)
    {
        if (!CanSelect()) return;
        YutSavedResult result = playerPool.First(steps);
        if (result == null) return;
        selectedResultId = result.Id;
        ShowSelection();
    }
    private void ShowSelection()
    {
        var result = playerPool.Find(selectedResultId);
        string piece = selectedIndex < 0 ? "Select P1-P4" :
            $"P{selectedIndex + 1} [{playerPieces[selectedIndex].CurrentNodeName}], group {YutGroupRules.Members(Nodes(playerPieces), selectedIndex).Count}";
        string value = result == null ? "select a result" : YutThrow.Name(result.Steps);
        SetState(TurnState.PlayerInput, piece + " / " + value + ". Then press Move. " + Score());
    }
    public void ConfirmMove()
    {
        if (!CanSelect() || selectedIndex < 0 || playerPieces[selectedIndex].IsFinished) return;
        var result = playerPool.Find(selectedResultId);
        if (result == null) return;
        var piece = playerPieces[selectedIndex];
        if (piece.HasRouteChoice)
        {
            SetState(TurnState.ChoosingRoute,
                $"{result.Steps} steps - Outer: {piece.PreviewDestination(result.Steps, false)} / Shortcut: {piece.PreviewDestination(result.Steps, true)}");
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
        SetState(TurnState.PlayerMoving, "Player group moving...");
        StartCoroutine(PlayPlayerAction(selectedIndex, selectedResultId, shortcut));
    }
    private IEnumerator PlayPlayerAction(int index, int resultId, bool shortcut)
    {
        var group = StartGroupMove(playerPieces, index, playerPool, resultId, shortcut);
        if (group == null) yield break;
        RefreshUI();
        while (AnyMoving(group)) yield return null;
        int captured = ResolveLanding(playerPieces, opponentPieces, index, "Player");
        if (!playerPool.CompleteMove(captured, allowCaptureBonusAfterYutMo)) { Fail("Missing pending player move."); yield break; }
        if (captured > 0) lastOutcome += playerPool.PendingRolls > 0 ? " Roll again." : " No second Yut/Mo bonus.";
        if (CheckVictory(playerPieces, true)) yield break;
        SetState(TurnState.PlayerMoving, lastOutcome + " " + Score());
        yield return new WaitForSeconds(resultHoldSeconds);
        if (playerPool.IsComplete)
        {
            if (skipOpponentForRouteTests) yield return BeginPlayerTurn();
            else yield return OpponentTurn();
        }
        else yield return PreparePlayerPhase();
    }
    private IEnumerator BeginPlayerTurn()
    {
        playerPool.BeginTurn();
        yield return PreparePlayerPhase();
    }
    private IEnumerator PreparePlayerPhase()
    {
        showingPlayerPool = true;
        selectedIndex = -1;
        selectedResultId = -1;
        SetState(TurnState.Preparing, "Your turn - camera moving...");
        if (playerPool.CanRoll) cameraDirector.ShowThrow();
        else cameraDirector.ShowBoard();
        yield return WaitForCamera();
        if (playerPool.CanRoll) SetState(TurnState.AwaitingRoll, "Press Roll. " + lastOutcome);
        else if (playerPool.CanMove) SetState(TurnState.PlayerInput, "Select a saved result and P1-P4, then Move. " + Score());
        else Fail("Player phase has no available action.");
    }

    private IEnumerator OpponentTurn()
    {
        opponentPool.BeginTurn();
        showingPlayerPool = false;
        selectedIndex = -1;
        selectedResultId = -1;
        SetState(TurnState.OpponentThinking, "Opponent turn.");
        cameraDirector.ShowDefault();
        yield return WaitForCamera();
        while (!opponentPool.IsComplete)
        {
            SetState(TurnState.OpponentThinking, "Opponent thinking... " + Score());
            yield return new WaitForSeconds(opponentThinkSeconds);
            if (opponentPool.CanRoll)
            {
                var roll = YutThrow.Roll(opponentRollRandom);
                SetState(TurnState.Rolling, "Opponent rolling...");
                cameraDirector.ShowThrow();
                yield return WaitForCamera();
                yield return throwPresenter.PlayThrow(roll.FaceMask, true);
                if (throwPresenter.LastCompletedMask != roll.FaceMask)
                { Fail("Opponent throw animation was interrupted."); yield break; }
                if (!opponentPool.RecordRoll(roll.Steps)) { Fail("Opponent roll rejected."); yield break; }
                SetState(TurnState.OpponentThinking, "Opponent rolled " + YutThrow.Name(roll.Steps));
                yield return new WaitForSeconds(resultHoldSeconds);
                continue;
            }
            if (!opponentPool.CanMove) { Fail("Opponent has a pending action."); yield break; }
            cameraDirector.ShowDefault();
            yield return WaitForCamera();
            List<int> candidates = GroupRepresentatives(opponentPieces);
            if (candidates.Count == 0) { Fail("No opponent piece available."); yield break; }
            int index = candidates[opponentDecisionRandom.Next(candidates.Count)];
            var result = opponentPool.Results[opponentDecisionRandom.Next(opponentPool.Results.Count)];
            bool shortcut = opponentPieces[index].HasRouteChoice;
            SetState(TurnState.OpponentMoving, $"Opponent P{index + 1}: {YutThrow.Name(result.Steps)}");
            var group = StartGroupMove(opponentPieces, index, opponentPool, result.Id, shortcut);
            if (group == null) yield break;
            RefreshUI();
            while (AnyMoving(group)) yield return null;
            int captured = ResolveLanding(opponentPieces, playerPieces, index, "Opponent");
            if (!opponentPool.CompleteMove(captured, allowCaptureBonusAfterYutMo)) { Fail("Missing pending opponent move."); yield break; }
            if (captured > 0) lastOutcome += opponentPool.PendingRolls > 0 ? " Roll again." : " No second Yut/Mo bonus.";
            if (CheckVictory(opponentPieces, false)) yield break;
            SetState(TurnState.OpponentMoving, lastOutcome + " " + Score());
            yield return new WaitForSeconds(resultHoldSeconds);
        }
        yield return BeginPlayerTurn();
    }

    private List<YutPieceMovement> StartGroupMove(YutPieceMovement[] team, int index,
        YutTurnPool pool, int resultId, bool shortcut)
    {
        var result = pool.Find(resultId);
        if (result == null || !pool.CanMove || index < 0 || index >= team.Length)
        { Fail("Result or piece is unavailable."); return null; }
        var group = new List<YutPieceMovement>();
        foreach (int member in YutGroupRules.Members(Nodes(team), index)) group.Add(team[member]);
        if (group.Count == 0) { Fail("Finished pieces cannot move."); return null; }
        foreach (var piece in group)
            if (!piece.IsReady || !piece.isActiveAndEnabled || piece.IsMoving || piece.IsFinished || (shortcut && !piece.HasRouteChoice))
            { Fail("Invalid group move."); return null; }
        YutSavedResult consumed;
        if (!pool.TryUse(resultId, out consumed)) { Fail("Result already used."); return null; }
        foreach (var piece in group)
            if (!piece.TryMoveBySteps(consumed.Steps, shortcut))
            { Fail("Unexpected group move failure."); return null; }
        return group;
    }
    private int ResolveLanding(YutPieceMovement[] allies, YutPieceMovement[] enemies, int selected, string actor)
    {
        int destination = allies[selected].CurrentNodeId;
        List<int> captured = YutGroupRules.Captured(Nodes(enemies), destination);
        foreach (int index in captured) enemies[index].ResetPiece();
        RefreshStacks();
        int size = YutGroupRules.Members(Nodes(allies), selected).Count;
        lastOutcome = destination == YutRouteRules.Finished ? actor + " finished a group." :
            actor + " arrived at " + allies[selected].CurrentNodeName + $" (group {size}).";
        if (captured.Count > 0) lastOutcome += $" Captured {captured.Count}!";
        Debug.Log(lastOutcome, this);
        return captured.Count;
    }
    private bool CheckVictory(YutPieceMovement[] team, bool player)
    {
        if (!YutGroupRules.AllFinished(Nodes(team))) return false;
        SetState(TurnState.GameOver, (player ? "You win! " : "Opponent wins! ") + Score() + " Press Reset.");
        return true;
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
        if (throwPresenter != null) throwPresenter.ResetPresentation();
        selectedIndex = -1;
        selectedResultId = -1;
        lastOutcome = "";
        playerPool.Clear();
        opponentPool.Clear();
        testPlayerRolls.Clear();
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
        RefreshUI();
    }
    private void RefreshUI()
    {
        bool input = ready && currentState == TurnState.PlayerInput && playerPool.CanMove;
        if (rollButton != null) rollButton.interactable = ready && currentState == TurnState.AwaitingRoll && playerPool.CanRoll;
        var selected = playerPool.Find(selectedResultId);
        if (confirmMoveButton != null) confirmMoveButton.interactable = input && selectedIndex >= 0
            && selected != null && !playerPieces[selectedIndex].IsFinished;
        if (moveButtons != null)
            for (int i = 0; i < moveButtons.Length; i++)
            {
                if (moveButtons[i] != null) moveButtons[i].interactable = input && playerPool.Count(i + 1) > 0;
                if (i < resultLabels.Length && resultLabels[i] != null)
                    resultLabels[i].text = (selected != null && selected.Steps == i + 1 ? "* " : "")
                        + YutThrow.Name(i + 1) + " x" + playerPool.Count(i + 1);
            }
        if (pieceButtons != null)
            for (int i = 0; i < pieceButtons.Length; i++)
                if (pieceButtons[i] != null) pieceButtons[i].interactable = input && i < playerPieces.Length
                    && playerPieces[i] != null && !playerPieces[i].IsFinished;
        bool route = ready && currentState == TurnState.ChoosingRoute;
        if (outerRouteButton != null) outerRouteButton.interactable = route;
        if (shortcutRouteButton != null) shortcutRouteButton.interactable = route;
        if (resultText != null)
        {
            YutTurnPool pool = showingPlayerPool ? playerPool : opponentPool;
            var text = new StringBuilder(showingPlayerPool ? "Player" : "Opponent");
            text.Append(" | Rolls left: ").Append(pool.PendingRolls).Append("\nSaved: ");
            bool any = false;
            for (int steps = 1; steps <= 5; steps++)
            {
                int count = pool.Count(steps);
                if (count == 0) continue;
                if (any) text.Append(" / ");
                text.Append(YutThrow.Name(steps)).Append(" x").Append(count);
                any = true;
            }
            if (!any) text.Append("None");
            if (skipOpponentForRouteTests || testPlayerRolls.Count > 0) text.Append("\n[TEST MODE]");
            resultText.text = text.ToString();
        }
    }
    private void Fail(string reason)
    {
        SetState(TurnState.SetupError, "Setup error - check Console.");
        Debug.LogError("YutTurnManager: " + reason, this);
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        if (ready) ResetAllPieces();
        SetState(TurnState.Preparing, "Paused - enable and press Reset.");
    }

#if UNITY_EDITOR
    private bool PrepareTest(bool skipOpponent, params int[] forcedRolls)
    {
        if (!Application.isPlaying || !ready || !isActiveAndEnabled)
        { Debug.LogWarning("Enter Play mode with all fields assigned first.", this); return false; }
        StopAllCoroutines();
        ResetAllPieces();
        skipOpponentForRouteTests = skipOpponent;
        allowCaptureBonusAfterYutMo = false;
        foreach (int steps in forcedRolls) testPlayerRolls.Enqueue(steps);
        return true;
    }
    [ContextMenu("Tests/Visual Do")]
    private void VisualDo() { if (PrepareTest(true, 1)) StartCoroutine(BeginPlayerTurn()); }
    [ContextMenu("Tests/Visual Gae")]
    private void VisualGae() { if (PrepareTest(true, 2)) StartCoroutine(BeginPlayerTurn()); }
    [ContextMenu("Tests/Visual Geol")]
    private void VisualGeol() { if (PrepareTest(true, 3)) StartCoroutine(BeginPlayerTurn()); }
    [ContextMenu("Tests/Visual Yut")]
    private void VisualYut() { if (PrepareTest(true, 4, 1)) StartCoroutine(BeginPlayerTurn()); }
    [ContextMenu("Tests/Visual Mo")]
    private void VisualMo() { if (PrepareTest(true, 5, 1)) StartCoroutine(BeginPlayerTurn()); }
    [ContextMenu("Tests/Load Yut Then Gae")]
    private void LoadYutThenGae()
    {
        if (!PrepareTest(true, 4, 2)) return;
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Load Double Yut")]
    private void LoadDoubleYut()
    {
        if (!PrepareTest(true, 4, 4, 1)) return;
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Load Capture With Saved Result")]
    private void LoadCaptureWithSavedResult()
    {
        if (!PrepareTest(true, 4, 2, 1)) return;
        playerPieces[0].PlaceForTesting(3);
        opponentPieces[0].PlaceForTesting(5);
        opponentPieces[1].PlaceForTesting(5);
        RefreshStacks();
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Load Yut Capture No Double Bonus")]
    private void LoadYutCapture()
    {
        if (!PrepareTest(true, 4, 1)) return;
        playerPieces[0].PlaceForTesting(1);
        opponentPieces[0].PlaceForTesting(5);
        RefreshStacks();
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Load Stack Test")]
    private void LoadStackTest()
    {
        if (!PrepareTest(true, 2, 1)) return;
        playerPieces[0].PlaceForTesting(3);
        playerPieces[1].PlaceForTesting(5);
        RefreshStacks();
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Load Capture Test")]
    private void LoadCaptureTest()
    {
        if (!PrepareTest(false, 2, 1)) return;
        playerPieces[0].PlaceForTesting(3);
        opponentPieces[0].PlaceForTesting(5);
        opponentPieces[1].PlaceForTesting(5);
        RefreshStacks();
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Load Finish Test")]
    private void LoadFinishTest()
    {
        if (!PrepareTest(true, 1)) return;
        playerPieces[0].PlaceForTesting(30);
        playerPieces[1].PlaceForTesting(30);
        playerPieces[2].PlaceForTesting(20);
        playerPieces[3].PlaceForTesting(20);
        RefreshStacks();
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Load Win With Unused Result")]
    private void LoadWinWithUnusedResult()
    {
        if (!PrepareTest(true, 4, 1)) return;
        playerPieces[0].PlaceForTesting(30);
        playerPieces[1].PlaceForTesting(30);
        playerPieces[2].PlaceForTesting(30);
        playerPieces[3].PlaceForTesting(20);
        RefreshStacks();
        StartCoroutine(BeginPlayerTurn());
    }
#endif
}