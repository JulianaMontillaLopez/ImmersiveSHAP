Shader "Point/PointInstancedURP"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _PointScale("Point Scale", Float) = 0.03
        _Alpha("Alpha", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            // URP core helpers
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                uint instanceID   : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : TEXCOORD0;
                float  alpha       : TEXCOORD1;
            };

            // Buffers desde C# (MaterialPropertyBlock.SetBuffer)
            StructuredBuffer<float3> _PointPositions;
            StructuredBuffer<float3> _PointColors;

            CBUFFER_START(UnityPerMaterial)
                float _PointScale;
                float _Alpha;
                float4 _BaseColor;
            CBUFFER_END

            // Procedural setup hook (no-op but needed by pragma)
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            void setup()
            {
                // si necesitas inicializar algo para procedural instancing, aquí
            }
            #endif

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Leer datos de instancia (asegúrate que ComputeBuffer.Length >= instancia)
                float3 instPos = _PointPositions[IN.instanceID]; // asumimos local-space del renderer
                float3 instCol = _PointColors[IN.instanceID];

                // Escalar el vértice del mesh (posición objeto del mesh) por _PointScale
                float3 vertexOS = IN.positionOS.xyz * _PointScale;

                // Posición en espacio local del renderer: sumamos posición "instancia" (local) + vértice escalado
                float3 localPosition = instPos + vertexOS;

                // Transformar a espacio mundo (unity_ObjectToWorld viene de Core.hlsl)
                float4 worldPos = mul(unity_ObjectToWorld, float4(localPosition, 1.0));

                // Proyectar a clip space
                OUT.positionHCS = TransformWorldToHClip(worldPos.xyz);

                // Pasar color y alpha
                OUT.color = float4(instCol * _BaseColor.rgb, 1.0) * _BaseColor.a;
                OUT.alpha = _Alpha;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Si quieres aplicar gamma o corrección, hazlo aquí; por defecto devolvemos color directo.
                return half4(IN.color.rgb, IN.alpha * IN.color.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
