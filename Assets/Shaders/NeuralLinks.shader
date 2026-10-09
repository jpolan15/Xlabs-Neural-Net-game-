// Convergence/NeuralLinks: every connection of the big network in ONE mesh and ONE draw call.
// Each link is a camera-facing ribbon built in the vertex shader from the centerline (position), the direction
// (normal), and the side (uv.y = -1 or +1). Vertex colour carries the data:
//   r = weight size 0..1 (width and brightness), g = weight sign (1 positive, 0 negative),
//   b = progress along the track 0..1 (where the light has reached, see _LitUpTo), a = pulse phase.
// uv.x is the distance along the link in meters, so pulses flow continuously and race forward.
// Weight is a hierarchy, not a tint: a strong link is wide and bright, a weak one thin and faint, so the eye can tell them apart.
// _Reveal fades the connections in one by one (each ribbon's random phase decides when): 0 shows none, 1 shows all.
//
// Thin far-away lines are the classic VR shimmer problem: a ribbon narrower than a pixel breaks into dots that crawl
// as the head moves. So the ribbon is never drawn narrower than _MinPixels on screen; when it is widened, its
// brightness is scaled by the coverage it really has, so a distant line fades out smoothly instead of aliasing.
Shader "Convergence/NeuralLinks"
{
    Properties
    {
        _PosColor ("Positive Weight", Color) = (0.1, 0.88, 1, 1)
        _NegColor ("Negative Weight", Color) = (1, 0.8, 0.12, 1)
        _Width ("Width (m)", Float) = 0.14
        _MinPixels ("Minimum Width (pixels)", Float) = 1.6
        _MaxPixels ("Maximum Width (pixels)", Float) = 4
        _Focus ("Focus (1 = full, lower = the network steps back)", Range(0, 1)) = 1
        _Dim ("Brightness Before Lit", Range(0, 1)) = 0.4
        _Reveal ("Reveal (0 = dark, 1 = all)", Range(0, 1)) = 1
        _PulseSpacing ("Pulse Spacing (m)", Float) = 5
        _PulseSpeed ("Pulse Speed (m/s)", Float) = 7
        _NearStart ("Fade Out Closer Than (m)", Float) = 1.6
        _NearEnd ("Fully Visible Beyond (m)", Float) = 4.2
        _FarStart ("Start Fading At (m)", Float) = 38
        _FarEnd ("Gone By (m)", Float) = 95
        _LitUpTo ("Lit Up To (progress)", Float) = 0.06
        _Ignite ("Ignite", Range(0, 2)) = 0
        _Flow ("Flow Clock", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Links"
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
                half4 _PosColor;
                half4 _NegColor;
                float _Width;
                float _MinPixels;
                float _MaxPixels;
                half _Focus;
                half _Dim;
                half _Reveal;
                float _PulseSpacing;
                float _PulseSpeed;
                float _NearStart;
                float _NearEnd;
                float _FarStart;
                float _FarEnd;
                float _LitUpTo;
                half _Ignite;
                float _Flow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
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

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 directionWS = normalize(TransformObjectToWorldDir(input.normalOS));
                float3 toCamera = GetWorldSpaceViewDir(positionWS);
                float distanceToCamera = length(toCamera);
                float3 side = normalize(cross(directionWS, toCamera / max(distanceToCamera, 0.001)));

                // World size of one pixel at this distance, from the projection's vertical scale.
                float pixelWorld = 2.0 * distanceToCamera / (abs(UNITY_MATRIX_P._m11) * _ScreenParams.y);
                float width = _Width * (0.25 + 0.75 * input.color.r);
                float minWorld = pixelWorld * _MinPixels;
                float drawn = clamp(width, minWorld, max(minWorld, pixelWorld * _MaxPixels));
                positionWS += side * (input.uv.y * 0.5 * drawn);

                float nearFade = saturate((distanceToCamera - _NearStart) / max(0.01, _NearEnd - _NearStart));
                float farFade = 1.0 - saturate((distanceToCamera - _FarStart) / max(0.01, _FarEnd - _FarStart));

                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.color = input.color;
                output.fade = nearFade * farFade * saturate(width / drawn);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half across = 1.0h - abs(input.uv.y);
                half glow = across * across;
                half core = pow(across, 6.0h);

                // A bright head with a fading tail, racing forward along the link.
                float along = (input.uv.x - _Flow * _PulseSpeed) / max(0.1, _PulseSpacing) + input.color.a * 7.0;
                half f = frac(along);
                half pulse = smoothstep(0.0h, 0.04h, f) * (1.0h - smoothstep(0.04h, 0.40h, f));

                // Light that a solved stop has switched on, a soft front travelling along the track.
                half lit = 1.0h - smoothstep(_LitUpTo, _LitUpTo + 0.05h, input.color.b);
                half level = lerp(_Dim, 1.0h, lit) + _Ignite;

                // Strong weights burn bright and weak ones stay faint (squared, so the gap is wide).
                half weight = 0.15h + 0.85h * input.color.r * input.color.r;
                half3 hue = lerp(_NegColor.rgb, _PosColor.rgb, input.color.g);
                half brightness = (glow * 0.35h + core * 1.1h + pulse * (0.5h + 1.8h * glow)) * weight * level;

                // Connections appear one by one as _Reveal rises; the phase (a) is each ribbon's turn.
                half reveal = saturate((_Reveal * 1.1h - input.color.a) * 12.5h);

                half3 color = hue * brightness * input.fade * _Focus * reveal;
                color = MixFogColor(color, half3(0, 0, 0), input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
