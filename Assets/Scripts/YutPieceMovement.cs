using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Movement presentation uses YutRouteRules; turn manager owns capture/health/turns.
[DefaultExecutionOrder(1000)]
public class YutPieceMovement : MonoBehaviour
{
    // Keep old serialized field names to preserve scene assignments.
    [SerializeField] private Transform waitingPoint;
    [SerializeField] private Transform[] route;
    [SerializeField, Min(0.01f)] private float moveSpeed = 1.2f;
    [SerializeField] private float heightOffset = 0.015f;
    [Header("New shortcut nodes (center is separate)")]
    [SerializeField] private Transform centerPoint;
    [SerializeField] private Transform[] diagonalA = new Transform[4];
    [SerializeField] private Transform[] diagonalB = new Transform[4];
    [SerializeField] private int currentNodeId = YutRouteRules.Waiting;
    private bool isMoving;
    private bool initialized;
    private float stackOffset;
    private bool stackHidden;
    private readonly Dictionary<Collider,bool> colliderStates=new Dictionary<Collider,bool>();
    private YutStackCountLabel stackLabel;
    public bool IsStackHidden => stackHidden;
    public void SetStackPresentation(bool representative,int count)
    {
        stackHidden=!representative;stackOffset=0;
        foreach(var r in GetComponentsInChildren<Renderer>(true))if(r!=null)r.forceRenderingOff=stackHidden;
        foreach(var collider in GetComponentsInChildren<Collider>(true))
        {
            if(!colliderStates.ContainsKey(collider))colliderStates[collider]=collider.enabled;
            collider.enabled=colliderStates[collider] && !stackHidden;
        }
        if(stackLabel==null){stackLabel=GetComponent<YutStackCountLabel>();if(stackLabel==null)stackLabel=gameObject.AddComponent<YutStackCountLabel>();}
        stackLabel.SetCount(representative?count:1);
    }
    private void LateUpdate()
    {
        // Includes selection/FX proxies created after initial model setup.
        if(stackHidden)foreach(var r in GetComponentsInChildren<Renderer>(true))r.forceRenderingOff=true;
    }

    public bool IsReady => initialized;
    public int CurrentNodeId => currentNodeId;
    public bool IsMoving => isMoving;
    public bool IsFinished => initialized && currentNodeId == YutRouteRules.Finished;
    public bool HasRouteChoice => initialized && !isMoving && YutRouteRules.HasRouteChoice(currentNodeId);
    public string CurrentNodeName => YutRouteRules.NodeName(currentNodeId);

    private void Start()
    {
        initialized = ValidateSetup();
        if (initialized)
        {
            YutBurnVisual.GetOrAdd(gameObject).SetOutline(false); // Pieces use burn FX only; pointer selection remains separate.
            ResetPiece();
        }
    }

    private bool ValidateSetup()
    {
        if (waitingPoint == null || centerPoint == null || route == null || route.Length != 21
            || diagonalA == null || diagonalA.Length != 4 || diagonalB == null || diagonalB.Length != 4)
            return SetupError("Assign Waiting Point, Center Point, Route(21), Diagonal A(4), Diagonal B(4).");
        var assigned = new HashSet<Transform>();
        for (int id = 0; id <= 30; id++)
        {
            Transform point = GetPoint(id);
            if (point == null) return SetupError("Missing point: " + YutRouteRules.NodeName(id));
            if (!assigned.Add(point)) return SetupError("Duplicate point: " + point.name);
        }
        return true;
    }
    private bool SetupError(string message)
    {
        Debug.LogError("YutPieceMovement: " + message, this);
        return false;
    }

    // Compatibility with the earlier prototype; direct callers take the outer route.
    public void MoveBySteps(int steps) { TryMoveBySteps(steps, false); }

