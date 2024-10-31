Shader "ARTEC/GaussianNoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Scale", float) = 1
        _Mean ("Mean", Range(0,1)) = 0.5
        _Variance ("Variance", Range(0,1)) = 0.1
        _Seed ("Seed", float) = 0   
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "gaussianNoise.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Scale;
            float _Mean, _Variance, _Seed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                i.uv += _Seed;
                i.uv = abs(i.uv);
                
                float col = GaussianNoise(floor(i.uv * _Scale), _Mean, _Variance);

                // Bilinear interpolation
                // https://en.wikipedia.org/wiki/Bilinear_interpolation
                float colX = GaussianNoise(floor(i.uv * _Scale) + float2(1, 0), _Mean, _Variance);
                float colY = GaussianNoise(floor(i.uv * _Scale) + float2(0, 1), _Mean, _Variance);
                float colXY = GaussianNoise(floor(i.uv * _Scale) + float2(1, 1), _Mean, _Variance);
                
                float2 dec = frac(i.uv * _Scale);

                // Smoothstep to avoid star pattern in pixel corners
                dec = smoothstep(0, 1, dec);

                float a00 = col;
                float a10 = colX - col;
                float a01 = colY - col;
                float a11 = colXY - colX - colY + col;

                col = a00 + a10 * dec.x + a01 * dec.y + a11 * dec.x * dec.y;
                // End of bilinear interpolation

                return col;
            }
            ENDCG
        }
    }
}
