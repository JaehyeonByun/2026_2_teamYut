using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.EventSystems;

// Phase order: resolve all earned rolls, select a saved result and piece,
// confirm movement, resolve landing, then repeat until the turn pool is empty.
public class YutTurnManager : MonoBehaviour
{
    public enum TurnState
    {
        Preparing, PlayerInput, PlayerMoving, OpponentThinking,
        OpponentMoving, GameOver, SetupError, ChoosingRoute, AwaitingRoll, Rolling, CardPlaying, ChargingThrow
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
    [Header("Soul result UI")]
    [SerializeField] private bool useSoulResultUI = true;
    [SerializeField] private bool hideLegacyResultText = true;
    [SerializeField] private YutSoulResultUI soulResultUI;
    private bool legacyResultWasEnabled;
    private bool soulUIInitialized;
    [Header("Hold to throw")]
    [SerializeField] private bool usePowerCharge = true;
    [Header("Destination click movement")]
    [SerializeField] private bool useDestinationClicks = true;
    private YutDestinationPicker destinationPicker;
    private YutBoardPointer boardPointer;
    [Header("Player view buttons")]
    [SerializeField] private bool createPlayerViewButtons=true;
    private YutPlayerViewPanel playerViewPanel;
    [Header("Camera")]
    [SerializeField] private CameraDirector cameraDirector;
    [SerializeField] private CinemachineBrain cameraBrain;
    [Header("3D throw presentation")]
    [SerializeField] private YutThrowPresenter throwPresenter;
    [Header("Opponent reactions (optional)")]
    [SerializeField] private OpponentReactionController opponentReaction;
    [Header("Soul duel health (required)")]
    [SerializeField] private DuelHealth duelHealth;
    [Header("Legacy card UI - disabled for this demo")]
    [SerializeField] private Button healingCardButton;
    [SerializeField] private TMP_Text healingCardText;
    [Header("Scale presentation")]
    [SerializeField, Min(0f)] private float scaleLeadSeconds = 0.12f;
    [SerializeField, Min(0f)] private float scaleHoldSeconds = 2.6f;
    [SerializeField, Min(0f)] private float minimumScaleImpactViewSeconds = 2.6f;
    [SerializeField] private SoulFlameAsset.SoulFlameDuelPresenter scaleImpactPresenter;
    [Header("Timing")]
    [SerializeField, Min(0f)] private float opponentThinkSeconds = 0.8f;
    [SerializeField, Min(0f)] private float resultHoldSeconds = 0.6f;
    [Header("Throw readability")]
    [SerializeField, Min(0f)] private float throwLeadSeconds = 0.3f;
    [SerializeField, Min(0f)] private float minimumResultViewSeconds = 1.8f;
    [SerializeField, Min(0f)] private float opponentBonusLookSeconds = 1f;
    [SerializeField, Min(0f)] private float opponentTurnEndLookSeconds = 1f;
    [Header("Testing only")]
    [SerializeField] private bool skipOpponentForRouteTests;
    [SerializeField] private TurnState currentState = TurnState.Preparing;

    private readonly YutTurnPool playerPool = new YutTurnPool();
    private readonly YutTurnPool opponentPool = new YutTurnPool();
    // AI decision randomness cannot alter either side's throw sequence.
    private readonly System.Random playerRollRandom = new System.Random(System.Guid.NewGuid().GetHashCode());
    private readonly System.Random opponentRollRandom = new System.Random(System.Guid.NewGuid().GetHashCode());
    private readonly System.Random opponentDecisionRandom = new System.Random(System.Guid.NewGuid().GetHashCode());
    private readonly System.Random opponentPowerRandom = new System.Random(System.Guid.NewGuid().GetHashCode());
    private readonly Queue<int> testPlayerRolls = new Queue<int>();
    private readonly Queue<int> testOpponentRolls = new Queue<int>();
    private bool ready;
    public bool CandleResultsArePlayer => showingPlayerPool;
    public int CandleResultCount => ready && isActiveAndEnabled
        && currentState!=TurnState.SetupError && currentState!=TurnState.GameOver
        ? (showingPlayerPool?playerPool:opponentPool).Results.Count : 0;
    private bool showingPlayerPool = true;
    private int selectedIndex = -1;
    private int selectedResultId = -1;
    private string lastOutcome = "";
    private string latestRoll = "None";
    private string lastUsedResult = "None";
    private string selectionPrompt = "";
    [SerializeField, Min(0f)] private float comboStepPause = 0.16f;
    private readonly List<YutMoveOption> shownMoves = new List<YutMoveOption>();
    private readonly TMP_Text[] resultLabels = new TMP_Text[5];

