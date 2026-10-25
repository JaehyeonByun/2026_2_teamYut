using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace StylizedCandleHolderAsset.Editor
{
    [InitializeOnLoad]
    public static class CandleHolderSetup
    {
        [Serializable] class MeshData { public Vector3[] vertices; public Vector3[] normals; public Vector2[] uv; public int[] triangles; }
        static CandleHolderSetup() { EditorApplication.delayCall += AutoSetup; }
        static string Root()
        {
            foreach (string guid in AssetDatabase.FindAssets("CandleHolderSetup t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                const string suffix = "/Editor/CandleHolderSetup.cs";
                if (path.EndsWith(suffix, StringComparison.Ordinal)) return path.Substring(0, path.Length - suffix.Length);
            }
            return null;
        }
        static void AutoSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoSetup; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string root = Root();
            if (root != null && File.Exists(root + "/Source/CandleMesh.json") && !File.Exists(root + "/Generated/setup_v2.txt")) Build();
        }
        static T GetOrCreate<T>(string path, Func<T> factory) where T : UnityEngine.Object
        {
            var old = AssetDatabase.LoadAssetAtPath<T>(path); if (old != null) return old;
            if (File.Exists(path)) throw new IOException("Unreadable existing asset: " + path);
            T asset = factory();
            try { AssetDatabase.CreateAsset(asset, path); return asset; }
            catch { UnityEngine.Object.DestroyImmediate(asset); throw; }
        }
        static Shader FindShader(bool unlit)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            string name = pipeline == null ? (unlit ? "Unlit/Texture" : "Standard") :
                pipeline.GetType().Name.Contains("HDRender") ? (unlit ? "HDRP/Unlit" : "HDRP/Lit") :
                (unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            var shader = Shader.Find(name);
            if (shader == null) throw new InvalidOperationException("Candle holder shader unavailable: " + name);
            return shader;
        }
        static Material MakeMaterial(Texture2D texture, bool unlit)
        {
            var mat = new Material(FindShader(unlit)) { name = unlit ? "Candle_AlwaysVisible" : "Candle_StylizedLit" };
            foreach (string p in new[] { "_MainTex", "_BaseMap", "_BaseColorMap", "_UnlitColorMap" }) if (mat.HasProperty(p)) mat.SetTexture(p, texture);
            foreach (string p in new[] { "_Color", "_BaseColor", "_UnlitColor" }) if (mat.HasProperty(p)) mat.SetColor(p, Color.white);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .15f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .15f);
            return mat;
        }
        [MenuItem("Tools/Stylized Candle Holder/Apply Inverted Panel Update")]
        public static void ApplyPanelUpdate() { Build(); }
        [MenuItem("Tools/Stylized Candle Holder/Create Missing Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play Mode before setup."); return; }
            try
            {
                string root = Root(); if (root == null) throw new DirectoryNotFoundException("CandleHolderSetup script not found.");
                string gen = root + "/Generated";
                if (!AssetDatabase.IsValidFolder(gen)) AssetDatabase.CreateFolder(root, "Generated");
                // Update the same Mesh asset in place: existing prefab and scene references stay valid.
                var mesh = GetOrCreate(gen + "/CandleMesh.asset", () => new Mesh());
                var d = JsonUtility.FromJson<MeshData>(File.ReadAllText(root + "/Source/CandleMesh.json"));
                mesh.Clear(); mesh.name = "CandleHolder_1142tri_InvertedPanel";
                mesh.vertices = d.vertices; mesh.triangles = d.triangles; mesh.normals = d.normals; mesh.uv = d.uv;
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                EditorUtility.SetDirty(mesh);
                var texture = GetOrCreate(gen + "/CandlePalette.asset", () => {
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                    try {
                        string path = root + "/Source/CandlePalette.png.bytes";
                        if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(path), false)) throw new InvalidDataException("Candle palette PNG could not be decoded.");
                        tex.name = "CandlePalette"; tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
                        tex.Apply(false, false); return tex;
                    } catch { UnityEngine.Object.DestroyImmediate(tex); throw; }
                });
                var lit = GetOrCreate(gen + "/Candle_StylizedLit.mat", () => MakeMaterial(texture, false));
                // Optional unlit material: do not block primary prefab if unavailable in a custom pipeline.
                try { GetOrCreate(gen + "/Candle_AlwaysVisible.mat", () => MakeMaterial(texture, true)); }
                catch (Exception ex) { Debug.LogWarning("Optional unlit material skipped: " + ex.Message); }
                string prefabPath = gen + "/StylizedCandleHolder.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    var go = new GameObject("StylizedCandleHolder");
                    try {
                        go.AddComponent<MeshFilter>().sharedMesh = mesh;
                        go.AddComponent<MeshRenderer>().sharedMaterial = lit;
                        // Static non-convex mesh collider preserves gaps in the open lattice.
                        go.AddComponent<MeshCollider>().sharedMesh = mesh;
                        var anchor = new GameObject("FlameAnchor"); anchor.transform.SetParent(go.transform, false);
                        anchor.transform.localPosition = new Vector3(.001f, .563f, -.035f);
                        bool success; PrefabUtility.SaveAsPrefabAsset(go, prefabPath, out success);
                        if (!success) throw new IOException("Could not save candle holder prefab.");
                    } finally { UnityEngine.Object.DestroyImmediate(go); }
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText(gen + "/setup_v2.txt", "Candle holder created. Restore missing assets with Tools/Stylized Candle Holder/Create Missing Assets.\n");
                AssetDatabase.ImportAsset(gen + "/setup_v2.txt");
                Debug.Log("Candle holder ready: " + prefabPath + ". Add your flame effect under FlameAnchor.");
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }
}
