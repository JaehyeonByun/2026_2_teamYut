using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace ToonTwinCandles.Editor {
[InitializeOnLoad]
public static class ToonTwinCandlesSetup {
    [Serializable] public class Batch { public Part[] meshes; }
    [Serializable] public class Part { public string name,material; public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles; }
    static bool building;
    static ToonTwinCandlesSetup(){EditorApplication.delayCall+=AutoBuild;}
    static string Root(){
        foreach(string guid in AssetDatabase.FindAssets("ToonTwinCandlesSetup t:MonoScript")){
            string p=AssetDatabase.GUIDToAssetPath(guid);const string suffix="/Editor/ToonTwinCandlesSetup.cs";
            if(p.EndsWith(suffix,StringComparison.Ordinal))return p.Substring(0,p.Length-suffix.Length);
        }
        throw new FileNotFoundException("ToonTwinCandlesSetup.cs not found.");
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
    [MenuItem("Tools/Toon Twin Candles/Create Assets")]
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
            var colors=new Dictionary<string,Color>{
                {"Wax",new Color(.91f,.79f,.56f)}, {"Wick",new Color(.12f,.09f,.075f)},
                {"Metal",new Color(.20f,.23f,.25f)}, {"Trim",new Color(.53f,.36f,.17f)}
            };
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
            foreach(string prefabName in new[]{"Candle_Short","Candle_Tall","TwinHolder_Empty","TwinCandles"}){
                string path=dst+"/"+prefabName+".prefab";
                if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)continue;
                Scene temp=EditorSceneManager.NewPreviewScene();GameObject go=null;
                try{
                    go=new GameObject(prefabName);SceneManager.MoveGameObjectToScene(go,temp);
                    if(prefabName=="Candle_Short")AddCandle(go,"Short",Vector3.zero,meshes,parts,materials);
                    else if(prefabName=="Candle_Tall")AddCandle(go,"Tall",Vector3.zero,meshes,parts,materials);
                    else {
                        AddMesh(go,"Holder_Metal",meshes,parts,materials);
                        AddMesh(go,"Holder_Trim",meshes,parts,materials);
                        AddBox(go,new Vector3(0,.016f,0),new Vector3(.23f,.032f,.23f));
                        AddBox(go,new Vector3(0,.16f,0),new Vector3(.04f,.28f,.04f));
                        AddBox(go,new Vector3(0,.283f,0),new Vector3(.32f,.026f,.026f));
                        foreach(float x in new[]{-.15f,.15f})AddBox(go,new Vector3(x,.338f,0),new Vector3(.12f,.025f,.12f));
                        if(prefabName=="TwinCandles"){
                            AddCandle(go,"Short",new Vector3(-.15f,.341f,0),meshes,parts,materials);
                            AddCandle(go,"Tall",new Vector3(.15f,.341f,0),meshes,parts,materials);
                        } else {
                            Anchor(go,"CandleSeat_Left",new Vector3(-.15f,.341f,0),false);
                            Anchor(go,"CandleSeat_Right",new Vector3(.15f,.341f,0),false);
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(go,path);
                }finally{
                    if(go!=null)UnityEngine.Object.DestroyImmediate(go);
                    EditorSceneManager.ClosePreviewScene(temp);
                }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(dst+"/setup_v1.txt","ToonTwinCandles generated. Existing assets are preserved.");AssetDatabase.Refresh();
            Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(dst+"/TwinCandles.prefab");EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log("Toon Twin Candles ready. Drag Generated/TwinCandles.prefab into the scene. Attach flames under each FlameSocket.");
        }finally{building=false;}
    }
    static void AddMesh(GameObject parent,string name,Dictionary<string,Mesh> meshes,Dictionary<string,Part> parts,Dictionary<string,Material> materials){
        var child=new GameObject(name);child.transform.SetParent(parent.transform,false);
        child.AddComponent<MeshFilter>().sharedMesh=meshes[name];child.AddComponent<MeshRenderer>().sharedMaterial=materials[parts[name].material];
    }
    static void AddCandle(GameObject parent,string kind,Vector3 position,Dictionary<string,Mesh> meshes,Dictionary<string,Part> parts,Dictionary<string,Material> materials){
        var candle=new GameObject("Candle_"+kind);candle.transform.SetParent(parent.transform,false);candle.transform.localPosition=position;
        AddMesh(candle,kind+"_Wax",meshes,parts,materials);AddMesh(candle,kind+"_Wick",meshes,parts,materials);
        float h=kind=="Short"?.18f:.28f;float radius=kind=="Short"?.035f:.026f;
        var collider=candle.AddComponent<CapsuleCollider>();collider.center=new Vector3(0,h*.5f,0);collider.height=h;collider.radius=radius;
        Anchor(candle,"FlameSocket",new Vector3(0,h+.014f,0),true);
    }
    static void Anchor(GameObject parent,string name,Vector3 position,bool marker){
        var go=new GameObject(name);go.transform.SetParent(parent.transform,false);go.transform.localPosition=position;
        if(marker)go.AddComponent<FlameSocketMarker>();
    }
    static void AddBox(GameObject go,Vector3 center,Vector3 size){var c=go.AddComponent<BoxCollider>();c.center=center;c.size=size;}
}
}