    private IEnumerator Start()
    {
        SetState(TurnState.Preparing, "Preparing...");
        yield return null;
        ready = ValidateSetup();
        if (!ready) { Fail("Check CM_Throw, CM_Scale, Duel Health, pieces/UI/cameras and Throw Presenter (including its four sticks and landing points)."); yield break; }
        if (moveButtons != null)
            for (int i = 0; i < Mathf.Min(5, moveButtons.Length); i++)
                if (moveButtons[i] != null) resultLabels[i] = moveButtons[i].GetComponentInChildren<TMP_Text>(true);
        if (useDestinationClicks)
        {
            destinationPicker = GetComponent<YutDestinationPicker>();
            if (destinationPicker == null) destinationPicker = gameObject.AddComponent<YutDestinationPicker>();
            if (EventSystem.current == null || !destinationPicker.isActiveAndEnabled
                || !destinationPicker.Initialize(cameraBrain.GetComponent<Camera>()))
            { ready = false; Fail("Check EventSystem, Main Camera and Resources/YutInteraction/WhitePulse shader."); yield break; }
            boardPointer = GetComponent<YutBoardPointer>();
            if (boardPointer == null) boardPointer = gameObject.AddComponent<YutBoardPointer>();
            if (!boardPointer.isActiveAndEnabled || !boardPointer.Initialize(this, cameraBrain.GetComponent<Camera>(), playerPieces, opponentPieces, destinationPicker))
            { ready = false; Fail("Check piece meshes and Resources/YutInteraction/PieceOutline shader."); yield break; }
        }
        cameraDirector.BindPlayerViewGuard(()=>CanInspectPlayerView);
        if(createPlayerViewButtons)
        {
            playerViewPanel=GetComponent<YutPlayerViewPanel>();
            if(playerViewPanel==null)playerViewPanel=gameObject.AddComponent<YutPlayerViewPanel>();
            playerViewPanel.Initialize(this,resultText!=null?resultText.font:null);
        }
        if(usePowerCharge)
        {
            var powerButton=rollButton.GetComponent<YutPowerThrowButton>();
            if(powerButton==null) powerButton=rollButton.gameObject.AddComponent<YutPowerThrowButton>();
            if(!powerButton.isActiveAndEnabled || !powerButton.Initialize(this,rollButton,resultText.font))
            { ready=false;Fail("Power throw needs an enabled YutPowerThrowButton on the Roll UI button.");yield break; }
        }
        if(useSoulResultUI)
        {
            if(!soulResultUI) soulResultUI=GetComponent<YutSoulResultUI>();
            if(!soulResultUI) soulResultUI=gameObject.AddComponent<YutSoulResultUI>();
            legacyResultWasEnabled=resultText.enabled;
            soulUIInitialized=soulResultUI.isActiveAndEnabled && soulResultUI.Initialize(cameraBrain.GetComponent<Camera>(),resultText.font);
        }
        RestartMatch();
    }
    private bool ValidateSetup()
    {
        if (turnText == null || resultText == null || turnText == resultText
            || duelHealth == null || !duelHealth.isActiveAndEnabled
            || cameraDirector == null || !cameraDirector.HasScaleCamera || !cameraDirector.HasThrowCamera || cameraBrain == null
            || throwPresenter == null || !throwPresenter.IsReady || !throwPresenter.isActiveAndEnabled
            || playerPieces == null || playerPieces.Length != 4
            || opponentPieces == null || opponentPieces.Length != 4
            || (!useDestinationClicks && (moveButtons == null || moveButtons.Length != 5))
            || (!useDestinationClicks && (pieceButtons == null || pieceButtons.Length != 4))) return false;
        var pieces = new HashSet<YutPieceMovement>();
        foreach (var team in new[] {playerPieces, opponentPieces})
            foreach (var piece in team)
                if (piece == null || !piece.IsReady || !piece.isActiveAndEnabled || !pieces.Add(piece)) return false;
        var buttons = new HashSet<Button>();
        foreach (var set in new[] {useDestinationClicks ? new Button[0] : moveButtons, useDestinationClicks ? new Button[0] : pieceButtons,
            useDestinationClicks ? new[] {rollButton} : new[] {outerRouteButton, shortcutRouteButton, rollButton, confirmMoveButton}})
            foreach (var button in set)
                if (button == null || !buttons.Add(button)) return false;
        if (healingCardButton != null && buttons.Contains(healingCardButton)) return false;
        return true;
    }
    private bool CanSelect() => ready && isActiveAndEnabled && cameraDirector!=null && !cameraDirector.IsBlending
        && currentState == TurnState.PlayerInput && playerPool.CanMove;

    public bool BoardPointerEnabled => useDestinationClicks && CanSelect();
    public bool CanPointerSelectPiece(int index)
    { return BoardPointerEnabled && index >= 0 && index < playerPieces.Length && PieceAvailable(playerPieces,index); }
    public bool SamePlayerGroup(int a, int b)
    { return YutSelectionRules.SameGroup(Nodes(playerPieces),a,b); }
    public bool IsSelectedPlayerGroup(int index)
    { return BoardPointerEnabled && selectedIndex >= 0 && SamePlayerGroup(index,selectedIndex); }

    public bool IsCapturableOpponent(int index)
    {
        return BoardPointerEnabled && selectedIndex>=0 && index>=0 && index<opponentPieces.Length
            && YutMoveOptions.CanCapture(shownMoves,opponentPieces[index].CurrentNodeId);
    }

    public bool IsStackablePlayer(int index)
    {
        return BoardPointerEnabled && selectedIndex>=0 && index>=0 && index<playerPieces.Length
            && !IsSelectedPlayerGroup(index) && PieceAvailable(playerPieces,index)
            && YutMoveOptions.CanLandOn(shownMoves,playerPieces[index].CurrentNodeId);
    }
    public void PreviewMove(YutMoveOption move)
    {
        if(!BoardPointerEnabled || turnText==null) return;
        turnText.text=selectionPrompt+(move==null?"":"\n"+move.Describe());
    }

