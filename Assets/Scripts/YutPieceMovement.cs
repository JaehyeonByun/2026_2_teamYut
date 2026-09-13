using System.Collections;
using UnityEngine;

// First prototype: one piece, one fixed outer route, and a separate finish point.
// This is not the final rule engine (no shortcuts, captures, stacks, or turns yet).
public class YutPieceMovement : MonoBehaviour
{
    [SerializeField] private Transform waitingPoint;
    [SerializeField] private Transform[] route;
    [SerializeField, Min(0.01f)] private float moveSpeed = 1.2f;
    [SerializeField] private float heightOffset = 0.06f;

    // -1 means waiting. The last route element is the finish point.
    private int currentIndex = -1;
    private bool isMoving;
    private bool initialized;

    public bool IsMoving => isMoving;
    public bool IsFinished => initialized && currentIndex == route.Length - 1;

    private void Start()
    {
        initialized = ValidateSetup();
        if (initialized) ResetPiece();
    }

    private bool ValidateSetup()
    {
        if (waitingPoint == null || route == null || route.Length == 0)
        {
            Debug.LogError("Assign Waiting Point and every Route element.", this);
            return false;
        }

        for (int i = 0; i < route.Length; i++)
        {
            if (route[i] == null)
            {
                Debug.LogError($"Route Element {i} is empty.", this);
                return false;
            }
        }
        return true;
    }

    // Assign this method to a UI Button On Click event, with an integer 1 to 5.
    public void MoveBySteps(int steps)
    {
        if (!initialized || !isActiveAndEnabled || isMoving || IsFinished) return;
        if (steps < 1 || steps > 5) return;

        int targetIndex = Mathf.Min(currentIndex + steps, route.Length - 1);
        isMoving = true;
        StartCoroutine(MoveRoutine(targetIndex));
    }

    private IEnumerator MoveRoutine(int targetIndex)
    {
        while (currentIndex < targetIndex)
        {
            int nextIndex = currentIndex + 1;
            Vector3 target = route[nextIndex].position + Vector3.up * heightOffset;

            while ((transform.position - target).sqrMagnitude > 0.000001f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, target,
                    Mathf.Max(0.01f, moveSpeed) * Time.deltaTime);
                yield return null;
            }

            transform.position = target;
            currentIndex = nextIndex;
        }

        isMoving = false;
        Debug.Log(IsFinished ? "Finished!" : $"Arrived: {route[currentIndex].name}", this);
    }

    public void ResetPiece()
    {
        if (!initialized) return;
        StopAllCoroutines();
        currentIndex = -1;
        isMoving = false;
        transform.position = waitingPoint.position + Vector3.up * heightOffset;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isMoving = false;
        if (!initialized) return;
        Transform anchor = currentIndex < 0 ? waitingPoint : route[currentIndex];
        transform.position = anchor.position + Vector3.up * heightOffset;
    }
}
