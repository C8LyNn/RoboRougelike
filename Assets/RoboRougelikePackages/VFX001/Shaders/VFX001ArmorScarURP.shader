Shader "RoboRougelike/VFX/ArmorScarURP"
{
    Properties
    {
        _BaseColor("Scorch Color", Color) = (0.08,0.06,0.06,0.6)
        _Seed("Pattern Seed", Float) = 0
        _CrackStrength("Crack Strength", Range(0,1)) = 0.5
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
            Offset -1,-1
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Seed;
                float _CrackStrength;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.uv=v.uv;
                return o;
            }
            half4 Frag(Varyings v):SV_Target
            {
                float2 p=(v.uv-.5)*2.;
                float angle=atan2(p.y,p.x);
                float radial=length(p);
                float jagged=.025*sin(angle*11.+_Seed*1.7)+.018*sin(angle*19.+_Seed*3.3);
                float body=saturate((.93+jagged-radial)*6.);
                body*=saturate((radial-.05)*7.);
                float spokes=pow(saturate(1.-abs(sin(angle*7.+_Seed))*18.),1.4);
                float cracks=spokes*saturate((radial-.16)*4.)*saturate((.9-radial)*5.);
                float alpha=saturate(body*.46+cracks*.62*_CrackStrength)*_BaseColor.a;
                return half4(_BaseColor.rgb,alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
