using UnityEngine;
using Unity.Cinemachine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Requires Cinemachine 3.x. Attach to an empty CameraManager GameObject.
public class CameraDirector : MonoBehaviour
{
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineCamera defaultCamera;
    [SerializeField] private CinemachineCamera boardCamera;
    [SerializeField] private CinemachineCamera throwCamera;
    [SerializeField] private bool enableDebugKeyboard = true;

    [SerializeField] private CinemachineCamera scaleCamera;
    [Tooltip("Optional. If empty, Default Camera is used for opponent reactions.")]
    [SerializeField] private CinemachineCamera opponentCamera;
    public enum PlayerView { Board, Scale, Front, Throw }
    private System.Func<bool> playerViewGuard;
    private CinemachineCamera activeTarget;
    public bool HasThrowCamera => ready && throwCamera!=null && throwCamera.isActiveAndEnabled;
    public bool IsBlending => brain==null || brain.IsBlending || (activeTarget!=null && brain.ActiveVirtualCamera!=activeTarget);
    public bool IsThrowViewReady => ready && isActiveAndEnabled && throwCamera!=null && throwCamera.isActiveAndEnabled
        && !brain.IsBlending && activeTarget==throwCamera && brain.ActiveVirtualCamera==throwCamera;
    public void BindPlayerViewGuard(System.Func<bool> guard) { playerViewGuard=guard; }
    public bool CanChoosePlayerView => ready && isActiveAndEnabled && !scaleFocus && !throwFocus && !IsBlending
        && playerViewGuard!=null && playerViewGuard();
    private CinemachineCamera CameraFor(PlayerView view)
    {
        switch(view) {case PlayerView.Board:return boardCamera;case PlayerView.Scale:return scaleCamera;
            case PlayerView.Front:return defaultCamera;default:return throwCamera;}
    }
    public bool IsPlayerViewAvailable(PlayerView view) { var c=CameraFor(view);return c!=null && c.isActiveAndEnabled; }
    public bool IsSelectedPlayerView(PlayerView view) {return activeTarget==CameraFor(view);}
    public bool TryChoosePlayerView(PlayerView view)
    {
        if(!CanChoosePlayerView || !IsPlayerViewAvailable(view))return false;
        Activate(CameraFor(view));return true;
    }
    private bool throwFocus;
    private bool scaleFocus;
    // Includes both manual inspection and automatic damage focus; never during a blend.
    public bool IsScaleViewReady => ready && isActiveAndEnabled && brain!=null
        && scaleCamera!=null && scaleCamera.isActiveAndEnabled
        && activeTarget==scaleCamera && !brain.IsBlending && brain.ActiveVirtualCamera==scaleCamera;
    public bool HasScaleCamera => ready && scaleCamera != null && scaleCamera.isActiveAndEnabled;
    private bool showingBoard;
    private bool ready;

    private void Awake()
    {
        ready = brain != null && defaultCamera != null && boardCamera != null
            && defaultCamera != boardCamera
            && (throwCamera == null || (throwCamera != defaultCamera && throwCamera != boardCamera))
            && (scaleCamera == null || (scaleCamera != defaultCamera && scaleCamera != boardCamera && scaleCamera != throwCamera))
            && (opponentCamera == null || (opponentCamera != boardCamera && opponentCamera != throwCamera && opponentCamera != scaleCamera));

        if (!ready)
        {
            Debug.LogError("CameraDirector: assign Brain and two different Cinemachine cameras.", this);
            enabled = false;
            return;
        }

        ApplyFixedThrowPose();
        ShowDefault();
    }

    private void Update()
    {
        if (!enableDebugKeyboard) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            ToggleView();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space))
            ToggleView();
#endif
    }

    // Also callable from a UI Button On Click event.
    public void ToggleView()
    {
        if (!CanChoosePlayerView) return;
        TryChoosePlayerView(showingBoard?PlayerView.Front:PlayerView.Board);
    }

    public void ShowDefault() { if (ready && !scaleFocus && !throwFocus) Activate(defaultCamera); }
    public void ShowBoard() { if (ready && !scaleFocus && !throwFocus) Activate(boardCamera); }
    public void ShowThrow() { if (ready && !scaleFocus && !throwFocus) Activate(throwCamera != null ? throwCamera : boardCamera); }
    // Preserve the existing manager API. Board/flight bounds no longer change the shot.
    public void PrepareThrowView(Bounds envelope, Vector3 playerSide)
    {
        if (ready) ApplyFixedThrowPose();
    }
    private void ApplyFixedThrowPose()
    {
        if (throwCamera == null) return;
        Transform pose = throwCamera.transform;
        // Inspector Transform values are local to the parent (world values if unparented).
        pose.localPosition = new Vector3(0f, 1.9f, -1.45f);
        pose.localRotation = Quaternion.Euler(40f, 0f, 0f);
        pose.localScale = Vector3.one;
        throwCamera.ForceCameraPosition(pose.position, pose.rotation);
    }
    public void BeginThrowFocus()
    {
        if (!ready || scaleFocus) return;
        throwFocus = true;
        Activate(throwCamera != null ? throwCamera : boardCamera);
    }
    public void BeginBoardThrowFocus()
    {
        if (!ready || scaleFocus) return;
        throwFocus=true;
        Activate(boardCamera);
    }
    public void BeginOpponentReactionFocus()
    {
        if (!ready || scaleFocus) return;
        throwFocus=true;
        Activate(opponentCamera != null && opponentCamera.isActiveAndEnabled ? opponentCamera : defaultCamera);
    }
    public void EndThrowFocus() { throwFocus = false; }
    public void BeginScaleFocus()
    {
        if (!HasScaleCamera) return;
        scaleFocus = true;
        Activate(scaleCamera);
    }
    public void EndScaleFocus() { scaleFocus = false; }
    private void Activate(CinemachineCamera target)
    {
        activeTarget=target;
        if (target == throwCamera) ApplyFixedThrowPose();
        defaultCamera.Priority = target == defaultCamera ? 20 : 10;
        boardCamera.Priority = target == boardCamera ? 20 : 10;
        if (throwCamera != null) throwCamera.Priority = target == throwCamera ? 20 : 10;
        if (scaleCamera != null) scaleCamera.Priority = target == scaleCamera ? 20 : 10;
        if (opponentCamera != null) opponentCamera.Priority = target == opponentCamera ? 20 : 10;
        showingBoard = target == boardCamera;
    }
}