    public bool CanInspectPlayerView => ready && isActiveAndEnabled
        && (currentState==TurnState.AwaitingRoll || currentState==TurnState.PlayerInput);
    public bool WaitingForPlayerRoll => ready && currentState==TurnState.AwaitingRoll && playerPool.CanRoll;
    public CameraDirector PlayerCameraDirector => cameraDirector;
    public bool CanThrowFromCurrentView => ready && isActiveAndEnabled && playerPool.CanRoll
        && (currentState==TurnState.AwaitingRoll || currentState==TurnState.ChargingThrow)
        && cameraDirector!=null && cameraDirector.IsThrowViewReady;
    public bool TryChoosePlayerView(CameraDirector.PlayerView view)
    {
        if(!CanInspectPlayerView || cameraDirector==null)return false;
        bool changed=cameraDirector.TryChoosePlayerView(view);
        if(changed)RefreshUI();return changed;
    }
    private void Update()
    {
        // Refresh when a Cinemachine blend completes, without rebuilding move highlights.
        if(rollButton!=null)rollButton.interactable=CanThrowFromCurrentView;
    }
    public bool IsPowerCharging => ready && isActiveAndEnabled && currentState==TurnState.ChargingThrow;
    public bool TryBeginPowerCharge()
    {
        if(!usePowerCharge || currentState!=TurnState.AwaitingRoll || !CanThrowFromCurrentView) return false;
        SetState(TurnState.ChargingThrow,"Hold to charge. Release on the button to throw; move outside to cancel.");
        return true;
    }
    public void CancelPowerCharge()
    {
        if(!IsPowerCharging) return;
        SetState(TurnState.AwaitingRoll,"Charge cancelled. Hold Roll to try again.");
    }
    public void ReleasePowerCharge(float power)
    {
        if(!IsPowerCharging) return;
        if(!CanThrowFromCurrentView) { CancelPowerCharge();return; }
        BeginPlayerRoll(power);
    }
    public void PlayerRoll()
    {
        // Preserve the old Button.OnClick binding without a second throw on pointer-up.
        if(usePowerCharge || currentState!=TurnState.AwaitingRoll || !CanThrowFromCurrentView) return;
        BeginPlayerRoll(0.5f);
    }
    private void BeginPlayerRoll(float power)
    {
        if(!CanThrowFromCurrentView)return;
        power=float.IsNaN(power)||float.IsInfinity(power)?0.5f:Mathf.Clamp01(power);
        SetState(TurnState.Rolling,"Rolling at "+Mathf.RoundToInt(power*100f)+"% power...");
        StartCoroutine(RollForPlayer(power));
    }
    private IEnumerator RollForPlayer(float power)
    {
        cameraDirector.PrepareThrowView(throwPresenter.ThrowViewBounds,throwPresenter.PlayerSide);
        cameraDirector.BeginThrowFocus();
        yield return WaitForCamera();
        yield return new WaitForSeconds(throwLeadSeconds);
        bool forced = testPlayerRolls.Count > 0;
        if (forced)
        {
            int forcedSteps = testPlayerRolls.Dequeue();
            yield return throwPresenter.PlayThrow(forcedSteps == 5 ? 0 : (1 << forcedSteps) - 1, false, power);
        }
        else yield return throwPresenter.PlayPhysicalThrow(false, power);
        if (throwPresenter.LastCompletedMask < 0)
        { Fail("Physical throw failed: " + throwPresenter.LastError); yield break; }
        YutThrow generated = YutThrow.FromMask(throwPresenter.LastCompletedMask);
        int steps = generated.Steps;
        if (!playerPool.RecordRoll(steps)) { Fail("Roll had no matching roll credit."); yield break; }
        latestRoll = "Player: " + YutThrow.Name(steps) + " (" + steps + " steps) | Power " + Mathf.RoundToInt(power*100f) + "%";
        lastOutcome = (forced ? "[TEST] " : "") + "Player rolled " + YutThrow.Name(steps) + ".";
        if (!forced) Debug.Log($"Player face mask: {generated.FaceMask}, result: {steps}", this);
        SetState(TurnState.Rolling, lastOutcome);
        yield return PresentSoulResult(playerPool,true);
        yield return throwPresenter.DissolvePresentation();
        cameraDirector.EndThrowFocus();
        yield return PreparePlayerPhase();
    }
    private IEnumerator PresentSoulResult(YutTurnPool pool,bool player)
    {
        float startTime=Time.time;
        if(useSoulResultUI && soulUIInitialized && soulResultUI && soulResultUI.isActiveAndEnabled && pool.Results.Count>0)
        {
            var saved=pool.Results[pool.Results.Count-1];
            Transform centre=playerPieces[0].GetBoardPoint(25);
            Vector3 point=centre ? centre.position+Vector3.up*.08f : Vector3.zero;
            yield return soulResultUI.Present(saved.Id,saved.Steps,player,point);
        }
        float remaining=Mathf.Max(resultHoldSeconds,minimumResultViewSeconds)-(Time.time-startTime);
        if(remaining>0) yield return new WaitForSeconds(remaining);
    }
    private void SyncSoulResultUI()
    {
        if(!soulUIInitialized || !soulResultUI) return;
        bool active=useSoulResultUI && soulResultUI.isActiveAndEnabled;
        if(resultText) resultText.enabled=active && hideLegacyResultText ? false : legacyResultWasEnabled;
        if(!active || currentState==TurnState.SetupError) {soulResultUI.ClearAll();return;}
        soulResultUI.SyncResults(showingPlayerPool,(showingPlayerPool ? playerPool : opponentPool).Results);
    }
    public void SelectPlayerPiece(int index)
    {
        if (!CanSelect() || index < 0 || index >= 4 || !PieceAvailable(playerPieces, index)) return;
        selectedIndex = useDestinationClicks ? YutSelectionRules.Toggle(Nodes(playerPieces), selectedIndex, index) : index;
        ShowSelection();
    }
    // Keeps existing On Click bindings. This now SELECTS a result; it cannot manufacture one.
    public void PlayerMove(int steps) { SelectResult(steps); }
    public void SelectResult(int steps)
    {
        if (useDestinationClicks || !CanSelect()) return;
        YutSavedResult result = playerPool.First(steps);
        if (result == null) return;
        selectedResultId = result.Id;
        ShowSelection();
    }
    private void ShowSelection()
    {
        var result = playerPool.Find(selectedResultId);
        string piece = selectedIndex < 0 ? (useDestinationClicks ? "Click a player piece" : "Select P1-P4") :
            $"P{selectedIndex + 1} [{playerPieces[selectedIndex].CurrentNodeName}], group {YutGroupRules.Members(Nodes(playerPieces), selectedIndex).Count}";
        string value = useDestinationClicks ? "all saved results" : (result == null ? "select a result" : YutThrow.Name(result.Steps));
        SetState(TurnState.PlayerInput, piece + " / " + value + (useDestinationClicks ? ". Click a glowing circle; click this piece again to cancel. " : ". Then press Move. ") + Score());
    }
    public void ConfirmMove()
    {
        if (useDestinationClicks) { if (CanSelect()) ShowSelection(); return; }
        if (!CanSelect() || selectedIndex < 0 || !PieceAvailable(playerPieces, selectedIndex)) return;
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
        if (useDestinationClicks || !ready || !isActiveAndEnabled || currentState != TurnState.ChoosingRoute) return;
        StartPlayerMove(shortcut);
    }
    private void StartPlayerMove(bool shortcut)
    {
        SetState(TurnState.PlayerMoving, "Player group moving...");
        StartCoroutine(PlayPlayerAction(selectedIndex, selectedResultId, shortcut));
    }
    private IEnumerator PlayPlayerAction(int index, int resultId, bool shortcut)
    {
        var result=playerPool.Find(resultId);
        if(result==null) { Fail("Missing move result."); yield break; }
        int start=playerPieces[index].CurrentNodeId;
        var path=YutRouteRules.BuildMove(start,result.Steps,shortcut);
        if(path.Count==0) { Fail("No move path."); yield break; }
        yield return PlayPlayerPlan(index,new YutMoveOption(new YutMoveLeg(start,path[path.Count-1],shortcut,result.Id,result.Steps)));
    }
    private IEnumerator PlayPlayerPlan(int index,YutMoveOption plan)
    {
        for(int step=0;step<plan.Legs.Count;step++)
        {
            var leg=plan.Legs[step];
            var result=playerPool.Find(leg.ResultId);
            if(!playerPool.CanMove || !PieceAvailable(playerPieces,index)
                || playerPieces[index].CurrentNodeId!=leg.StartNode || result==null || result.Steps!=leg.Steps)
            { Fail("Combined move no longer matches the board/result pool."); yield break; }
            // Recompute group after every landing: newly stacked allies join the next move.
            var group=StartGroupMove(playerPieces,index,playerPool,leg.ResultId,leg.UseShortcut);
            if(group==null) yield break;
            RefreshUI();
            while(AnyMoving(group)) yield return null;
            if(playerPieces[index].CurrentNodeId!=leg.NodeId)
            { Fail("Combined move arrived at an unexpected node."); yield break; }
            bool finishing=playerPieces[index].IsFinished;
            if(finishing) yield return FocusScaleForAction();
            int captured=ResolveLanding(playerPieces,opponentPieces,index,"Player",group);
            while (CaptureEffectsRunning()) yield return null;
            if(!playerPool.CompleteMove(captured)) { Fail("Missing pending player move."); yield break; }
            if(captured>0) lastOutcome+=playerPool.PendingRolls>0?" Roll again.":" Turn bonus already used: no extra roll.";
            if(CheckDuelVictory()) yield break;
            DiscardIfAllResting(playerPieces,playerPool);
            SetState(TurnState.PlayerMoving,lastOutcome+" "+Score());
            bool more=step+1<plan.Legs.Count;
            if(finishing) yield return WaitForScaleImpact();
            else yield return new WaitForSeconds(more?comboStepPause:resultHoldSeconds);
            if(finishing) cameraDirector.EndScaleFocus();
            // Never force a second leg after a finish or a required bonus-roll phase.
            if(finishing || playerPool.CanRoll || playerPool.IsComplete) break;
        }
        if(playerPool.IsComplete)
        {
            if(skipOpponentForRouteTests) yield return BeginPlayerTurn();
            else yield return OpponentTurn();
        }
        else yield return PreparePlayerPhase();
    }
    private IEnumerator BeginPlayerTurn()
    {
        duelHealth.BeginTurn(true);
        playerPool.BeginTurn();
        yield return PreparePlayerPhase();
    }
    private IEnumerator PreparePlayerPhase()
    {
        showingPlayerPool = true;
        selectedIndex = -1;
        selectedResultId = -1;
        SetState(TurnState.Preparing, "Your turn - camera moving...");
        cameraDirector.EndThrowFocus();
        if (playerPool.CanRoll)
        {
            cameraDirector.PrepareThrowView(throwPresenter.ThrowViewBounds,throwPresenter.PlayerSide);
            cameraDirector.ShowThrow();
        }
        else cameraDirector.ShowBoard();
        yield return WaitForCamera();
        if (playerPool.CanRoll) SetState(TurnState.AwaitingRoll, (usePowerCharge?"Hold Roll and release to throw. ":"Press Roll. ") + lastOutcome);
        else if (playerPool.CanMove) SetState(TurnState.PlayerInput, (useDestinationClicks ? "Click a player piece to see all destinations. Click the same piece again to cancel. " : "Select a saved result and P1-P4, then Move. ") + Score());
        else Fail("Player phase has no available action.");
    }

