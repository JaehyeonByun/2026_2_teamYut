using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace SoulFlameTokenAsset {
[InitializeOnLoad]
public static class SoulFlameTokenSetup {
    [Serializable] public class Batch { public Part[] meshes; }
    [Serializable] public class Part { public string name, material; public Vector3[] vertices,normals; public Vector2[] uv; public int[] triangles; }
    static SoulFlameTokenSetup() { EditorApplication.delayCall+=AutoBuild; }
    static string Root() {
        foreach(string guid in AssetDatabase.FindAssets("SoulFlameTokenSetup t:MonoScript")) {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            const string suffix="/Editor/SoulFlameTokenSetup.cs";
            if(path.EndsWith(suffix,StringComparison.Ordinal)) return path.Substring(0,path.Length-suffix.Length);
        }
        throw new FileNotFoundException("SoulFlameTokenSetup.cs not found");
    }
    static void AutoBuild() {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall+=AutoBuild; return; }
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        try { if(!File.Exists(Root()+"/Generated/SoulFlameToken.prefab")) Build(); }
        catch(Exception e) { Debug.LogException(e); }
    }
    static T Get<T>(string path,Func<T> create) where T:UnityEngine.Object {
        T value=AssetDatabase.LoadAssetAtPath<T>(path); if(value!=null) return value;
        value=create(); AssetDatabase.CreateAsset(value,path); return value;
    }
    [MenuItem("Tools/Soul Flame Token/Create Prefab")]
    public static void Build() {
        string root=Root(),dst=root+"/Generated";
        if(!AssetDatabase.IsValidFolder(dst)) AssetDatabase.CreateFolder(root,"Generated");
        Shader shader=Shader.Find(GraphicsSettings.currentRenderPipeline==null?"Standard":"Universal Render Pipeline/Lit");
        if(shader==null) throw new InvalidOperationException("Use Built-in or URP rendering pipeline.");
        var data=JsonUtility.FromJson<Batch>(File.ReadAllText(root+"/Source/Meshes.json"));
        string path=dst+"/SoulFlameToken.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null) {
            var go=new GameObject("SoulFlameToken");
            try {
                foreach(var part in data.meshes) {
                    var p=part;
                    Color color;
                    switch(p.material) {
                        case "Base":color=new Color32(43,62,65,255);break;
                        case "Outer":color=new Color32(24,113,127,255);break;
                        case "Inner":color=new Color32(39,192,184,255);break;
                        default:color=new Color32(185,247,221,255);break;
                    }
                    var mat=Get<Material>(dst+"/"+p.material+".mat",()=> {
                        var m=new Material(shader);m.name=p.material;
                        if(m.HasProperty("_Color"))m.SetColor("_Color",color);
                        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);
                        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.2f);
                        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.2f);
                        return m;
                    });
                    var mesh=Get<Mesh>(dst+"/"+p.name+".asset",()=> {
                        var m=new Mesh();m.name=p.name;m.vertices=p.vertices;m.normals=p.normals;m.uv=p.uv;m.triangles=p.triangles;
                        m.RecalculateBounds();m.RecalculateTangents();return m;
                    });
                    var child=new GameObject(p.name);child.transform.SetParent(go.transform,false);
                    child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>().sharedMaterial=mat;
                }
                var collider=go.AddComponent<BoxCollider>();collider.center=new Vector3(0,.1575f,-.01f);collider.size=new Vector3(.218f,.315f,.15f);
                PrefabUtility.SaveAsPrefabAsset(go,path);
            } finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        EditorGUIUtility.PingObject(Selection.activeObject);Debug.Log("Soul flame token ready: "+path);
    }
}
}
