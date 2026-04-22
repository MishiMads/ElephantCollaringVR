Shader "Custom/StoneShader_VR"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.75, 0.75, 0.75, 1)
        _BaseMap("Base Map", 2D) = "white" {}

        _LightStrength("Light Strength", Range(0, 2)) = 1
        _ShadowStrength("Shadow Strength", Range(0, 2)) = 0.8

        _CavityStrength("Cavity Strength", Range(0, 8)) = 2.5
        _CavityPower("Cavity Power", Range(0.1, 8)) = 2

        _RimStrength("Rim Strength", Range(0, 4)) = 1.2
        _RimPower("Rim Power", Range(0.1, 8)) = 3

        _HighlightColor("Highlight Color", Color) = (1, 1, 1, 1)
        _CavityColor("Cavity Color", Color) = (0.2, 0.2, 0.2, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // VR + lighting variants
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

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
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _LightStrength;
                float _ShadowStrength;
                float _CavityStrength;
                float _CavityPower;
                float _RimStrength;
                float _RimPower;
                float4 _HighlightColor;
                float4 _CavityColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normalize(normInputs.normalWS);
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = GetShadowCoord(posInputs);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 baseCol = tex.rgb * _BaseColor.rgb;

                float3 smoothNormal = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                // Flat face normal (stable version)
                float3 dpdx = ddx(IN.positionWS);
                float3 dpdy = ddy(IN.positionWS);
                float3 faceNormal = normalize(cross(dpdx, dpdy));

                // Ensure consistent orientation
                faceNormal = faceforward(faceNormal, -viewDir, smoothNormal);

                // Lighting
                Light mainLight = GetMainLight(IN.shadowCoord);
                float shadow = mainLight.shadowAttenuation;

                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(faceNormal, lightDir));

                float lightTerm = lerp(_ShadowStrength, _LightStrength, NdotL);
                lightTerm *= shadow;

                // Cavity
                float normalDiff = 1.0 - saturate(dot(smoothNormal, faceNormal));
                float cavityMask = pow(saturate(normalDiff * _CavityStrength), _CavityPower);

                // Rim
                float rim = 1.0 - saturate(dot(faceNormal, viewDir));
                rim = pow(rim, _RimPower) * _RimStrength;
                rim = saturate(rim);

                float3 col = baseCol * lightTerm;

                col += _HighlightColor.rgb * (cavityMask * 0.55 + rim * 0.35);
                col = lerp(col, col * _CavityColor.rgb, cavityMask * 0.35);

                return float4(col, tex.a * _BaseColor.a);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}