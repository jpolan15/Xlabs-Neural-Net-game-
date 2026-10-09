// Convergence/NeuralNodes: every neuron, speck of dust, and haze cloud of the big network in ONE mesh and ONE draw call.
// Each node is a camera-facing glowing disc built in the vertex shader from its centre (position), the quad
// corner (uv, -1..1) and its radius in meters (uv1.x). Vertex colour carries the data:
//   r = progress along the track 0..1 (where the light has reached), g = hue mix 0..1 (cyan to violet),
//   b = kind (0 neuron, 0.5 dust, 1 the neuron a stop docks at, 0.15 haze cloud), a = phase.
// _Reveal fades neurons and dust in one by one (each one's phase decides when): 0 shows none, 1 shows all. Haze is never
// hidden, so the void keeps its depth while the network is dark. Dust is a dim cool grey so it recedes behind the neurons.
Shader "Convergence/NeuralNodes"
{
    Properties
    {
        _ColorA ("Hue A", Color) = (0.1, 0.88, 1, 1)
        _ColorB ("Hue B", Color) = (0.55, 0.4, 0.95, 1)
        _Dim ("Brightness Before Lit", Range(0, 1)) = 0.4
        _Reveal ("Reveal (0 = dark, 1 = all)", Range(0, 1)) = 1
        _NearStart ("Fade Out Closer Than (m)", Float) = 1.6
        _NearEnd ("Fully Visible Beyond (m)", Float) = 4.2
        _FarStart ("Start Fading At (m)", Float) = 45
        _FarEnd ("Gone By (m)", Float) = 98
        _HazeStrength ("Haze Strength", Range(0, 0.5)) = 0.28
        _Focus ("Focus (1 = full, lower = the network steps back)", Range(0, 1)) = 1
        _LitUpTo ("Lit Up To (progress)", Float) = 0.06
        _Ignite ("Ignite", Range(0, 2)) = 0
        _Flow ("Flow Clock", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Nodes"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA;
                half4 _ColorB;
                half _Dim;
                half _Reveal;
                float _NearStart;
                float _NearEnd;
                float _FarStart;
                float _FarEnd;
                half _HazeStrength;
                half _Focus;
                float _LitUpTo;
                half _Ignite;
                float _Flow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 radius : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : TEXCOORD1;
                half fade : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 centerWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 right = UNITY_MATRIX_V[0].xyz;
                float3 up = UNITY_MATRIX_V[1].xyz;
                float distanceToCamera = length(GetWorldSpaceViewDir(centerWS));

                // Never smaller than about two pixels, so a far neuron stays a clean point of light instead of crawling.
                float pixelWorld = 2.0 * distanceToCamera / (abs(UNITY_MATRIX_P._m11) * _ScreenParams.y);
                float radius = max(input.radius.x, pixelWorld * 2.2);
                float3 positionWS = centerWS + (right * input.uv.x + up * input.uv.y) * radius;

                half kind = input.color.b;
                half isHaze = step(0.05h, kind) * step(kind, 0.25h);
                float nearFade = lerp(saturate((distanceToCamera - _NearStart) / max(0.01, _NearEnd - _NearStart)), 1.0, isHaze);
                half isDockKind = step(0.75h, kind);
                float farFade = lerp(1.0 - saturate((distanceToCamera - _FarStart) / max(0.01, _FarEnd - _FarStart)), 1.0, max(isHaze, isDockKind));

                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.color = input.color;
                output.fade = nearFade * farFade * saturate(input.radius.x / radius);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half r = length(input.uv);
                half kind = input.color.b;
                half isHaze = step(0.05h, kind) * step(kind, 0.25h);
                half isDust = step(0.25h, kind) * step(kind, 0.75h);
                half isDock = step(0.75h, kind);

                half halo = pow(saturate(1.0h - r), 2.2h);
                half body = smoothstep(0.34h, 0.20h, r);
                half centre = smoothstep(0.20h, 0.0h, r);
                half membrane = smoothstep(0.07h, 0.0h, abs(r - 0.38h));
                half twinkle = 0.65h + 0.35h * sin(_Time.y * (1.5h + input.color.a * 3.0h) + input.color.a * 40.0h);
                half beat = 1.0h + isDock * 0.22h * sin(_Time.y * 3.0h);

                // A neuron: a glowing body, a hot centre, a fine membrane ring, and a soft halo.
                half neuron = halo * 0.55h + body * 0.75h + centre * 0.9h + membrane * 0.7h;
                half dust = halo * halo * 1.5h * twinkle;
                half haze = pow(saturate(1.0h - r), 1.6h) * _HazeStrength;
                half shape = lerp(neuron, dust, isDust);
                shape = lerp(shape, haze, isHaze);

                half lit = 1.0h - smoothstep(_LitUpTo, _LitUpTo + 0.05h, input.color.r);
                half level = lerp(_Dim, 1.0h, lit) + _Ignite;
                level = lerp(level, 1.0h, isDust * 0.6h);
                level = lerp(level, 1.0h, isHaze);

                half3 hue = lerp(_ColorA.rgb, _ColorB.rgb, input.color.g);
                hue = lerp(hue, half3(1.0h, 1.0h, 1.0h), 0.5h * isDock * centre + 0.25h * isDock);
                // Dust sinks into the background: a cool grey, well under the neurons' brightness.
                hue = lerp(hue, half3(0.6h, 0.65h, 0.75h), 0.6h * isDust);
                half emphasis = (1.0h + 0.3h * isDock) * lerp(1.0h, 0.45h, isDust);

                // Neurons appear one by one as _Reveal rises; the phase (a) is each one's turn. Haze is never hidden.
                half reveal = lerp(saturate((_Reveal * 1.1h - input.color.a) * 12.5h), 1.0h, isHaze);

                half3 color = hue * shape * level * beat * emphasis * reveal * input.fade * lerp(_Focus, 1.0h, max(isHaze, isDock));
                color = MixFogColor(color, half3(0, 0, 0), lerp(input.fogFactor, 1.0h, isHaze));
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
