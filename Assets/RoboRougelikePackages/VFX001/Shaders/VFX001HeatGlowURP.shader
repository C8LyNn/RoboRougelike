Shader "RoboRougelike/VFX/HeatGlowURP"
{
    Properties
    {
        _Tint("Tint", Color) = (1,.5,.2,.8)
        _Intensity("Intensity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS: POSITION; float2 uv: TEXCOORD0; };
            struct Varyings { float4 positionCS: SV_POSITION; float2 uv: TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Intensity;
            CBUFFER_END
            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }
            half4 Frag(Varyings i): SV_Target
            {
                float2 centered = (i.uv - .5) * 2.;
                float d = length(centered);
                float core = saturate((.75 - d) * 3.);
                float rim = saturate(1. - abs(d - .55) * 4.);
                float alpha = saturate(core * .75 + rim * .25) * _Tint.a * _Intensity;
                return half4(_Tint.rgb * (1. + 1.7 * _Intensity), alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
