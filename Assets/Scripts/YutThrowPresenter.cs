using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Root +Y = flat face normal, +Z = long axis. No predetermined production result.
public class YutThrowPresenter : MonoBehaviour
{
    [SerializeField] private Transform[] sticks = new Transform[4];
    [SerializeField] private BoxCollider boardBounds;
    [Tooltip("Place on the player's side. Only its horizontal direction from board centre is used.")]
    [SerializeField] private Transform playerThrowOrigin;
    [SerializeField] private Transform opponentThrowOrigin;
    [Header("Release from hand")]
    [SerializeField, Min(0.5f)] private float weakFlightSeconds = 1.15f;
    [SerializeField, Min(0.5f)] private float strongFlightSeconds = 0.9f;
    [SerializeField, Min(0.1f)] private float maxReleaseSpeed = 2f;
    [Header("Temporary throw diagnostics")]
    [SerializeField] private bool logThrowDiagnostics = true;
    [Header("End-over-end rotation (local X)")]
    [SerializeField, Min(0.1f)] private float weakEndOverSpeed = 9f;
    [SerializeField, Min(0.1f)] private float strongEndOverSpeed = 13f;
    [SerializeField, Range(0f,0.4f)] private float spinVariation = 0.2f;
    [SerializeField, Min(0.1f)] private float maxEndOverSpeed = 15f;
    [SerializeField, Range(0f,0.5f)] private float fullChargeSpinBoost = 0.15f;
    [Header("Spread inside board")]
    [SerializeField, Range(0.25f,1f)] private float landingSpread = 0.75f;
    [Header("Timed landing result")]
    [SerializeField, Min(0f)] private float resultDecisionDelaySeconds = 0.75f;
    [Header("Legacy values (unused for origin launch)")]
    [SerializeField] private float lowForwardSpeed = 0.35f;
    [SerializeField] private float highForwardSpeed = 1.15f;
    [SerializeField] private float lowUpSpeed = 0.65f;
    [SerializeField] private float highUpSpeed = 1.25f;
    [SerializeField] private float spinSpeed = 12f;
    [SerializeField] private float timeoutSeconds = 12f;
    [Tooltip("Assign all gameplay piece colliders. Sticks ignore them so pieces do not become obstacles.")]
    [SerializeField] private Collider[] ignoreColliders = new Collider[0];
    [Header("Sequential release")]
    [SerializeField, Min(0f)] private float releaseIntervalSeconds = 0.05f;
    [SerializeField, Range(0.5f,2f)] private float throwSpeedMultiplier = 1.5f;
    [Header("Readable throw motion")]
    [SerializeField, Range(0.25f, 1f)] private float motionSpeed = 0.5f;
    public Bounds ThrowViewBounds
    {
        get
        {
            Vector3 span = new Vector3(Mathf.Abs(right.x)*halfWidth+Mathf.Abs(forward.x)*halfDepth,
                radius, Mathf.Abs(right.z)*halfWidth+Mathf.Abs(forward.z)*halfDepth);
            var bounds = new Bounds(centre+Vector3.up*radius,span*2f);
            float slow = Mathf.Clamp(motionSpeed,0.25f,1f);
            float g = Mathf.Max(0.1f,Mathf.Abs(Physics.gravity.y))*slow*slow;
            foreach(bool opponent in new[]{false,true})
            {
                Vector3 origin = ReleaseOrigin(opponent);
                float t = Mathf.Max(weakFlightSeconds,strongFlightSeconds)*0.45f/slow;
                float vy = (centre.y+radius-origin.y)/Mathf.Max(0.5f,t)+0.5f*g*t;
                float peak = Mathf.Max(0f,vy)*Mathf.Max(0f,vy)/(2f*g);
                bounds.Encapsulate(new Bounds(origin+Vector3.up*(radius+peak*0.5f),
                    Vector3.one*(radius*4f+0.04f)+Vector3.up*peak));
            }
            return bounds;
        }
    }
    public Vector3 PlayerSide => Vector3.ProjectOnPlane(playerThrowOrigin.position-centre,Vector3.up).normalized;
    private float activeSpeed = 1f;
    public bool IsReady { get; private set; }
    public bool IsPlaying { get; private set; }
    public int LastCompletedMask { get; private set; } = -1;
    public string LastError { get; private set; }
    private readonly Rigidbody[] bodies = new Rigidbody[4];
    private readonly float[] radii = new float[4];
    private readonly List<Collider> stickColliders = new List<Collider>();
    private readonly List<Collider[]> ignoredPairs = new List<Collider[]>();
    private GameObject cage;
    private Vector3 centre, right, forward;
    private float halfWidth, halfDepth, radius, roofHeight;
    private int generation;

