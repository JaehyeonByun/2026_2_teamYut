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
        if (!ready || scaleFocus || brain.IsBlending) return;
        if (showingBoard) ShowDefault();
        else ShowBoard();
    }

    public void ShowDefault() { if (ready && !scaleFocus) Activate(defaultCamera); }
    public void ShowBoard() { if (ready && !scaleFocus) Activate(boardCamera); }
    public void ShowThrow() { if (ready && !scaleFocus) Activate(throwCamera != null ? throwCamera : defaultCamera); }
    public void BeginScaleFocus()
    {
        if (!HasScaleCamera) return;
        scaleFocus = true;
        Activate(scaleCamera);
    }
    public void EndScaleFocus() { scaleFocus = false; }
    private void Activate(CinemachineCamera target)
    {
        defaultCamera.Priority = target == defaultCamera ? 20 : 10;
        boardCamera.Priority = target == boardCamera ? 20 : 10;
        if (throwCamera != null) throwCamera.Priority = target == throwCamera ? 20 : 10;
        if (scaleCamera != null) scaleCamera.Priority = target == scaleCamera ? 20 : 10;
        showingBoard = target == boardCamera;
    }
}
