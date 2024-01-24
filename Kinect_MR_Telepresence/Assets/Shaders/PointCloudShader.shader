/*
Shader "Custom/PointCloudShader"
{
    Properties
    {
        _Radius ("Sphere Radius", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
*/
/*
Shader "Custom/PointCloudShader"
{
    Properties
    {
        _Radius ("Sphere Radius", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
*/
Shader"Custom/PointCloudShader" {
    Properties{
        _Radius("Sphere Radius", float) = 0.005
    }
 
        SubShader{
LOD 200
        Tags
{"RenderType" = "Opaque"
}
        Pass
        {
Cull Off
        CGPROGRAM
        #pragma vertex vertex_shader
        #pragma geometry geometry_shader
        #pragma fragment fragment_shader
        #pragma target 5.0
        #pragma only_renderers d3d11
#include "UnityCG.cginc"
 
        // Variables
float _Radius;
 
        // VERTEX DATA
struct appData
{
    float4 pos : POSITION;
    float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
};
 
        // GEOMETRY DATA
struct v2g
{
    float4 pos : SV_POSITION;
    float4 color : COLOR0;
    float3 normal : NORMAL;
    float r : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
};
 
        // FRAGMENT DATA
struct g2f
{
    float4 pos : POSITION;
    float4 color : COLOR0;
    float3 normal : NORMAL;
            UNITY_VERTEX_OUTPUT_STEREO
};
 
        // FUNCTION: Calculate "random" number
float rand(float3 p)
{
    return frac(sin(dot(p.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
}
 
        // FUNCTION: Calculate rotation
float2x2 rotate2d(float a)
{
    float s = sin(a);
    float c = cos(a);
    return float2x2(c, -s, s, c);
}
 
        // VERTEX SHADER: computes normal wrt camera
v2g vertex_shader(appData v)
{
 
            // Output struct to geometry shader
    v2g o;
 
            // For single passed stereo rendering
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
 
            // Output clipping position
    o.pos = UnityObjectToClipPos(v.pos);
 
            // Distance from vertex to camera
    float distance = length(WorldSpaceViewDir(v.pos));
 
            // White
    //o.color = float4(1, 1, 1, 1);
    o.color = v.color;
 
            // Normal facing camera
    o.normal = ObjSpaceViewDir(v.pos);
 
            // Calc random value based on object space pos
    o.r = rand(v.pos);
 
    return o;
}
 
 
        // GEOMETRY SHADER: Creates an equilateral triangle centered at vertex
[maxvertexcount(3)]
        void geometry_shader(point v2g i[1], inout TriangleStream<g2f> triangleStream)
{
    //UNITY_SETUP_INSTANCE_ID(i);
 
            // Dimention of geometry
    float2 dim = float2(_Radius, _Radius);
 
           // Create equilateral triangle
    float2 p[3];
    p[0] = float2(-dim.x, dim.y * .57735026919);
    p[1] = float2(0., -dim.y * 1.15470053838);
    p[2] = float2(dim.x, dim.y * .57735026919);
 
           // Get the rotation from random vert input
    float2x2 r = rotate2d(i[0].r * 3.14159);
 
           // Output struct
    g2f o;
    o.color = i[0].color;
    o.normal = i[0].normal;
 
           // Update geometry
           [unroll]
    for (int idx = 0; idx < 3; idx++)
    {
        p[idx] = mul(r, p[idx]); // apply rotation
        p[idx].x *= _ScreenParams.y / _ScreenParams.x; // make square
        o.pos = i[0].pos + float4(p[idx], 0, 0) / 2.;
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        triangleStream.Append(o);
    }
 
}
 
        // FRAGMENT SHADER
float4 fragment_shader(g2f i) : COLOR
{
            // Use vertex color
    return i.color;
}
ENDCG
}
    }
FallBack"Diffuse"
}