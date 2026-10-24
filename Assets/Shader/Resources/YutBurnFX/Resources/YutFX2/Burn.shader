Shader "YutFX2/Burn" { Properties { _BaseMap("Base Map",2D)="white" {} _BaseColor("Base Color",Color)=(1,1,1,1)
_Progress("Progress",Range(0,1))=0 _EdgeWidth("Fire Edge",Float)=0.08 [HDR]_FireColor("Fire",Color)=(4,0.65,0.04,1) } SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
Pass { Tags { "LightMode"="SRPDefaultUnlit" } Cull Back ZWrite On ZTest LEqual
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
#include "UnityCG.cginc"
struct A { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; float3 normal:TEXCOORD2; };
sampler2D _BaseMap; float4 _BaseMap_ST,_BaseColor,_FireColor;
float _Progress,_EdgeWidth; float3 _BoundsMin,_BoundsSize; float4x4 _WorldToEffect;
float hash31(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
float noise3(float3 p) { float3 a=floor(p), f=frac(p); f=f*f*(3-2*f);
return lerp(lerp(lerp(hash31(a),hash31(a+float3(1,0,0)),f.x),lerp(hash31(a+float3(0,1,0)),hash31(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash31(a+float3(0,0,1)),hash31(a+float3(1,0,1)),f.x),lerp(hash31(a+float3(0,1,1)),hash31(a+float3(1,1,1)),f.x),f.y),f.z); }
V vert(A a) { V v; v.world=mul(unity_ObjectToWorld,a.pos).xyz; v.normal=UnityObjectToWorldNormal(a.normal);
v.pos=mul(UNITY_MATRIX_VP,float4(v.world,1)); v.uv=a.uv; return v; }
float4 frag(V v):SV_Target {
float3 local=mul(_WorldToEffect,float4(v.world,1)).xyz;
float3 q=saturate((local-_BoundsMin)/max(_BoundsSize,float3(.0001,.0001,.0001)));
float n=.72*noise3(q*9)+.28*noise3(q*23);
float field=.55*n+.45*q.y;
float remaining=field-lerp(-.03,1.03,_Progress);
clip(remaining);
float edge=(1-smoothstep(0,_EdgeWidth,remaining))*step(.0001,_Progress);
float flicker=.82+.18*sin(_Time.y*25+q.y*29+q.x*15);
float3 baseRGB=tex2D(_BaseMap,v.uv*_BaseMap_ST.xy+_BaseMap_ST.zw).rgb*_BaseColor.rgb;
float shade=.58+.42*saturate(dot(normalize(v.normal),normalize(float3(.3,.8,-.4))));
float scorch=1-.75*(1-smoothstep(_EdgeWidth,_EdgeWidth*2.5,remaining))*step(.0001,_Progress);
return float4(baseRGB*shade*scorch+_FireColor.rgb*edge*flicker,1);
}
ENDHLSL
} }
SubShader { Tags { "RenderType"="Opaque" "Queue"="Geometry" }
Pass { Tags { "LightMode"="Always" } Cull Back ZWrite On ZTest LEqual
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
#include "UnityCG.cginc"
struct A { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; float3 normal:TEXCOORD2; };
sampler2D _BaseMap; float4 _BaseMap_ST,_BaseColor,_FireColor;
float _Progress,_EdgeWidth; float3 _BoundsMin,_BoundsSize; float4x4 _WorldToEffect;
float hash31(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
float noise3(float3 p) { float3 a=floor(p), f=frac(p); f=f*f*(3-2*f);
return lerp(lerp(lerp(hash31(a),hash31(a+float3(1,0,0)),f.x),lerp(hash31(a+float3(0,1,0)),hash31(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash31(a+float3(0,0,1)),hash31(a+float3(1,0,1)),f.x),lerp(hash31(a+float3(0,1,1)),hash31(a+float3(1,1,1)),f.x),f.y),f.z); }
V vert(A a) { V v; v.world=mul(unity_ObjectToWorld,a.pos).xyz; v.normal=UnityObjectToWorldNormal(a.normal);
v.pos=mul(UNITY_MATRIX_VP,float4(v.world,1)); v.uv=a.uv; return v; }
float4 frag(V v):SV_Target {
float3 local=mul(_WorldToEffect,float4(v.world,1)).xyz;
float3 q=saturate((local-_BoundsMin)/max(_BoundsSize,float3(.0001,.0001,.0001)));
float n=.72*noise3(q*9)+.28*noise3(q*23);
float field=.55*n+.45*q.y;
float remaining=field-lerp(-.03,1.03,_Progress);
clip(remaining);
float edge=(1-smoothstep(0,_EdgeWidth,remaining))*step(.0001,_Progress);
float flicker=.82+.18*sin(_Time.y*25+q.y*29+q.x*15);
float3 baseRGB=tex2D(_BaseMap,v.uv*_BaseMap_ST.xy+_BaseMap_ST.zw).rgb*_BaseColor.rgb;
float shade=.58+.42*saturate(dot(normalize(v.normal),normalize(float3(.3,.8,-.4))));
float scorch=1-.75*(1-smoothstep(_EdgeWidth,_EdgeWidth*2.5,remaining))*step(.0001,_Progress);
return float4(baseRGB*shade*scorch+_FireColor.rgb*edge*flicker,1);
}
ENDHLSL
} } Fallback Off }
