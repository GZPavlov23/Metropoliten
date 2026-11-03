// A small grey picture tube: the feed drained of colour, with scan lines, a slow roll and a bright middle.
Shader "Metropoliten/Monitor"
{
    Properties
    {
        _BaseMap ("Feed", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 middle = uv - 0.5;
                float3 feed = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
                float grey = dot(feed, float3(0.3, 0.59, 0.11)) * 2.6 + 0.05;

                float lines = 0.8 + 0.2 * sin(uv.y * 420.0);
                float roll = 0.93 + 0.07 * sin(uv.y * 7.0 - _Time.y * 1.3);
                float falloff = 1.0 - dot(middle, middle) * 1.5;
                float noise = frac(sin(dot(floor(uv * float2(160.0, 120.0)) + floor(_Time.y * 24.0), float2(12.9898, 78.233))) * 43758.5453) * 0.05;

                return half4((grey * lines * roll * falloff + noise) * float3(0.8, 0.9, 0.84), 1.0);
            }
            ENDHLSL
        }
    }
}
