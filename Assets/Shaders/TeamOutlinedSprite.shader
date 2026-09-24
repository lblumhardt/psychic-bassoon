Shader "Mons/Team Outlined Sprite"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Team Outline", Color) = (0.2,0.75,1,1)
        _OutlineWidth ("Outline Width", Range(0.5, 3)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float4 _Color;
            float4 _OutlineColor;
            float _OutlineWidth;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (sprite.a > 0.05h)
                    return sprite * _Color;

                float2 stepUv = _MainTex_TexelSize.xy * _OutlineWidth;
                half neighborAlpha = 0;
                neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(stepUv.x, 0)).a);
                neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(stepUv.x, 0)).a);
                neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, stepUv.y)).a);
                neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(0, stepUv.y)).a);
                neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + stepUv).a);
                neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - stepUv).a);
                neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(stepUv.x, -stepUv.y)).a);
                neighborAlpha = max(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-stepUv.x, stepUv.y)).a);

                if (neighborAlpha > 0.05h)
                    return half4(_OutlineColor.rgb, _OutlineColor.a * neighborAlpha);
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
