using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HanokInterior.Editor {
[InitializeOnLoad]
public static class HanokInteriorSetup {
    [Serializable] public class Batch { public Part[] meshes; }
    [Serializable] public class Part {
        public string name, material;
        public Vector3[] vertices, normals;
        public Vector2[] uv;
        public int[] triangles;
    }
    static HanokInteriorSetup() { EditorApplication.delayCall += AutoBuild; }
    static string Root() {
        foreach (string guid in AssetDatabase.FindAssets("HanokInteriorSetup t:MonoScript")) {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            const string suffix = "/Editor/HanokInteriorSetup.cs";
            if (p.EndsWith(suffix, StringComparison.Ordinal)) return p.Substring(0, p.Length-suffix.Length);
        }
        throw new FileNotFoundException("HanokInteriorSetup.cs not found");
    }
    static void AutoBuild() {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoBuild; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try { if (!File.Exists(Root()+"/Generated/setup_v1.txt")) Build(); }
        catch (Exception e) { Debug.LogException(e); }
    }
    static T Get<T>(string path, Func<T> create) where T:UnityEngine.Object {
        T value=AssetDatabase.LoadAssetAtPath<T>(path);
        if (value!=null) return value;
        value=create(); AssetDatabase.CreateAsset(value,path); return value;
    }
    static void Folder(string path) {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent=path.Substring(0,path.LastIndexOf('/'));
        Folder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    [MenuItem("Tools/Hanok Interior/Create Missing Assets")]
    public static void Build() {
        string root=Root(), dst=root+"/Generated";
        Folder(dst); Folder(dst+"/Meshes"); Folder(dst+"/Materials"); Folder(dst+"/Textures");
        var batch=JsonUtility.FromJson<Batch>(File.ReadAllText(root+"/Source/RoomMeshes.json"));
        Shader shader;
        if (GraphicsSettings.currentRenderPipeline==null) shader=Shader.Find("Standard");
        else if (GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRender")) shader=Shader.Find("HDRP/Lit");
        else shader=Shader.Find("Universal Render Pipeline/Lit");
        if (shader==null) throw new InvalidOperationException("Supported Lit shader not found. Use Built-in or URP.");
        var materials=new Dictionary<string,Material>();
        foreach (string name in new[]{"Timber","Floorboards","Plaster","Hanji"}) {
            string n=name;
            var texture=Get<Texture2D>(dst+"/Textures/"+n+".asset",()=> {
                var t=new Texture2D(2,2,TextureFormat.RGBA32,true,false); t.name=n;
                if (!ImageConversion.LoadImage(t,File.ReadAllBytes(root+"/Source/"+n+".png.bytes"))) throw new InvalidDataException(n);
                t.wrapMode=TextureWrapMode.Repeat; t.filterMode=FilterMode.Trilinear; t.anisoLevel=8; return t;
            });
            materials[n]=Get<Material>(dst+"/Materials/"+n+".mat",()=> {
                var m=new Material(shader); m.name=n;
                if(m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap",texture);
                if(m.HasProperty("_MainTex")) m.SetTexture("_MainTex",texture);
                if(m.HasProperty("_BaseColorMap")) m.SetTexture("_BaseColorMap",texture);
                if(m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",Color.white);
                if(m.HasProperty("_Color")) m.SetColor("_Color",Color.white);
                if(m.HasProperty("_Metallic")) m.SetFloat("_Metallic",0);
                if(m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",0.15f);
                if(m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness",0.15f);
                return m;
            });
        }
        string prefabPath=dst+"/HanokInterior.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)==null) {
            var room=new GameObject("HanokInterior");
            try {
                foreach(Part part in batch.meshes) {
                    Part p=part;
                    var mesh=Get<Mesh>(dst+"/Meshes/"+p.name+".asset",()=> {
                        var m=new Mesh(); m.name=p.name; m.vertices=p.vertices; m.normals=p.normals; m.uv=p.uv; m.triangles=p.triangles;
                        m.RecalculateBounds(); m.RecalculateTangents(); Unwrapping.GenerateSecondaryUVSet(m); return m;
                    });
                    var go=new GameObject(p.name); go.transform.SetParent(room.transform,false); go.isStatic=true;
                    go.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=materials[p.material]; renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
                    if(p.name=="Structural_Timber" || p.name=="Main_Crossbeams" || p.name=="Ceiling_Boards" || p.name=="Roof_Rafters" || p.name=="Roof_Purlins") go.AddComponent<MeshCollider>().sharedMesh=mesh;
                }
                var collision=new GameObject("Collision"); collision.transform.SetParent(room.transform,false); collision.isStatic=true;
                Box(collision,new Vector3(0,-.12f,0),new Vector3(12,.24f,16));
                Box(collision,new Vector3(-6.1f,1.75f,0),new Vector3(.2f,3.5f,16.4f));
                Box(collision,new Vector3(6.1f,1.75f,0),new Vector3(.2f,3.5f,16.4f));
                Box(collision,new Vector3(0,2.8f,-8.1f),new Vector3(12.4f,5.6f,.2f));
                Box(collision,new Vector3(0,2.8f,8.1f),new Vector3(12.4f,5.6f,.2f));
                var center=new GameObject("RoomCenter"); center.transform.SetParent(room.transform,false);
                PrefabUtility.SaveAsPrefabAsset(room,prefabPath);
            } finally { UnityEngine.Object.DestroyImmediate(room); }
        }
        string scenePath=dst+"/HanokInterior_Preview.unity";
        if(!File.Exists(scenePath)) MakePreview(prefabPath,scenePath);
        AssetDatabase.SaveAssets();
        File.WriteAllText(dst+"/setup_v1.txt","Hanok Interior assets created. Existing assets are preserved.");
        AssetDatabase.Refresh();
        Debug.Log("Hanok Interior ready: "+prefabPath+". Open HanokInterior_Preview.unity to view.");
    }
    static void Box(GameObject go,Vector3 center,Vector3 size) { var c=go.AddComponent<BoxCollider>(); c.center=center; c.size=size; }
    static void MakePreview(string prefabPath,string scenePath) {
        Scene previous=SceneManager.GetActiveScene();
        Scene preview=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try {
            SceneManager.SetActiveScene(preview);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),preview);
            var camObject=new GameObject("Main Camera"); camObject.tag="MainCamera";
            var cam=camObject.AddComponent<Camera>(); camObject.AddComponent<AudioListener>();
            cam.transform.position=new Vector3(3.4f,1.8f,-6.5f); cam.transform.LookAt(new Vector3(-.8f,2.65f,5.5f));
            cam.fieldOfView=57; cam.nearClipPlane=.05f; cam.farClipPlane=100; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.18f,.16f,.13f);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.5f,.46f,.39f);
            var lighting=new GameObject("Preview Lighting - adjust for your game");
            foreach(float x in new[]{-3.8f,3.8f}) foreach(float z in new[]{-5f,0f,5f}) {
                var go=new GameObject("Warm fill"); go.transform.SetParent(lighting.transform); go.transform.position=new Vector3(x,2.9f,z);
                var light=go.AddComponent<Light>(); light.type=LightType.Point; light.color=new Color(1,.91f,.78f); light.intensity=1.4f; light.range=9; light.shadows=LightShadows.None;
            }
            EditorSceneManager.SaveScene(preview,scenePath);
        } finally {
            if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(preview,true);
        }
    }
}
}