    private IEnumerator OpponentTurn()
    {
        duelHealth.BeginTurn(false);
        opponentPool.BeginTurn();
        showingPlayerPool = false;
        selectedIndex = -1;
        selectedResultId = -1;
        SetState(TurnState.OpponentThinking, "Opponent turn.");
        cameraDirector.ShowDefault();
        yield return WaitForCamera();
        bool bonusThrowPrepared=false;
        while (!opponentPool.IsComplete)
        {
            SetState(TurnState.OpponentThinking, "Opponent thinking... " + Score());
            if(!bonusThrowPrepared) yield return new WaitForSeconds(opponentThinkSeconds);
            if (opponentPool.CanRoll)
            {
                bool forced = testOpponentRolls.Count > 0;
                int forcedMask = -1;
                if (forced)
                {
                    int steps = testOpponentRolls.Dequeue();
                    forcedMask = steps == 5 ? 0 : (1 << steps) - 1;
                }
                SetState(TurnState.Rolling, "Opponent rolling...");
                cameraDirector.PrepareThrowView(throwPresenter.ThrowViewBounds,throwPresenter.PlayerSide);
                cameraDirector.BeginThrowFocus();
                bonusThrowPrepared=false;
                yield return WaitForCamera();
                yield return new WaitForSeconds(throwLeadSeconds);
                float power = 0.35f + (float)opponentPowerRandom.NextDouble()*0.5f;
                if (forced) yield return throwPresenter.PlayThrow(forcedMask, true, power);
                else yield return throwPresenter.PlayPhysicalThrow(true, power);
                if (throwPresenter.LastCompletedMask < 0)
                { Fail("Opponent physical throw failed: " + throwPresenter.LastError); yield break; }
                YutThrow roll = YutThrow.FromMask(throwPresenter.LastCompletedMask);
                if (!opponentPool.RecordRoll(roll.Steps)) { Fail("Opponent roll rejected."); yield break; }
                latestRoll = "Opponent: " + YutThrow.Name(roll.Steps) + " (" + roll.Steps + " steps)";
                SetState(TurnState.OpponentThinking, "Opponent rolled " + YutThrow.Name(roll.Steps));
                if (roll.Steps >= 4 && opponentReaction != null) opponentReaction.OnGoodRoll();
                yield return PresentSoulResult(opponentPool,false);
                yield return throwPresenter.DissolvePresentation();
                cameraDirector.EndThrowFocus();
                // Only a Yut/Mo which actually earned the remaining throw gets this cutaway.
                if(roll.Steps>=4 && opponentPool.CanRoll)
                {
                    cameraDirector.BeginOpponentReactionFocus();
                    yield return WaitForCamera();
                    yield return new WaitForSeconds(opponentBonusLookSeconds);
                    cameraDirector.EndThrowFocus();
                    cameraDirector.BeginThrowFocus();
                    yield return WaitForCamera();
                    bonusThrowPrepared=true;
                }
                continue;
            }
            if (!opponentPool.CanMove) { Fail("Opponent has a pending action."); yield break; }
            cameraDirector.ShowBoard();
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
            bool finishing = opponentPieces[index].IsFinished;
            if (finishing) yield return FocusScaleForAction();
            int captured = ResolveLanding(opponentPieces, playerPieces, index, "Opponent", group);
            while (CaptureEffectsRunning()) yield return null;
            if (!opponentPool.CompleteMove(captured)) { Fail("Missing pending opponent move."); yield break; }
            if (captured > 0) lastOutcome += opponentPool.PendingRolls > 0 ? " Roll again." : " Turn bonus already used: no extra roll.";
            if (CheckDuelVictory()) yield break;
            DiscardIfAllResting(opponentPieces, opponentPool);
            SetState(TurnState.OpponentMoving, lastOutcome + " " + Score());
            if(finishing) yield return WaitForScaleImpact();
            else yield return new WaitForSeconds(resultHoldSeconds);
            if (finishing) cameraDirector.EndScaleFocus();
        }
        SetState(TurnState.Preparing, "Opponent turn complete.");
        cameraDirector.BeginOpponentReactionFocus();
        yield return WaitForCamera();
        yield return new WaitForSeconds(opponentTurnEndLookSeconds);
        cameraDirector.EndThrowFocus();
        yield return BeginPlayerTurn();
    }

