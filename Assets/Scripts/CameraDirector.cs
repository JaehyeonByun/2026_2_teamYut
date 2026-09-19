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

    private bool showingBoard;
    private bool ready;

    private void Awake()
    {
        ready = brain != null && defaultCamera != null && boardCamera != null
            && defaultCamera != boardCamera
            && (throwCamera == null || (throwCamera != defaultCamera && throwCamera != boardCamera));

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
        if (!ready || brain.IsBlending) return;
        if (showingBoard) ShowDefault();
        else ShowBoard();
    }

    public void ShowDefault()
    {
        if (!ready) return;
        defaultCamera.Priority = 20;
        boardCamera.Priority = 10;
        if (throwCamera != null) throwCamera.Priority = 10;
        showingBoard = false;
    }

    public void ShowBoard()
    {
        if (!ready) return;
        defaultCamera.Priority = 10;
        boardCamera.Priority = 20;
        if (throwCamera != null) throwCamera.Priority = 10;
        showingBoard = true;
    }

    public void ShowThrow()
    {
        if (!ready) return;
        if (throwCamera == null) { ShowDefault(); return; }
        defaultCamera.Priority = 10;
        boardCamera.Priority = 10;
        throwCamera.Priority = 20;
        showingBoard = false;
    }
}

