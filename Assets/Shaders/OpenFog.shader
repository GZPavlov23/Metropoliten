// The morning at the end of the line: real fog with some body to it, lying thick along the ground, thinning upward and
// drifting past in slow banks. It marches each view ray through a block of noise, so that it has a grain you can see
// and things go into it by degrees. Goes over the solid world, under anything see-through.
// Runs through URP's Full Screen Pass renderer feature on PC_Renderer. Underground it costs a single branch.
Shader "Hidden/Metropoliten/OpenFog"
{
    Properties
    {
        _Noise ("Noise", 3D) = "gray" {}
        _Density ("Density", Range(0, 0.3)) = 0.046
        _Ceiling ("Height It Thins Over", Range(2, 80)) = 16
        _Reach ("Reach", Range(40, 420)) = 280
        _Banks ("Size Of The Banks", Range(10, 200)) = 38
        _Wisps ("Size Of The Wisps", Range(2, 60)) = 9
        _Lumps ("Lumpiness", Range(0, 4)) = 2.4
        _Low ("Colour", Color) = (0.17, 0.18, 0.2, 1)
        _High ("Colour Overhead", Color) = (0.12, 0.135, 0.16, 1)
        _Glow ("Dawn In It", Color) = (0.3, 0.2, 0.13, 1)
        _Wind ("Wind", Vector) = (0.9, 0.05, 0.5, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "OpenFog"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define STEPS 14

            TEXTURE3D(_Noise);
            SAMPLER(sampler_Noise);

            float _Density, _Ceiling, _Reach, _Banks, _Wisps, _Lumps;
            float4 _Low, _High, _Glow, _Wind;

            // Set by OpenAir. Nothing at all underground, and nothing short of where the line comes out into the open.
            float _OpenFog;
            float _OpenFogFrom;

            // Bright where it's thin, so that the banks stand out from each other as well as from what's behind them.
            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                if (_OpenFog <= 0.0) return float4(0.0, 0.0, 0.0, 0.0);

                float2 uv = input.texcoord;
                float depth = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
                #endif
                float3 world = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                float3 ray = world - _WorldSpaceCameraPos;
                float toSolid = length(ray);
                float3 way = ray / toSolid;
                float end = min(toSolid, _Reach);

                // Steps get longer with distance. Each pixel starts at a different point along the first one, and the film grain hides the rest.
                float start = InterleavedGradientNoise(input.positionCS.xy, (int)(_Time.y * 24.0) % 64);
                float optical = 0.0, shade = 0.0;
                float3 drift = _Wind.xyz * _Time.y;

                for (int i = 0; i < STEPS; i++)
                {
                    float from = (i + start) / STEPS, to = (i + 1.0 + start) / STEPS;
                    float t = end * from * from, span = end * (to * to - from * from);
                    float3 at = _WorldSpaceCameraPos + way * t;

                    float height = max(at.y, 0.0);
                    float3 bank = (at - drift) / _Banks, wisp = (at - drift * 1.8) / _Wisps;
                    float slow = SAMPLE_TEXTURE3D_LOD(_Noise, sampler_Noise, bank * float3(1.0, 2.2, 1.0), 0.0).r;
                    float fine = SAMPLE_TEXTURE3D_LOD(_Noise, sampler_Noise, wisp * float3(1.0, 1.8, 1.0), 0.0).r;

                    float layer = exp(-height / (_Ceiling * (0.45 + slow * 1.1)));
                    float lump = max(1.0 + (slow * 0.66 + fine * 0.34 - 0.5) * _Lumps * 2.0, 0.0);
                    float amount = _Density * layer * lump
                        * smoothstep(0.6, 5.0, t)
                        * smoothstep(_OpenFogFrom - 8.0, _OpenFogFrom + 14.0, at.z);

                    optical += amount * span;
                    shade += amount * span * (0.62 + 0.76 * fine * slow * 2.0);
                }

                float mist = 1.0 - exp(-optical);
                shade = optical > 0.0001 ? shade / optical : 1.0;

                float3 color = lerp(_Low.rgb, _High.rgb, saturate(way.y * 2.6)) * lerp(0.82, 1.2, saturate(shade * 0.8));
                float east = saturate(dot(way, normalize(float3(1.0, 0.1, 0.32))));
                color += _Glow.rgb * pow(east, 5.0) * (1.0 + shade * 0.25);

                return float4(color, mist * _OpenFog);
            }
            ENDHLSL
        }
    }
}
