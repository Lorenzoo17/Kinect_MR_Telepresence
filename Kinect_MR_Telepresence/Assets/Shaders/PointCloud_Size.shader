Shader "Custom/PointCloud_Size"
{
    Properties
    {
        _Scale("Scale", Float) = 1
    }
        SubShader
    {
        Tags {
            "RenderType" = "Opaque"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float _Scale;

            struct vertInput
            {
                float4 vertex : POSITION;
                float4 color : COLOR0;
                float3 normal : NORMAL;
            };

            struct vertOutput
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR0;
                float3 normal : NORMAL;
            };

            vertOutput vert (vertInput v)
            {
                vertOutput o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(vertOutput i) : COLOR
            {
                // sample the texture
                return i.color;
            }
            ENDCG
        }
    }
}
