using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoulScaleAsset.Editor
{
    public sealed class SoulScaleModelImport : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.EndsWith("/Models/SoulScale_Rig.fbx", StringComparison.Ordinal)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.optimizeGameObjects = false;
            importer.importAnimation = false;
            importer.globalScale = 1f;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }
    }
    [InitializeOnLoad]
    public static class SoulScaleSetup
    {
        static SoulScaleSetup() { EditorApplication.delayCall += AutoSetup; }
        private static string Root()
        {
            foreach (string guid in AssetDatabase.FindAssets("SoulScaleSetup t:MonoScript"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("/Editor/SoulScaleSetup.cs", StringComparison.Ordinal))
                    return p.Substring(0, p.Length - "/Editor/SoulScaleSetup.cs".Length);
            }
            return null;
        }
        private static void AutoSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += AutoSetup; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string root = Root();
            if (root != null && !File.Exists(root + "/Generated/SoulScale.prefab")) Build();
        }
        private static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        [MenuItem("Tools/Soul Scale/Create Missing Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            GameObject instance = null;
            try
            {
                string root = Root();
                if (root == null) throw new InvalidOperationException("SoulScale folder not found.");
                string gen = root + "/Generated";
                if (!AssetDatabase.IsValidFolder(gen)) AssetDatabase.CreateFolder(root, "Generated");
                string prefabPath = gen + "/SoulScale.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                { Debug.Log("SoulScale prefab already exists; existing edits kept."); return; }
                string modelPath = root + "/Models/SoulScale_Rig.fbx";
                var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Import SoulScale_Rig.fbx first.");
                if (importer.optimizeGameObjects || importer.animationType != ModelImporterAnimationType.Generic || importer.importAnimation)
                {
                    importer.optimizeGameObjects = false; importer.animationType = ModelImporterAnimationType.Generic;
                    importer.importAnimation = false; importer.SaveAndReimport();
                }
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null) throw new InvalidOperationException("FBX import is not ready. Run the menu after import finishes.");
                var material = AssetDatabase.LoadAssetAtPath<Material>(gen + "/SoulScale_Bronze.mat");
                if (material == null)
                {
                    var pipeline = GraphicsSettings.currentRenderPipeline;
                    string shaderName = pipeline == null ? "Standard" : pipeline.GetType().Name.Contains("HDRender") ? "HDRP/Lit" : "Universal Render Pipeline/Lit";
                    var shader = Shader.Find(shaderName);
                    if (shader == null) throw new InvalidOperationException("Shader unavailable: " + shaderName);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(gen + "/SoulScale_Palette.asset");
                    if (texture == null)
                    {
                        texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, false);
                        if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(root + "/Models/SoulScale_Palette.png")))
                            throw new InvalidOperationException("Could not decode SoulScale palette.");
                        texture.name = "SoulScale_Palette"; texture.wrapMode = TextureWrapMode.Clamp;
                        AssetDatabase.CreateAsset(texture, gen + "/SoulScale_Palette.asset");
                    }
                    material = new Material(shader) { name = "SoulScale_Bronze" };
                    foreach (string prop in new[] { "_MainTex", "_BaseMap", "_BaseColorMap" })
                        if (material.HasProperty(prop)) material.SetTexture(prop, texture);
                    foreach (string prop in new[] { "_Color", "_BaseColor" })
                        if (material.HasProperty(prop)) material.SetColor(prop, Color.white);
                    if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .65f);
                    foreach (string prop in new[] { "_Smoothness", "_Glossiness" })
                        if (material.HasProperty(prop)) material.SetFloat(prop, .4f);
                    AssetDatabase.CreateAsset(material, gen + "/SoulScale_Bronze.mat");
                }
                instance = new GameObject("SoulScale");
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.transform.SetParent(instance.transform, false);
                // Standalone scripted rig: remove the auto-added Animator from this prefab instance.
                foreach (var animator in visual.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterials = new[] { material };
                    var skin = renderer as SkinnedMeshRenderer;
                    if (skin != null) skin.updateWhenOffscreen = true;
                }
                var beam = Find(visual.transform, "Beam");
                var left = Find(visual.transform, "Hang_L"); var right = Find(visual.transform, "Hang_R");
                if (beam == null || left == null || right == null) throw new InvalidOperationException("Missing rig bones. Disable Optimize Game Objects and reimport the FBX.");
                var la = Find(visual.transform, "FlameAnchor_L"); var ra = Find(visual.transform, "FlameAnchor_R");
                if (la == null || ra == null) throw new InvalidOperationException("Missing flame anchors in FBX.");
                instance.AddComponent<SoulScaleRigController>().Configure(beam, left, right, la, ra);
                bool success; PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out success);
                if (!success) throw new IOException("Could not save SoulScale.prefab.");
                AssetDatabase.SaveAssets();
                Debug.Log("SoulScale ready: " + prefabPath + ". Play, then use component Tests menu.");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            catch (Exception ex) { Debug.LogException(ex); }
            finally { if (instance != null) UnityEngine.Object.DestroyImmediate(instance); }
        }
    }
}
