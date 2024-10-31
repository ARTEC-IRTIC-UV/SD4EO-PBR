Shader "ARTEC/HexTilingTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TileSize ("Tile Size", Float) = 1.0
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

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _TileSize;

            // Function to create hexagonal tiling UV coordinates
            float2 HexTileUV(float2 uv)
            {
                // Scale UV coordinates by tile size
                uv *= _TileSize;

                // Calculate the hex grid coordinates
                float2 q = float2(uv.x * 2.0 / 3.0, (uv.x / 3.0 + uv.y) * 2.0 / sqrt(3.0));

                // Convert to hex grid coordinates
                float2 r = float2(floor(q.x + 0.5), floor(q.y + 0.5));

                // Calculate the offset within the hex cell
                float2 offset = q - r;

                // Adjust the offset to be within the hex cell bounds
                if (offset.x + offset.y > 1.0)
                {
                    offset = 1.0 - offset;
                }

                // Calculate the final UV coordinates within the hex cell
                return (r + offset) / _TileSize;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Apply hex tiling to the UV coordinates
                float2 hexUV = HexTileUV(i.uv);

                // Sample the texture with the hex-tiling UV coordinates
                fixed4 col = tex2D(_MainTex, hexUV);

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}