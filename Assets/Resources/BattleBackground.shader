Shader "Mons/Groovy Battle Background"
{
    Properties
    {
        _Pattern ("Design", Range(0, 2)) = 0
        _AnimationTime ("Animation Time", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite On
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            CBUFFER_START(UnityPerMaterial)
                float _Pattern;
                float _AnimationTime;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // Screen coordinates make the floor and distant backdrop one continuous design.
                float2 uv = input.positionCS.xy / _ScaledScreenParams.xy;
                float2 p = (floor(uv * float2(320, 240)) / float2(320, 240) - 0.5) * 2;
                p.x *= _ScaledScreenParams.x / _ScaledScreenParams.y;
                float t = _AnimationTime * 0.32;
                float value;
                float3 darkColor, midColor, lightColor;
                if (_Pattern < 0.5)
                {
                    // Violet / teal checkerboard with opposing horizontal sine distortions.
                    float2 q = p;
                    q.x += 0.22 * sin(p.y * 8 + t * 2);
                    q.y += 0.10 * sin(p.x * 6 - t);
                    q += float2(t * 0.15, -t * 0.12);
                    value = 0.5 + 0.5 * sin(q.x * 14) * sin(q.y * 14);
                    darkColor = float3(0.055, 0.025, 0.13);
                    midColor = float3(0.28, 0.075, 0.38);
                    lightColor = float3(0.055, 0.48, 0.48);
                }
                else if (_Pattern < 1.5)
                {
                    // Gold / coral concentric waves, stretched and gently wobbling.
                    float2 q = p + 0.12 * float2(sin(p.y * 5 + t), cos(p.x * 4 - t));
                    float rings = length(q * float2(0.8, 1.25)) * 25;
                    value = 0.5 + 0.5 * sin(rings - t * 3 + sin(atan2(q.y, q.x) * 5 + t));
                    darkColor = float3(0.12, 0.035, 0.055);
                    midColor = float3(0.43, 0.10, 0.18);
                    lightColor = float3(0.65, 0.38, 0.075);
                }
                else
                {
                    // Blue / lime plasma with layered diagonal currents.
                    float wave = sin(p.x * 9 + t) + sin(p.y * 11 - t * 1.3);
                    wave += sin((p.x + p.y) * 7 + sin(p.y * 4 + t));
                    value = 0.5 + 0.5 * sin(wave * 1.8 + t);
                    darkColor = float3(0.02, 0.055, 0.16);
                    midColor = float3(0.035, 0.24, 0.39);
                    lightColor = float3(0.32, 0.52, 0.16);
                }
                // Banded palettes and subtle scanlines evoke the original low-resolution effects.
                value = floor(saturate(value) * 5) / 5;
                float3 color = value < 0.5 ? lerp(darkColor, midColor, value * 2)
                    : lerp(midColor, lightColor, (value - 0.5) * 2);
                color *= lerp(0.72, 1.0, saturate(1.0 - dot(p * 0.32, p * 0.32)));
                color *= 0.94 + 0.06 * cos(uv.y * 240 * 3.141593);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
