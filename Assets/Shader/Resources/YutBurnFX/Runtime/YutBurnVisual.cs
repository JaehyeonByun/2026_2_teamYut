using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class YutBurnVisual : MonoBehaviour
{
    [Header("Direct shader references (optional if bundled Resources are present)")]
    [SerializeField] private Shader burnShader;
    [SerializeField] private Shader outlineShader;
    [Tooltip("Optional: explicitly assign only the actual model renderers, not highlight copies.")]
    [SerializeField] private Renderer[] targetRenderers;
    [Header("Effect settings")]
    [Min(.05f)] public float disappearSeconds = .55f;
    [Min(.05f)] public float appearSeconds = .45f;
    [Min(0)] public float hiddenSeconds = .10f;
    [Min(0)] public float outlineWidth = .0012f;
    [ColorUsage(false,true)] public Color outlineColor = new Color(1.8f,1.55f,1.1f,1);
    [ColorUsage(false,true)] public Color fireColor = new Color(4f,.65f,.04f,1);
    [Range(.01f,.25f)] public float fireEdgeWidth = .08f;
    public bool alwaysOutline;
    public bool IsBurning { get; private set; }
    class Part
    {
        public Renderer source, outline;
        public Material[] original, burn;
        public Mesh outlineMesh;
    }
    readonly List<Part> parts = new List<Part>();
    Material outlineMaterial;
    bool initialized, swapped;
    Vector3 boundsMin, boundsSize;
    bool warned;
    bool burnAvailable;
    public static YutBurnVisual GetOrAdd(GameObject target)
    {
        var fx = target.GetComponent<YutBurnVisual>();
        return fx ? fx : target.AddComponent<YutBurnVisual>();
    }
    bool Initialize()
    {
        if (initialized) return parts.Count > 0;
        if (!burnShader) burnShader = Resources.Load<Shader>("YutFX2/Burn");
        if (!outlineShader) outlineShader = Resources.Load<Shader>("YutFX2/Outline");
        if (!burnShader) burnShader = Shader.Find("YutFX2/Burn");
        if (!outlineShader) outlineShader = Shader.Find("YutFX2/Outline");
        burnAvailable = burnShader && burnShader.isSupported;
        bool outlineAvailable = outlineShader && outlineShader.isSupported;
        if ((!burnAvailable || !outlineAvailable) && !warned)
        {
            warned = true;
            Debug.LogWarning("[Yut FX2] " + name + ": burn=" + ShaderStatus(burnShader) +
                ", outline=" + ShaderStatus(outlineShader) +
                ". Assign Burn.shader and Outline.shader directly on YutBurnVisual; inspect red Shader errors if unsupported.",this);
        }
        if (!burnAvailable && !outlineAvailable) return false;
        initialized = true;
        if(outlineAvailable) outlineMaterial = new Material(outlineShader) { name = "Yut outline (runtime)" };
        bool hasBounds = false; Bounds bounds = new Bounds();
        var candidates = targetRenderers != null && targetRenderers.Length > 0 ? targetRenderers : GetComponentsInChildren<Renderer>(true);
        var seen = new HashSet<Renderer>();
        foreach (var source in candidates)
        {
            if (!source || !seen.Add(source) || !source.enabled || !source.gameObject.activeSelf || IsProxy(source)) continue;
            var mf = source.GetComponent<MeshFilter>();
            var skin = source as SkinnedMeshRenderer;
            Mesh mesh = skin ? skin.sharedMesh : (mf ? mf.sharedMesh : null);
            if (!mesh) continue;
            Bounds mb = mesh.bounds;
            for (int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
            {
                Vector3 p = transform.InverseTransformPoint(source.transform.TransformPoint(mb.center + Vector3.Scale(mb.extents,new Vector3(x,y,z))));
                if (!hasBounds) { bounds = new Bounds(p,Vector3.zero); hasBounds = true; } else bounds.Encapsulate(p);
            }
            var original = source.sharedMaterials;
            var mats = new Material[burnAvailable ? original.Length : 0];
            for(int i=0;i<mats.Length;i++)
            {
                mats[i] = new Material(burnShader) { name = "Yut burn (runtime)" };
                var old = original[i];
                if (!old) continue;
                string tex = old.HasProperty("_BaseMap") ? "_BaseMap" : old.HasProperty("_MainTex") ? "_MainTex" : old.HasProperty("_BaseColorMap") ? "_BaseColorMap" : null;
                if (tex != null)
                {
                    mats[i].SetTexture("_BaseMap",old.GetTexture(tex));
                    mats[i].SetTextureScale("_BaseMap",old.GetTextureScale(tex));
                    mats[i].SetTextureOffset("_BaseMap",old.GetTextureOffset(tex));
                }
                Color c = old.HasProperty("_BaseColor") ? old.GetColor("_BaseColor") : old.HasProperty("_Color") ? old.GetColor("_Color") : Color.white;
                mats[i].SetColor("_BaseColor",c);
            }
            Mesh smooth = SmoothMesh(mesh);
            var child = new GameObject("Yut burn outline (runtime)"); child.layer = source.gameObject.layer;
            child.transform.SetParent(source.transform,false);
            Renderer outline;
            if (skin)
            {
                var s = child.AddComponent<SkinnedMeshRenderer>(); s.sharedMesh = smooth;
                s.bones=skin.bones; s.rootBone=skin.rootBone; s.localBounds=skin.localBounds; outline=s;
            }
            else { child.AddComponent<MeshFilter>().sharedMesh=smooth; outline=child.AddComponent<MeshRenderer>(); }
            var om = new Material[smooth.subMeshCount]; for(int i=0;i<om.Length;i++) om[i]=outlineMaterial;
            outline.sharedMaterials=om; outline.enabled=false; outline.shadowCastingMode=ShadowCastingMode.Off; outline.receiveShadows=false;
            parts.Add(new Part {source=source,outline=outline,original=original,burn=mats,outlineMesh=smooth == mesh ? null : smooth});
        }
        boundsMin=bounds.min; boundsSize=bounds.size;
        boundsSize=new Vector3(Mathf.Max(.0001f,boundsSize.x),Mathf.Max(.0001f,boundsSize.y),Mathf.Max(.0001f,boundsSize.z));
        if(parts.Count == 0) Debug.LogWarning("[Yut FX2] No active model Renderer found below " + name + ". Attach to the piece/stick root, or assign Target Renderers.",this);
        return parts.Count > 0;
    }
    static string ShaderStatus(Shader shader)
    { return !shader ? "MISSING" : shader.isSupported ? "OK ("+shader.name+")" : "UNSUPPORTED ("+shader.name+")"; }

    [ContextMenu("FX2/Check Connections (Play Mode)")]
    void CheckConnections()
    {
        if(!Application.isPlaying) { Debug.Log("Enter Play mode to inspect runtime renderers.",this); return; }
        Initialize();
        Debug.Log("[Yut FX2] " + name + ": " + parts.Count + " renderer(s), burn="+ShaderStatus(burnShader)+", outline="+ShaderStatus(outlineShader),this);
    }
    [ContextMenu("FX2/Preview Half Burn (Play Mode)")]
    void PreviewHalfBurn()
    {
        if(!Application.isPlaying) { Debug.Log("Enter Play mode; preview while gameplay is idle.",this); return; }
        if(IsBurning) { Debug.Log("An effect is already running. Wait or reset the match.",this); return; }
        if(BeginBurn()) SetProgress(.5f);
    }
    [ContextMenu("FX2/Restore Preview")]
    void RestorePreview() { Restore(); }
    public static bool IsProxy(Renderer r)
    {
        return r.name.StartsWith("Selection outline") || r.name.StartsWith("Selection surface glow") || r.name.StartsWith("Yut burn outline");
    }
    static Mesh SmoothMesh(Mesh source)
    {
        if (!source.isReadable) return source;
        var v=source.vertices; var n=source.normals; if(v.Length!=n.Length) return source;
        var sums=new Dictionary<Vector3,Vector3>();
        for(int i=0;i<v.Length;i++) { Vector3 s; sums.TryGetValue(v[i],out s); sums[v[i]]=s+n[i]; }
        for(int i=0;i<v.Length;i++) n[i]=sums[v[i]].normalized;
        var copy=Instantiate(source); copy.name=source.name+" (burn outline)"; copy.normals=n; return copy;
    }
    public void SetOutline(bool value) { alwaysOutline=value; Initialize(); UpdateOutline(); }
    public bool BeginBurn()
    {
        if (!Initialize() || !burnAvailable) return false;
        if (!swapped)
        {
            foreach(var p in parts) if(p.source) { p.original=p.source.sharedMaterials; p.source.sharedMaterials=p.burn; }
            swapped=true;
        }
        IsBurning=true; SetProgress(0); UpdateOutline(); return true;
    }
    public void SetProgress(float value)
    {
        foreach(var p in parts) foreach(var m in p.burn)
        {
            m.SetFloat("_Progress",Mathf.Clamp01(value)); m.SetFloat("_EdgeWidth",fireEdgeWidth);
            m.SetColor("_FireColor",fireColor); m.SetVector("_BoundsMin",boundsMin); m.SetVector("_BoundsSize",boundsSize);
            m.SetMatrix("_WorldToEffect",transform.worldToLocalMatrix);
        }
    }
    public IEnumerator BurnAway()
    {
        if(!BeginBurn()) yield break;
        float d=Mathf.Max(.05f,disappearSeconds);
        for(float t=0;t<d;t+=Time.deltaTime) { SetProgress(t/d); yield return null; }
        SetProgress(1); // Stay invisible until teleport/reset.
    }
    public IEnumerator BurnIn()
    {
        if(!swapped && !BeginBurn()) yield break;
        SetProgress(1);
        float d=Mathf.Max(.05f,appearSeconds);
        for(float t=0;t<d;t+=Time.deltaTime) { SetProgress(1-t/d); yield return null; }
        Restore();
    }
    public void Restore()
    {
        if(swapped) foreach(var p in parts) if(p.source) p.source.sharedMaterials=p.original;
        swapped=false; IsBurning=false; UpdateOutline();
    }
    void UpdateOutline()
    {
        if(outlineMaterial) { outlineMaterial.SetFloat("_Width",outlineWidth); outlineMaterial.SetColor("_Color",outlineColor); }
        foreach(var p in parts) if(p.outline) p.outline.enabled=isActiveAndEnabled && alwaysOutline && outlineMaterial && !IsBurning && p.source && p.source.enabled && p.source.gameObject.activeInHierarchy;
    }
    void LateUpdate() { UpdateOutline(); }
    void OnDisable() { Restore(); }
    void OnDestroy()
    {
        Restore();
        foreach(var p in parts) { foreach(var m in p.burn) if(m) Destroy(m); if(p.outline) Destroy(p.outline.gameObject); if(p.outlineMesh) Destroy(p.outlineMesh); }
        if(outlineMaterial) Destroy(outlineMaterial);
    }
}
