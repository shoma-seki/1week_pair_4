Shader "Custom/AudienceFanGauge"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Waiting Color", Color) = (0.5, 0.5, 0.5, 1)
        _FanColor ("Fan Color", Color) = (1, 1, 1, 1)
        _FanFill ("Fan Fill", Range(0, 1)) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        [HideInInspector] _BoundsMinY ("Bounds Min Y", Float) = -0.5
        [HideInInspector] _BoundsMaxY ("Bounds Max Y", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

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
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float heightOS : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _FanColor;
                float _FanFill;
                float _Cutoff;
                float _BoundsMinY;
                float _BoundsMaxY;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.heightOS = input.positionOS.y;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 textureColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(textureColor.a - _Cutoff);

                float normalizedHeight = saturate(
                    (input.heightOS - _BoundsMinY) / max(_BoundsMaxY - _BoundsMinY, 0.0001)
                );
                half isFilled = step(normalizedHeight, _FanFill);
                half4 gaugeColor = lerp(_BaseColor, _FanColor, isFilled);
                return textureColor * gaugeColor;
            }
            ENDHLSL
        }
    }
}
