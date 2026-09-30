using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace YutLowPoly.Editor
{
    [InitializeOnLoad]
    public static class YutAssetSetup
    {
        const string Root = "Assets/YutLowPoly";
        const string Gen = Root + "/Generated";
        const string Done = Gen + "/setup_v1.txt";
        [Serializable] class MeshData { public Vector3[] vertices; public Vector3[] normals; public Vector2[] uv; public int[] triangles; }
        static YutAssetSetup() { EditorApplication.delayCall += AutoSetup; }
        static void AutoSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoSetup; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(Done)) return;
            if (File.Exists(Root + "/Source/YutMesh.json")) Build();
        }
        static T GetOrCreate<T>(string path, Func<T> create) where T : UnityEngine.Object
        {
            T old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (old != null) return old;
            T asset = create(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        static Shader FindShader()
        {
            var pipe = GraphicsSettings.currentRenderPipeline;
            string shaderName = pipe == null ? "Standard" : pipe.GetType().Name.Contains("HDRender") ? "HDRP/Lit" : "Universal Render Pipeline/Lit";
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Yut: shader unavailable: " + shaderName + ". Use Built-in or URP with a 3D renderer.");
            return shader;
        }
        // Decode the source PNG directly so setup does not depend on its importer state.
        static Texture2D WoodTexture(string textureName)
        {
            string sourcePath = Root + "/Textures/" + textureName + ".png";
            return GetOrCreate(Gen + "/" + textureName + "_Texture.asset", () => {
                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException("Yut: PNG file missing. Restore this file from the ZIP: " + sourcePath, sourcePath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, false);
                try {
                    if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(sourcePath), false))
                        throw new InvalidDataException("Yut: Could not decode PNG: " + sourcePath);
                    tex.name = textureName;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Trilinear;
                    tex.anisoLevel = 2;
                    tex.Apply(true, false);
                    return tex;
                } catch {
                    UnityEngine.Object.DestroyImmediate(tex);
                    throw;
                }
            });
        }
        static Material Wood(string name, string textureName)
        {
            return GetOrCreate(Gen + "/" + name + ".mat", () => {
                var shader = FindShader();
                var tex = WoodTexture(textureName);
                var mat = new Material(shader) { name = name };
                foreach (string p in new[] { "_MainTex", "_BaseMap", "_BaseColorMap" }) if (mat.HasProperty(p)) mat.SetTexture(p, tex);
                foreach (string p in new[] { "_Color", "_BaseColor" }) if (mat.HasProperty(p)) mat.SetColor(p, Color.white);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .18f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .18f);
                return mat;
            });
        }
        static GameObject MakeStick(string name, Mesh mesh, Material mat, bool backDo)
        {
            string path = Gen + "/" + name + ".prefab";
            var old = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (old != null) return old;
            var go = new GameObject(name);
            try {
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                var col = go.AddComponent<MeshCollider>(); col.sharedMesh = mesh; col.convex = true;
                var rb = go.AddComponent<Rigidbody>(); rb.mass = .045f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.solverIterations = 12; rb.solverVelocityIterations = 4;
                go.AddComponent<YutStick>().isBackDo = backDo;
                return PrefabUtility.SaveAsPrefabAsset(go, path);
            } finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [MenuItem("Tools/Yut Low Poly/Create Missing Assets")]
        public static void Build()
        {
            try {
                Directory.CreateDirectory(Gen); AssetDatabase.Refresh();
                var mesh = GetOrCreate(Gen + "/YutMesh.asset", () => {
                    var d = JsonUtility.FromJson<MeshData>(File.ReadAllText(Root + "/Source/YutMesh.json"));
                    var m = new Mesh { name = "Yut_LowPoly_68_Triangles", vertices = d.vertices, uv = d.uv, triangles = d.triangles, normals = d.normals };
                    m.RecalculateBounds(); m.RecalculateTangents(); return m;
                });
                var regular = MakeStick("Yut_Standard", mesh, Wood("Wood", "Yut_Wood"), false);
                var backdo = MakeStick("Yut_BackDo", mesh, Wood("Wood_BackDo", "Yut_BackDo"), true);
                string setPath = Gen + "/Yut_Set_4.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(setPath) == null) {
                    var set = new GameObject("Yut_Set_4");
                    try {
                        for (int i=0;i<4;i++) {
                            var child = (GameObject)PrefabUtility.InstantiatePrefab(i == 3 ? backdo : regular);
                            child.transform.SetParent(set.transform, false);
                            child.transform.localPosition = new Vector3((i-1.5f)*.085f,0,0);
                        }
                        PrefabUtility.SaveAsPrefabAsset(set, setPath);
                    } finally { UnityEngine.Object.DestroyImmediate(set); }
                }
                CreateDemo();
                AssetDatabase.SaveAssets();
                File.WriteAllText(Done, "v1 - Assets created. Tools/Yut Low Poly/Create Missing Assets restores missing assets.\n");
                AssetDatabase.ImportAsset(Done);
                Debug.Log("Yut ready: Assets/YutLowPoly/Generated. Open Yut_Demo and press Play, then TOSS YUT.");
            } catch (Exception ex) { Debug.LogException(ex); }
        }
        static void CreateDemo()
        {
            string path = Gen + "/Yut_Demo.unity"; if (File.Exists(path)) return;
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try {
                SceneManager.SetActiveScene(scene);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Floor";
                floor.transform.position = new Vector3(0,-.045f,0); floor.transform.localScale = new Vector3(4,.09f,4);
                var floorMat = GetOrCreate(Gen + "/Floor.mat", () => {
                    var m = new Material(FindShader()); m.color = new Color(.17f,.23f,.24f); return m;
                }); floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                var cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag = "MainCamera";
                cam.transform.position = new Vector3(.7f,1.3f,-1.3f); cam.transform.LookAt(new Vector3(0,.08f,0));
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.09f,.12f,.14f);
                cam.nearClipPlane = .01f; cam.farClipPlane = 30f; cam.fieldOfView = 50;
                cam.gameObject.AddComponent<AudioListener>();
                var light = new GameObject("Key Light").AddComponent<Light>(); light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(48,-30,0); light.intensity = 1.2f; light.shadows = LightShadows.Soft;
                var set = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Gen + "/Yut_Set_4.prefab"));
                set.transform.position = new Vector3(0,.12f,0);
                var demo = new GameObject("Toss Demo").AddComponent<YutTossDemo>(); demo.sticks = set.GetComponentsInChildren<Rigidbody>();
                if (!EditorSceneManager.SaveScene(scene,path)) throw new IOException("Could not save Yut demo scene.");
            } finally {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene,true);
            }
        }
    }
}
