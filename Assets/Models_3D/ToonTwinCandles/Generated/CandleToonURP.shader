Shader "ToonTwinCandles/URP" {
Properties {
 _BaseColor("Color",Color)=(1,1,1,1)
 _ShadowLevel("Shadow Level",Range(0,1))=0.32
 _MiddleLevel("Middle Level",Range(0,1))=0.65
 _ThresholdLow("Low Threshold",Range(0,1))=0.25
 _ThresholdHigh("High Threshold",Range(0,1))=0.65
 _BaseMap("Compatibility Texture",2D)="white"{}
 _Cutoff("Compatibility Cutoff",Range(0,1))=0.5
}
SubShader {
 Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
 Pass {
  Name "ForwardLit"
  Tags { "LightMode"="UniversalForwardOnly" }
  HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #pragma target 3.5
  #pragma multi_compile_fog
  #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
  #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
  #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
  #pragma multi_compile_fragment _ _SHADOWS_SOFT
  #pragma multi_compile _ _FORWARD_PLUS
  #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  CBUFFER_START(UnityPerMaterial)
  half4 _BaseColor;
  half _ShadowLevel,_MiddleLevel,_ThresholdLow,_ThresholdHigh;
  CBUFFER_END
  struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
  struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half fog:TEXCOORD2; half3 vertexLight:TEXCOORD3; };
  Varyings vert(Attributes v) {
   Varyings o;
   o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
   o.positionCS=TransformWorldToHClip(o.positionWS);
   o.normalWS=TransformObjectToWorldNormal(v.normalOS);
   o.fog=ComputeFogFactor(o.positionCS.z);
   o.vertexLight=VertexLighting(o.positionWS,o.normalWS);
   return o;
  }
  half3 EvaluateCandleLight(half3 n,Light light) {
   half ndl=saturate(dot(n,light.direction));
   half band=ndl<_ThresholdLow?_ShadowLevel:(ndl<_ThresholdHigh?_MiddleLevel:1);
   return band*light.color*light.distanceAttenuation*light.shadowAttenuation;
  }
  half4 frag(Varyings input):SV_Target {
   InputData inputData=(InputData)0;
   inputData.positionWS=input.positionWS;
   inputData.normalWS=normalize(input.normalWS);
   inputData.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS);
   inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
   Light mainLight=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
   half3 illumination=max(0,SampleSH(inputData.normalWS));
   illumination+=EvaluateCandleLight(inputData.normalWS,mainLight);
   #if defined(_ADDITIONAL_LIGHTS_VERTEX)
   illumination+=input.vertexLight;
   #endif
   #if defined(_ADDITIONAL_LIGHTS)
    #if USE_FORWARD_PLUS || USE_CLUSTER_LIGHT_LOOP
     UNITY_LOOP for(uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++) {
      Light light=GetAdditionalLight(lightIndex,input.positionWS,half4(1,1,1,1));
      illumination+=EvaluateCandleLight(inputData.normalWS,light);
     }
    #endif
    uint pixelLightCount=GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
     Light light=GetAdditionalLight(lightIndex,input.positionWS,half4(1,1,1,1));
     illumination+=EvaluateCandleLight(inputData.normalWS,light);
    LIGHT_LOOP_END
   #endif
   return half4(MixFog(_BaseColor.rgb*illumination,input.fog),1);
  }
  ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 UsePass "Universal Render Pipeline/Lit/DepthNormals"
}
Fallback Off
}
