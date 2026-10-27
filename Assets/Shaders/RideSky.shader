// Convergence/RideSky: the ride's sky, one draw for the whole background (Quest 2 cheap: no textures, no loops).
// A gradient from the horizon colour up to the zenith colour, a soft glow band on the horizon, sparse static stars
// above it, and below it an infinite grid floor a few metres under the eye. The floor is found per eye from the camera
// position, so it has real stereo depth and scrolls as the pod travels. Colours are globals set by RegionAtmosphere
// (every camera shares them, and they blend region to region):
//   _RideSkyTop      zenith colour
//   _RideSkyHorizon  horizon colour (the fog matches it, so far things sink into the horizon)
//   _RideSkyGlow     rgb: horizon glow colour, a: its strength
//   _RideSkyFloor    rgb: grid colour, a: grid strength (0 = no floor)
//   _RideSkyParams   x: star brightness, y: floor depth below the eye (m), z: grid spacing (m)
Shader "Convergence/RideSky"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "RideSky"
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

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 d = normalize(input.direction);
                float y = d.y;

                // Gradient: horizon up to the zenith; below the horizon the same colours, darker, toward the nadir.
                half3 up = lerp(_RideSkyHorizon.rgb, _RideSkyTop.rgb, pow(saturate(y), 0.55));
                half3 down = lerp(_RideSkyHorizon.rgb * 0.8, _RideSkyTop.rgb * 0.45, pow(saturate(-y), 0.45));
                half3 color = y >= 0.0 ? up : down;

                // The horizon glow: a soft band, a little wider above than below.
                color += _RideSkyGlow.rgb * _RideSkyGlow.a * exp(-abs(y) * (y >= 0.0 ? 7.0 : 12.0));

                // Stars: one in about seventy cells of a 90-per-radian grid, a soft dot a few pixels wide, static.
                float3 cell = floor(d * 90.0);
                float pick = Hash(cell);
                float3 jitter = float3(Hash(cell + 1.7), Hash(cell + 3.1), Hash(cell + 5.3)) - 0.5;
                float3 centre = normalize((cell + 0.5 + jitter * 0.6) / 90.0);
                float star = (1.0 - smoothstep(0.0008, 0.0030, distance(d, centre))) * step(0.986, pick);
                color += star * _RideSkyParams.x * saturate(y * 5.0) * (0.45 + 0.55 * Hash(cell + 9.2));

                // The floor: where this pixel's ray meets a plane depth metres below the eye, a grid of lines.
                // Derivatives are taken for every pixel (outside any branch), then masked to below the horizon.
                float down01 = max(-y, 0.012);
                float reach = _RideSkyParams.y / down01;
                float3 eye = GetCameraPositionWS();
                float2 p = (eye.xz + d.xz * reach) / max(_RideSkyParams.z, 0.1);
                float2 aa = fwidth(p);
                float2 dist = abs(frac(p - 0.5) - 0.5);
                float2 lines = 1.0 - smoothstep(0.018 - aa, 0.018 + aa, dist);
                float grid = max(lines.x, lines.y);
                float fade = exp(-reach * 0.022) * saturate(1.0 - max(aa.x, aa.y) * 3.0) * saturate((-y - 0.01) * 12.0);
                color += _RideSkyFloor.rgb * (_RideSkyFloor.a * grid * fade);
                // A faint wash of the floor colour near the eye, so the floor reads as a surface, not only lines.
                color += _RideSkyFloor.rgb * (_RideSkyFloor.a * 0.06 * fade);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
