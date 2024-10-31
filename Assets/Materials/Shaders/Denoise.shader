////////////////////////////////////////////////////////////////////////////////
// Denoise Shader
// Author: ARTEC Group - IRTIC-UV - University of Valencia - artecadm@irtic.uv.es
//
// This shader implements a denoise filter. 
// Denoise patch size is proportional to the distance to the camera.
//
// Based on:
// https://github.com/BrutPitt/glslSmartDeNoise/
// Copyright (c) 2018-2019 Michele Morrone
//
////////////////////////////////////////////////////////////////////////////////

Shader "ARTEC/Denoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Radius ("Radius", Float) = 1
        _Iterations ("Iterations", Int) = 1
        _Threshold("Threshold", Float) = 0.5
        _TextureSizes ("Texture Sizes", Vector) = (1,1,1,1)
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
            float _Radius;
            int _Iterations;
            float4 _TextureSizes;
            float _Threshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Get patch center color
                float4 center = tex2D(_MainTex, i.uv);
                float4 col = center;
                float cont = 1;

                // Get distance from camera (is in alpha channel)
                float dist = saturate((center.a-0.1) * 100);

                // Compute denoise iteration proportionally to distance
                float iters = _Iterations * pow(dist,2);
                
                // Compute median of all colors in the patch
                UNITY_LOOP
                for (int x=-iters; x<=iters; x++) {
                    UNITY_LOOP
                    for (int y=-iters; y<=iters; y++) {

                        float2 offset = float2(
                                x*_TextureSizes.z*_Radius,
                                y*_TextureSizes.w*_Radius);
                        
                        float4 sample = tex2D(_MainTex, i.uv + offset);
                        
                        center += sample;
                        cont+= 1;
                    }
                }

                // Reference color for denoising is 80% the center and 20% the median
                center = lerp(center/cont, col, 0.8);
                cont = 1;

                // Denoise loop
                UNITY_LOOP
                for (int x=-iters; x<=iters; x++) {
                    UNITY_LOOP
                    for (int y=-iters; y<=iters; y++) {
                        
                        float2 offset = float2(
                            x*_TextureSizes.z*_Radius,
                            y*_TextureSizes.w*_Radius);

                        float4 sample = tex2D(_MainTex, i.uv + offset);

                        // Compute difference between center and sample
                        float3 diff = (sample - center);

                        // Compute weight of the sample based on the difference
                        float delta = exp (-dot(diff, diff) * _Threshold);

                        // Accumulate color and weight
                        col += (delta * sample);
                        cont += delta;
                    }
                }

                // Normalize color
                col /= cont;
                col.a = 1;
                
                return col;
            }
            ENDCG
        }
    }
}
