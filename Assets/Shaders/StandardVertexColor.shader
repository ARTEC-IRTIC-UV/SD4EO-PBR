Shader "ARTEC/StandardVertexColor"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _B8 ("B8", Vector) = (0.5, 0.1, 0, 0)
        _RenderB8 ("Render B8", Range(0, 1)) = 0
    }
    SubShader
    {
        
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        #include "UnityCG.cginc"
        #include "gaussianNoise.cginc"

        sampler2D _MainTex;
        float4 _MainTex_ST;
        sampler2D _BumpMap;
        float _RenderB8;
        

        struct Input
        {
            float4 pos : SV_POSITION;
            float2 tcoord0 : TEXCOORD0;
            float2 tcoord1 : TEXCOORD1;
            float2 screenPos : TEXCOORD2;
            float4 color : COLOR0;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _B8;
        

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v, out Input o)
        {
            o.pos = UnityObjectToClipPos(v.vertex);
            o.tcoord0 = TRANSFORM_TEX(v.texcoord, _MainTex);
            o.tcoord1 = v.texcoord;
            o.color = v.color;
            o.screenPos = ComputeScreenPos(o.pos);
            // o.color = v.color;
            // o.color = fixed4(1,0,0,1);
            // v.color = fixed4(1,0,0,1);
            // v.texcoord = v.texcoord;
            //v.uv = v.texcoord;
            //v.uv_MainTex = v.texcoord;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.tcoord0) * _Color;

            if (_RenderB8 < 0.5)
            {
                o.Albedo = c.rgb * IN.color.rgb;
            }
            else
            {
                fixed luminance = dot(c.rgb, float3(0.299, 0.587, 0.114));
                fixed noise = GaussianNoise(IN.screenPos * 1024, _B8.x, _B8.y);
                o.Albedo = lerp(noise, noise * luminance, 0.2);
            }

            // Apply normal map
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.tcoord0));
            
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
