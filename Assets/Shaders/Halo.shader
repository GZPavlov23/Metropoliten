// A soft glow added over whatever is behind it, for lamps seen through a dark tunnel. It falters with the room.
Shader "Metropoliten/Halo"
{
    Properties
    {
        _Color ("Color", Color) = (0.9, 0.8, 0.62, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Halo"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            float _RoomDim;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float fromCentre = length(input.uv - 0.5) * 2.0;
                float edge = saturate(1.0 - fromCentre);

                // A tight core inside a wide, faint bloom.
                float glow = edge * edge * edge * 0.55 + pow(edge, 14.0) * 1.2;
                return float4(_Color.rgb * glow * saturate(1.0 - _RoomDim * 3.0), 1.0);
            }
            ENDHLSL
        }
    }
}
