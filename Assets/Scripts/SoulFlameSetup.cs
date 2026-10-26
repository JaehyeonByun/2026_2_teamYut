using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoulFlameAsset.Editor
{
    public static class SoulFlameSetup
    {
        private static string Root()
        {
            foreach (string guid in AssetDatabase.FindAssets("SoulFlameSetup t:MonoScript"))
            {
                string p=AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("/Editor/SoulFlameSetup.cs", StringComparison.Ordinal))
                    return p.Substring(0,p.Length-"/Editor/SoulFlameSetup.cs".Length);
            }
            throw new InvalidOperationException("SoulFlame folder not found.");
        }
        [MenuItem("Tools/Soul Flame/Create Missing Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                string gen=Root()+"/Generated";
                if (!AssetDatabase.IsValidFolder(gen)) AssetDatabase.CreateFolder(Root(),"Generated");
                var shader=Shader.Find("SoulFlame/ProceduralUnlit");
                if (shader==null) throw new InvalidOperationException("Import SoulFlameUnlit.shader and wait for compilation first.");
                var material=AssetDatabase.LoadAssetAtPath<Material>(gen+"/SoulFlame.mat");
                if (material==null)
                {
                    material=new Material(shader) {name="SoulFlame"};
                    material.SetColor("_Tint",Color.white);
                    AssetDatabase.CreateAsset(material,gen+"/SoulFlame.mat");
                }
                CreatePrefab(gen,"SoulFlame_Player",new Color(.08f,.8f,1f,1f),material);
                CreatePrefab(gen,"SoulFlame_Opponent",new Color(1f,.25f,.045f,1f),material);
                AssetDatabase.SaveAssets();
                Debug.Log("Soul flames ready: "+gen+". Attach Player/Opponent prefabs to the scale's flame anchors.");
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }
        private static void CreatePrefab(string gen,string name,Color color,Material mat)
        {
            string path=gen+"/"+name+".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null) return;
            var root=new GameObject(name);
            try
            {
                var art=new GameObject("FlameVisual");art.transform.SetParent(root.transform,false);
                var core=MakeParticles("Core",art.transform,mat,0);
                var sparks=MakeParticles("Sparks",art.transform,mat,1);
                var burst=MakeParticles("HitBurst",art.transform,mat,2);
                var lightObject=new GameObject("Glow");lightObject.transform.SetParent(art.transform,false);
                lightObject.transform.localPosition=new Vector3(0,.1f,0);
                var light=lightObject.AddComponent<Light>();light.type=LightType.Point;
                light.color=color;light.intensity=.7f;light.range=.5f;light.shadows=LightShadows.None;
                root.AddComponent<SoulFlameVisual>().Configure(art.transform,core,sparks,burst,light,color);
                bool ok;PrefabUtility.SaveAsPrefabAsset(root,path,out ok);
                if (!ok) throw new InvalidOperationException("Could not save "+path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static ParticleSystem MakeParticles(string name,Transform parent,Material mat,int kind)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);
            obj.transform.localPosition=new Vector3(0,kind==0?.065f:.09f,0);
            var ps=obj.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;
            main.duration=1f;main.loop=kind!=2;main.playOnAwake=kind!=2;
            main.prewarm=kind!=2;
            main.startLifetime=kind==0?.45f:kind==1?.75f:.35f;
            main.startSpeed=0f;main.maxParticles=kind==0?40:32;
            main.startColor=Color.white;
            main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.scalingMode=ParticleSystemScalingMode.Hierarchy;
            main.startSize3D=true;
            main.startSizeX=kind==0?.11f:.018f;
            main.startSizeY=kind==0?.2f:.033f;
            main.startSizeZ=kind==0?.11f:.018f;
            var emission=ps.emission;emission.enabled=true;emission.rateOverTime=kind==0?28f:kind==1?7f:0f;
            var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Sphere;
            shape.radius=kind==0?.016f:.035f;
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;
            // Use matching curve modes on XYZ for compatibility with particle module validation.
            velocity.x=new ParticleSystem.MinMaxCurve(-.035f,.035f);
            velocity.y=new ParticleSystem.MinMaxCurve(kind==0?.10f:.20f,kind==0?.16f:.33f);
            velocity.z=new ParticleSystem.MinMaxCurve(-.035f,.035f);
            var fade=ps.colorOverLifetime;fade.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(
                new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(kind==0?.6f:1f,.15f),new GradientAlphaKey(0,1)});
            fade.color=new ParticleSystem.MinMaxGradient(gradient);
            var size=ps.sizeOverLifetime;size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1f,AnimationCurve.Linear(0,1,1,.2f));
            var renderer=obj.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mat;
            renderer.renderMode=ParticleSystemRenderMode.Billboard;
            renderer.alignment=ParticleSystemRenderSpace.View;
            renderer.maxParticleSize=1f;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return ps;
        }
    }
}
