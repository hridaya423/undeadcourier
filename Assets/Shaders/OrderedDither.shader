Shader "UndeadCourier/OrderedDither"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 1)) = 0.35
        _Steps ("Palette Steps", Range(2, 16)) = 5
        _LumaInfluence ("Luminance Influence", Range(0, 1)) = 1
        _PixelSize ("Dither Cell Size", Range(1, 8)) = 2
        _DitherSpread ("Dither Spread", Range(0, 2)) = 1

        _Duotone ("Duotone Blend", Range(0, 1)) = 0
        _ShadowColor ("Duotone Shadow", Color) = (0.02, 0.025, 0.04, 1)
        _LightColor ("Duotone Light", Color) = (0.62, 0.63, 0.60, 1)
        _DuoBlack ("Duotone Black Level", Range(0, 0.5)) = 0.02
        _DuoWhite ("Duotone White Level", Range(0.01, 1)) = 0.15

        _BloodColorDark ("Blood Ink Dark", Color) = (0.055, 0.002, 0.006, 1)
        _BloodColorMid ("Blood Ink Mid", Color) = (0.20, 0.018, 0.025, 1)
        _BloodColorLight ("Blood Ink Light", Color) = (0.55, 0.075, 0.06, 1)
        _BloodSplatSeed ("Blood Splat Seed", Float) = 0
        _BloodCenter ("Blood Center (viewport)", Vector) = (0.5, 0.5, 0, 0)
        _BloodRadius ("Blood Radius", Range(0, 3)) = 0
        _BloodEdge ("Blood Edge Softness", Range(0.01, 1)) = 0.25
        _BloodAmount ("Blood Amount", Range(0, 1)) = 0
        _BloodDrainCenter ("Blood Drain Center (viewport)", Vector) = (0, 0, 0, 0)
        _BloodDrainRadius ("Blood Drain Radius", Range(0, 3)) = 0
        _GrainFloor ("Paper Grain Floor", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "OrderedDither"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag

            float _Intensity;
            float _Steps;
            float _LumaInfluence;
            float _PixelSize;
            float _DitherSpread;
            float _Duotone;
            half4 _ShadowColor;
            half4 _LightColor;
            float _DuoBlack;
            float _DuoWhite;
            half4 _BloodColorDark;
            half4 _BloodColorMid;
            half4 _BloodColorLight;
            float _BloodSplatSeed;
            float4 _BloodCenter;
            float _BloodRadius;
            float _BloodEdge;
            float _BloodAmount;
            float4 _BloodDrainCenter;
            float _BloodDrainRadius;
            float _GrainFloor;

            static const float BAYER8[64] =
            {
                 0.0/64.0, 32.0/64.0,  8.0/64.0, 40.0/64.0,  2.0/64.0, 34.0/64.0, 10.0/64.0, 42.0/64.0,
                48.0/64.0, 16.0/64.0, 56.0/64.0, 24.0/64.0, 50.0/64.0, 18.0/64.0, 58.0/64.0, 26.0/64.0,
                12.0/64.0, 44.0/64.0,  4.0/64.0, 36.0/64.0, 14.0/64.0, 46.0/64.0,  6.0/64.0, 38.0/64.0,
                60.0/64.0, 28.0/64.0, 52.0/64.0, 20.0/64.0, 62.0/64.0, 30.0/64.0, 54.0/64.0, 22.0/64.0,
                 3.0/64.0, 35.0/64.0, 11.0/64.0, 43.0/64.0,  1.0/64.0, 33.0/64.0,  9.0/64.0, 41.0/64.0,
                51.0/64.0, 19.0/64.0, 59.0/64.0, 27.0/64.0, 49.0/64.0, 17.0/64.0, 57.0/64.0, 25.0/64.0,
                15.0/64.0, 47.0/64.0,  7.0/64.0, 39.0/64.0, 13.0/64.0, 45.0/64.0,  5.0/64.0, 37.0/64.0,
                63.0/64.0, 31.0/64.0, 55.0/64.0, 23.0/64.0, 61.0/64.0, 29.0/64.0, 53.0/64.0, 21.0/64.0
            };

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float px = max(_PixelSize, 1.0);
                float2 cell = floor(input.positionCS.xy / px);

                float2 pixelUV = (cell * px + px * 0.5) / _ScaledScreenParams.xy;
                float2 uv = lerp(input.texcoord, pixelUV, saturate(_Duotone));

                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                int bx = (int)cell.x & 7;
                int by = (int)cell.y & 7;
                float threshold = BAYER8[by * 8 + bx] - 0.5;

                float lum = dot(col.rgb, half3(0.2126, 0.7152, 0.0722));

                float steps = max(_Steps, 2.0);
                half3 quant = saturate(floor(col.rgb * steps + 0.5 + threshold * _DitherSpread) / steps);
                float shadowWeight = saturate(1.0 - lum * 2.2);
                float w = _Intensity * lerp(1.0, shadowWeight, _LumaInfluence);
                w *= smoothstep(0.0, 0.2, lum);
                half3 legacyOut = lerp(col.rgb, quant, saturate(w));

                float shapedRaw = saturate((lum - _DuoBlack) / max(_DuoWhite - _DuoBlack, 0.0001));
                float grainMask = smoothstep(_DuoBlack, _DuoBlack + 0.035, lum);
                float shaped = max(shapedRaw, _GrainFloor * grainMask);

                float aspect = _ScaledScreenParams.x / _ScaledScreenParams.y;
                float2 d = input.texcoord - _BloodCenter.xy;
                d.x *= aspect;
                float dist = length(d);
                float angle = atan2(d.y, d.x);
                float lobes = sin(angle * 5.0 + _BloodSplatSeed) * 0.12 + sin(angle * 9.0 + _BloodSplatSeed * 1.7) * 0.06;
                float localRadius = _BloodRadius * saturate(0.88 + lobes);
                float bloodDist = dist;
                float coreBlood = 1.0 - smoothstep(localRadius - _BloodEdge, localRadius, bloodDist);

                float drip1 = 1.0 - smoothstep(0.0, _BloodRadius * 0.34, length(d - float2(0.10, -0.22) * _BloodRadius));
                float drip2 = 1.0 - smoothstep(0.0, _BloodRadius * 0.20, length(d - float2(-0.28, 0.10) * _BloodRadius));
                float drip3 = 1.0 - smoothstep(0.0, _BloodRadius * 0.14, length(d - float2(0.32, 0.19) * _BloodRadius));
                float blood = saturate(coreBlood + (drip1 + drip2 + drip3) * 0.72) * _BloodAmount;

                float2 dd = input.texcoord - _BloodDrainCenter.xy;
                dd.x *= aspect;
                float drainDist = length(dd) + threshold * 0.08;
                blood *= smoothstep(_BloodDrainRadius - _BloodEdge, _BloodDrainRadius, drainDist);

                float duoSteps = max(_Steps, 2.0);
                float tonal = saturate(shaped + threshold * _DitherSpread / duoSteps);
                float on = floor(tonal * (duoSteps - 1.0) + 0.5) / (duoSteps - 1.0);
                float wash = saturate(blood * 0.58);
                float bloodTonal = saturate(wash + threshold * _DitherSpread / duoSteps);
                float bloodOn = floor(bloodTonal * (duoSteps - 1.0) + 0.5) / (duoSteps - 1.0);
                float grain = frac(sin(dot(cell, float2(12.9898, 78.233)) + _BloodSplatSeed * 9.17) * 43758.5453);
                float centerFade = saturate(1.0 - dist / max(_BloodRadius, 0.0001));
                float bloodShade = saturate(on * 0.30 + bloodOn * 0.34 + grain * 0.24 + centerFade * 0.10);
                float brightWet = saturate(centerFade * 0.18 + grain * 0.14 + threshold * 0.08);
                float bloodAlpha = saturate(bloodOn * (0.42 + grain * 0.22));

                half3 bloodInk = lerp(_BloodColorDark.rgb, _BloodColorMid.rgb, bloodShade);
                bloodInk = lerp(bloodInk, _BloodColorLight.rgb, brightWet * 0.35);
                half3 duoOut = lerp(_ShadowColor.rgb, _LightColor.rgb, on);
                duoOut = lerp(duoOut, bloodInk, bloodAlpha);

                col.rgb = lerp(legacyOut, duoOut, saturate(_Duotone));
                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
