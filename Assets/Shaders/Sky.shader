// The sky over the end of the line: an overcast morning that isn't quite light yet, cloud in slow layers, and a dull
// glow low down in the east where the sun is trying to come through.
Shader "Metropoliten/Sky"
{
    Properties
    {
        _Low ("Horizon", Color) = (0.42, 0.42, 0.42, 1)
        _High ("Overhead", Color) = (0.1, 0.12, 0.16, 1)
        _Dawn ("Dawn", Color) = (0.62, 0.46, 0.36, 1)
        _Noise ("Noise", 3D) = "gray" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+500" }

        Pass
        {
            Cull Front
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Low, _High, _Dawn;
            CBUFFER_END

            TEXTURE3D(_Noise);
            SAMPLER(sampler_Noise);

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 way = normalize(input.direction);
                float up = saturate(way.y);

                // The underside of the cloud, a long way up, sliding very slowly east.
                float2 over = way.xz / (up + 0.16);
                float drift = _Time.y * 0.0012;
                float heavy = SAMPLE_TEXTURE3D(_Noise, sampler_Noise, float3(over.x * 0.09 - drift, 0.2, over.y * 0.09)).r;
                float torn = SAMPLE_TEXTURE3D(_Noise, sampler_Noise, float3(over.x * 0.31 - drift * 2.0, 0.6, over.y * 0.31)).r;
                float cloud = saturate(heavy * 0.7 + torn * 0.3);

                half3 color = lerp(_Low.rgb, _High.rgb, pow(up, 0.5));
                color *= lerp(0.7, 1.26, smoothstep(0.26, 0.74, cloud));

                // The sun's coming up behind it, out beyond the road.
                float east = saturate(dot(way, normalize(float3(1.0, 0.04, 0.32))));
                color += _Dawn.rgb * pow(east, 6.0) * saturate(1.0 - up * 2.6) * lerp(0.45, 1.0, cloud);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
