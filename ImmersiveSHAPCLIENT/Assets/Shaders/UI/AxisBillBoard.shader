Shader "UI/AxisBillboard"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _CameraPosition ("Camera Position", Vector) = (0,0,0,0)
    }
    
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float3 _CameraPosition;
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // Billboard: siempre frente a la cámara
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 viewDir = normalize(_CameraPosition - worldPos);
                float3 right = normalize(cross(float3(0,1,0), viewDir));
                float3 up = cross(viewDir, right);
                
                // Aplicar offset para mantener tamaño constante
                worldPos += right * IN.positionOS.x + up * IN.positionOS.y;
                
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = tex2D(_MainTex, IN.uv) * _Color;
                return col;
            }
            ENDHLSL
        }
    }
}