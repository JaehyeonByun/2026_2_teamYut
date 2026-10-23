Shader "YutInteraction/PieceOutline"
{
    Properties
    {
        [HDR] _Color ("Outline", Color) = (2,2,2,1)
        _Width ("World width", Float) = 0.002
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 position : SV_POSITION; };
            float4 _Color;
            float _Width;
            v2f vert(appdata v)
            {
                v2f o;
                float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
                world+=UnityObjectToWorldNormal(v.normal)*_Width;
                o.position=mul(UNITY_MATRIX_VP,float4(world,1));
                return o;
            }
            float4 frag(v2f i) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Always" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 position : SV_POSITION; };
            float4 _Color;
            float _Width;
            v2f vert(appdata v)
            {
                v2f o;
                float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
                world+=UnityObjectToWorldNormal(v.normal)*_Width;
                o.position=mul(UNITY_MATRIX_VP,float4(world,1));
                return o;
            }
            float4 frag(v2f i) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
    Fallback Off
}
