Shader "URP/PointInstanced"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _Scale("Scale", Float) = 0.02
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : TEXCOORD2;
                uint instanceID : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Scale;
                float _Smoothness;
                float _Metallic;
            CBUFFER_END

            StructuredBuffer<float3> _PositionsBuffer;
            StructuredBuffer<float4> _ColorsBuffer;

            Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output;
                
                // Get instance data from buffers
                float3 instancePosition = _PositionsBuffer[instanceID];
                float4 instanceColor = _ColorsBuffer[instanceID];

                // Apply scale and position
                float3 positionOS = input.positionOS.xyz * _Scale;
                float3 positionWS = instancePosition + positionOS;
                
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = instanceColor;
                output.instanceID = instanceID;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Basic lighting
                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                float NdotL = saturate(dot(normal, mainLight.direction));
                
                // Get color from instance data
                float4 color = input.color;
                
                // Apply lighting
                float3 lighting = NdotL * mainLight.color + mainLight.color * 0.1;
                color.rgb *= lighting;
                
                return color;
            }
            ENDHLSL
        }
    }
}