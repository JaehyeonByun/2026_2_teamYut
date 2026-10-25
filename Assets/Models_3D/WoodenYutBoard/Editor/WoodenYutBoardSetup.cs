using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace WoodenYutBoardAsset.Editor
{
    [InitializeOnLoad]
    public static class WoodenYutBoardSetup
    {
        [Serializable] class MeshData { public Vector3[] vertices; public Vector3[] normals; public Vector2[] uv; public int[] triangles; }
        [Serializable] class NodeData { public int index; public string name; public string role; public Vector3 position; }
        [Serializable] class NodeList { public NodeData[] nodes; }
        static WoodenYutBoardSetup() { EditorApplication.delayCall += AutoSetup; }
        static string Root()
        {
            const string suffix = "/Editor/WoodenYutBoardSetup.cs";
            foreach (var guid in AssetDatabase.FindAssets("WoodenYutBoardSetup t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(suffix, StringComparison.Ordinal)) return path.Substring(0, path.Length - suffix.Length);
            }
            return null;
        }
        static void AutoSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoSetup; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string root = Root();
            if (root != null && File.Exists(root + "/Source/BoardMesh.json") && !File.Exists(root + "/Generated/setup_v1.txt")) Build();
        }
        static T GetOrCreate<T>(string path, Func<T> factory) where T : UnityEngine.Object
        {
            var old = AssetDatabase.LoadAssetAtPath<T>(path); if (old != null) return old;
            if (File.Exists(path)) throw new IOException("Unreadable existing asset: " + path);
            T asset = factory();
            try { AssetDatabase.CreateAsset(asset, path); return asset; }
            catch { UnityEngine.Object.DestroyImmediate(asset); throw; }
        }
        static Shader FindShader()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            string name = pipeline == null ? "Standard" : pipeline.GetType().Name.Contains("HDRender") ? "HDRP/Lit" : "Universal Render Pipeline/Lit";
            var shader = Shader.Find(name);
            if (shader == null) throw new InvalidOperationException("Yut board shader unavailable: " + name);
            return shader;
        }
        static Texture2D ReadTexture(string path, string name, bool normal)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, normal);
            try {
                if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(path), false)) throw new InvalidDataException("Could not decode " + path);
                tex.name = name; tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Trilinear; tex.anisoLevel = 8;
                tex.Apply(true, false); return tex;
            } catch { UnityEngine.Object.DestroyImmediate(tex); throw; }
        }
        [MenuItem("Tools/Wooden Yut Board/Create Missing Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play Mode before board setup."); return; }
            try {
                string root = Root(); if (root == null) throw new DirectoryNotFoundException("WoodenYutBoardSetup script not found.");
                string gen = root + "/Generated";
                if (!AssetDatabase.IsValidFolder(gen)) AssetDatabase.CreateFolder(root, "Generated");
                var mesh = GetOrCreate(gen + "/BoardMesh.asset", () => {
                    var d = JsonUtility.FromJson<MeshData>(File.ReadAllText(root + "/Source/BoardMesh.json"));
                    var m = new Mesh { name = "WoodenYutBoard_44tri" };
                    m.vertices = d.vertices; m.triangles = d.triangles; m.normals = d.normals; m.uv = d.uv;
                    m.RecalculateBounds(); m.RecalculateTangents(); return m;
                });
                var albedo = GetOrCreate(gen + "/BoardAlbedo.asset", () => ReadTexture(root + "/Source/YutBoard_Albedo.png.bytes", "BoardAlbedo", false));
                var normal = GetOrCreate(gen + "/BoardNormal.asset", () => ReadTexture(root + "/Source/YutBoard_Normal.png.bytes", "BoardNormal", true));
                var material = GetOrCreate(gen + "/EngravedWood.mat", () => {
                    var mat = new Material(FindShader()) { name = "EngravedWood" };
                    foreach (string p in new[] { "_MainTex", "_BaseMap", "_BaseColorMap" }) if (mat.HasProperty(p)) mat.SetTexture(p, albedo);
                    foreach (string p in new[] { "_Color", "_BaseColor" }) if (mat.HasProperty(p)) mat.SetColor(p, Color.white);
                    if (mat.HasProperty("_BumpMap")) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
                    if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", .7f);
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                    if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .18f);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .18f);
                    return mat;
                });
                string prefabPath = gen + "/WoodenYutBoard.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    var list = JsonUtility.FromJson<NodeList>(File.ReadAllText(root + "/Source/BoardNodes.json"));
                    if (list.nodes == null || list.nodes.Length != 29) throw new InvalidDataException("Expected exactly 29 board nodes.");
                    var go = new GameObject("WoodenYutBoard");
                    try {
                        go.AddComponent<MeshFilter>().sharedMesh = mesh;
                        go.AddComponent<MeshRenderer>().sharedMaterial = material;
                        var collider = go.AddComponent<BoxCollider>(); collider.center = new Vector3(0, .0125f, 0); collider.size = new Vector3(.6f, .025f, .6f);
                        var layout = go.AddComponent<WoodenYutBoardLayout>(); layout.nodes = new Transform[29];
                        var parent = new GameObject("Nodes"); parent.transform.SetParent(go.transform, false);
                        foreach (var d in list.nodes)
                        {
                            if (d.index < 0 || d.index >= 29 || layout.nodes[d.index] != null) throw new InvalidDataException("Invalid or duplicate node index.");
                            var node = new GameObject(d.name); node.transform.SetParent(parent.transform, false); node.transform.localPosition = d.position;
                            var data = node.AddComponent<WoodenYutBoardNode>(); data.index = d.index; data.role = d.role;
                            var hit = node.AddComponent<SphereCollider>(); hit.isTrigger = true; hit.radius = d.role == "corner" || d.role == "center" ? .027f : .017f;
                            layout.nodes[d.index] = node.transform;
                        }
                        bool success; PrefabUtility.SaveAsPrefabAsset(go, prefabPath, out success);
                        if (!success) throw new IOException("Could not save board prefab.");
                    } finally { UnityEngine.Object.DestroyImmediate(go); }
                }
                AssetDatabase.SaveAssets(); File.WriteAllText(gen + "/setup_v1.txt", "Board and 29 nodes created.\n"); AssetDatabase.ImportAsset(gen + "/setup_v1.txt");
                Debug.Log("Yut board ready: " + prefabPath + ". Select the root with Gizmos enabled to see all 29 node labels.");
            } catch (Exception ex) { Debug.LogException(ex); }
        }
    }
}
