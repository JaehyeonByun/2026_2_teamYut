using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace DokkaebiTokenAsset {
[InitializeOnLoad]
public static class DokkaebiTokenSetup {
    [Serializable] public class Batch { public Part[] meshes; }
    [Serializable] public class Part { public string name, material; public Vector3[] vertices,normals; public Vector2[] uv; public int[] triangles; }
    static DokkaebiTokenSetup() { EditorApplication.delayCall+=AutoBuild; }
    static string Root() {
        foreach(string guid in AssetDatabase.FindAssets("DokkaebiTokenSetup t:MonoScript")) {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            const string suffix="/Editor/DokkaebiTokenSetup.cs";
            if(path.EndsWith(suffix,StringComparison.Ordinal)) return path.Substring(0,path.Length-suffix.Length);
        }
        throw new FileNotFoundException("DokkaebiTokenSetup.cs not found");
    }
    static void AutoBuild() {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall+=AutoBuild; return; }
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        try { if(!File.Exists(Root()+"/Generated/DokkaebiToken.prefab")) Build(); }
        catch(Exception e) { Debug.LogException(e); }
    }
    static T Get<T>(string path,Func<T> create) where T:UnityEngine.Object {
        T value=AssetDatabase.LoadAssetAtPath<T>(path); if(value!=null) return value;
        value=create(); AssetDatabase.CreateAsset(value,path); return value;
    }
    [MenuItem("Tools/Dokkaebi Token/Create Prefab")]
    public static void Build() {
        string root=Root(),dst=root+"/Generated";
        if(!AssetDatabase.IsValidFolder(dst)) AssetDatabase.CreateFolder(root,"Generated");
        Shader shader=Shader.Find(GraphicsSettings.currentRenderPipeline==null?"Standard":"Universal Render Pipeline/Lit");
        if(shader==null) throw new InvalidOperationException("Use Built-in or URP rendering pipeline.");
        var texture=Get<Texture2D>(dst+"/WoodTexture.asset",()=> {
            var t=new Texture2D(2,2,TextureFormat.RGBA32,true,false);t.name="Dokkaebi Wood";
            if(!ImageConversion.LoadImage(t,File.ReadAllBytes(root+"/Source/Timber.png.bytes"))) throw new InvalidDataException("Wood texture decoding failed");
            t.wrapMode=TextureWrapMode.Repeat;t.filterMode=FilterMode.Trilinear;return t;
        });
        var material=Get<Material>(dst+"/Wood.mat",()=> {
            var m=new Material(shader);m.name="Dokkaebi Wood";
            if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",texture);
            if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",texture);
            if(m.HasProperty("_Color"))m.SetColor("_Color",Color.white);
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",Color.white);
            if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.13f);
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.13f);
            return m;
        });
        var mesh=Get<Mesh>(dst+"/DokkaebiMesh.asset",()=> {
            var data=JsonUtility.FromJson<Batch>(File.ReadAllText(root+"/Source/Meshes.json"));
            var combines=new CombineInstance[data.meshes.Length];
            try {
                for(int i=0;i<data.meshes.Length;i++) {
                    var p=data.meshes[i];var m=new Mesh();m.vertices=p.vertices;m.normals=p.normals;m.uv=p.uv;m.triangles=p.triangles;
                    combines[i].mesh=m;combines[i].transform=Matrix4x4.identity;
                }
                var combined=new Mesh();combined.name="DokkaebiToken";combined.CombineMeshes(combines,true,false);combined.RecalculateBounds();combined.RecalculateTangents();return combined;
            } finally { foreach(var c in combines) if(c.mesh!=null) UnityEngine.Object.DestroyImmediate(c.mesh); }
        });
        string path=dst+"/DokkaebiToken.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null) {
            var go=new GameObject("DokkaebiToken");
            try {
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
                var collider=go.AddComponent<CapsuleCollider>();collider.center=new Vector3(0,.147f,0);collider.radius=.115f;collider.height=.294f;
                PrefabUtility.SaveAsPrefabAsset(go,path);
            } finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        EditorGUIUtility.PingObject(Selection.activeObject);Debug.Log("Dokkaebi token ready: "+path);
    }
}
}
