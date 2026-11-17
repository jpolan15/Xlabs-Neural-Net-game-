// Convergence/FestivalGlow: every soft light of the festival in one additive draw (lantern halos, light pools on the
// street, sky lanterns, the moon). No texture: the falloff is computed. Per vertex:
//   position  the glow's centre (all four corners share it) when uv1.x > 0; else the corner itself (a flat pool)
//   uv0       the corner, -1..1
//   uv1       x: half size in metres (billboard; 0 = flat), y: rise speed in m/s (sky lanterns; 0 = still)
//   uv2       x: a hard disc core radius 0..1 (the moon; 0 = none), y: loop height for risers
//   colour    rgb tint, a strength
// Risers loop from their position up through the loop height, fading in and out, each with its own phase.
Shader "Convergence/FestivalGlow"
{
    Properties
    {
        _Strength ("Strength", Range(0, 3)) = 1
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "FestivalGlow"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Strength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : TEXCOORD0;
                float2 corner : TEXCOORD1;
                half core : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                half fade = 1.0;
                if (input.uv1.y > 0.0)
                {
                    float loopHeight = max(1.0, input.uv2.y);
                    float seed = frac(sin(dot(floor(world.xz * 3.0), float2(12.9898, 78.233))) * 43758.5453);
                    float t = frac(_Time.y * input.uv1.y / loopHeight + seed);
                    world.y += t * loopHeight;
                    world.x += sin(_Time.y * 0.4 + seed * 6.28) * 0.6;
                    fade = smoothstep(0.0, 0.12, t) * (1.0 - smoothstep(0.75, 1.0, t));
                }

                float3 view = TransformWorldToView(world);
                if (input.uv1.x > 0.0) view.xy += input.uv0 * input.uv1.x;
                o.positionCS = TransformWViewToHClip(view);
                o.color = half4(input.color.rgb, input.color.a * fade);
                o.corner = input.uv0;
                o.core = input.uv2.x;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half r = length(input.corner);
                half soft = saturate(1.0h - r);
                half glow = soft * soft * (0.55h + 0.45h * soft);
                half disc = input.core > 0.0h ? (1.0h - smoothstep(input.core - 0.02h, input.core, r)) * 1.6h : 0.0h;
                half3 c = input.color.rgb * (glow + disc) * input.color.a * _Strength;
                // Additive: fog fades a glow toward nothing, not toward the fog colour.
                // The moon (a disc) hangs beyond the fog.
                if (input.core <= 0.0h) c = MixFogColor(c, half3(0, 0, 0), input.fogFactor);
                return half4(c, 1.0h);
            }
            ENDHLSL
        }
    }
}
