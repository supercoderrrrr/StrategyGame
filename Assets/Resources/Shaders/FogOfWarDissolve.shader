Shader "StrategyGame/FogOfWarDissolve"
{
    Properties
    {
        _FogColor("Fog Color", Color) = (0.015, 0.025, 0.045, 1)
        _FogHighlight("Fog Highlight", Color) = (0.08, 0.14, 0.2, 1)
        _RevealAmount("Reveal Amount", Range(0, 1)) = 0
        _NoiseScale("Noise Scale", Float) = 0.72
        _NoiseSpeed("Noise Speed", Float) = 0.48
        _BreakupStrength("Breakup Strength", Range(0, 0.5)) = 0.32
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "FogOfWar"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                half4 _FogHighlight;
                float _RevealAmount;
                float _NoiseScale;
                float _NoiseSpeed;
                float _BreakupStrength;
            CBUFFER_END

            float Hash31(float3 value)
            {
                value = frac(value * 0.1031);
                value += dot(value, value.yzx + 33.33);
                return frac((value.x + value.y) * value.z);
            }

            float ValueNoise(float3 position)
            {
                float3 cell = floor(position);
                float3 blend = frac(position);
                blend = blend * blend * (3.0 - 2.0 * blend);

                float x00 = lerp(Hash31(cell), Hash31(cell + float3(1, 0, 0)), blend.x);
                float x10 = lerp(Hash31(cell + float3(0, 1, 0)), Hash31(cell + float3(1, 1, 0)), blend.x);
                float x01 = lerp(Hash31(cell + float3(0, 0, 1)), Hash31(cell + float3(1, 0, 1)), blend.x);
                float x11 = lerp(Hash31(cell + float3(0, 1, 1)), Hash31(cell + float3(1, 1, 1)), blend.x);
                return lerp(lerp(x00, x10, blend.y), lerp(x01, x11, blend.y), blend.z);
            }

            float FogNoise(float3 positionWS)
            {
                float time = _Time.y * _NoiseSpeed;
                float3 drift = float3(time, time * 0.27, -time * 0.63);
                float lowFrequency = ValueNoise(positionWS * _NoiseScale + drift);
                float mediumFrequency = ValueNoise(positionWS * (_NoiseScale * 2.05) - drift * 1.35);
                float highFrequency = ValueNoise(positionWS * (_NoiseScale * 4.1) + drift * 1.9);
                return lowFrequency * 0.5 + mediumFrequency * 0.32 + highFrequency * 0.18;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float noise = FogNoise(input.positionWS);
                float revealActive = step(0.0001, _RevealAmount);
                float contrast = 1.0 + _BreakupStrength * 3.0;
                float dissolveNoise = saturate((noise - 0.5) * contrast + 0.5);
                float dissolveField = dissolveNoise - _RevealAmount;
                clip(lerp(1.0, dissolveField, revealActive));

                half wisps = smoothstep(0.18, 0.86, noise);
                half dissolveEdge = revealActive * (1.0h - smoothstep(0.0h, 0.065h, dissolveField));
                half3 color = lerp(_FogColor.rgb, _FogHighlight.rgb, wisps * 0.38h + dissolveEdge * 0.28h);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
