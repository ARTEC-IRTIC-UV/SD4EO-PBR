const float M_PI = 3.14159265359;

float2 hash(float2 p)
{
    float2 r = mul(float2x2(127.1, 311.7, 269.5, 183.3), p);
    return frac(sin(r)*43758.5453);
}

float2x2 LoadRot2x2(int2 idx, float rotStrength)
{
    float angle = abs(idx.x*idx.y) + abs(idx.x+idx.y) + M_PI;
    // Remap to +/-pi.
    angle = fmod(angle, 2*M_PI);
    if(angle<0)
        angle += 2*M_PI;
    
    if(angle>M_PI)
        angle -= 2*M_PI;
    
    angle *= rotStrength;
    float cs = cos(angle);
    float si = sin(angle);
    return float2x2(cs, -si, si, cs);
}

float2 MakeCenST(int2 Vertex)
{
    float2x2 invSkewMat = float2x2(1.0, 0.5, 0.0, 1.0/1.15470054);
    return mul(invSkewMat, Vertex) / (2 * sqrt(3));
}

// Input: vM is the tangent-space normal in [-1, 1]
// Output: convert vM to a derivative
float2 TspaceNormalToDerivative(float3 vM)
{
    const float scale = 1.0/128.0;
    // Ensure vM delivers a positive third component using abs() and
    // constrain vM.z so the range of the derivative is [-128, 128].
    const float3 vMa = abs(vM);
    const float z_ma = max(vMa.z, scale*max(vMa.x, vMa.y));
    // Set to match positive vertical texture coordinate axis.
    const bool gFlipVertDeriv = false;
    const float s = gFlipVertDeriv ? -1.0 : 1.0;
    return -float2(vM.x, s*vM.y)/z_ma;
}

float2 SampleDeriv(Texture2D nmap, SamplerState samp, float2 st, float2 dSTdx, float2 dSTdy) : SV_Target
{
    // Sample
    float3 vM = 2.0*nmap.SampleGrad(samp, st, dSTdx, dSTdy)-1.0;
    return TspaceNormalToDerivative(vM);
}

float3 Gain3(float3 x, float r)
{
    // Increase contrast when r > 0.5 and
    // reduce contrast if less.
    float k = log(1-r) / log(0.5);
    float3 s = 2*step(0.5, x);
    float3 m = 2*(1 - s);
    float3 res = 0.5*s + 0.25*m * pow(max(0.0, s + x*m), k);
    return res.xyz / (res.x+res.y+res.z);
}

float3 ProduceHexWeights(float3 W, int2 vertex1, int2 vertex2, int2 vertex3)
{
    float3 res = 0.0;
    int v1 = (vertex1.x-vertex1.y)%3;
    if(v1<0) v1+=3;
    int vh = v1<2 ? (v1+1) : 0;
    int vl = v1>0 ? (v1-1) : 2;
    int v2 = vertex1.x<vertex3.x ? vl : vh;
    int v3 = vertex1.x<vertex3.x ? vh : vl;
    res.x = v3==0 ? W.z : (v2==0 ? W.y : W.x);
    res.y = v3==1 ? W.z : (v2==1 ? W.y : W.x);
    res.z = v3==2 ? W.z : (v2==2 ? W.y : W.x);
    return res;
}