    private List<YutPieceMovement> StartGroupMove(YutPieceMovement[] team, int index,
        YutTurnPool pool, int resultId, bool shortcut)
    {
        var result = pool.Find(resultId);
        if (result == null || !pool.CanMove || index < 0 || index >= team.Length || !PieceAvailable(team, index))
        { Fail("Result or piece is unavailable."); return null; }
        var group = new List<YutPieceMovement>();
        foreach (int member in YutGroupRules.Members(Nodes(team), index)) group.Add(team[member]);
        if (group.Count == 0) { Fail("Finished pieces cannot move."); return null; }
        foreach (var piece in group)
            if (!piece.IsReady || !piece.isActiveAndEnabled || piece.IsMoving || piece.IsFinished || (shortcut && !piece.HasRouteChoice))
            { Fail("Invalid group move."); return null; }
        YutSavedResult consumed;
        if (!pool.TryUse(resultId, out consumed)) { Fail("Result already used."); return null; }
        lastUsedResult = (team == playerPieces ? "Player: " : "Opponent: ") + YutThrow.Name(consumed.Steps);
        foreach (var piece in group)
            if (!piece.TryMoveBySteps(consumed.Steps, shortcut))
            { Fail("Unexpected group move failure."); return null; }
        return group;
    }
    private int ResolveLanding(YutPieceMovement[] allies, YutPieceMovement[] enemies, int selected,
        string actor, List<YutPieceMovement> movingGroup)
    {
        if (allies[selected].IsFinished)
        {
            // The moving group was captured BEFORE movement. Finished-node grouping returns empty,
            // and counting every finished piece would repeatedly damage for old completions.
            var members = new List<int>();
            foreach (var piece in movingGroup) members.Add(System.Array.IndexOf(allies, piece));
            bool player = allies == playerPieces;
            int damage = duelHealth.ResolveFinish(player, members);
            foreach (var piece in movingGroup) piece.ResetPiece();
            RefreshStacks();
            lastOutcome = $"{actor} finished {movingGroup.Count} piece(s): {damage} damage. Rest until next own turn.";
            if (opponentReaction != null && !duelHealth.IsOver)
            {
                if (player) opponentReaction.OnPiecesLost();
                else opponentReaction.OnCapture();
            }
            Debug.Log(lastOutcome + " " + Score(), this);
            return 0;
        }
        int destination = allies[selected].CurrentNodeId;
        List<int> captured = YutGroupRules.Captured(Nodes(enemies), destination);
        foreach (int index in captured) enemies[index].BeginCaptureReturn();
        RefreshStacks();
        int size = YutGroupRules.Members(Nodes(allies), selected).Count;
        lastOutcome = destination == YutRouteRules.Finished ? actor + " finished a group." :
            actor + " arrived at " + allies[selected].CurrentNodeName + $" (group {size}).";
        if (captured.Count > 0)
        {
            lastOutcome += $" Captured {captured.Count}!";
            if (opponentReaction != null)
            {
                if (allies == playerPieces) opponentReaction.OnPiecesLost();
                else opponentReaction.OnCapture();
            }
        }
        Debug.Log(lastOutcome, this);
        return captured.Count;
    }
    private bool CaptureEffectsRunning()
    {
        foreach(var piece in playerPieces) if(piece && piece.IsMoving) return true;
        foreach(var piece in opponentPieces) if(piece && piece.IsMoving) return true;
        return false;
    }
    private bool CheckDuelVictory()
    {
        if (!duelHealth.IsOver) return false;
        bool playerWon = duelHealth.PlayerWon;
        playerPool.DiscardRemainingActions(); opponentPool.DiscardRemainingActions();
        selectedIndex = selectedResultId = -1;
        SetState(TurnState.GameOver, (playerWon ? "You win! " : "Opponent wins! ") + Score() + " Press Reset.");
        if (opponentReaction != null) opponentReaction.ShowMatchResult(!playerWon);
        cameraDirector.BeginScaleFocus();
        StartCoroutine(PresentDuelResult());
        return true;
    }
    private IEnumerator PresentDuelResult()
    {
        yield return WaitForCamera();
        if (currentState == TurnState.GameOver && duelHealth.IsOver) duelHealth.PublishResult();
    }
    private void DiscardIfAllResting(YutPieceMovement[] team, YutTurnPool pool)
    {
        if (GroupRepresentatives(team).Count > 0) return;
        pool.DiscardRemainingActions();
        lastOutcome += " All pieces resting: unused results discarded, turn ends.";
    }
    private bool PieceAvailable(YutPieceMovement[] team, int index)
    {
        return index >= 0 && index < team.Length && team[index] != null && !team[index].IsFinished
            && duelHealth != null && duelHealth.CanUse(team == playerPieces, index);
    }
    private IEnumerator FocusScaleForAction()
    {
        cameraDirector.BeginScaleFocus();
        yield return WaitForCamera();
        yield return new WaitForSeconds(scaleLeadSeconds);
    }
    private IEnumerator WaitForScaleImpact()
    {
        // Existing scenes retain the old 0.85 value; the new minimum extends them as well.
        float until=Time.time+Mathf.Max(scaleHoldSeconds,minimumScaleImpactViewSeconds);
        if(scaleImpactPresenter==null || !scaleImpactPresenter.UsesHealth(duelHealth))
        {
            scaleImpactPresenter=null;
            foreach(var candidate in FindObjectsOfType<SoulFlameAsset.SoulFlameDuelPresenter>())
                if(candidate.UsesHealth(duelHealth)) {scaleImpactPresenter=candidate;break;}
        }
        // Give the health-observing presenter one Update after ResolveLanding.
        yield return null;
        while(Time.time<until || (scaleImpactPresenter!=null && scaleImpactPresenter.IsPresenting))
            yield return null;
    }
    // Preserve an existing UI binding while cards are excluded from the demo.
    public void UseHealingCard()
    { Debug.Log("Cards are disabled in the two-roll demo.", this); }
    private IEnumerator WaitForCamera()
    {
        yield return null;
        while (cameraBrain.IsBlending) yield return null;
    }
    private bool demoBusy;
    public bool TryDemoScale(bool targetPlayer, bool victory, int damage = 20)
    {
        if (demoBusy || !CanInspectPlayerView || duelHealth==null || duelHealth.IsOver
            || cameraDirector==null || !cameraDirector.HasScaleCamera || cameraDirector.IsBlending)
            return false;
        demoBusy=true;
        StopAllCoroutines();
        StartCoroutine(PlayDemoScale(targetPlayer,victory,Mathf.Max(1,damage)));
        return true;
    }
    private IEnumerator PlayDemoScale(bool targetPlayer,bool victory,int damage)
    {
        TurnState previousState=currentState;
        string previousMessage=turnText!=null?turnText.text:"";
        var previousView=CameraDirector.PlayerView.Board;
        foreach(CameraDirector.PlayerView view in System.Enum.GetValues(typeof(CameraDirector.PlayerView)))
            if(cameraDirector.IsSelectedPlayerView(view))previousView=view;
        SetState(TurnState.Preparing,"Demo: scale presentation");
        yield return FocusScaleForAction();
        // Move the camera BEFORE changing HP, so the beginning of the effect is visible.
        int amount=victory ? duelHealth.OpponentHP : damage;
        duelHealth.ApplyDemoDamage(victory?false:targetPlayer,amount);
        yield return WaitForScaleImpact();
        if(duelHealth.IsOver)
        {
            CheckDuelVictory();
            demoBusy=false;
            yield break;
        }
        cameraDirector.EndScaleFocus();
        SetState(previousState,previousMessage);
        cameraDirector.TryChoosePlayerView(previousView);
        demoBusy=false;
    }
    public bool TryDemoReset()
    {
        if(demoBusy || !ready || !isActiveAndEnabled
            || !(CanInspectPlayerView || currentState==TurnState.GameOver))return false;
        RestartMatch();return true;
    }
    public void RestartMatch()
    {
        if (!ready || !isActiveAndEnabled) return;
        if (cameraDirector != null) cameraDirector.EndThrowFocus();
        StopAllCoroutines();
        demoBusy=false;
        skipOpponentForRouteTests = false;
        ResetAllPieces();
        StartCoroutine(BeginPlayerTurn());
    }
    private void ResetAllPieces()
    {
        if(soulResultUI) soulResultUI.ClearAll();
        if (cameraDirector != null) cameraDirector.EndScaleFocus();
        if (duelHealth != null) duelHealth.ResetMatch();
        if (throwPresenter != null) throwPresenter.ResetPresentation();
        if (opponentReaction != null) opponentReaction.ResetReaction();
        selectedIndex = -1;
        selectedResultId = -1;
        lastOutcome = "";
        latestRoll = "None";
        lastUsedResult = "None";
        playerPool.Clear();
        opponentPool.Clear();
        testPlayerRolls.Clear();
        testOpponentRolls.Clear();
        foreach (var piece in playerPieces) piece.ResetPiece();
        foreach (var piece in opponentPieces) piece.ResetPiece();
        RefreshStacks();
    }
    private void RefreshStacks()
    {
        foreach(var team in new[]{playerPieces,opponentPieces})
        {
            var groups=new Dictionary<int,List<YutPieceMovement>>();
            foreach(var piece in team)
            {
                // Captured members remain visually hidden until their own waiting-point respawn.
                if(piece.IsMoving)continue;
                var countLabel=piece.GetComponent<YutStackCountLabel>();
                if(countLabel==null)countLabel=piece.gameObject.AddComponent<YutStackCountLabel>();
                countLabel.SetOpponent(team==opponentPieces);
                piece.SetStackLevel(0);
                int node=piece.CurrentNodeId;
                if(!YutGroupRules.IsOnBoard(node)){piece.SetStackPresentation(true,1);continue;}
                if(!groups.ContainsKey(node))groups[node]=new List<YutPieceMovement>();
                groups[node].Add(piece);
            }
            foreach(var group in groups.Values)
                for(int i=0;i<group.Count;i++)group[i].SetStackPresentation(i==0,group.Count);
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
    private List<int> GroupRepresentatives(YutPieceMovement[] team)
    {
        var result = new List<int>();
        var seen = new HashSet<int>();
        for (int i = 0; i < team.Length; i++)
        {
            int node = team[i].CurrentNodeId;
            if (!PieceAvailable(team, i)) continue;
            if (node == YutRouteRules.Waiting || seen.Add(node)) result.Add(i);
        }
        return result;
    }
    private string Score() => duelHealth == null ? "HP unavailable" : duelHealth.Summary;

    private void SetState(TurnState state, string message)
    {
        currentState = state;
        if(state==TurnState.PlayerInput) selectionPrompt=message;
        if (opponentReaction != null) opponentReaction.SetThinking(state == TurnState.OpponentThinking);
        if (turnText != null) turnText.text = message;
        RefreshUI();
    }
    private void RefreshUI()
    {
        if (healingCardButton != null) healingCardButton.interactable = false;
        if (healingCardText != null) healingCardText.text = "Cards disabled";

        bool input = ready && currentState == TurnState.PlayerInput && playerPool.CanMove;
        if (rollButton != null) rollButton.interactable = CanThrowFromCurrentView;
        var selected = playerPool.Find(selectedResultId);
        if (confirmMoveButton != null) confirmMoveButton.interactable = !useDestinationClicks && input && selectedIndex >= 0
            && selected != null && PieceAvailable(playerPieces, selectedIndex);
        if (moveButtons != null)
            for (int i = 0; i < moveButtons.Length; i++)
            {
                if (moveButtons[i] != null) moveButtons[i].interactable = !useDestinationClicks && input && playerPool.Count(i + 1) > 0;
                if (i < resultLabels.Length && resultLabels[i] != null)
                    resultLabels[i].text = (selected != null && selected.Steps == i + 1 ? "* " : "")
                        + YutThrow.Name(i + 1) + " x" + playerPool.Count(i + 1);
            }
        if (pieceButtons != null)
            for (int i = 0; i < pieceButtons.Length; i++)
                if (pieceButtons[i] != null) pieceButtons[i].interactable = !useDestinationClicks && input && i < playerPieces.Length
                    && PieceAvailable(playerPieces, i);
        bool route = !useDestinationClicks && ready && currentState == TurnState.ChoosingRoute;
        if (outerRouteButton != null) outerRouteButton.interactable = route;
        if (shortcutRouteButton != null) shortcutRouteButton.interactable = route;
        if (resultText != null)
        {
            YutTurnPool pool = showingPlayerPool ? playerPool : opponentPool;
            var text = new StringBuilder("Last roll: " + latestRoll + "\n");
            text.Append(showingPlayerPool ? "Player" : "Opponent");
            text.Append(" | Throws: ").Append(pool.RollsTaken).Append(" / ").Append(YutTurnPool.MaxRollsPerTurn);
            text.Append(" | Ready: ").Append(pool.PendingRolls);
            text.Append("\nBonus: ").Append(pool.BonusAwarded ? "already awarded" : "Yut / Mo / Capture required");
            text.Append("\nSaved: ");
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
            text.Append("\nLast used: ").Append(lastUsedResult);
            if (duelHealth != null)
            {
                text.Append("\nResting until next own turn:");
                bool resting = false;
                for (int i = 0; i < 4; i++)
                    if (duelHealth.IsResting(showingPlayerPool, i)) { text.Append(" P").Append(i + 1); resting = true; }
                if (!resting) text.Append(" None");
            }
            if (skipOpponentForRouteTests || testPlayerRolls.Count > 0 || testOpponentRolls.Count > 0) text.Append("\n[TEST MODE]");
            resultText.text = text.ToString();
        }
        SyncSoulResultUI();
        RefreshDestinations();
        if (boardPointer != null) boardPointer.RefreshVisuals();
    }
    private void RefreshDestinations()
    {
        shownMoves.Clear();
        if (destinationPicker == null) return;
        destinationPicker.Clear();
        if (!useDestinationClicks || !CanSelect() || selectedIndex < 0 || !PieceAvailable(playerPieces, selectedIndex)) return;
        int pieceIndex = selectedIndex;
        int startNode = playerPieces[pieceIndex].CurrentNodeId;
        shownMoves.AddRange(YutMoveOptions.Build(startNode,playerPool.Results));
        destinationPicker.ShowMoves(playerPieces[pieceIndex], shownMoves,
            option => CommitDestination(pieceIndex,startNode,option));
    }
    private void CommitDestination(int pieceIndex, int startNode, YutMoveOption chosen)
    {
        if (!useDestinationClicks || !CanSelect() || pieceIndex != selectedIndex || chosen == null
            || !PieceAvailable(playerPieces, pieceIndex) || playerPieces[pieceIndex].CurrentNodeId != startNode) return;
        // Rebuild from the live pool. Old markers cannot spend an already-used result.
        foreach (var option in YutMoveOptions.Build(startNode,playerPool.Results))
            if (YutMoveOptions.Matches(option,chosen))
            {
                selectedResultId=option.Legs[0].ResultId;
                SetState(TurnState.PlayerMoving,option.Describe());
                StartCoroutine(PlayPlayerPlan(pieceIndex,option));
                return;
            }
    }
    private void Fail(string reason)
    {
        if (cameraDirector != null) cameraDirector.EndThrowFocus();
        if (throwPresenter != null) throwPresenter.ResetPresentation();
        SetState(TurnState.SetupError, "Setup error: " + reason + " (see Console)");
        Debug.LogError("YutTurnManager: " + reason, this);
    }
    private void OnDisable()
    {
        // Scene teardown may already have destroyed pieces and board transforms.
        // Do not restart gameplay, reset HP/pieces, or refresh board UI here.
        StopAllCoroutines();
        currentState = TurnState.Preparing;
        demoBusy=false;
        selectedIndex = selectedResultId = -1;
        playerPool.Clear();
        opponentPool.Clear();
        testPlayerRolls.Clear();
        testOpponentRolls.Clear();
        shownMoves.Clear();
        if (cameraDirector != null)
        {
            cameraDirector.EndThrowFocus();
            cameraDirector.EndScaleFocus();
        }
        if(soulResultUI) soulResultUI.ClearAll();
        if(soulUIInitialized && resultText) resultText.enabled=legacyResultWasEnabled;
    }

#if UNITY_EDITOR
    private bool PrepareTest(bool skipOpponent, params int[] forcedRolls)
    {
        if (!Application.isPlaying || !ready || !isActiveAndEnabled)
        { Debug.LogWarning("Enter Play mode with all fields assigned first.", this); return false; }
        if (cameraDirector != null) cameraDirector.EndThrowFocus();
        StopAllCoroutines();
        ResetAllPieces();
        skipOpponentForRouteTests = skipOpponent;
        foreach (int steps in forcedRolls) testPlayerRolls.Enqueue(steps);
        return true;
    }
    [ContextMenu("Tests/Combo/1 Mo Then Geol")]
    private void ComboMoGeol()
    { if(PrepareTest(true,5,3)) StartCoroutine(BeginPlayerTurn()); }
    [ContextMenu("Tests/Combo/2 Capture On Both Landings")]
    private void ComboCapture()
    {
        if(!PrepareTest(true,5,3)) return;
        opponentPieces[0].PlaceForTesting(5); opponentPieces[1].PlaceForTesting(8);
        RefreshStacks(); StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Combo/3 Join Ally Then Continue")]
    private void ComboStack()
    {
        if(!PrepareTest(true,5,3)) return;
        playerPieces[1].PlaceForTesting(5);
        RefreshStacks(); StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Combo/4 Stack Then Finish")]
    private void ComboFinish()
    {
        if(!PrepareTest(true,5,3)) return;
        playerPieces[0].PlaceForTesting(15); playerPieces[1].PlaceForTesting(20);
        RefreshStacks(); StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Combo/5 Double Yut")]
    private void ComboDouble()
    { if(PrepareTest(true,4,4)) StartCoroutine(BeginPlayerTurn()); }
    [ContextMenu("Tests/All Results/1 Four Destinations")]
    private void AllResultsFour()
    {
        if (!PrepareTest(true, 4, 2)) return;
        playerPieces[0].PlaceForTesting(5);
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/All Results/2 Capture Targets")]
    private void AllResultsCapture()
    {
        if (!PrepareTest(true, 4, 2)) return;
        playerPieces[0].PlaceForTesting(5);
        opponentPieces[0].PlaceForTesting(7); opponentPieces[1].PlaceForTesting(7);
        opponentPieces[2].PlaceForTesting(23); opponentPieces[3].PlaceForTesting(12);
        RefreshStacks(); StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/All Results/3 Same Finish Uses Do")]
    private void AllResultsFinish()
    {
        if (!PrepareTest(true, 4, 1)) return;
        playerPieces[0].PlaceForTesting(20);
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/All Results/4 Double Yut")]
    private void AllResultsDouble()
    {
        if (!PrepareTest(true, 4, 4)) return;
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Pointer/1 Friendly Occupied Destination")]
    private void PointerFriendly()
    {
        if (!PrepareTest(true, 2)) return;
        playerPieces[0].PlaceForTesting(3); playerPieces[1].PlaceForTesting(5);
        RefreshStacks(); StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Pointer/2 Enemy Occupied Destination")]
    private void PointerEnemy()
    {
        if (!PrepareTest(true, 2, 1)) return;
        playerPieces[0].PlaceForTesting(3); opponentPieces[0].PlaceForTesting(5);
        RefreshStacks(); StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Pointer/3 Stacked Selection")]
    private void PointerStacked()
    {
        if (!PrepareTest(true, 3)) return;
        playerPieces[0].PlaceForTesting(5); playerPieces[1].PlaceForTesting(5);
        RefreshStacks(); StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Destination/1 Node 5 Geol")]
    private void DestinationFive()
    {
        if (!PrepareTest(true, 3)) return;
        playerPieces[0].PlaceForTesting(5);
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Destination/2 Node 10 Geol")]
    private void DestinationTen()
    {
        if (!PrepareTest(true, 3)) return;
        playerPieces[0].PlaceForTesting(10);
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Destination/3 Finish From 20")]
    private void DestinationFinish()
    {
        if (!PrepareTest(true, 1)) return;
        playerPieces[0].PlaceForTesting(20);
        StartCoroutine(BeginPlayerTurn());
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
        if (!PrepareTest(true, 4, 4)) return;
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Load Capture With Saved Result")]
    private void LoadCaptureWithSavedResult()
    {
        if (!PrepareTest(true, 4, 2)) return;
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
        if (!PrepareTest(true, 2)) return;
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
    private void LoadFinishTest() { HealthStackDamage(); }
    [ContextMenu("Tests/Load Win With Unused Result")]
    private void LoadWinWithUnusedResult() { HealthPlayerWin(); }

    [ContextMenu("Tests/Turn Limit/1 Double Yut")]
    private void LimitDoubleYut()
    { if (PrepareTest(true, 4, 4)) StartCoroutine(BeginPlayerTurn()); }
    [ContextMenu("Tests/Turn Limit/2 Capture Then Mo")]
    private void LimitCaptureThenMo()
    {
        if (!PrepareTest(true, 2, 5)) return;
        playerPieces[0].PlaceForTesting(3);
        opponentPieces[0].PlaceForTesting(5);
        opponentPieces[1].PlaceForTesting(5);
        RefreshStacks(); StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Turn Limit/3 Capture After Two Rolls")]
    private void LimitCaptureAfterTwoRolls()
    {
        if (!PrepareTest(true, 4, 2)) return;
        playerPieces[0].PlaceForTesting(3);
        opponentPieces[0].PlaceForTesting(5);
        RefreshStacks(); StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Turn Limit/4 Opponent Yut Then Mo")]
    private void LimitOpponent()
    {
        if (!PrepareTest(false)) return;
        testOpponentRolls.Enqueue(4); testOpponentRolls.Enqueue(5);
        StartCoroutine(OpponentTurn());
    }
    [ContextMenu("Tests/Health/1 Single Finish")]
    private void HealthSingleFinish()
    {
        if (!PrepareTest(true, 4, 1)) return;
        playerPieces[0].PlaceForTesting(20);
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Health/2 Stack Damage")]
    private void HealthStackDamage()
    {
        if (!PrepareTest(true, 4, 1)) return;
        playerPieces[0].PlaceForTesting(20); playerPieces[1].PlaceForTesting(20);
        RefreshStacks(); StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Health/3 Player Win")]
    private void HealthPlayerWin()
    {
        if (!PrepareTest(true, 4, 1)) return;
        duelHealth.SetHealthForTesting(duelHealth.PlayerHP, 1);
        playerPieces[0].PlaceForTesting(20);
        StartCoroutine(BeginPlayerTurn());
    }
    [ContextMenu("Tests/Health/4 Opponent Win")]
    private void HealthOpponentWin()
    {
        if (!PrepareTest(false)) return;
        duelHealth.SetHealthForTesting(1, duelHealth.OpponentHP);
        foreach (var piece in opponentPieces) piece.PlaceForTesting(20);
        testOpponentRolls.Enqueue(1);
        RefreshStacks(); StartCoroutine(OpponentTurn());
    }
    [ContextMenu("Tests/Health/5 All Pieces Resting")]
    private void HealthAllResting()
    {
        if (!PrepareTest(false, 4, 1)) return;
        foreach (var piece in playerPieces) piece.PlaceForTesting(20);
        testOpponentRolls.Enqueue(1);
        RefreshStacks(); StartCoroutine(BeginPlayerTurn());
    }
#endif
}
