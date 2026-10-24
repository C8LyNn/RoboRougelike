Shader "RoboRougelike/VFX/ProceduralFlameURP"
{
    Properties
    {
        _BaseColor("Hot Color", Color) = (1,0.55,0.08,1)
        _TipColor("Tip Color", Color) = (0.7,0.05,0.01,0)
        _Thrust01("Thrust", Range(0,1)) = 1
        _NoiseSpeed("Noise Speed", Float) = 6
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float _Thrust01;
                float _NoiseSpeed;
            CBUFFER_END
            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 p = v.positionOS.xyz;
                float t = _Time.y * _NoiseSpeed;
                float bend = sin(v.uv.y * 15.0 + t) * .028 * v.uv.y;
                p.x += bend;
                p.y += sin(v.uv.y * 11.0 - t * .7) * .02 * v.uv.y;
                o.positionHCS = TransformObjectToHClip(p);
                o.uv = v.uv;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float progress = saturate(i.uv.y);
                float noise = .84 + .16 * sin(progress * 31.0 + _Time.y * _NoiseSpeed * 2.0);
                half4 color = lerp(_BaseColor, _TipColor, progress);
                color.rgb *= 1.6 * noise;
                color.a = (1.0 - progress) * noise * _Thrust01;
                return color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
