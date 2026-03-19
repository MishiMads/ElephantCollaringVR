Shader "Custom/StoneShader"
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
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
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normalize(normInputs.normalWS);
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 baseCol = tex.rgb * _BaseColor.rgb;

                float3 smoothNormal = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                float3 dpdx = ddx(IN.positionWS);
                float3 dpdy = ddy(IN.positionWS);
                float3 faceNormal = normalize(cross(dpdx, dpdy));

                if (dot(faceNormal, smoothNormal) < 0.0)
                {
                    faceNormal = -faceNormal;
                }

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(faceNormal, mainLight.direction));
                float lightTerm = lerp(_ShadowStrength, _LightStrength, NdotL);

                float normalDiff = 1.0 - saturate(dot(smoothNormal, faceNormal));
                float cavityMask = pow(saturate(normalDiff * _CavityStrength), _CavityPower);

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
    }
}