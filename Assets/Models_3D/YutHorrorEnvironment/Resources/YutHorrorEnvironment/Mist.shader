Shader "YutHorrorEnvironment/PeripheralMist"
{
 Properties { _Tint("Mist Color",Color)=(.16,.21,.23,1) _Opacity("Opacity",Range(0,.5))=.12 _BoardCenter("Protected Board Center",Vector)=(0,.825,0,0) }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f {float4 pos:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
   float4 _Tint,_BoardCenter;float _Opacity;
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.uv=v.uv;o.color=v.color;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float2 p=i.uv*2-1;
    float edge=pow(saturate(1-dot(p,p)),2);
    float ripple=.65+.20*sin(i.world.x*3.7+i.world.z*2.1+_Time.y*.19)+.15*sin(i.world.y*5-i.world.z*3+_Time.y*.11);
    float3 d=i.world-_BoardCenter.xyz;
    // Central play area stays free of mist even when a large particle overlaps it.
    float peripheral=smoothstep(1.1,1.75,abs(d.x));
    float nearFade=smoothstep(.65,1.4,distance(i.world,_WorldSpaceCameraPos));
    float alpha=edge*saturate(ripple)*peripheral*nearFade*i.color.a*_Opacity;
    return float4(_Tint.rgb,alpha);
   }
   ENDCG
  }
 }
 Fallback Off
}