    public bool TryMoveBySteps(int steps, bool useShortcut)
    {
        if (!initialized || !isActiveAndEnabled || isMoving || IsFinished || steps < 1 || steps > 5)
            return false;
        if (useShortcut && !HasRouteChoice) return false;
        List<int> plan = YutRouteRules.BuildMove(currentNodeId, steps, useShortcut);
        if (plan.Count == 0) return false;
        isMoving = true;
        // Commit the rule result once; animation only displays this plan.
        currentNodeId = plan[plan.Count - 1];
        StartCoroutine(AnimateMove(plan));
        return true;
    }

    public string PreviewDestination(int steps, bool useShortcut)
    {
        if (!initialized || steps < 1 || steps > 5 || IsFinished) return "Unavailable";
        List<int> plan = YutRouteRules.BuildMove(currentNodeId, steps, useShortcut);
        return YutRouteRules.NodeName(plan[plan.Count - 1]);
    }

    private IEnumerator AnimateMove(List<int> plan)
    {
        foreach (int node in plan)
        {
            Vector3 target = GetPoint(node).position + Vector3.up * (heightOffset + stackOffset);
            while ((transform.position - target).sqrMagnitude > 0.000001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target,
                    Mathf.Max(0.01f, moveSpeed) * Time.deltaTime);
                yield return null;
            }
            transform.position = target;
        }
        isMoving = false;
        Debug.Log(name + ": " + CurrentNodeName, this);
    }

    // Expose the already assigned board anchor for destination UI. No remapping needed.
    public Transform GetBoardPoint(int node)
    {
        if (!initialized || node < 0 || node > YutRouteRules.Finished) return null;
        return GetPoint(node);
    }

    private Transform GetPoint(int node)
    {
        if (node == 0) return waitingPoint;
        if (node >= 1 && node <= 20) return route[node - 1];
        if (node >= 21 && node <= 24) return diagonalA[node - 21];
        if (node == 25) return centerPoint;
        if (node >= 26 && node <= 29) return diagonalB[node - 26];
        return route[20];
    }

    public void BeginCaptureReturn()
    {
        if(!initialized || !isActiveAndEnabled) { ResetPiece(); return; }
        StopAllCoroutines();
        var fx=YutBurnVisual.GetOrAdd(gameObject);
        fx.Restore();
        currentNodeId=YutRouteRules.Waiting;
        stackOffset=0;
        isMoving=true;
        if(stackLabel!=null)stackLabel.SetCount(1);
        StartCoroutine(CaptureReturn(fx));
    }
    private IEnumerator CaptureReturn(YutBurnVisual fx)
    {
        yield return fx.BurnAway();
        transform.position=waitingPoint.position+Vector3.up*heightOffset;
        fx.SetProgress(1);
        SetStackPresentation(true,1);
        if(fx.IsBurning && fx.hiddenSeconds>0) yield return new WaitForSeconds(fx.hiddenSeconds);
        yield return fx.BurnIn();
        isMoving=false;
    }
    private void RestoreBurnVisual()
    {
        var fx=GetComponent<YutBurnVisual>();
        if(fx) fx.Restore();
    }

    public void ResetPiece()
    {
        if (!initialized) return;
        StopAllCoroutines();
        RestoreBurnVisual();
        SetStackPresentation(true,1);
        currentNodeId = YutRouteRules.Waiting;
        stackOffset = 0f;
        isMoving = false;
        transform.position = waitingPoint.position + Vector3.up * heightOffset;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        RestoreBurnVisual();
        isMoving = false;
        if (initialized)
            transform.position = GetPoint(currentNodeId).position + Vector3.up * (heightOffset + stackOffset);
    }

    // Cosmetic only: game rules compare node IDs, never Transform positions.
    public void SetStackLevel(int level)
    {
        stackOffset = 0f; // Stack count UI replaces vertical stacking.
        if (initialized && !isMoving)
            transform.position = GetPoint(currentNodeId).position + Vector3.up * (heightOffset + stackOffset);
    }

#if UNITY_EDITOR
    public void PlaceForTesting(int node)
    {
        if (!initialized || node < 0 || node > 30) return;
        StopAllCoroutines();
        RestoreBurnVisual();
        isMoving = false;
        SetStackPresentation(true,1);
        currentNodeId = node;
        SetStackLevel(0);
    }
#endif
}