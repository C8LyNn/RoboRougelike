Shader "B0132/ScanSweepTransparentURP"
{
    Properties { _VisualOpacity("Visual Opacity", Range(0,1)) = 1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "ScanSweepUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionHCS : SV_POSITION; half4 color : COLOR; };
            CBUFFER_START(UnityPerMaterial)
                float _VisualOpacity;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS);
                o.color = input.color;
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                return half4(input.color.rgb, saturate(input.color.a * _VisualOpacity));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
