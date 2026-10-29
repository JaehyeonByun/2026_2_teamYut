#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class YutHorrorEnvironmentSetup
{
    private const string Menu="Tools/Yut Duel/Horror Environment/";
    private static YutHorrorEnvironmentRig FindRig()
    {
        foreach(var r in Object.FindObjectsOfType<YutHorrorEnvironmentRig>(true))
            if(r.gameObject.scene==SceneManager.GetActiveScene()) return r;
        return null;
    }
    private static bool CanEdit()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Stop Play mode first.");return false;}
        if(GraphicsSettings.currentRenderPipeline!=null && GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRender"))
        {Debug.LogError("This preset targets Built-in or URP 3D Renderer, not HDRP.");return false;}
        if(UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()!=null)
        {Debug.LogWarning("Return to the game scene, not Prefab Mode.");return false;}
        return true;
    }
    [MenuItem(Menu+"1 Create Dark Table Setup")]
    public static void Create()
    {
        if(!CanEdit())return;
        if(FindRig()!=null){Debug.LogWarning("Rig already exists. Use 2 Reapply instead.");return;}
        var mistShader=Resources.Load<Shader>("YutHorrorEnvironment/Mist");
        var blackShader=Resources.Load<Shader>("YutHorrorEnvironment/BlackBackdrop");
        if(mistShader==null || blackShader==null || !mistShader.isSupported || !blackShader.isSupported)
        {Debug.LogError("Import both YutHorrorEnvironment Resources shaders and resolve Console shader errors first.");return;}
        Vector3 center=new Vector3(0,.825f,0);
        var selected=Selection.activeGameObject;
        if(selected!=null && selected.scene==SceneManager.GetActiveScene())
        {
            bool found=false;Bounds bounds=new Bounds();
            foreach(var r in selected.GetComponentsInChildren<Renderer>())
            {if(r is ParticleSystemRenderer)continue;if(!found){bounds=r.bounds;found=true;}else bounds.Encapsulate(r.bounds);}
            if(found)center=new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);
        }
        var root=new GameObject("Yut_Horror_Environment");Undo.RegisterCreatedObjectUndo(root,"Create horror environment");
        root.transform.position=center;
        var rig=Undo.AddComponent<YutHorrorEnvironmentRig>(root);
        Backup(rig);
        rig.tableKey=Spot("Table_Key_Warm",root.transform,new Vector3(0,2.15f,-.65f),new Vector3(0,0,.05f),new Color(1f,.76f,.49f),2.1f,5f,72f,true);
        rig.coolFill=Spot("Side_Fill_Cold",root.transform,new Vector3(-1.8f,1.0f,.6f),new Vector3(0,.15f,0),new Color(.33f,.49f,.60f),.32f,3.8f,68f,false);
        rig.scaleAccent=Spot("Scale_Accent",root.transform,new Vector3(1.1f,1.4f,-.6f),new Vector3(1.35f,.4f,0),new Color(.70f,.76f,.65f),.5f,2.8f,48f,false);
        string folder="Assets/YutHorrorEnvironment/Generated";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/YutHorrorEnvironment","Generated");
        var black=new Material(blackShader){name="Horror_Backdrop"};
        AssetDatabase.CreateAsset(black,AssetDatabase.GenerateUniqueAssetPath(folder+"/Horror_Backdrop.mat"));
        Backdrop("Back_Darkness",root.transform,new Vector3(0,2.2f,4.5f),new Vector3(14,8,.12f),black);
        Backdrop("Left_Darkness",root.transform,new Vector3(-4.8f,2.2f,1.8f),new Vector3(.12f,8,8),black);
        Backdrop("Right_Darkness",root.transform,new Vector3(4.8f,2.2f,1.8f),new Vector3(.12f,8,8),black);
        rig.mistMaterial=new Material(mistShader){name="Peripheral_Mist"};
        AssetDatabase.CreateAsset(rig.mistMaterial,AssetDatabase.GenerateUniqueAssetPath(folder+"/Peripheral_Mist.mat"));
        rig.leftMist=Mist("Mist_Left",root.transform,new Vector3(-2.1f,.45f,1.1f),rig.mistMaterial,1);
        rig.rightMist=Mist("Mist_Right",root.transform,new Vector3(2.1f,.45f,1.1f),rig.mistMaterial,2);
        Apply(rig);Selection.activeGameObject=root;AssetDatabase.SaveAssets();
        Debug.Log("Dark setup created. Check all gameplay cameras; adjust the rig and lights in Scene view. Save the scene.",rig);
    }
    private static Light Spot(string name,Transform parent,Vector3 position,Vector3 target,Color color,float power,float range,float cone,bool shadows)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;
        g.transform.localRotation=Quaternion.LookRotation(target-position,Vector3.up);
        var l=g.AddComponent<Light>();l.type=LightType.Spot;l.color=color;l.intensity=power;l.range=range;l.spotAngle=cone;
        l.shadows=shadows?LightShadows.Soft:LightShadows.None;l.shadowStrength=.85f;l.shadowBias=.035f;l.shadowNormalBias=.15f;
        l.lightmapBakeType=LightmapBakeType.Realtime;return l;
    }
    private static void Backdrop(string name,Transform parent,Vector3 pos,Vector3 size,Material mat)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);
        g.transform.localPosition=pos;g.transform.localScale=size;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
    }
    private static ParticleSystem Mist(string name,Transform parent,Vector3 pos,Material material,uint seed)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;
        var ps=g.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.useAutoRandomSeed=false;ps.randomSeed=seed;
        var main=ps.main;main.duration=16;main.loop=true;main.prewarm=true;main.playOnAwake=true;
        main.startLifetime=16;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(1.2f,2.2f);
        main.startColor=Color.white;main.maxParticles=32;main.simulationSpace=ParticleSystemSimulationSpace.Local;
        var emission=ps.emission;emission.rateOverTime=1.2f;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(.65f,.65f,2.0f);
        var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;
        velocity.x=new ParticleSystem.MinMaxCurve(-.018f,.018f);velocity.y=new ParticleSystem.MinMaxCurve(.005f,.014f);velocity.z=new ParticleSystem.MinMaxCurve(-.025f,.025f);
        var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.55f,.25f),new GradientAlphaKey(.55f,.65f),new GradientAlphaKey(0,1)});
        fade.color=new ParticleSystem.MinMaxGradient(gradient);
        var renderer=g.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
        renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.maxParticleSize=1f;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        return ps;
    }
    private static void Backup(YutHorrorEnvironmentRig r)
    {
        if(r.hasBackup)return;
        r.oldFog=RenderSettings.fog;r.oldFogMode=RenderSettings.fogMode;r.oldFogColor=RenderSettings.fogColor;
        r.oldFogStart=RenderSettings.fogStartDistance;r.oldFogEnd=RenderSettings.fogEndDistance;r.oldFogDensity=RenderSettings.fogDensity;
        r.oldAmbientMode=RenderSettings.ambientMode;r.oldAmbientLight=RenderSettings.ambientLight;r.oldAmbientSky=RenderSettings.ambientSkyColor;
        r.oldAmbientEquator=RenderSettings.ambientEquatorColor;r.oldAmbientGround=RenderSettings.ambientGroundColor;
        r.oldAmbientIntensity=RenderSettings.ambientIntensity;r.oldReflectionIntensity=RenderSettings.reflectionIntensity;
        r.oldSkybox=RenderSettings.skybox;r.oldSun=RenderSettings.sun;r.hasBackup=true;
    }
    [MenuItem(Menu+"2 Reapply Settings From Rig")]
    public static void Reapply(){if(!CanEdit())return;var r=FindRig();if(r==null){Debug.LogWarning("Create the rig first.");return;}Apply(r);}
    private static void Apply(YutHorrorEnvironmentRig r)
    {
        Backup(r);Undo.RecordObject(r,"Apply dark environment");
        if(r.disableExistingSceneLights)
        foreach(var light in Object.FindObjectsOfType<Light>(true))
        {
            if(light.gameObject.scene!=r.gameObject.scene || light.transform.IsChildOf(r.transform))continue;
            if(!light.enabled)continue;
            if(!r.savedLights.Exists(s=>s.light==light))r.savedLights.Add(new YutHorrorEnvironmentRig.LightState{light=light,enabled=light.enabled});
            Undo.RecordObject(light,"Mute old environment light");light.enabled=false;EditorUtility.SetDirty(light);
        }
        else foreach(var state in r.savedLights)if(state.light!=null) {Undo.RecordObject(state.light,"Restore light");state.light.enabled=state.enabled;EditorUtility.SetDirty(state.light);}
        var camera=Camera.main;
        if(camera!=null && camera.gameObject.scene==r.gameObject.scene)
        {
            if(!r.savedCameras.Exists(s=>s.camera==camera))r.savedCameras.Add(new YutHorrorEnvironmentRig.CameraState{camera=camera,clearFlags=camera.clearFlags,background=camera.backgroundColor});
            Undo.RecordObject(camera,"Dark camera background");camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.006f,.008f,.012f);EditorUtility.SetDirty(camera);
        }
        RenderSettings.skybox=null;RenderSettings.sun=null;RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.62f,.70f,.82f)*r.ambientLevel;RenderSettings.ambientIntensity=1f;RenderSettings.reflectionIntensity=.08f;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.009f,.013f,.018f);
        RenderSettings.fogStartDistance=Mathf.Max(0,r.fogStart);RenderSettings.fogEndDistance=Mathf.Max(r.fogStart+.1f,r.fogEnd);
        SetPower(r.tableKey,r.keyIntensity);SetPower(r.coolFill,r.fillIntensity);SetPower(r.scaleAccent,r.scaleIntensity);
        if(r.mistMaterial!=null){Undo.RecordObject(r.mistMaterial,"Mist opacity");r.mistMaterial.SetFloat("_Opacity",r.mistOpacity);r.mistMaterial.SetVector("_BoardCenter",r.transform.position);EditorUtility.SetDirty(r.mistMaterial);}
        EditorUtility.SetDirty(r);EditorSceneManager.MarkSceneDirty(r.gameObject.scene);
    }
    private static void SetPower(Light light,float value){if(light!=null){Undo.RecordObject(light,"Set light intensity");light.intensity=value;EditorUtility.SetDirty(light);}}
    [MenuItem(Menu+"3 Restore Previous Environment")]
    public static void Restore()
    {
        if(!CanEdit())return;var r=FindRig();if(r==null || !r.hasBackup)return;
        RenderSettings.fog=r.oldFog;RenderSettings.fogMode=r.oldFogMode;RenderSettings.fogColor=r.oldFogColor;
        RenderSettings.fogStartDistance=r.oldFogStart;RenderSettings.fogEndDistance=r.oldFogEnd;RenderSettings.fogDensity=r.oldFogDensity;
        RenderSettings.ambientMode=r.oldAmbientMode;RenderSettings.ambientLight=r.oldAmbientLight;RenderSettings.ambientSkyColor=r.oldAmbientSky;
        RenderSettings.ambientEquatorColor=r.oldAmbientEquator;RenderSettings.ambientGroundColor=r.oldAmbientGround;
        RenderSettings.ambientIntensity=r.oldAmbientIntensity;RenderSettings.reflectionIntensity=r.oldReflectionIntensity;
        RenderSettings.skybox=r.oldSkybox;RenderSettings.sun=r.oldSun;
        foreach(var s in r.savedLights)if(s.light!=null){Undo.RecordObject(s.light,"Restore light");s.light.enabled=s.enabled;EditorUtility.SetDirty(s.light);}
        foreach(var s in r.savedCameras)if(s.camera!=null){Undo.RecordObject(s.camera,"Restore camera");s.camera.clearFlags=s.clearFlags;s.camera.backgroundColor=s.background;EditorUtility.SetDirty(s.camera);}
        var scene=r.gameObject.scene;Undo.DestroyObjectImmediate(r.gameObject);EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Previous environment restored. Generated materials remain available; save the scene.");
    }
}

#endif
