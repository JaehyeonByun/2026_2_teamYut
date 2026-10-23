using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// World-space white light on the existing node circles. No overlay labels/canvas.
[DisallowMultipleComponent]
public class YutDestinationPicker : MonoBehaviour
{
    [Header("Circle size (0 = use the board node SphereCollider radius)")]
    [SerializeField, Min(0f)] private float radiusOverride;
    [SerializeField, Min(0.001f)] private float fallbackRadius = 0.035f;
    [SerializeField, Min(0.1f)] private float radiusMultiplier = 1f;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.002f;
    [Header("White fade")]
    [SerializeField, Min(0.1f)] private float fadePeriod = 1.6f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.85f;
    [SerializeField, Min(0f)] private float lightIntensity = 2f;
    [SerializeField, Min(0.1f)] private float destinationSizeBoost = 1.2f;
    private Material material;
    private Mesh quad;
    private readonly List<Marker> markers = new List<Marker>();
    private Action<YutMoveOption> onChoose;
    private float shownAt;
    private int generation;
    private sealed class Marker
    {
        public GameObject Object;
        public Transform Anchor;
        public YutMoveOption Move;
        public float Radius;
    }
    public struct Hit
    {
        public YutMoveOption Move;
        public int Node => Move == null ? -1 : Move.NodeId;
        public float Distance;
        public int Generation;
    }
    public bool Initialize(Camera camera)
    {
        if (camera == null) return false;
        if (material != null) return true;
        Shader shader = Resources.Load<Shader>("YutInteraction/WhitePulse");
        if (shader == null || !shader.isSupported)
        { Debug.LogError("WhitePulse shader missing/unsupported. Import Resources/YutInteraction shaders.", this); return false; }
        material = new Material(shader) { name = "Yut white destination light (runtime)" };
        quad = new Mesh { name = "Yut destination quad (runtime)" };
        quad.vertices = new[] {new Vector3(-1,0,-1),new Vector3(-1,0,1),new Vector3(1,0,1),new Vector3(1,0,-1)};
        quad.uv = new[] {new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
        quad.triangles = new[] {0,1,2,0,2,3};
        quad.RecalculateBounds();
        return true;
    }
    public void Clear()
    {
        generation++;
        onChoose = null;
        foreach (var marker in markers)
            if (marker.Object != null) { marker.Object.SetActive(false); Destroy(marker.Object); }
        markers.Clear();
    }
    public void ShowMoves(YutPieceMovement piece, List<YutMoveOption> options, Action<YutMoveOption> chosen)
    {
        Clear();
        if (!isActiveAndEnabled || material == null || piece == null) return;
        onChoose = chosen;
        shownAt = Time.unscaledTime;
        material.SetFloat("_Alpha", minAlpha);
        foreach (var option in options)
        {
            var anchor = piece.GetBoardPoint(option.NodeId);
            if (anchor == null) continue;
            var obj = new GameObject("White destination " + option.NodeId, typeof(MeshFilter), typeof(MeshRenderer));
            obj.layer = anchor.gameObject.layer;
            obj.GetComponent<MeshFilter>().sharedMesh = quad;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            markers.Add(new Marker {Object=obj,Anchor=anchor,Move=option});
        }
        UpdatePositions();
    }
    private float Radius(Transform anchor)
    {
        if (radiusOverride > 0f) return radiusOverride * radiusMultiplier * destinationSizeBoost;
        var sphere = anchor.GetComponent<SphereCollider>();
        float radius = sphere == null ? fallbackRadius : sphere.radius * Mathf.Max(Mathf.Abs(anchor.lossyScale.x), Mathf.Abs(anchor.lossyScale.z));
        return Mathf.Max(0.001f, radius * radiusMultiplier * destinationSizeBoost);
    }
    private void UpdatePositions()
    {
        foreach (var marker in markers)
        {
            if (marker.Anchor == null || marker.Object == null) continue;
            marker.Radius = Radius(marker.Anchor);
            marker.Object.transform.position = marker.Anchor.position + Vector3.up * surfaceOffset;
            marker.Object.transform.rotation = Quaternion.identity;
            // Shader's bright circumference is at 82% of quad half-width.
            marker.Object.transform.localScale = Vector3.one * (marker.Radius / 0.82f);
        }
    }
    public float HighlightAlpha => Mathf.Lerp(minAlpha,maxAlpha,
        YutSelectionRules.Pulse(Time.unscaledTime-shownAt,fadePeriod));
    public float HighlightIntensity => lightIntensity;
    private void LateUpdate()
    {
        if (material == null) return;
        UpdatePositions();
        float alpha = HighlightAlpha;
        material.SetFloat("_Alpha", alpha);
        material.SetColor("_Color", new Color(lightIntensity,lightIntensity,lightIntensity,1f));
    }
    public bool TryHit(Ray ray, out Hit hit)
    {
        hit = new Hit {Distance=float.PositiveInfinity};
        if (!isActiveAndEnabled || onChoose == null) return false;
        UpdatePositions();
        bool found = false;
        float bestScore = float.PositiveInfinity;
        foreach (var marker in markers)
        {
            if (marker.Object == null || marker.Anchor == null) continue;
            var plane = new Plane(Vector3.up, marker.Object.transform.position);
            float distance;
            if (!plane.Raycast(ray,out distance)) continue;
            Vector3 delta = ray.GetPoint(distance)-marker.Object.transform.position;
            if (delta.sqrMagnitude > marker.Radius*marker.Radius) continue;
            float score = delta.sqrMagnitude / (marker.Radius*marker.Radius);
            if(found && (distance > hit.Distance + 0.001f || (Mathf.Abs(distance-hit.Distance)<=0.001f && score>=bestScore))) continue;
            hit = new Hit {Move=marker.Move,Distance=distance,Generation=generation};
            bestScore=score;
            found = true;
        }
        return found;
    }
    // A tall piece may hide its floor circle. Clicking its body still chooses that node.
    public bool TryGetNodeHit(int node, out Hit hit)
    {
        hit = new Hit();
        if(!isActiveAndEnabled || onChoose==null) return false;
        foreach(var marker in markers)
            if(marker.Move.NodeId==node)
            { hit=new Hit {Move=marker.Move,Generation=generation}; return true; }
        return false;
    }
    public void Choose(Hit hit)
    {
        if (hit.Generation != generation || onChoose == null || !isActiveAndEnabled) return;
        bool valid = markers.Exists(m=>YutMoveOptions.Matches(m.Move,hit.Move));
        if (!valid) return;
        var callback = onChoose;
        Clear();
        callback(hit.Move);
    }
    private void OnDisable() { Clear(); }
    private void OnDestroy()
    {
        Clear();
        if (material != null) Destroy(material);
        if (quad != null) Destroy(quad);
    }
}
