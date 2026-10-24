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
    private bool throwFocus;
    private bool scaleFocus;
    public bool HasScaleCamera => ready && scaleCamera != null && scaleCamera.isActiveAndEnabled;
    private bool showingBoard;
    private bool ready;

    private void Awake()
    {
        ready = brain != null && defaultCamera != null && boardCamera != null
            && defaultCamera != boardCamera
            && (throwCamera == null || (throwCamera != defaultCamera && throwCamera != boardCamera))
            && (scaleCamera == null || (scaleCamera != defaultCamera && scaleCamera != boardCamera && scaleCamera != throwCamera));

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
        if (!ready || scaleFocus || throwFocus || brain.IsBlending) return;
        if (showingBoard) ShowDefault();
        else ShowBoard();
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
        if (target == throwCamera) ApplyFixedThrowPose();
        defaultCamera.Priority = target == defaultCamera ? 20 : 10;
        boardCamera.Priority = target == boardCamera ? 20 : 10;
        if (throwCamera != null) throwCamera.Priority = target == throwCamera ? 20 : 10;
        if (scaleCamera != null) scaleCamera.Priority = target == scaleCamera ? 20 : 10;
        showingBoard = target == boardCamera;
    }
}
