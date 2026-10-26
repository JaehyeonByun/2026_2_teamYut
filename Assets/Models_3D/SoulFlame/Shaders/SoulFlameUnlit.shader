Shader "SoulFlame/ProceduralUnlit"
{
    Properties
    {
        _Tint ("Flame Tint", Color) = (0.08,0.8,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            // No LightMode tag: default unlit pass in Built-in and URP.
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            float4 _Tint; float _Opacity;
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float width=lerp(0.88,0.10,smoothstep(-0.35,1.0,p.y));
                p.x+=0.09*sin(p.y*7+_Time.y*5)*saturate(p.y+0.4);
                float d=length(float2(p.x/width,(p.y+0.15)/1.05));
                float a=pow(saturate(1-d),1.3)*i.color.a*_Opacity*_Tint.a;
                float hot=pow(saturate(1-d*2.2),2);
                float3 rgb=lerp(_Tint.rgb,float3(0.85,0.95,1),hot*0.45)*i.color.rgb*1.4;
                return float4(rgb,a);
            }
            ENDCG
        }
    }
    Fallback Off
}
