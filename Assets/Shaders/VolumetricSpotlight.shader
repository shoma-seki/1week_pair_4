Shader "Custom/VolumetricSpotlight"
{
    Properties
    {
        [HDR] _Color ("Beam Color", Color) = (1, 0.25, 0.6, 1)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _EdgeSoftness ("Edge Softness", Range(0.01, 1)) = 0.35
        _LengthFade ("Length Fade", Range(0.01, 1)) = 0.2
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.12
        _NoiseScale ("Noise Scale", Float) = 2
        _NoiseSpeed ("Noise Speed", Float) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+10"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "VolumetricBeam"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Front

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Intensity;
                float _EdgeSoftness;
                float _LengthFade;
                float _NoiseStrength;
                float _NoiseScale;
                float _NoiseSpeed;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            // Cheap, texture-free noise keeps the beam from looking like solid plastic.
            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 viewDirection = SafeNormalize(GetCameraPositionWS() - input.positionWS);
                half facing = saturate(abs(dot(normalize(input.normalWS), viewDirection)));

                // uv.y runs from the lamp (0) to the wide end (1).
                float lampFade = smoothstep(0.0, max(_LengthFade, 0.001), input.uv.y);
                float endFade = 1.0 - smoothstep(1.0 - _LengthFade, 1.0, input.uv.y);
                float lengthMask = lampFade * endFade;
                float edgeMask = pow(saturate(1.0 - facing), lerp(4.0, 0.55, _EdgeSoftness));

                float noise = Hash31(floor(input.positionWS * _NoiseScale + _Time.y * _NoiseSpeed));
                noise = lerp(1.0, 0.55 + noise * 0.9, _NoiseStrength);

                half alpha = saturate(_Color.a * _Intensity * lengthMask * edgeMask * noise);
                return half4(_Color.rgb * _Intensity, alpha);
            }
            ENDHLSL
        }
    }
}
