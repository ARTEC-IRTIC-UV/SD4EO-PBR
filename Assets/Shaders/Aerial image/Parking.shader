Shader "ARTEC/Parking"
{
    Properties
    {
        _Color ("Color", Color) = (0.8,0.8,0.8,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _LineWidth ("Line Width", Range(0, 0.2)) = 0.05
        _LineColor ("Line Color", Color) = (1,1,1,1)
        
        _StreetWidth ("Street Width", Range(0, 20)) = 5
        _StreetAngle ("Street Angle", Range(0, 360)) = 0

        _VehicleWidth ("Vehicle Width", Range(0, 20)) = 2
        _VehicleAngle ("Vehicle Angle", Range(0, 90)) = 0
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
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _LineWidth;
        fixed4 _LineColor;
        float _StreetWidth;
        float _StreetAngle;
        float _VehicleWidth;
        float _VehicleAngle;
        

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 c = _Color;

            // Position in world space
            float2 p = IN.worldPos.xz;

            // Rotate the street
            float angle = _StreetAngle * 3.14159265359 / 180.0;
            p = float2(cos(angle) * p.x - sin(angle) * p.y, sin(angle) * p.x + cos(angle) * p.y);
            
            int streetIndex = abs(floor(p.x / _StreetWidth)%2);

            // Street Space
            p.x = frac(p.x / _StreetWidth);
            
            float isLine = step(p.x * _StreetWidth, _LineWidth/2);

            // Check the line distance to street separation
            //p.y = frac(p.y * _StreetWidth / _VehicleWidth);
            p.y = frac(p.y / _VehicleWidth);
            
            angle = -(_VehicleAngle)* 3.14159265359 / 180.0;
            if (streetIndex == 0) p.x = 1-p.x;
            float streetW = p.x;
            //if (streetIndex == 0) angle;


            //p = float2(cos(angle) * p.x - sin(angle) * p.y, sin(angle) * p.x + cos(angle) * p.y);
            p.y -= p.x * _VehicleAngle/45;
            float isLine2 = step(abs(p.y) * _StreetWidth, _LineWidth);
            isLine = max(isLine, isLine2);
            if (streetW > 0.7) isLine = 0;
            //if (streetIndex == 0)  isLine = 0;
            
            // float isLine2 = step((1-p.y), _LineWidth/2) + step(p.y, _LineWidth/2);



            // // Check the line distance to street separation
            // float2 streetPoint = p ;
            // //int streetIndex = abs(floor(streetPoint.x)%2);
            // float2 streetPointFrac = frac(streetPoint);
            // //if (streetIndex == 0) streetPointFrac.x = 1-streetPointFrac.x;
            
            // if (streetPointFrac.x > 0.5) p.y = 1-p.y;

            //float lineDist = min(p.x,1-streetPointFrac.x) * _StreetWidth;
            
            // // Check the line distance to vehicle separation
            // angle = (_VehicleAngle)* 3.14159265359 / 180.0;
            // p = float2(cos(angle) * p.x - sin(angle) * p.y, sin(angle) * p.x + cos(angle) * p.y);
            // p.y = frac(p.y / _VehicleWidth);

            

            // // float vehicleAngle = _VehicleAngle * 3.14159265359 / 180.0;
            
            // // p = streetPoint * _StreetWidth;
            // // float2 vehiclePoint = float2(cos(vehicleAngle) * p.x - sin(vehicleAngle) * p.y, sin(vehicleAngle) * p.x + cos(vehicleAngle) * p.y);
            // // vehiclePoint /= _VehicleWidth;
            // // vehiclePoint = abs(vehiclePoint);
            // // vehiclePoint = frac(vehiclePoint);
            // // float lineDist2 = min(vehiclePoint.x,1-vehiclePoint.x) * _VehicleWidth;
            
            // // lineDist = min(lineDist, lineDist2);
            

            // // Remove the line from the middle of the street
            // //isLine = isLine - step(0.3, streetPointFrac.x) * step(streetPointFrac.x, 0.7);
            
            // if (streetIndex != 0) isLine = 0;
            
            // c.rgb = lerp(c.rgb, _LineColor.rgb, saturate(isLine));
            //c.rgb = isLine;

            //c.rgb = lerp(_Color.rgb, _LineColor.rgb, saturate(lineDist));
            //c.rgb = streetPointFrac.x;

            c.rgb = isLine;


            // // Albedo comes from a texture tinted by color
            // fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = 0;
            o.Smoothness = 0;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
