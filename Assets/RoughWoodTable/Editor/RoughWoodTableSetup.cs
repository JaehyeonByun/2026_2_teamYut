using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoughWoodTableAsset.Editor
{
    // Editor-only installer. The generated table has no runtime scripts or Rigidbody.
    [InitializeOnLoad]
    public static class RoughWoodTableSetup
    {
        [Serializable] class MeshData
        {
            public Vector3[] vertices;
            public Vector3[] normals;
            public Vector2[] uv;
            public int[] triangles;
        }
        static RoughWoodTableSetup() { EditorApplication.delayCall += AutoSetup; }
        static string Root()
        {
            foreach (string guid in AssetDatabase.FindAssets("RoughWoodTableSetup t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/Editor/RoughWoodTableSetup.cs", StringComparison.Ordinal))
                    return path.Substring(0, path.Length - "/Editor/RoughWoodTableSetup.cs".Length);
            }
            return null;
        }
        static void AutoSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += AutoSetup; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string root = Root();
            if (root == null || File.Exists(root + "/Generated/setup_v1.txt")) return;
            if (File.Exists(root + "/Source/TableMesh.json")) Build();
        }
        static T GetOrCreate<T>(string path, Func<T> factory) where T : UnityEngine.Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new IOException("An unreadable asset already exists: " + path);
            T asset = factory();
            try { AssetDatabase.CreateAsset(asset, path); return asset; }
            catch { UnityEngine.Object.DestroyImmediate(asset); throw; }
        }
        static Shader ChooseShader()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            string name = pipeline == null ? "Standard" :
                pipeline.GetType().Name.Contains("HDRender") ? "HDRP/Lit" : "Universal Render Pipeline/Lit";
            var shader = Shader.Find(name);
            if (shader == null) throw new InvalidOperationException("Rough Wood Table: shader unavailable: " + name);
            return shader;
        }
        [MenuItem("Tools/Rough Wood Table/Create Missing Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("Stop Play Mode before creating table assets."); return; }
            try
            {
                string root = Root();
                if (root == null) throw new DirectoryNotFoundException("RoughWoodTableSetup.cs could not be located.");
                string gen = root + "/Generated";
                if (!AssetDatabase.IsValidFolder(gen)) AssetDatabase.CreateFolder(root, "Generated");
                Mesh mesh = GetOrCreate(gen + "/TableMesh.asset", () => {
                    var d = JsonUtility.FromJson<MeshData>(File.ReadAllText(root + "/Source/TableMesh.json"));
                    var m = new Mesh { name = "RoughWoodTable_206tri" };
                    m.vertices = d.vertices; m.triangles = d.triangles;
                    m.normals = d.normals; m.uv = d.uv;
                    m.RecalculateBounds(); m.RecalculateTangents(); return m;
                });
                Texture2D texture = GetOrCreate(gen + "/WoodTexture.asset", () => {
                    string source = root + "/Source/RoughWoodTable_Albedo.png.bytes";
                    if (!File.Exists(source)) throw new FileNotFoundException("Restore the Source folder from the ZIP.", source);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, false);
                    try {
                        if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(source), false))
                            throw new InvalidDataException("Could not decode wood texture.");
                        tex.name = "RoughWood_Albedo"; tex.wrapMode = TextureWrapMode.Clamp;
                        tex.filterMode = FilterMode.Trilinear; tex.anisoLevel = 4;
                        tex.Apply(true, false); return tex;
                    } catch { UnityEngine.Object.DestroyImmediate(tex); throw; }
                });
                Material material = GetOrCreate(gen + "/RoughWood.mat", () => {
                    var mat = new Material(ChooseShader()) { name = "RoughWood" };
                    foreach (string prop in new[] { "_MainTex", "_BaseMap", "_BaseColorMap" })
                        if (mat.HasProperty(prop)) mat.SetTexture(prop, texture);
                    foreach (string prop in new[] { "_Color", "_BaseColor" })
                        if (mat.HasProperty(prop)) mat.SetColor(prop, Color.white);
                    foreach (string prop in new[] { "_Glossiness", "_Smoothness", "_Metallic" })
                        if (mat.HasProperty(prop)) mat.SetFloat(prop, 0f);
                    return mat;
                });
                string prefabPath = gen + "/RoughWoodTable.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    var go = new GameObject("RoughWoodTable");
                    try {
                        go.AddComponent<MeshFilter>().sharedMesh = mesh;
                        go.AddComponent<MeshRenderer>().sharedMaterial = material;
                        var top = go.AddComponent<BoxCollider>();
                        top.center = new Vector3(0, .61f, 0); top.size = new Vector3(1.6f, .08f, .8f);
                        foreach (float x in new[] { -.663f, .663f })
                            foreach (float z in new[] { -.2725f, .2725f })
                            {
                                var leg = go.AddComponent<BoxCollider>();
                                leg.center = new Vector3(x, .285f, z); leg.size = new Vector3(.126f, .57f, .125f);
                            }
                        bool success;
                        PrefabUtility.SaveAsPrefabAsset(go, prefabPath, out success);
                        if (!success) throw new IOException("Failed to save table prefab.");
                    } finally { UnityEngine.Object.DestroyImmediate(go); }
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText(gen + "/setup_v1.txt", "Rough Wood Table created. Tools/Rough Wood Table/Create Missing Assets restores missing assets.\n");
                AssetDatabase.ImportAsset(gen + "/setup_v1.txt");
                Debug.Log("Table ready: " + prefabPath + ". Drag the prefab into your scene.");
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }
}
