// The glow round a lamp in fog: a soft ball of light that always faces the viewer, faded out where it would cut through
// something solid. Every corner of each quad is stored at the lamp itself, with the UVs saying which way to push it.
Shader "Metropoliten/FogGlow"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.62, 0.3, 1)
        _Size ("Radius", Range(0.1, 12)) = 2.6
        _Falloff ("How Far It Carries", Range(0, 0.1)) = 0.012
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FogGlow"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Size;
                float _Falloff;
            CBUFFER_END

            float _OpenFog;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 view : TEXCOORD1;
                float fade : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 center = TransformObjectToWorld(input.positionOS.xyz);
                float3 view = TransformWorldToView(center);
                float away = length(view);
                view.xy += (input.uv - 0.5) * 2.0 * _Size * (1.0 + away * 0.012);
                output.positionCS = TransformWViewToHClip(view);
                output.uv = input.uv;
                output.view = view;
                output.fade = exp(-away * _Falloff) * _OpenFog;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float fromCentre = length(input.uv - 0.5) * 2.0;
                float edge = saturate(1.0 - fromCentre);
                float glow = edge * edge * edge * 0.42 + pow(edge, 9.0) * 0.9;

                float2 screen = input.positionCS.xy / _ScaledScreenParams.xy;
                float solid = LinearEyeDepth(SampleSceneDepth(screen), _ZBufferParams);
                float soft = saturate((solid + input.view.z) / 1.4);
                return float4(_Color.rgb * glow * soft * input.fade, 1.0);
            }
            ENDHLSL
        }
    }
}
