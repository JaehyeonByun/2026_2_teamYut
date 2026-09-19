using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Visual-only throw. The game supplies the mask; animation never rolls again.
// Stick root: local +Y is the marked/flat face, local +Z is the long axis.
public class YutThrowPresenter : MonoBehaviour
{
    [SerializeField] private Transform[] sticks = new Transform[4];
    [SerializeField] private Transform[] landingPoints = new Transform[4];
    [SerializeField] private Transform playerThrowOrigin;
    [SerializeField] private Transform opponentThrowOrigin;
    [SerializeField, Min(0.1f)] private float flightSeconds = 0.85f;
    [SerializeField, Min(0f)] private float staggerSeconds = 0.06f;
    [SerializeField, Min(0f)] private float arcHeight = 0.3f;
    [SerializeField, Min(0.05f)] private float bounceSeconds = 0.22f;
    [SerializeField, Min(0f)] private float bounceHeight = 0.035f;

    public bool IsReady { get; private set; }
    public bool IsPlaying { get; private set; }
    public int LastCompletedMask { get; private set; } = -1;
    private int generation;

    private void Awake()
    {
        IsReady = ValidateSetup();
        ResetPresentation();
    }
    private bool ValidateSetup()
    {
        if (sticks == null || sticks.Length != 4 || landingPoints == null || landingPoints.Length != 4
            || playerThrowOrigin == null || opponentThrowOrigin == null)
            return Error("Assign four sticks, four landing points and both origins.");
        var seen = new HashSet<Transform>();
        for (int i = 0; i < 4; i++)
        {
            if (sticks[i] == null || landingPoints[i] == null || !seen.Add(sticks[i]))
                return Error("Missing or duplicate stick.");
            // A moving parent would invalidate the supposedly fixed destination.
            if (sticks[i] == transform || transform.IsChildOf(sticks[i]))
                return Error("Stick roots must not contain the presenter object.");
            if (Vector3.Dot(landingPoints[i].up, Vector3.up) < 0.999f)
                return Error("Landing points may rotate around Y only; reset X and Z rotation.");
            if ((sticks[i].lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
                return Error("Keep each stick root and its parents at Scale 1,1,1. Scale the Body child instead.");
            var rb = sticks[i].GetComponentInChildren<Rigidbody>();
            if (rb != null && !rb.isKinematic)
                return Error("Remove Rigidbody or set Is Kinematic; this is a scripted throw.");
        }
        for (int i = 0; i < 4; i++)
        {
            if (!seen.Add(landingPoints[i])) return Error("Landing points must be four different fixed objects.");
            for (int j = 0; j < 4; j++)
                if (landingPoints[i].IsChildOf(sticks[j]) || sticks[i].IsChildOf(sticks[j]) && i != j)
                    return Error("Do not nest sticks or place landing points under a stick.");
        }
        return true;
    }
    private bool Error(string message)
    {
        Debug.LogError("YutThrowPresenter: " + message, this);
        return false;
    }
    public static Quaternion LandingRotation(int mask, int index, Quaternion anchorRotation)
    {
        bool flatUp = (mask & (1 << index)) != 0;
        return anchorRotation * Quaternion.AngleAxis(flatUp ? 0f : 180f, Vector3.forward);
    }
    public IEnumerator PlayThrow(int mask, bool opponent)
    {
        if (!IsReady || !isActiveAndEnabled || IsPlaying || mask < 0 || mask > 15)
        { LastCompletedMask = -1; yield break; }
        int ticket = ++generation;
        LastCompletedMask = -1;
        IsPlaying = true;
        Transform origin = opponent ? opponentThrowOrigin : playerThrowOrigin;
        var starts = new Vector3[4];
        var ends = new Vector3[4];
        var rotations = new Quaternion[4];
        for (int i = 0; i < 4; i++)
        {
            starts[i] = origin.position + origin.right * ((i - 1.5f) * 0.055f);
            ends[i] = landingPoints[i].position;
            rotations[i] = LandingRotation(mask, i, landingPoints[i].rotation);
            sticks[i].gameObject.SetActive(false);
        }
        float flight = Mathf.Max(0.1f, flightSeconds);
        float bounce = Mathf.Max(0.05f, bounceSeconds);
        float stagger = Mathf.Max(0f, staggerSeconds);
        float total = flight + bounce + stagger * 3f;
        float elapsed = 0f;
        while (elapsed < total)
        {
            if (ticket != generation || !isActiveAndEnabled) yield break;
            for (int i = 0; i < 4; i++)
            {
                float localTime = elapsed - stagger * i;
                if (localTime < 0f) continue;
                sticks[i].gameObject.SetActive(true);
                if (localTime < flight)
                {
                    float t = Mathf.Clamp01(localTime / flight);
                    Vector3 pos = Vector3.Lerp(starts[i], ends[i], t)
                        + Vector3.up * (4f * Mathf.Max(0f, arcHeight) * t * (1f - t));
                    // Finish spinning before contact to avoid sweeping through the table.
                    float spin = 1f - Mathf.Clamp01(t / 0.75f);
                    Quaternion tumble = Quaternion.Euler(spin * 360f * (2 + i % 2), 0f, spin * 360f);
                    sticks[i].SetPositionAndRotation(pos, rotations[i] * tumble);
                }
                else
                {
                    float t = Mathf.Clamp01((localTime - flight) / bounce);
                    float height = Mathf.Sin(t * Mathf.PI) * Mathf.Max(0f, bounceHeight);
                    sticks[i].SetPositionAndRotation(ends[i] + Vector3.up * height, rotations[i]);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (ticket != generation || !isActiveAndEnabled) yield break;
        for (int i = 0; i < 4; i++)
        {
            sticks[i].gameObject.SetActive(true);
            sticks[i].SetPositionAndRotation(ends[i], rotations[i]);
        }
        IsPlaying = false;
        LastCompletedMask = mask;
    }
    public void ResetPresentation()
    {
        generation++;
        IsPlaying = false;
        LastCompletedMask = -1;
        if (sticks == null) return;
        foreach (var stick in sticks)
            if (stick != null && stick != transform && !transform.IsChildOf(stick)) stick.gameObject.SetActive(false);
    }
    private void OnDisable() { ResetPresentation(); }
}

#if UNITY_EDITOR
public static class YutThrowVisualChecks
{
    [UnityEditor.MenuItem("Tools/Yut/Run Throw Rotation Checks")]
    public static void Run()
    {
        for (int mask = 0; mask < 16; mask++)
            for (int i = 0; i < 4; i++)
            {
                Quaternion rotation = YutThrowPresenter.LandingRotation(mask, i, Quaternion.Euler(0, 17 * i, 0));
                float dot = Vector3.Dot(rotation * Vector3.up, Vector3.up);
                bool expectedUp = (mask & (1 << i)) != 0;
                if ((expectedUp && dot < 0.999f) || (!expectedUp && dot > -0.999f))
                    throw new System.Exception("Incorrect top face orientation.");
            }
        Debug.Log("Yut throw rotation checks passed: 64 orientations.");
    }
}
#endif
