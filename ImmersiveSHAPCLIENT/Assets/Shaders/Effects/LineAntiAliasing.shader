Shader "Effects/LineAntiAliasing"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Thickness ("Thickness", Range(0.001, 0.1)) = 0.01
        _Smoothness ("Smoothness", Range(0.001, 0.1)) = 0.005
    }
    
    SubShader
    {
        Tags {"Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };
            
            float4 _Color;
            float _Thickness;
            float _Smoothness;
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionHCS = TransformWorldToHClip(OUT.worldPos);
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                // Anti-aliasing para líneas suaves
                float distance = length(fwidth(IN.worldPos));
                float alpha = 1.0 - smoothstep(_Thickness - _Smoothness, _Thickness + _Smoothness, distance);
                
                return half4(_Color.rgb, _Color.a * alpha);
            }
            ENDHLSL
        }
    }
}