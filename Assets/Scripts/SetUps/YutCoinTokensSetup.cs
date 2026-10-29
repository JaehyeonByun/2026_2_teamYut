#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace YutCoinTokens.Editor {
[InitializeOnLoad]
public static class YutCoinTokensSetup {
    [Serializable] public class Batch { public Part[] meshes; }
    [Serializable] public class Part { public string name,material; public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles; }
    static bool building;
    static YutCoinTokensSetup(){EditorApplication.delayCall+=AutoBuild;}
    static string Root()
    {
        const string kitName = "YutCoinTokens";
        string found = null;
        foreach (string guid in AssetDatabase.FindAssets(kitName + " t:Folder", new[] { "Assets" }))
        {
            string folder = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileName(folder) != kitName) continue;
            if (!System.IO.File.Exists(folder + "/Source/Meshes.json")) continue;
            if (found != null)
                throw new System.InvalidOperationException(kitName + " has multiple source folders: " + found + " and " + folder);
            found = folder;
        }
        if (found != null) return found;
        throw new System.IO.DirectoryNotFoundException("Cannot find " + kitName + "/Source/Meshes.json under Assets. Keep the kit folder name and its Source contents together. Script location does not matter.");
    }
    static void AutoBuild(){
        if(EditorApplication.isCompiling || EditorApplication.isUpdating){EditorApplication.delayCall+=AutoBuild;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        try{if(!File.Exists(Root()+"/Generated/setup_v1.txt"))Build();}catch(Exception e){Debug.LogException(e);}
    }
    static T Get<T>(string path,Func<T> create) where T:UnityEngine.Object {
        T value=AssetDatabase.LoadAssetAtPath<T>(path);if(value!=null)return value;
        value=create();AssetDatabase.CreateAsset(value,path);return value;
    }
    [MenuItem("Tools/Yut Coin Tokens/Create Assets")]
    public static void Build(){
        if(building)return;building=true;
        try{
            string root=Root(),dst=root+"/Generated";
            if(!AssetDatabase.IsValidFolder(dst))AssetDatabase.CreateFolder(root,"Generated");
            bool urp=GraphicsSettings.currentRenderPipeline!=null;
            if(urp && !GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal"))throw new InvalidOperationException("This kit targets Built-in or URP; HDRP needs custom materials.");
            string shaderPath=dst+(urp?"/CandleToonURP.shader":"/CandleToonBuiltIn.shader");
            if(!File.Exists(shaderPath)){
                File.Copy(root+"/Source/"+(urp?"ToonURP.shader.txt":"ToonBuiltIn.shader.txt"),shaderPath);
                AssetDatabase.ImportAsset(shaderPath,ImportAssetOptions.ForceSynchronousImport);
            }
            Shader shader=AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if(shader==null)throw new InvalidOperationException("Toon shader did not import. Check Console.");
            if(ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Toon shader has a compilation error. Check Console and your Unity/URP version.");
            var colors=new Dictionary<string,Color>{{"AllyBody",new Color(0.045f,0.19f,0.23f)},{"AllyRim",new Color(0.16f,0.66f,0.73f)},{"AllyFace",new Color(0.07f,0.3f,0.36f)},{"AllySymbol",new Color(0.48f,0.96f,0.94f)},{"EnemyBody",new Color(0.24f,0.065f,0.045f)},{"EnemyRim",new Color(0.87f,0.3f,0.075f)},{"EnemyFace",new Color(0.39f,0.1f,0.055f)},{"EnemySymbol",new Color(1f,0.68f,0.2f)}};
            var materials=new Dictionary<string,Material>();
            foreach(var kv in colors){string name=kv.Key;Color color=kv.Value;
                materials[name]=Get<Material>(dst+"/"+name+".mat",()=>{
                    var m=new Material(shader);m.name=name;
                    if(m.HasProperty("_Color"))m.SetColor("_Color",color);
                    if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);
                    return m;
                });
            }
            var data=JsonUtility.FromJson<Batch>(File.ReadAllText(root+"/Source/Meshes.json"));
            var meshes=new Dictionary<string,Mesh>();var parts=new Dictionary<string,Part>();
            foreach(Part part in data.meshes){Part p=part;parts[p.name]=p;
                meshes[p.name]=Get<Mesh>(dst+"/"+p.name+".asset",()=>{
                    var m=new Mesh();m.name=p.name;m.vertices=p.vertices;m.normals=p.normals;m.uv=p.uv;m.triangles=p.triangles;
                    m.RecalculateBounds();m.RecalculateTangents();return m;
                });
            }
            foreach(string team in new[]{"Ally","Enemy"}){
                string path=dst+"/"+team+"_Coin.prefab";
                if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)continue;
                Scene temp=EditorSceneManager.NewPreviewScene();GameObject go=null;
                try{
                    go=new GameObject(team+"_Coin");SceneManager.MoveGameObjectToScene(go,temp);
                    var visual=new GameObject("Visual");visual.transform.SetParent(go.transform,false);
                    foreach(Part p in data.meshes)if(p.name.StartsWith(team+"_"))AddMesh(visual,p.name,meshes,parts,materials);
                    var collider=go.AddComponent<BoxCollider>();collider.center=new Vector3(0,.008f,0);collider.size=new Vector3(.094f,.016f,.094f);
                    var socket=new GameObject("StackSocket");socket.transform.SetParent(go.transform,false);socket.transform.localPosition=new Vector3(0,.017f,0);
                    PrefabUtility.SaveAsPrefabAsset(go,path);
                }finally{
                    if(go!=null)UnityEngine.Object.DestroyImmediate(go);
                    EditorSceneManager.ClosePreviewScene(temp);
                }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(dst+"/setup_v1.txt","YutCoinTokens generated. Existing assets are preserved.");AssetDatabase.Refresh();
            Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(dst+"/Ally_Coin.prefab");EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log("Yut Coin Tokens ready. Drag Generated/Ally_Coin.prefab into the scene.");
        }finally{building=false;}
    }
    static void AddMesh(GameObject parent,string name,Dictionary<string,Mesh> meshes,Dictionary<string,Part> parts,Dictionary<string,Material> materials){
        var child=new GameObject(name);child.transform.SetParent(parent.transform,false);
        child.AddComponent<MeshFilter>().sharedMesh=meshes[name];child.AddComponent<MeshRenderer>().sharedMaterial=materials[parts[name].material];
    }
}
}

#endif
