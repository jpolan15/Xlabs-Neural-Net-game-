// Convergence/ExplainerFlat: the flat, clean look of the Level 1 explainers (3Blue1Brown-style shapes on the
// blackboard). One mesh draws many shapes (ShapeBatch): each quad carries its own shape in its texture coordinates,
// so a square can round into a circle, a filled cell can have a stroke, and every edge is anti-aliased by its signed
// distance instead of by MSAA. Unlit, alpha blended, no fog (the blackboard has no colour cast), two sided.
//   COLOR  fill colour (alpha = how much is drawn)
//   UV0    texture coordinates (a picture such as a real handwritten digit; white when there is none)
//   UV1    xy: this pixel's position from the shape's centre in metres; zw: the shape's half width and half height
//   UV2    x: corner radius in metres (half the short side = a circle or a round-capped line); y: stroke width
//   UV3    stroke colour
// A quad without a shape in UV1 (z or w = 0; a plain Unity quad has its lightmap UVs there), draws its texture and colour with no shape mask.
Shader "Convergence/ExplainerFlat"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap ("Picture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ExplainerFlat"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 shape : TEXCOORD1;
                float4 style : TEXCOORD2;
                half4 stroke : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 shape : TEXCOORD1;
                float4 style : TEXCOORD2;
                half4 stroke : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _BaseColor;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.shape = input.shape;
                output.style = input.style;
                output.stroke = input.stroke;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 fill = input.color * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float2 hs = input.shape.zw;
                if (min(hs.x, hs.y) < 1e-6) return fill;

                // Rounded-rectangle signed distance, metres (negative inside).
                float r = min(input.style.x, min(hs.x, hs.y));
                float2 q = abs(input.shape.xy) - (hs - r);
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
                float aa = max(fwidth(d), 1e-5);
                half inside = saturate(0.5 - d / aa);

                float w = input.style.y;
                half ring = w > 0.0 ? saturate(0.5 - (abs(d + w * 0.5) - w * 0.5) / aa) : 0.0;
                half strokeA = input.stroke.a * ring;
                half fillA = fill.a * inside;
                half outA = strokeA + fillA * (1.0 - strokeA);
                half3 rgb = (input.stroke.rgb * strokeA + fill.rgb * fillA * (1.0 - strokeA)) / max(outA, 1e-4);
                return half4(rgb, outA);
            }
            ENDHLSL
        }
    }
}
