Shader "YutIllustratedScale/PaintedFlame"
{
    Properties
    {
        _MainTex("Painted Flame",2D)="white"{}
        _Opacity("Opacity",Range(0,1))=1
        _Hit("Hit Flash",Range(0,1))=0
        _Warm("Warm Team",Range(0,1))=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0; };
            sampler2D _MainTex; float4 _MainTex_ST;float _Opacity,_Hit,_Warm;
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                float value=saturate(max(c.r,max(c.g,c.b)));
                float3 warm=lerp(float3(.28,.08,.13),float3(1,.76,.40),value);
                c.rgb=lerp(c.rgb,warm,_Warm);
                c.rgb=lerp(c.rgb,float3(1,.95,.80),_Hit*.78);
                c.a*=_Opacity;return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
