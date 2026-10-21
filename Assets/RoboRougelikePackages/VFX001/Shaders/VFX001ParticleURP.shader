Shader "RoboRougelike/VFX/ProceduralParticleURP"
{
    Properties
    {
        _MainTex("Radial Shape", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
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
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color * _BaseColor;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                clip(color.a - 0.008);
                return color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
