Shader "YutToonFlame/ToonFlame"
{
 Properties
 {
  _Warm("Warm Palette",Range(0,1))=0
  _Brightness("Hit Dim",Range(0,1))=1
  _Gust("Wind Bend",Range(-.65,.65))=0
  _WindDirection("Wind World Direction",Vector)=(1,0,0,0)
  _Sway("Tip Sway",Range(0,.15))=.045
  _Speed("Sway Speed",Float)=1.6
 }
 SubShader
 {
  Tags {"RenderType"="Opaque" "Queue"="Geometry"}
  Cull Back ZWrite On
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
   struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float height:TEXCOORD2;};
   float _Warm,_Brightness,_Gust,_Sway,_Speed;float4 _WindDirection;
   v2f vert(appdata v)
   {
    v2f o;
    float h=saturate(v.vertex.y);
    float phase=_Time.y*_Speed*3.0 + unity_ObjectToWorld._m03*2.0;
    float3 wind=mul((float3x3)unity_WorldToObject,_WindDirection.xyz);
    wind.y=0;wind=normalize(wind+float3(.00001,0,0));
    v.vertex.x+=(sin(phase+h*3.0)*_Sway+_Gust*wind.x)*h*h;
    v.vertex.z+=(cos(phase*.83+h*2.0)*_Sway*.65+_Gust*wind.z)*h*h;
    // Inverse transpose of the height-dependent shear, preserving bent surface normals.
    float dx=2*h*_Gust*wind.x+_Sway*(2*h*sin(phase+h*3.0)+3*h*h*cos(phase+h*3.0));
    float dz=2*h*_Gust*wind.z+_Sway*.65*(2*h*cos(phase*.83+h*2.0)-2*h*h*sin(phase*.83+h*2.0));
    if(v.vertex.y>0 && v.vertex.y<1) v.normal.y-=dx*v.normal.x+dz*v.normal.z;
    o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
    o.normal=UnityObjectToWorldNormal(v.normal);o.height=h;return o;
   }
   fixed4 frag(v2f i):SV_Target
   {
    float3 n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos-i.world);
    float facing=saturate(dot(n,view));
    float3 outer=lerp(float3(.025,.28,.46),float3(.60,.105,.055),_Warm);
    float3 middle=lerp(float3(.06,.77,.77),float3(1,.40,.08),_Warm);
    float3 heart=lerp(float3(.65,1,.88),float3(1,.90,.48),_Warm);
    // Three solid color bands make the curved volume read as cartoon flame.
    float3 color=lerp(outer,middle,step(.30,facing));
    color=lerp(color,heart,step(.83,facing)*step(i.height,.65));
    color*=_Brightness;
    return float4(color,1);
   }
   ENDCG
  }
 }
 Fallback Off
}
