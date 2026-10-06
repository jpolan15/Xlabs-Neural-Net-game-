Shader "Chamber01/NeuronBoundary"
{
    Properties
    {
        _W1 ("Weight ROCK", Float) = 0
        _W2 ("Weight ICE", Float) = 0
        _B  ("Bias", Float) = -1
        _LineWidth ("Line half-width", Float) = 0.02
        _FireColor ("Fire side (cyan)", Color) = (0.1, 0.8, 0.9, 1)
        _HoldColor ("Hold side (slate)", Color) = (0.30, 0.38, 0.46, 1)
        _GridColor ("Grid", Color) = (0.7, 0.85, 1, 0.35)
        _H1 ("Hidden 1", Vector) = (0, 0, 0, 0)
        _H2 ("Hidden 2", Vector) = (0, 0, 0, 0)
        _O ("Output mix", Vector) = (0, 0, 0, 0)
        _UseHidden ("Use hidden layer", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _W1, _W2, _B, _LineWidth;
                half4 _FireColor, _HoldColor, _GridColor;
                float4 _H1, _H2, _O;
                float _UseHidden;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv * 1.5 - 0.25;
                float s = _W1 * p.x + _W2 * p.y + _B;
                float len = max(length(float2(_W1, _W2)), 1e-3);
                float d = s / len;
                float aa = max(fwidth(d), 1e-4);
                float side = smoothstep(-aa, aa, d);
                half3 col = lerp(_HoldColor.rgb, _FireColor.rgb, side) * 0.22;

                if (_UseHidden > 0.5)
                {
                    float h1 = step(0.0, dot(_H1.xy, p) + _H1.z);
                    float h2 = step(0.0, dot(_H2.xy, p) + _H2.z);
                    float mixed = _O.x * h1 + _O.y * h2 + _O.z;
                    float hiddenSide = step(0.0, mixed);
                    col = lerp(_HoldColor.rgb, _FireColor.rgb, hiddenSide) * 0.22;
                    float h1len = max(length(_H1.xy), 1e-3);
                    float h2len = max(length(_H2.xy), 1e-3);
                    float d1 = (dot(_H1.xy, p) + _H1.z) / h1len;
                    float d2 = (dot(_H2.xy, p) + _H2.z) / h2len;
                    float faint1 = 1.0 - smoothstep(_LineWidth, _LineWidth + aa, abs(d1));
                    float faint2 = 1.0 - smoothstep(_LineWidth, _LineWidth + aa, abs(d2));
                    col = lerp(col, half3(0.75, 0.9, 1.0), saturate(faint1 + faint2) * 0.45);
                    d = mixed;
                    len = 1.0;
                }

                float2 g = abs(frac(p + 0.5) - 0.5);
                float2 gw = fwidth(p);
                float grid = 1.0 - smoothstep(0.0, 1.5 * max(gw.x, gw.y), min(g.x, g.y));
                col = lerp(col, _GridColor.rgb, grid * _GridColor.a);

                float line = 1.0 - smoothstep(_LineWidth, _LineWidth + aa, abs(d / max(len, 1e-3)));
                if (_UseHidden > 0.5) line = 0.0;
                col = lerp(col, half3(1, 1, 1), line);

                return half4(col, saturate(0.45 + 0.55 * line));
            }
            ENDHLSL
        }
    }
}
