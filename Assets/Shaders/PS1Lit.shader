Shader "Metropoliten/PS1Lit"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _VertexGridSize ("Vertex Snap Grid", Range(32, 1024)) = 256
        _ColorLevels ("Color Levels", Range(2, 64)) = 24
        _JitterEnabled ("Enable Vertex Jitter", Float) = 1
        _PosterizeEnabled ("Enable Color Posterize", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogCoord : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _VertexGridSize;
            float _ColorLevels;
            float _JitterEnabled;
            float _PosterizeEnabled;

            // 4x4 ordered (Bayer) dither matrix, normalized to [0,1). Breaks up hard color-quantize
            // banding into a fine grain instead of flat blotchy steps.
            static const float k_Bayer4x4[16] = {
                0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0, 4.0/16.0, 14.0/16.0,  6.0/16.0,
                3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0, 7.0/16.0, 13.0/16.0,  5.0/16.0
            };

            float GetBayerDither(float2 screenPos)
            {
                uint2 p = uint2(screenPos) % 4;
                return k_Bayer4x4[p.y * 4 + p.x];
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                float4 positionCS = vertexInput.positionCS;

                if (_JitterEnabled > 0.5)
                {
                    float gridSize = max(_VertexGridSize, 1.0);
                    float2 snapped = floor(positionCS.xy / positionCS.w * gridSize) / gridSize * positionCS.w;
                    positionCS.xy = snapped;
                }

                OUT.positionCS = positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.positionWS = vertexInput.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.fogCoord = ComputeFogFactor(positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 albedo = texColor.rgb * _BaseColor.rgb;

                half3 normalWS = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                half3 lighting = mainLight.color * saturate(dot(normalWS, mainLight.direction));

                uint additionalLightsCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < additionalLightsCount; lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, IN.positionWS);
                    lighting += light.color * light.distanceAttenuation * saturate(dot(normalWS, light.direction));
                }

                half3 ambient = SampleSH(normalWS);
                half3 color = albedo * (lighting + ambient);

                if (_PosterizeEnabled > 0.5)
                {
                    float levels = max(_ColorLevels, 2.0);
                    float dither = (GetBayerDither(IN.positionCS.xy) - 0.5) / levels;
                    color = floor(color * levels + dither + 0.5) / levels;
                }

                color = MixFog(color, IN.fogCoord);
                return half4(color, texColor.a);
            }
            ENDHLSL
        }
    }
}
