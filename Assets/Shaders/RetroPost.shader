// The game's final image: a film shoulder, a slightly dulled grade, a vignette and per-pixel colour grain.
// Runs through URP's Full Screen Pass renderer feature on PC_Renderer.
Shader "Hidden/Metropoliten/RetroPost"
{
    Properties
    {
        _Exposure ("Exposure", Range(0.25, 6)) = 1.6
        _Saturation ("Saturation", Range(0, 1.5)) = 0.9
        _Contrast ("Contrast", Range(0.5, 2)) = 1.1
        _Blacks ("Blacks Tint", Color) = (0.012, 0.011, 0.01, 1)
        _Whites ("Whites Tint", Color) = (1, 0.99, 0.97, 1)
        _Vignette ("Vignette", Range(0, 2)) = 0.7
        _Grain ("Grain", Range(0, 0.3)) = 0.11
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "RetroPost"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Exposure;
            float _Saturation;
            float _Contrast;
            float4 _Blacks;
            float4 _Whites;
            float _Vignette;
            float _Grain;
            float _RoomDim;
            float _Brightness;

            // pcg3d: three independent randoms per pixel per frame.
            float3 PixelNoise(uint2 pixel, uint frame)
            {
                uint3 v = uint3(pixel, frame) * 1664525u + 1013904223u;
                v.x += v.y * v.z;
                v.y += v.z * v.x;
                v.z += v.x * v.y;
                v ^= v >> 16u;
                v.x += v.y * v.z;
                v.y += v.z * v.x;
                v.z += v.x * v.y;
                return float3(v) / 4294967295.0;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float3 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0).rgb;

                color = 1.0 - exp(-color * _Exposure * (1.0 + _Brightness) * (1.0 - _RoomDim));
                color = LinearToSRGB(color);

                float luma = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(luma.xxx, color, _Saturation);
                color = saturate((color - 0.5) * _Contrast + 0.5);
                color = lerp(_Blacks.rgb, _Whites.rgb, color);

                float2 fromCenter = (uv - 0.5) * 2.0;
                float falloff = dot(fromCenter, fromCenter) * 0.5;
                color *= saturate(1.0 - _Vignette * falloff * (0.6 + falloff));

                float3 noise = PixelNoise(uint2(input.positionCS.xy), uint(_Time.y * 24.0));
                color += (noise - 0.5) * _Grain * lerp(1.0, 0.6, luma);

                return float4(SRGBToLinear(saturate(color)), 1.0);
            }
            ENDHLSL
        }
    }
}
