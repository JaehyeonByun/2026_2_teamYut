Shader "YutInteraction/PieceGlow"
{
    Properties { [HDR] _Color ("White glow", Color) = (2,2,2,0.3) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            Cull Back
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float3 normal : TEXCOORD1; };
            float4 _Color;
            v2f vert(appdata v)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float rim=1-saturate(abs(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world))));
                return float4(_Color.rgb,_Color.a*(0.3+0.7*rim*rim));
            }
            ENDHLSL
        }
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Always" }
            Blend SrcAlpha One
            Cull Back
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float3 normal : TEXCOORD1; };
            float4 _Color;
            v2f vert(appdata v)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float rim=1-saturate(abs(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world))));
                return float4(_Color.rgb,_Color.a*(0.3+0.7*rim*rim));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
