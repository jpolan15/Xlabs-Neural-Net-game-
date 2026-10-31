// Convergence/DaySky: Level 2's daylight sky, one draw for the whole background (Quest 2 cheap: two texture reads per pixel, no loops).
// A gradient from the horizon colour up to the zenith colour, a sun with a soft halo, drifting clouds on a plane high above, and below
// the horizon a meadow on the ground plane (world y = _GroundY) that fades into the horizon haze exactly as the fog fades objects, so
// the road and the trees sit on it. The ground is found per eye from the camera position, so it has real stereo depth and scrolls as
// the bike travels. Colours are the same globals RegionAtmosphere sets for Convergence/RideSky (they blend stop to stop):
//   _RideSkyTop      zenith colour
//   _RideSkyHorizon  horizon colour (the fog matches it)
//   _RideSkyGlow     rgb: sun and halo colour, a: halo strength
//   _RideSkyFloor    rgb: meadow colour, a: how much meadow (0 = horizon colour all the way down)
//   _RideSkyParams   x: cloud cover (0 none, 1 fair weather, 2 overcast)
Shader "Convergence/DaySky"
{
    Properties
    {
        _CloudTex ("Cloud Noise (tileable)", 2D) = "gray" {}
        _SunDir ("Sun Direction", Vector) = (0.35, 0.42, 0.84, 0)
        _GroundY ("Ground Height (world y)", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "DaySky"
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half4 _RideSkyTop;
            half4 _RideSkyHorizon;
            half4 _RideSkyGlow;
            half4 _RideSkyFloor;
            float4 _RideSkyParams;

            TEXTURE2D(_CloudTex);
            SAMPLER(sampler_CloudTex);
            float4 _SunDir;
            float _GroundY;

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 d = normalize(input.direction);
                float y = d.y;
                float3 eye = GetCameraPositionWS();
                float3 sun = normalize(_SunDir.xyz);
                float toSun = saturate(dot(d, sun));

                // Sky: horizon up to the zenith, a warm halo around the sun and a small bright disc.
                half3 sky = lerp(_RideSkyHorizon.rgb, _RideSkyTop.rgb, pow(saturate(y), 0.5));
                sky += _RideSkyGlow.rgb * (_RideSkyGlow.a * (0.55 * pow(toSun, 10.0) + 0.25 * pow(toSun, 2.5)));
                sky = lerp(sky, half3(1.0, 0.98, 0.9), smoothstep(0.9985, 0.9992, toSun));

                // Clouds: two reads of one tileable noise on a plane 450 m up, drifting; thin near the horizon.
                float up = max(y, 0.03);
                float2 cuv = (eye.xz + d.xz * (450.0 / up)) / 3200.0 + float2(_Time.x * 0.04, _Time.x * 0.015);
                half n = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cuv).r * 0.62h + SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cuv * 2.9 + 0.37).r * 0.38h;
                half cover = saturate(_RideSkyParams.x);
                half cloud = smoothstep(0.78h - 0.3h * cover, 0.98h - 0.3h * cover, n) * saturate(y * 7.0) * step(0.001, cover);
                half3 cloudColor = lerp(_RideSkyHorizon.rgb, half3(1.0, 1.0, 1.0), 0.8h) + _RideSkyGlow.rgb * (0.25h * toSun);
                cloudColor *= lerp(0.82h, 1.0h, smoothstep(0.8h, 1.0h, n)); // a little shading inside the thicker parts
                sky = lerp(sky, cloudColor, cloud * 0.92h);

                // The meadow: where this ray meets the ground, two scales of soft patches, fading into the haze like the fog.
                float down = max(-y, 0.0006);
                float reach = max(eye.y - _GroundY, 0.05) / down;
                float2 g = eye.xz + d.xz * reach;
                half p1 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, g / 140.0).r;
                half p2 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, g / 31.0 + 0.5).r;
                half3 grass = _RideSkyFloor.rgb * (0.86h + 0.28h * p1);
                grass = lerp(grass, grass * half3(1.12h, 1.08h, 0.72h), smoothstep(0.45h, 0.75h, p2) * 0.6h);
                half haze = 1.0h - saturate(exp2(-unity_FogParams.x * reach));
                haze = max(haze, smoothstep(0.02, 0.0, -y)); // the last sliver before the horizon is all haze
                half3 ground = lerp(grass, _RideSkyHorizon.rgb, haze);
                ground = lerp(_RideSkyHorizon.rgb, ground, _RideSkyFloor.a);

                half3 color = y >= 0.0 ? sky : ground;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
