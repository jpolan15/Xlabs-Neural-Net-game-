// Convergence/Festival: Level 1's night festival town in one opaque material (Quest 2: no textures, no lights).
// Vertex colour rgb is the colour, alpha is how much it glows: 0 is a matte surface (wood, roofs, stone) shaded by a
// fixed key from above plus a warm bounce from the lanterns below; 1 is a paper lantern or a lit window, full bright,
// with a slow flicker that differs per lantern (from its world position). Fog fades the town into the night.
Shader "Convergence/Festival"
{
    Properties
    {
        _Warm ("Warm Bounce", Color) = (0.32, 0.18, 0.10, 1)
        _Flicker ("Flicker", Range(0, 0.3)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Festival"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Warm;
                half _Flicker;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : TEXCOORD0;
                half light : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs v = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = v.positionCS;
                float3 n = TransformObjectToWorldNormal(input.normalOS);
                // Moonlight from above, warm lantern bounce from below and the sides.
                half key = saturate(dot(n, normalize(float3(-0.3, 0.8, 0.4))));
                half bounce = saturate(0.6 - n.y * 0.5);
                half matte = 0.38 + 0.5 * key;
                // One flicker per lantern: the world position, rounded to the lantern, picks the phase.
                float3 cell = floor(v.positionWS * 0.4);
                float phase = frac(sin(dot(cell, float3(12.9898, 78.233, 37.719))) * 43758.5453) * 6.2831;
                half flicker = 1.0 - _Flicker + _Flicker * sin(_Time.y * 2.7 + phase) * 0.5 + _Flicker * 0.5 * sin(_Time.y * 7.1 + phase * 2.0);
                o.light = lerp(matte, flicker * 1.15, input.color.a);
                o.color = half4(input.color.rgb + _Warm.rgb * bounce * (1.0 - input.color.a), input.color.a);
                o.fogFactor = ComputeFogFactor(v.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 c = input.color.rgb * input.light;
                // Lanterns keep more of their colour through the fog than the wood around them.
                half fog = lerp(input.fogFactor, input.fogFactor * 0.55, input.color.a);
                c = MixFog(c, fog);
                return half4(c, 1.0h);
            }
            ENDHLSL
        }
    }
}
