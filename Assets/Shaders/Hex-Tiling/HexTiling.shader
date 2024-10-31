Shader "ARTEC/HexTiling"
{
    Properties
    {
        _MainTex ("Color Map", 2D) = "white" {}
        _NormalTex ("Normal Map", 2D) = "white" {}
        _TileRate ("Tile Rate", int) = 10
        _RotStrength ("Rot Strength", float) = 0
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
            #include "hextiling.cginc"

            uniform float4x4 _ScrToView; // equivalent to g_mScrToView
            uniform float4x4 _ViewToWorld; // equivalent to g_mViewToWorld

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv: TEXCOORD0;
                //float3 worldPos : SV_POSITION;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            Texture2D _MainTex;
            float4 _MainTex_ST;
            Texture2D _NormalTex;
            float4 _NormalTex_ST;
            SamplerState sampler_MainTex;

            half _RotStrength;
            int _TileRate;
            const half g_fallOffContrast = 0.6;
            const int g_exp = 7;

            // Output: weights associated with each hex tile and integer centers
            void TriangleGrid(out float w1, out float w2, out float w3, out int2 vertex1, out int2 vertex2, out int2 vertex3, float2 st)
            {
                // Scaling of the input
                st *= 2 * sqrt(3);
                // Skew input space into simplex triangle grid.
                const float2x2 gridToSkewedGrid =
                float2x2(1.0, -0.57735027, 0.0, 1.15470054);
                float2 skewedCoord = mul(gridToSkewedGrid, st);
                int2 baseId = int2( floor ( skewedCoord ));
                float3 temp = float3( frac( skewedCoord ), 0);
                temp.z = 1.0 - temp.x - temp.y;
                float s = step(0.0, -temp.z);
                float s2 = 2*s-1;
                w1 = -temp.z*s2;
                w2 = s - temp.y*s2;
                w3 = s - temp.x*s2;
                vertex1 = baseId + int2(s,s);
                vertex2 = baseId + int2(s,1-s);
                vertex3 = baseId + int2(1-s,s);
            }
            
            // Input: nmap is a normal map
            // Input: r increase contrast when r > 0.5
            // Output: deriv is a derivative dHduv wrt units in pixels
            // Output: weights shows the weight of each hex tile
            void bumphex2derivNMap(out float2 deriv, out float3 weights, Texture2D nmap, SamplerState samp, float2 st, float rotStrength, float r=0.5)
            {
                float2 dSTdx = ddx(st), dSTdy = ddy(st);
                // Get triangle info.
                float w1, w2, w3;
                int2 vertex1, vertex2, vertex3;
                TriangleGrid(w1, w2, w3, vertex1, vertex2, vertex3, st);
                float2x2 rot1 = LoadRot2x2(vertex1, rotStrength);
                float2x2 rot2 = LoadRot2x2(vertex2, rotStrength);
                float2x2 rot3 = LoadRot2x2(vertex3, rotStrength);
                float2 cen1 = MakeCenST(vertex1);
                float2 cen2 = MakeCenST(vertex2);
                float2 cen3 = MakeCenST(vertex3);
                float2 st1 = mul(st - cen1, rot1) + cen1 + hash(vertex1);
                float2 st2 = mul(st - cen2, rot2) + cen2 + hash(vertex2);
                float2 st3 = mul(st - cen3, rot3) + cen3 + hash(vertex3);
                // Fetch input.
                float2 d1 = SampleDeriv(nmap, samp, st1,
                mul(dSTdx, rot1), mul(dSTdy, rot1));
                float2 d2 = SampleDeriv(nmap, samp, st2,
                mul(dSTdx, rot2), mul(dSTdy, rot2));
                float2 d3 = SampleDeriv(nmap, samp, st3,
                mul(dSTdx, rot3), mul(dSTdy, rot3));
                d1 = mul(rot1, d1); d2 = mul(rot2, d2); d3 = mul(rot3, d3);
                // Produce sine to the angle between the conceptual normal
                // in tangent space and the Z-axis.
                float3 D = float3( dot(d1,d1), dot(d2,d2), dot(d3,d3));
                float3 Dw = sqrt(D/(1.0+D));
                Dw = lerp(1.0, Dw, g_fallOffContrast); // 0.6
                float3 W = Dw*pow(float3(w1, w2, w3), g_exp); // 7
                W /= (W.x+W.y+W.z);
                if(r!=0.5)
                    W = Gain3(W, r);
                deriv = W.x * d1 + W.y * d2 + W.z * d3;
                weights = ProduceHexWeights(W.xyz, vertex1, vertex2, vertex3);
            }

            // Input: tex is a texture with color
            // Input: r increase contrast when r > 0.5
            // Output: color is the blended result
            // Output: weights shows the weight of each hex tile
            void hex2colTex(out float4 color, out float3 weights,
            Texture2D tex, SamplerState samp, float2 st,
            float rotStrength, float r=0.5)
            {
                float2 dSTdx = ddx(st), dSTdy = ddy(st);
                // Get triangle info.
                float w1, w2, w3;
                int2 vertex1, vertex2, vertex3;
                TriangleGrid(w1, w2, w3, vertex1, vertex2, vertex3, st);
                float2x2 rot1 = LoadRot2x2(vertex1, rotStrength);
                float2x2 rot2 = LoadRot2x2(vertex2, rotStrength);
                float2x2 rot3 = LoadRot2x2(vertex3, rotStrength);
                float2 cen1 = MakeCenST(vertex1);
                float2 cen2 = MakeCenST(vertex2);
                float2 cen3 = MakeCenST(vertex3);
                float2 st1 = mul(st - cen1, rot1) + cen1 + hash(vertex1);
                float2 st2 = mul(st - cen2, rot2) + cen2 + hash(vertex2);
                float2 st3 = mul(st - cen3, rot3) + cen3 + hash(vertex3);
                // Fetch input.
                float4 c1 = tex.SampleGrad(samp, st1,
                mul(dSTdx, rot1), mul(dSTdy, rot1));
                float4 c2 = tex.SampleGrad(samp, st2,
                mul(dSTdx, rot2), mul(dSTdy, rot2));
                float4 c3 = tex.SampleGrad(samp, st3,
                mul(dSTdx, rot3), mul(dSTdy, rot3));
                // Use luminance as weight.
                float3 Lw = float3(0.299, 0.587, 0.114);
                float3 Dw = float3(dot(c1.xyz,Lw),dot(c2.xyz,Lw),dot(c3.xyz,Lw));
                Dw = lerp(1.0, Dw, g_fallOffContrast); // 0.6
                float3 W = Dw*pow(float3(w1, w2, w3), g_exp); // 7
                W /= (W.x+W.y+W.z);
                if(r!=0.5) W = Gain3(W, r);
                color = W.x * c1 + W.y * c2 + W.z * c3;
                weights = ProduceHexWeights(W.xyz, vertex1, vertex2, vertex3);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                //fixed4 col = tex2D(_MainTex, i.uv);
                fixed3 weights;
                //float4 worldPos = fixed4(i.worldPos, 1.0);
                float4 worldPos = mul(unity_ObjectToWorld, i.vertex);
                float3 sp = _TileRate * worldPos.xyz;
                float2 st = float2(sp.x, -sp.z);
                st = float2(st.x, 1.0-st.y);
                fixed4 c;
                hex2colTex(c, weights, _MainTex, sampler_MainTex, st, _RotStrength);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                //c *= fixed4(weights*0.75 + 0.25, 1.0);
                //c = fixed4(weights, 1.0);
                return c;
            }
            ENDCG
        }
    }
}
