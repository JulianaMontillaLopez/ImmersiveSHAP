Shader "Point/PointQuadSimple"
{
    Properties
    {
        _PointScale("Point Scale", Float) = 0.03
        _Alpha("Alpha", Range(0, 1)) = 1.0
        _BaseColor("Base Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Geometry"
            "IgnoreProjector"="True"
        }

        Blend Off
        ZWrite On
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            // Buffers para GPU instancing
            StructuredBuffer<float3> _PointPositions;
            StructuredBuffer<float4> _PointColors;
            float _PointScale;
            float _Alpha;
            float4 _BaseColor;

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                // ✅ CORREGIDO: Sin verificaciones de Length()
                float3 pointPos = _PointPositions[instanceID];
                float4 pointColor = _PointColors[instanceID];

                // Aplicar escala y transformar
                float3 worldPos = pointPos + v.vertex.xyz * _PointScale;
                o.pos = UnityObjectToClipPos(float4(worldPos, 1.0));
                o.color = pointColor;
                o.color.a *= _Alpha;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}