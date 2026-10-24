Shader "YutFX2/Outline" { Properties { [HDR]_Color("Outline",Color)=(1.8,1.55,1.1,1) _Width("World Width",Float)=0.0012 } SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
Pass { Tags { "LightMode"="SRPDefaultUnlit" } Cull Front ZWrite Off ZTest LEqual
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
#include "UnityCG.cginc"
struct A { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; float3 normal:TEXCOORD2; };
float4 _Color; float _Width;
V vert(A a) { V v; v.world=mul(unity_ObjectToWorld,a.pos).xyz; v.normal=UnityObjectToWorldNormal(a.normal);
v.world+=normalize(v.normal)*_Width;
v.pos=mul(UNITY_MATRIX_VP,float4(v.world,1)); v.uv=a.uv; return v; }
float4 frag(V v):SV_Target {return _Color;}
ENDHLSL
} }
SubShader { Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }
Pass { Tags { "LightMode"="Always" } Cull Front ZWrite Off ZTest LEqual
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
#include "UnityCG.cginc"
struct A { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; float3 normal:TEXCOORD2; };
float4 _Color; float _Width;
V vert(A a) { V v; v.world=mul(unity_ObjectToWorld,a.pos).xyz; v.normal=UnityObjectToWorldNormal(a.normal);
v.world+=normalize(v.normal)*_Width;
v.pos=mul(UNITY_MATRIX_VP,float4(v.world,1)); v.uv=a.uv; return v; }
float4 frag(V v):SV_Target {return _Color;}
ENDHLSL
} } Fallback Off }
