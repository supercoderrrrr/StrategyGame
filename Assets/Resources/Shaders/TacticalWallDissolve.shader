Shader "StrategyGame/TacticalWallDissolve"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
        _CutoutPositionWS("Cutout World Position", Vector) = (0,0,0,1)
        _CutoutRadius("Cutout Radius", Range(0.01,0.5)) = 0.27
        _CutoutAmount("Cutout Amount", Range(0,1)) = 0
        _CutoutEdgeWidth("Cutout Edge Width", Range(0.001,0.1)) = 0.012
        _NoiseScale("Noise Scale", Float) = 2.5
        _NoiseWidth("Noise Width", Range(0,0.1)) = 0.018
        _BoundaryWaveAmplitude("Boundary Wave Amplitude", Range(0,0.05)) = 0.009
        _BoundaryNoiseAmplitude("Boundary Noise Amplitude", Range(0,0.05)) = 0.007
        _BoundaryWaveSpeed("Boundary Wave Speed", Range(0,3)) = 1.3
        _EdgeIntensity("Edge Intensity", Range(0,2)) = 0.35
        [HDR] _EdgeColor("Edge Color", Color) = (0.1,1.5,2.5,1)
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
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                float4 screenPosition : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EdgeColor;
                float4 _CutoutPositionWS;
                float _CutoutRadius;
                float _CutoutAmount;
                float _CutoutEdgeWidth;
                float _NoiseScale;
                float _NoiseWidth;
                float _BoundaryWaveAmplitude;
                float _BoundaryNoiseAmplitude;
                float _BoundaryWaveSpeed;
                float _EdgeIntensity;
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

                float x00 = lerp(Hash31(cell), Hash31(cell + float3(1,0,0)), blend.x);
                float x10 = lerp(Hash31(cell + float3(0,1,0)), Hash31(cell + float3(1,1,0)), blend.x);
                float x01 = lerp(Hash31(cell + float3(0,0,1)), Hash31(cell + float3(1,0,1)), blend.x);
                float x11 = lerp(Hash31(cell + float3(0,1,1)), Hash31(cell + float3(1,1,1)), blend.x);
                return lerp(lerp(x00, x10, blend.y), lerp(x01, x11, blend.y), blend.z);
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
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.screenPosition = ComputeScreenPos(positionInputs.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float smoothNoise = ValueNoise(input.positionWS * _NoiseScale);
                float blockNoise = Hash31(floor(input.positionWS * _NoiseScale * 1.35));
                float noise = lerp(smoothNoise, blockNoise, 0.68);
                float2 screenUV = input.screenPosition.xy / input.screenPosition.w;
                float4 cutoutPositionCS = TransformWorldToHClip(_CutoutPositionWS.xyz);
                float4 cutoutScreenPosition = ComputeScreenPos(cutoutPositionCS);
                float2 cutoutUV = cutoutScreenPosition.xy / cutoutScreenPosition.w;
                float2 circleOffset = screenUV - cutoutUV;
                circleOffset.x *= _ScaledScreenParams.x / _ScaledScreenParams.y;

                float angle = atan2(circleOffset.y, circleOffset.x);
                float waveTime = _Time.y * _BoundaryWaveSpeed;
                float boundaryWave = sin(angle * 7.0 + smoothNoise * 2.4 + waveTime) * 0.65;
                boundaryWave += sin(angle * 13.0 - smoothNoise * 1.7 - waveTime * 0.73) * 0.35;
                float boundaryNoise = (smoothNoise - 0.5) * 2.0;
                float circleDistance = length(circleOffset) - _CutoutRadius;
                circleDistance += boundaryWave * _BoundaryWaveAmplitude;
                circleDistance += boundaryNoise * _BoundaryNoiseAmplitude;
                float noiseDistance = (noise - saturate(_CutoutAmount)) * _NoiseWidth;
                float dissolveDistance = max(circleDistance, noiseDistance);
                float cutoutActive = step(0.001, _CutoutAmount);
                clip(lerp(1.0, dissolveDistance, cutoutActive));

                half4 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = albedoSample.rgb * _BaseColor.rgb;
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half lighting = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half3 litColor = albedo * (0.28h + mainLight.color * lighting * mainLight.shadowAttenuation);
                half edgeVariation = lerp(0.35h, 1.0h, smoothNoise);
                half edge = cutoutActive * _EdgeIntensity * edgeVariation;
                edge *= 1.0h - smoothstep(0.0h, _CutoutEdgeWidth, dissolveDistance);
                half3 color = litColor + _EdgeColor.rgb * edge;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 GetShadowPositionHClip(ShadowAttributes input)
            {
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                float3 lightDirectionWS = _LightDirection;

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    lightDirectionWS = normalize(_LightPosition - positionInputs.positionWS);
                #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionInputs.positionWS, normalInputs.normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            ShadowVaryings ShadowPassVertex(ShadowAttributes input)
            {
                ShadowVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }
    }
}
