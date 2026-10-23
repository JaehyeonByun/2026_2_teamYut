using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class YutPieceOutline : MonoBehaviour
{
    [SerializeField, Min(0.0001f)] private float outlineWidth = 0.002f;
    [SerializeField, Range(0f,1f)] private float hoverAlpha = 0.5f;
    [SerializeField, Min(0f)] private float brightness = 2f;
    [SerializeField, Min(0.1f)] private float widthBoost = 1.5f;
    [SerializeField, Range(0f,1f)] private float surfaceGlowStrength = 0.3f;
    private Material material;
    private Material glowMaterial;
    private readonly List<Part> parts = new List<Part>();
    private readonly List<Mesh> ownedMeshes = new List<Mesh>();
    private BoxCollider generatedCollider;
    private sealed class Part { public Renderer Source; public Renderer Outline; public Renderer Glow; }

    public bool Initialize()
    {
        if (material != null) return parts.Count > 0;
        var shader = Resources.Load<Shader>("YutInteraction/PieceOutline");
        if (shader == null || !shader.isSupported)
        { Debug.LogError("PieceOutline shader missing/unsupported. Import Resources/YutInteraction shaders.",this); return false; }
        var glowShader=Resources.Load<Shader>("YutInteraction/PieceGlow");
        if(glowShader==null || !glowShader.isSupported)
        { Debug.LogError("PieceGlow shader missing/unsupported. Import Resources/YutInteraction/PieceGlow.shader.",this); return false; }
        glowMaterial=new Material(glowShader) {name="Yut piece surface glow (runtime)"};
        material = new Material(shader) {name="Yut piece outline (runtime)"};
        Renderer[] sources = GetComponentsInChildren<Renderer>(true);
        foreach (var source in sources)
        {
            Mesh mesh = null;
            var skinned = source as SkinnedMeshRenderer;
            var filter = source.GetComponent<MeshFilter>();
            if (skinned != null) mesh = skinned.sharedMesh;
            else if (source is MeshRenderer && filter != null) mesh = filter.sharedMesh;
            if (mesh == null) continue;
            mesh = SmoothOutlineMesh(mesh);
            var obj = new GameObject("Selection outline (runtime)");
            obj.layer = source.gameObject.layer;
            obj.transform.SetParent(source.transform,false);
            Renderer outline;
            if (skinned != null)
            {
                var target = obj.AddComponent<SkinnedMeshRenderer>();
                target.sharedMesh = mesh;
                target.bones = skinned.bones;
                target.rootBone = skinned.rootBone;
                target.localBounds = skinned.localBounds;
                target.updateWhenOffscreen = true;
                outline = target;
            }
            else
            {
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                outline = obj.AddComponent<MeshRenderer>();
            }
            var materials = new Material[mesh.subMeshCount];
            for (int i=0;i<materials.Length;i++) materials[i]=material;
            outline.sharedMaterials = materials;
            outline.shadowCastingMode = ShadowCastingMode.Off;
            outline.receiveShadows = false;
            outline.enabled = false;
            var glowObject=Instantiate(obj,source.transform,false);
            glowObject.name="Selection surface glow (runtime)";
            var glow=glowObject.GetComponent<Renderer>();
            var glowMaterials=new Material[mesh.subMeshCount];
            for(int i=0;i<glowMaterials.Length;i++) glowMaterials[i]=glowMaterial;
            glow.sharedMaterials=glowMaterials;
            glow.enabled=false;
            parts.Add(new Part {Source=source,Outline=outline,Glow=glow});
        }
        EnsureCollider(sources);
        if (parts.Count==0) Debug.LogError("Piece needs a MeshRenderer or SkinnedMeshRenderer for outlining.",this);
        return parts.Count > 0;
    }
    private Mesh SmoothOutlineMesh(Mesh source)
    {
        if (!source.isReadable) return source;
        var vertices = source.vertices;
        var normals = source.normals;
        if (vertices.Length!=normals.Length) return source;
        // Average duplicated vertex normals only on our clone, so caps/seams stay closed.
        var sums = new Dictionary<Vector3,Vector3>();
        for (int i=0;i<vertices.Length;i++)
        { Vector3 sum; sums.TryGetValue(vertices[i],out sum); sums[vertices[i]]=sum+normals[i]; }
        for (int i=0;i<vertices.Length;i++) normals[i]=sums[vertices[i]].normalized;
        var clone = Instantiate(source);
        clone.name = source.name+" (outline clone)";
        clone.normals = normals;
        ownedMeshes.Add(clone);
        return clone;
    }
    private void EnsureCollider(Renderer[] sources)
    {
        foreach(var collider in GetComponentsInChildren<Collider>(true))
            if(collider.enabled && collider.gameObject.activeInHierarchy) return;
        bool hasBounds = false;
        Bounds bounds = new Bounds();
        foreach(var source in sources)
        {
            if(!(source is MeshRenderer) && !(source is SkinnedMeshRenderer)) continue;
            if(!source.enabled || !source.gameObject.activeInHierarchy) continue;
            Bounds world = source.bounds;
            for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
            {
                Vector3 corner = world.center+Vector3.Scale(world.extents,new Vector3(x,y,z));
                Vector3 local = transform.InverseTransformPoint(corner);
                if(!hasBounds){bounds=new Bounds(local,Vector3.zero);hasBounds=true;}
                else bounds.Encapsulate(local);
            }
        }
        if(!hasBounds) return;
        generatedCollider = gameObject.AddComponent<BoxCollider>();
        generatedCollider.center = bounds.center;
        generatedCollider.size = bounds.size;
        generatedCollider.isTrigger = true; // Pick-only; does not change piece/board physics.
    }
    public void SetState(bool hovered, bool selected, float pulseAlpha=1f)
    {
        if(material==null) return;
        material.SetFloat("_Width",outlineWidth * widthBoost);
        float alpha=Mathf.Clamp01(pulseAlpha)*(selected?1f:hoverAlpha);
        material.SetColor("_Color",new Color(brightness,brightness,brightness,alpha));
        if(glowMaterial!=null) glowMaterial.SetColor("_Color",new Color(brightness,brightness,brightness,alpha*surfaceGlowStrength));
        foreach(var part in parts)
        {
            if(part.Outline==null || part.Source==null) continue;
            part.Outline.enabled = isActiveAndEnabled && (hovered||selected) && part.Source.enabled;
            if(part.Glow!=null) part.Glow.enabled=part.Outline.enabled;
            var source=part.Source as SkinnedMeshRenderer;
            var outline=part.Outline as SkinnedMeshRenderer;
            if(source!=null && outline!=null && source.sharedMesh!=null)
                for(int i=0;i<source.sharedMesh.blendShapeCount;i++)
                {
                    outline.SetBlendShapeWeight(i,source.GetBlendShapeWeight(i));
                    var glow=part.Glow as SkinnedMeshRenderer;
                    if(glow!=null) glow.SetBlendShapeWeight(i,source.GetBlendShapeWeight(i));
                }
        }
    }
    private void OnDisable(){SetState(false,false);}
    private void OnDestroy()
    {
        foreach(var part in parts)
        {
            if(part.Outline!=null) Destroy(part.Outline.gameObject);
            if(part.Glow!=null) Destroy(part.Glow.gameObject);
        }
        foreach(var mesh in ownedMeshes) if(mesh!=null) Destroy(mesh);
        if(generatedCollider!=null) Destroy(generatedCollider);
        if(material!=null) Destroy(material);
        if(glowMaterial!=null) Destroy(glowMaterial);
    }
}
