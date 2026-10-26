using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Scene settings are applied by the Editor tool and serialized with the scene.
// This component stores the previous values for an explicit Restore operation.
public sealed class YutHorrorEnvironmentRig : MonoBehaviour
{
    [Header("Reapply using Tools > Yut Duel > Horror Environment")]
    [Range(0f,.3f)] public float ambientLevel=.055f;
    [Min(0f)] public float keyIntensity=2.1f;
    [Min(0f)] public float fillIntensity=.32f;
    [Min(0f)] public float scaleIntensity=.5f;
    [Min(0f)] public float fogStart=3.2f;
    [Min(.1f)] public float fogEnd=7.5f;
    [Range(0f,.5f)] public float mistOpacity=.12f;
    public bool disableExistingSceneLights=true;
    public Light tableKey, coolFill, scaleAccent;
    public ParticleSystem leftMist,rightMist;
    public Material mistMaterial;
    [HideInInspector] public bool hasBackup;
    [HideInInspector] public bool oldFog;
    [HideInInspector] public FogMode oldFogMode;
    [HideInInspector] public float oldFogStart,oldFogEnd,oldFogDensity,oldAmbientIntensity,oldReflectionIntensity;
    [HideInInspector] public Color oldFogColor,oldAmbientLight,oldAmbientSky,oldAmbientEquator,oldAmbientGround;
    [HideInInspector] public AmbientMode oldAmbientMode;
    [HideInInspector] public Material oldSkybox;
    [HideInInspector] public Light oldSun;
    [Serializable] public sealed class LightState {public Light light;public bool enabled;}
    [Serializable] public sealed class CameraState {public Camera camera;public CameraClearFlags clearFlags;public Color background;}
    [HideInInspector] public List<LightState> savedLights=new List<LightState>();
    [HideInInspector] public List<CameraState> savedCameras=new List<CameraState>();
}
