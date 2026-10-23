Shader "YutInteraction/WhitePulse"
{
    Properties
    {
        [HDR] _Color ("White light", Color) = (2,2,2,1)
        _Alpha ("Fade", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            float4 _Color;
            float _Alpha;
            v2f vert(appdata v)
            { v2f o; o.position=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float4 frag(v2f i) : SV_Target
            {
                float radius = length(i.uv * 2 - 1);
                float ring = exp(-pow((radius - 0.82) / 0.055, 2));
                float halo = exp(-pow((radius - 0.82) / 0.16, 2)) * 0.25;
                float fill = (1 - smoothstep(0.60, 0.86, radius)) * 0.09;
                float mask = saturate(ring + halo + fill) * (1-smoothstep(0.96,1.0,radius));
                return float4(_Color.rgb, mask * _Alpha * _Color.a);
            }
            ENDHLSL
        }
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="Always" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            float4 _Color;
            float _Alpha;
            v2f vert(appdata v)
            { v2f o; o.position=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float4 frag(v2f i) : SV_Target
            {
                float radius = length(i.uv * 2 - 1);
                float ring = exp(-pow((radius - 0.82) / 0.055, 2));
                float halo = exp(-pow((radius - 0.82) / 0.16, 2)) * 0.25;
                float fill = (1 - smoothstep(0.60, 0.86, radius)) * 0.09;
                float mask = saturate(ring + halo + fill) * (1-smoothstep(0.96,1.0,radius));
                return float4(_Color.rgb, mask * _Alpha * _Color.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
