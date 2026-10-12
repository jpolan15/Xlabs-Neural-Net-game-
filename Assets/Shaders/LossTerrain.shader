// Convergence/LossTerrain: the Valley's ground, whose height is real training loss. Coloured by height (low = deep,
// high = bright), with contour lines every _ContourSpacing in object-space height (one contour per 0.25 loss), a fixed
// key light and a rim, as in HoloLit. Object space is used for the height, so the gentle mode (the Valley root scaled
// in y) keeps the contours on the same loss values. Opaque, two-sided, so the closed hull never shows a hole.
Shader "Convergence/LossTerrain"
{
    Properties
    {
        _LowColor ("Low (less wrong)", Color) = (0.20, 0.07, 0.30, 1)
        _HighColor ("High (more wrong)", Color) = (1.00, 0.44, 0.57, 1)
        _HeightMin ("Height Of Low Color (object y)", Float) = -12
        _HeightMax ("Height Of High Color (object y)", Float) = 6
        _ContourColor ("Contour Line", Color) = (1, 0.85, 0.95, 1)
        _ContourSpacing ("Contour Spacing (object y)", Float) = 1.25
        _ContourOffset ("Contour Offset (object y)", Float) = 0
        _ContourWidth ("Contour Width (fraction)", Range(0.005, 0.3)) = 0.06
        _RimColor ("Rim Color", Color) = (1, 0.6, 0.85, 1)
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.35
        _Shade ("Shade Amount", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        Pass
        {
            Name "LossTerrain"
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
                half4 _LowColor;
                half4 _HighColor;
                float _HeightMin;
                float _HeightMax;
                half4 _ContourColor;
                float _ContourSpacing;
                float _ContourOffset;
                half _ContourWidth;
                half4 _RimColor;
                half _RimStrength;
                half _Shade;
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
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float heightOS : TEXCOORD2;
                half surface : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.heightOS = input.positionOS.y;
                output.surface = input.color.r; // 1 on the loss surface, 0 on the skirts and base
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input, bool front : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 n = normalize(input.normalWS) * (front ? 1.0h : -1.0h);
                half3 v = normalize(GetWorldSpaceViewDir(input.positionWS));
                half rim = pow(saturate(1.0h - saturate(dot(n, v))), 2.6h) * _RimStrength;
                half3 nView = normalize(TransformWorldToViewDir(n));
                half key = saturate(dot(nView, normalize(half3(-0.35h, 0.65h, 0.70h))));
                half shade = lerp(1.0h, 0.42h + 0.78h * key, _Shade);

                half t = saturate((input.heightOS - _HeightMin) / max(0.001, _HeightMax - _HeightMin));
                half3 albedo = lerp(_LowColor.rgb, _HighColor.rgb, t);

                // Contour lines, anti-aliased with the screen-space derivative so they stay crisp at any distance.
                float c = (input.heightOS - _ContourOffset) / max(0.001, _ContourSpacing);
                float d = abs(frac(c + 0.5) - 0.5);
                float w = max(_ContourWidth * 0.5, fwidth(c));
                half contour = (1.0h - smoothstep(w, w * 2.0, d)) * input.surface;

                half3 color = albedo * shade * lerp(0.55h, 1.0h, input.surface) + _RimColor.rgb * rim;
                color = lerp(color, _ContourColor.rgb, contour * 0.85h);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
