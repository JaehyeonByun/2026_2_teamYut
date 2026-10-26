Shader "YutHorrorEnvironment/BlackBackdrop"
{
 Properties {_Color("Near Black",Color)=(.006,.008,.012,1)}
 SubShader
 {
  Tags {"RenderType"="Opaque"}
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float4 _Color;
   struct appdata{float4 vertex:POSITION;};
   float4 vert(appdata v):SV_POSITION{return UnityObjectToClipPos(v.vertex);}
   fixed4 frag():SV_Target{return _Color;}
   ENDCG
  }
 }
 Fallback Off
}
