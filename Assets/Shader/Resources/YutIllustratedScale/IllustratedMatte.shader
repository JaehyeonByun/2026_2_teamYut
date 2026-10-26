Shader "YutIllustratedScale/IllustratedMatte"
{
    Properties
    {
        _MainTex("Painted Texture / Palette",2D)="white"{}
        _Color("Base Tint",Color)=(1,1,1,1)
        _ShadowColor("Shadow Tint",Color)=(.40,.32,.47,1)
        _ShadeStrength("Two Tone Shade",Range(0,1))=.28
        _LightDirection("Art Light Direction (world)",Vector)=(-.3,.8,-.5,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
            struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD1;float2 uv:TEXCOORD0;};
            sampler2D _MainTex;float4 _MainTex_ST,_Color,_ShadowColor,_LightDirection;float _ShadeStrength;
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float lit=step(.15,dot(normalize(i.normal),normalize(_LightDirection.xyz)));
                float3 base=tex2D(_MainTex,i.uv).rgb*_Color.rgb;
                return float4(lerp(base*lerp(float3(1,1,1),_ShadowColor.rgb,_ShadeStrength),base,lit),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