    private readonly YutLandingTimer landingTimer = new YutLandingTimer();
    private readonly YutStickContactProbe[] contactProbes = new YutStickContactProbe[4];
    private readonly int[] contactMasks = new int[4];
    private readonly bool[] lastClearFace = new bool[4];
    private readonly bool[] loggedFirstCollision = new bool[4];
    private bool diagnosticOpponent;
    private readonly bool[] enteredBoard = new bool[4];
    private Vector3 ReleaseOrigin(bool opponent)
    {
        if (!opponent) return playerThrowOrigin.position;
        if (opponentThrowOrigin != null) return opponentThrowOrigin.position;
        Vector3 p = playerThrowOrigin.position-centre;
        return centre + new Vector3(-p.x,p.y,-p.z);
    }
    private void SetEntryCollision(int index, bool enabled)
    {
        foreach(var col in sticks[index].GetComponentsInChildren<Collider>())
            foreach(var wall in cage.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(col,wall,!enabled);
    }
    private void ArmClearBoundaryPairs(int index)
    {
        // After entry, arm each pair only once it no longer overlaps the wall's volume.
        // Re-enabling a wall inside a rotating stick can create a large solver impulse.
        foreach(var col in sticks[index].GetComponentsInChildren<Collider>())
        {
            if(!col.enabled || col.isTrigger) continue;
            foreach(var wall in cage.GetComponentsInChildren<Collider>())
                if(Physics.GetIgnoreCollision(col,wall) && !col.bounds.Intersects(wall.bounds))
                    Physics.IgnoreCollision(col,wall,false);
        }
    }
    private void Awake()
    {
        IsReady = Setup();
        ResetPresentation();
    }
    private bool Error(string message)
    {
        LastError = message;
        Debug.LogError("YutThrowPresenter: " + message, this);
        return false;
    }
    private bool Setup()
    {
        if (boardBounds == null || playerThrowOrigin == null || sticks == null || sticks.Length != 4)
            return Error("Assign Board Bounds, Player Throw Origin and four Sticks.");
        Transform board = boardBounds.transform;
        if (!boardBounds.enabled || boardBounds.isTrigger || !board.gameObject.activeInHierarchy ||
            Vector3.Dot(board.up, Vector3.up) < 0.999f || board.lossyScale.x <= 0 || board.lossyScale.y <= 0 || board.lossyScale.z <= 0)
            return Error("Board must be horizontal, active, positive-scale and have a solid enabled BoxCollider.");
        if (boardBounds.attachedRigidbody != null && !boardBounds.attachedRigidbody.isKinematic)
            return Error("Board must not have a dynamic Rigidbody.");
        centre = board.TransformPoint(boardBounds.center + Vector3.up * boardBounds.size.y * 0.5f);
        right = board.right; forward = board.forward;
        halfWidth = boardBounds.size.x * board.lossyScale.x * 0.5f;
        halfDepth = boardBounds.size.z * board.lossyScale.z * 0.5f;
        var seen = new HashSet<Transform>();
        for (int i = 0; i < 4; i++)
        {
            var stick = sticks[i];
            if (stick == null || !seen.Add(stick) || stick == transform || transform.IsChildOf(stick))
                return Error("Missing, duplicate, or invalid stick root.");
            if ((stick.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
                return Error("Stick root and parents must have scale 1. Scale only the visual/collider child.");
            for (int j = 0; j < 4; j++)
                if (i != j && sticks[j] != null && stick.IsChildOf(sticks[j])) return Error("Do not nest stick roots.");
            foreach (var rb in stick.GetComponentsInChildren<Rigidbody>(true))
                if (rb.transform != stick) return Error("Remove child Rigidbody; use one Rigidbody on each stick root.");
            bodies[i] = stick.GetComponent<Rigidbody>();
            if (bodies[i] == null) bodies[i] = stick.gameObject.AddComponent<Rigidbody>();
            if (playerThrowOrigin.IsChildOf(stick) || (opponentThrowOrigin != null && opponentThrowOrigin.IsChildOf(stick)))
                return Error("Throw Origin markers must not be children of moving sticks.");
            contactProbes[i] = stick.GetComponent<YutStickContactProbe>();
            if(contactProbes[i]==null) contactProbes[i]=stick.gameObject.AddComponent<YutStickContactProbe>();
            contactProbes[i].enabled=true;
            contactProbes[i].Initialize(this,i);
            var body = bodies[i];
            body.isKinematic = true;
            body.useGravity = false; // Per-stick gravity enables slow motion without slowing UI/camera.
            body.mass = 0.025f;
            body.constraints = RigidbodyConstraints.None;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.solverIterations = 12;
            body.solverVelocityIterations = 8;
            body.maxAngularVelocity = 35f;
            body.sleepThreshold = 0.0005f;
#if UNITY_6000_0_OR_NEWER
            body.linearDamping = 0.12f; body.angularDamping = 0.15f;
#else
            body.drag = 0.12f; body.angularDrag = 0.15f;
#endif
            bool solid = false;
            // Use mesh bounds in root space, including pivot offset, to fit launch spheres inside cage.
            foreach (var mesh in stick.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mesh.sharedMesh == null) continue;
                Bounds b = mesh.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 p = b.center + Vector3.Scale(b.extents, new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    radii[i] = Mathf.Max(radii[i], stick.InverseTransformPoint(mesh.transform.TransformPoint(p)).magnitude);
                }
            }
            foreach (var col in stick.GetComponentsInChildren<Collider>(true))
            {
                if (!col.enabled || col.isTrigger) continue;
                if (col is MeshCollider mc && (!mc.convex || mc.sharedMesh == null))
                    return Error("Stick MeshColliders require a mesh and Convex enabled.");
                solid = true; stickColliders.Add(col); col.contactOffset = 0.001f;
                if (Physics.GetIgnoreLayerCollision(col.gameObject.layer, board.gameObject.layer))
                    return Error("Enable stick/board collision in Physics Layer Collision Matrix.");
            }
            if (!solid || radii[i] <= 0) return Error("Each stick needs a visible MeshFilter and a solid collider matching its shape.");
            radius = Mathf.Max(radius, radii[i]);
        }
        if (Mathf.Min(halfWidth, halfDepth) < radius * 2.3f)
            return Error("Sticks are too large for this board. Reduce Body size or enlarge board.");
        if (Vector3.ProjectOnPlane(playerThrowOrigin.position-centre,Vector3.up).sqrMagnitude < 0.001f)
            return Error("Move Player Throw Origin to the player's side of the board centre.");
        float up = Mathf.Max(0f, Mathf.Max(lowUpSpeed,highUpSpeed));
        roofHeight = radius * 3f + 0.12f + up*up/(2f*Mathf.Max(0.1f,Mathf.Abs(Physics.gravity.y))) + 0.5f;
        cage = new GameObject("Yut Physics Boundary (Runtime)");
        cage.transform.SetPositionAndRotation(centre, board.rotation);
        float t = Mathf.Max(0.05f,radius);
        Wall(new Vector3(-halfWidth-t/2,roofHeight/2,0),new Vector3(t,roofHeight,2*halfDepth+2*t));
        Wall(new Vector3(halfWidth+t/2,roofHeight/2,0),new Vector3(t,roofHeight,2*halfDepth+2*t));
        Wall(new Vector3(0,roofHeight/2,-halfDepth-t/2),new Vector3(2*halfWidth,roofHeight,t));
        Wall(new Vector3(0,roofHeight/2,halfDepth+t/2),new Vector3(2*halfWidth,roofHeight,t));
        Wall(new Vector3(0,roofHeight+t/2,0),new Vector3(2*halfWidth, t,2*halfDepth));
        foreach (var a in stickColliders)
            foreach (var b in ignoreColliders)
                if (b != null && !stickColliders.Contains(b) && !Physics.GetIgnoreCollision(a,b))
                { Physics.IgnoreCollision(a,b,true); ignoredPairs.Add(new[]{a,b}); }
        return true;
    }
    private void Wall(Vector3 position, Vector3 size)
    {
        var go = new GameObject("Invisible boundary");
        go.layer = boardBounds.gameObject.layer;
        go.transform.SetParent(cage.transform,false);
        go.transform.localPosition = position;
        var col = go.AddComponent<BoxCollider>(); col.size = size; col.contactOffset = 0.001f;
        // Cage affects only the four sticks, never gameplay pieces or other rigidbodies.
        foreach (var other in FindObjectsOfType<Collider>())
            if (other != col && !stickColliders.Contains(other)) Physics.IgnoreCollision(col,other,true);
    }
    public int GetSupportMask(int index, Collision collision)
    {
        if(!IsPlaying || index<0 || index>=4 || bodies[index]==null || bodies[index].isKinematic) return 0;
        if(logThrowDiagnostics && !loggedFirstCollision[index])
        {
            loggedFirstCollision[index]=true;
            Debug.Log($"[Yut First Contact] opponent={diagnosticOpponent}, stick={index}, other={collision.collider.name}, board={collision.collider==boardBounds}, pos={bodies[index].position.ToString("F3")}, spin={bodies[index].angularVelocity.ToString("F3")}",this);
        }
        int other=-1;
        for(int i=0;i<4;i++) if(i!=index && collision.rigidbody==bodies[i]) {other=i;break;}
        bool board=collision.collider==boardBounds;
        if(!board && other<0) return 0; // Walls, triggers, table sides are not landings.
        for(int i=0;i<collision.contactCount;i++)
        {
            ContactPoint contact=collision.GetContact(i);
            if(Vector3.Dot(contact.normal,Vector3.up)>0.2f && (!board || contact.point.y>=centre.y-0.015f))
                return board ? 16 : 1<<other;
        }
        return 0;
    }
    private int FreezeAndRead()
    {
        // Freeze all four first, then read the exact frozen rotations for one consistent result.
        foreach(var rb in bodies)
        {
            SetVelocity(rb,Vector3.zero);
            rb.angularVelocity=Vector3.zero;
            rb.isKinematic=true;
            rb.transform.SetPositionAndRotation(rb.position,rb.rotation);
        }
        int mask=0;
        for(int i=0;i<4;i++)
        {
            float dot=Vector3.Dot(bodies[i].rotation*Vector3.up,Vector3.up);
            if(Mathf.Abs(dot)<=0.00001f)
                Debug.LogWarning("Yut: exact edge pose; using last clear face for stick "+i,this);
            if(YutLandingTimer.ReadFace(dot,lastClearFace[i])) mask|=1<<i;
        }
        return mask;
    }
    private void FixedUpdate()
    {
        if (!IsPlaying) return;
        foreach (var rb in bodies)
            if (rb != null && !rb.isKinematic && !rb.IsSleeping())
                rb.AddForce(Physics.gravity * (activeSpeed * activeSpeed), ForceMode.Acceleration);
    }
    private void SetBoundaryActive(bool active)
    {
        if (cage == null) return;
        cage.SetActive(active);
        if (!active) return;
        // Reapply ignores after reactivation; collider disabling may clear pair state.
        var sceneColliders = FindObjectsOfType<Collider>();
        foreach (var wall in cage.GetComponentsInChildren<Collider>())
            foreach (var other in sceneColliders)
                if (other != wall && !stickColliders.Contains(other)) Physics.IgnoreCollision(wall, other, true);
    }
    public IEnumerator PlayPhysicalThrow(bool opponent, float power)
    {
        if (!IsReady || !isActiveAndEnabled || IsPlaying) { LastCompletedMask = -1; yield break; }
        power = float.IsNaN(power) || float.IsInfinity(power) ? 0.5f : Mathf.Clamp01(power);
        ResetPresentation();
        int ticket = generation;
        IsPlaying = true; LastError = null;
        diagnosticOpponent=opponent;
        float baseMotionSpeed = Mathf.Clamp(motionSpeed,0.25f,1f);
        float speedMultiplier = Mathf.Clamp(throwSpeedMultiplier,0.5f,2f);
        activeSpeed = baseMotionSpeed*speedMultiplier;
        Vector3 origin = ReleaseOrigin(opponent);
        Vector3 side = Vector3.ProjectOnPlane(origin-centre,Vector3.up).normalized;
        if(side.sqrMagnitude < 0.01f || Physics.gravity.y >= -0.1f)
        { Abort("Place throw origin on the player's/opponent's side and use downward gravity."); yield break; }
        if (opponent && Vector3.Dot(side,PlayerSide)>-0.1f)
        { Abort("Opponent Throw Origin must be on the opposite side from Player Throw Origin."); yield break; }
        Vector3 lateral = Vector3.Cross(Vector3.up,side);
        float flight = Mathf.Max(0.5f,Mathf.Lerp(weakFlightSeconds,strongFlightSeconds,power))*0.45f/baseMotionSpeed;
        Vector3 acceleration = Physics.gravity*baseMotionSpeed*baseMotionSpeed;
        if(logThrowDiagnostics)
            Debug.Log($"[Yut Geometry] opponent={opponent}, originWorld={origin.ToString("F3")}, boardTopWorld={centre.ToString("F3")}, halfWidth={halfWidth:F3}, halfDepth={halfDepth:F3}, stickRadius={radius:F3}, flight={flight:F3}",this);
        var velocities = new Vector3[4];
        var spins = new Vector3[4];
        var startPositions = new Vector3[4];
        var startRotations = new Quaternion[4];
        var released = new bool[4];
        var releasedAt = new float[4];
        float interval = Mathf.Max(0f,releaseIntervalSeconds);
        float latestFlight = flight;
        // One independently jittered destination per quadrant; no fixed final poses.
        int[] zones = {0,1,2,3};
        for(int i=3;i>0;i--) { int j=Random.Range(0,i+1); int tmp=zones[i];zones[i]=zones[j];zones[j]=tmp; }
        float safeX=Mathf.Max(0f,halfWidth-radius-0.03f);
        float safeZ=Mathf.Max(0f,halfDepth-radius-0.03f);
        // Start at the actual hand marker, with a non-overlapping two-level bundle.
        for(int i=0;i<4;i++)
        {
            Vector3 p = origin + lateral*((i%2==0?-1f:1f)*(radius+0.008f))
                + Vector3.up*((i/2)*(radius*2f+0.02f));
            int zone=zones[i];
            float targetX=((zone&1)==0?-1f:1f)*safeX*landingSpread*Random.Range(0.5f,0.95f);
            float targetZ=((zone&2)==0?-1f:1f)*safeZ*landingSpread*Random.Range(0.5f,0.95f);
            Vector3 target=centre+right*targetX+forward*targetZ+Vector3.up*(radius+0.015f);
            float stickFlight=flight;
            velocities[i]=(target-p)/stickFlight-acceleration*(stickFlight*0.5f);
            // Further quadrants may need more time, rather than a stronger launch.
            for(int attempt=0;attempt<25 && velocities[i].magnitude>maxReleaseSpeed;attempt++)
            {
                stickFlight+=0.04f;
                velocities[i]=(target-p)/stickFlight-acceleration*(stickFlight*0.5f);
            }
            latestFlight=Mathf.Max(latestFlight,stickFlight);
            if (p.y-centre.y < radius+0.02f || velocities[i].magnitude > maxReleaseSpeed)
            { Abort("Throw Origin is too low or too far away. Raise above the table or move closer; no high-speed launch was applied."); yield break; }
            // Clear interpolation history before showing a reused Rigidbody at a new origin.
            bodies[i].interpolation=RigidbodyInterpolation.None;
            float chargeAngle = power*Random.Range(45f,100f);
            Quaternion launchRotation = Quaternion.LookRotation(-side,Vector3.up)*Quaternion.Euler(
                Random.Range(-65f,65f)+chargeAngle, Random.Range(-20f,20f), Random.Range(-45f,45f));
            float chargeBoost = 1f+Mathf.Clamp(fullChargeSpinBoost,0f,0.5f)*power;
            float baseSpin = chargeBoost*Mathf.Lerp(weakEndOverSpeed,strongEndOverSpeed,power)*Random.Range(1f-spinVariation,1f+spinVariation);
            // +Z is length: rotating around local X carries the two ends over each other.
            Vector3 localSpin = new Vector3(baseSpin,baseSpin*Random.Range(-0.1f,0.1f),baseSpin*Random.Range(-0.22f,0.22f));
            spins[i] = launchRotation*Vector3.ClampMagnitude(localSpin,Mathf.Max(0.1f,maxEndOverSpeed)*chargeBoost)*activeSpeed;
            lastClearFace[i] = Vector3.Dot(launchRotation*Vector3.up,Vector3.up)>0f;
            enteredBoard[i]=false;
            startPositions[i]=p;
            startRotations[i]=launchRotation;
            velocities[i]*=speedMultiplier;
            // Pending sticks stay hidden and kinematic, with no waiting colliders in the launch path.
        }
        latestFlight/=speedMultiplier;
        if(ticket!=generation || !isActiveAndEnabled) yield break;
        SetBoundaryActive(true);
        float elapsed=0;
        bool loggedEarly=false,loggedLater=false;
        while (landingTimer.AllLandedAt>=0 || elapsed<3f*interval+Mathf.Max(Mathf.Max(2f,timeoutSeconds)/activeSpeed,latestFlight+1f))
        {
            // Absolute schedule avoids accumulating WaitForSeconds rounding across releases.
            for(int i=0;i<4;i++)
            {
                if(released[i] || !YutReleaseTiming.IsDue(elapsed,i,interval)) continue;
                if(ticket!=generation || !isActiveAndEnabled) yield break;
                Vector3 p=startPositions[i]; Quaternion rotation=startRotations[i];
                bodies[i].interpolation=RigidbodyInterpolation.None;
                sticks[i].SetPositionAndRotation(p,rotation);
                sticks[i].gameObject.SetActive(true);
                YutBurnVisual.GetOrAdd(sticks[i].gameObject).SetOutline(true);
                bodies[i].isKinematic=true;
                sticks[i].SetPositionAndRotation(p,rotation);
                bodies[i].position=p; bodies[i].rotation=rotation;
                Physics.SyncTransforms();
                SetEntryCollision(i,false);
                foreach(var a in sticks[i].GetComponentsInChildren<Collider>())
                    foreach(var b in ignoreColliders)
                        if(b!=null && b.enabled && b.gameObject.activeInHierarchy && !stickColliders.Contains(b))
                            Physics.IgnoreCollision(a,b,true);
                bodies[i].isKinematic=false;
#if UNITY_6000_0_OR_NEWER
                bodies[i].linearDamping=0f; bodies[i].angularDamping=0.15f*activeSpeed;
#else
                bodies[i].drag=0f; bodies[i].angularDrag=0.15f*activeSpeed;
#endif
                SetVelocity(bodies[i],velocities[i]); bodies[i].angularVelocity=spins[i];
                bodies[i].WakeUp(); released[i]=true; releasedAt[i]=elapsed;
                if(logThrowDiagnostics)
                    Debug.Log($"[Yut Release] opponent={opponent}, stick={i}, time={elapsed:F3}, scheduled={i*interval:F3}, position={p.ToString("F3")}, speed={Velocity(bodies[i]).ToString("F3")}, spin={bodies[i].angularVelocity.ToString("F3")}",this);
            }
            yield return new WaitForFixedUpdate();
            if(ticket!=generation || !isActiveAndEnabled) yield break;
            elapsed+=Time.fixedDeltaTime;
            for(int i=0;i<4;i++)
                if(released[i] && elapsed-releasedAt[i]>=2f*Time.fixedDeltaTime)
                    bodies[i].interpolation=RigidbodyInterpolation.Interpolate;
            if(logThrowDiagnostics && ((!loggedEarly && elapsed>=0.1f) || (!loggedLater && elapsed>=0.35f)))
            {
                for(int i=0;i<4;i++)
                    if(released[i]) Debug.Log($"[Yut In Flight] opponent={opponent}, t={elapsed:F2}, stick={i}, pos={bodies[i].position.ToString("F3")}, spin={bodies[i].angularVelocity.ToString("F3")}, rotation={bodies[i].rotation.eulerAngles.ToString("F1")}, kinematic={bodies[i].isKinematic}, constraints={bodies[i].constraints}",this);
                if(!loggedEarly)loggedEarly=true;else loggedLater=true;
            }
            int enteredMask=0;
            for(int i=0;i<4;i++)
            {
                if(!released[i]) continue;
                float flightAge=elapsed-releasedAt[i];
                var rb=bodies[i]; Vector3 delta=rb.position-centre;
                bool inside = YutBoardEntryRules.ContainsCentre(
                    Vector3.Dot(delta,right),delta.y,Vector3.Dot(delta,forward),halfWidth,halfDepth,roofHeight);
                if (!enteredBoard[i] && inside)
                {
                    enteredBoard[i]=true;
                    if(logThrowDiagnostics)
                        Debug.Log($"[Yut Board Entry] opponent={opponent}, stick={i}, t={elapsed:F2}, position={rb.position.ToString("F3")}",this);
                }
                if(enteredBoard[i]) ArmClearBoundaryPairs(i);
                if (!enteredBoard[i])
                {
                    if(flightAge>latestFlight+1f || delta.y < -radius)
                    { Abort($"Stick {i} did not enter board. opponent={opponent}, t={elapsed:F2}, pos={rb.position.ToString("F3")}, boardLocalOffset=({Vector3.Dot(delta,right):F3},{delta.y:F3},{Vector3.Dot(delta,forward):F3}), requiredHalf=({halfWidth-0.005f:F3},{halfDepth-0.005f:F3}). Check [Yut Geometry] and [Yut First Contact]."); yield break; }
                    continue;
                }
                if(Mathf.Abs(Vector3.Dot(delta,right))>halfWidth+radius || Mathf.Abs(Vector3.Dot(delta,forward))>halfDepth+radius || delta.y < -radius || delta.y>roofHeight+radius)
                { Abort("A stick escaped the boundary. Check collider sizes, layers and physics step."); yield break; }
                enteredMask |= 1<<i;
                float dot=Vector3.Dot(rb.rotation*Vector3.up,Vector3.up);
                if(Mathf.Abs(dot)>0.00001f) lastClearFace[i]=dot>0f;
            }
            for(int i=0;i<4;i++) contactMasks[i]=contactProbes[i].ContactMask;
            if(landingTimer.Tick(elapsed,resultDecisionDelaySeconds,enteredMask,contactMasks))
            {
                LastCompletedMask=FreezeAndRead();
                IsPlaying=false;
                SetBoundaryActive(false);
                yield break;
            }
        }
        Abort("Not all sticks landed on the board. Check board collider and origin; no result was awarded.");
    }
    private void Abort(string reason)
    { SetBoundaryActive(false); IsPlaying=false; LastCompletedMask=-1; foreach(var rb in bodies) if(rb!=null) rb.isKinematic=true; Error(reason); }
    // Compatibility for existing deterministic route-test queues. Never called by normal gameplay.
    public IEnumerator PlayThrow(int mask, bool opponent, float power=0.5f)
    {
        ResetPresentation();
        if(!IsReady || mask<0 || mask>15) yield break;
        Debug.LogWarning("[TEST] Forced result: physics intentionally bypassed.",this);
        LastCompletedMask=mask;
        yield break;
    }
    public static Quaternion LandingRotation(int mask,int index,Quaternion anchorRotation)
    { return anchorRotation*Quaternion.AngleAxis((mask&(1<<index))!=0?0f:180f,Vector3.forward); }
    private static Vector3 Velocity(Rigidbody rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }
    private static void SetVelocity(Rigidbody rb,Vector3 value)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity=value;
#else
        rb.velocity=value;
#endif
    }
    public IEnumerator DissolvePresentation()
    {
        int ticket=generation;
        var effects=new List<YutBurnVisual>();
        float duration=.05f;
        if(sticks!=null) foreach(var stick in sticks)
        {
            if(!stick || !stick.gameObject.activeInHierarchy) continue;
            var fx=YutBurnVisual.GetOrAdd(stick.gameObject);
            if(fx.BeginBurn()) { effects.Add(fx); duration=Mathf.Max(duration,fx.disappearSeconds); }
        }
        if(effects.Count>0)
        {
            for(float t=0;t<duration;t+=Time.deltaTime)
            {
                if(ticket!=generation || !isActiveAndEnabled) yield break;
                foreach(var fx in effects) if(fx) fx.SetProgress(t/Mathf.Max(.05f,fx.disappearSeconds));
                yield return null;
            }
            foreach(var fx in effects) if(fx) fx.SetProgress(1);
        }
        if(ticket==generation) ResetPresentation();
    }
    public void ResetPresentation()
    {
        generation++; IsPlaying=false; LastCompletedMask=-1;
        landingTimer.Reset();
        for(int i=0;i<4;i++)
        {
            enteredBoard[i]=false;contactMasks[i]=0;lastClearFace[i]=false;loggedFirstCollision[i]=false;
            if(contactProbes[i]!=null) contactProbes[i].Clear();
        }
        SetBoundaryActive(false);
        for(int i=0;i<4;i++) if(bodies[i]!=null) bodies[i].isKinematic=true;
        if(sticks!=null) foreach(var stick in sticks)
            if(stick!=null && stick!=transform && !transform.IsChildOf(stick)) stick.gameObject.SetActive(false);
    }
    private void OnDisable(){ ResetPresentation(); if(cage!=null)cage.SetActive(false); }
    private void OnEnable(){ SetBoundaryActive(false); }
    private void OnDestroy()
    {
        foreach(var pair in ignoredPairs) if(pair[0]!=null && pair[1]!=null) Physics.IgnoreCollision(pair[0],pair[1],false);
        if(cage!=null) Destroy(cage);
    }
}

// Pure geometry policy: a stick centre entering the board footprint counts as entry.
// Collision boundaries and actual landing contacts remain separate responsibilities.
public static class YutBoardEntryRules
{
    public static bool ContainsCentre(float x,float y,float z,float halfWidth,float halfDepth,float roofHeight)
    {
        const float edgeInset=0.005f;
        return System.Math.Abs(x)<halfWidth-edgeInset && System.Math.Abs(z)<halfDepth-edgeInset
            && y>-0.01f && y<roofHeight;
    }
}

public static class YutReleaseTiming
{
    public static bool IsDue(float elapsed,int index,float interval)
    { return index>=0 && index<4 && elapsed+0.000001f>=index*System.Math.Max(0f,interval); }
}
