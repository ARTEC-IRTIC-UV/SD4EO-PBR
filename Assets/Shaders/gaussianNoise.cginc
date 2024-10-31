


float3 hash3i( float3 p ) 
{
    uint3 x = uint3( p.x, p.y, p.z );
 #   define scramble  x = ( (x>>8U) ^ x.yzx ) * 1103515245U // GLIB-C const
     scramble; scramble; scramble; 
     //x = ( (x>>8U) ^ x.yzx ) * 1103515245U;
     //x = ( (x>>8U) ^ x.yzx ) * 1103515245U;
     //x = ( (x>>8U) ^ x.yzx ) * 1103515245U;
     return float3(x) / float(0xffffffffU) + 1e-30; // <- eps to fix a windows/angle bug
}

float grad(float r, float2 p) {
    int h = int(r*256.) & 15;
    float u = h<8 ? p.x : p.y,                 // 12 gradient directions
          v = h<4 ? p.y : h==12||h==14 ? p.x : 0.; // p.z;
    return ((h&1) == 0 ? u : -u) + ((h&2) == 0 ? v : -v);
}

float hash(float2 v){
    float x = hash3i(float3(v.x, v.y,11)).x;
    return x;
}

#define mod(x,y) (x - y * floor(x/y))
float2 Gt2(float2 v, float2 I, float2 cycle, float2 p) {
    float2 vv = float2(grad(hash(mod(I+v,cycle)),p-v), grad(hash(mod(I+v,cycle)+117.),p-v));
    return vv;
}

#define fade(t)  t * t * t * (t * (t * 6. - 15.) + 10.) // super-smoothstep
#define mix(a,b,t)  a + (b - a) * t

float2 InoiseT22(float2 p, float2 cycle) {
    float2 I = floor(p); p -= I;    
    float2 f = fade(p);
    
    return lerp( lerp( Gt2(float2(0,0), I, cycle, p),Gt2(float2(1,0), I, cycle, p), f.x),
                lerp( Gt2(float2(0,1), I, cycle, p),Gt2(float2(1,1), I, cycle, p), f.x), 
                f.y);
}  

float2 fbmT22(float2 p, float2 cycle) {
    float2 v = float2(0,0); float  a = .5;
    //mat2 R = rot(.37);

    for (int i = 0; i < 3; i++, p*=2.,a/=2., cycle*=2.) 
        //p *= R,
        v += a * InoiseT22(p,cycle);

    return v;
}


float Hash21(float2 p){
    p = frac(p*float2(234.34,435.345));
    p += dot(p, p+34.23);
    return frac(p.x*p.y);
}

float nrand( float2 n )
{
	return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
}

const float PI = 3.14159265359;
const float ALPHA = 0.14;
const float INV_ALPHA = 1.0 / 0.14;
const float K = 2.0 / (3.14159265359 * 0.14);

float inv_error_function(float x)
{
	float y = log(1.0 - x*x);
	float z = K + 0.5 * y;
	return sqrt(sqrt(z*z - y * INV_ALPHA) - z) * sign(x);
}

float gaussian_rand( float2 n, float mean, float variance)
{
	//float t = frac( _Time.x );
	//float x = nrand( n + 0.07*t );

    float x = nrand(n);
    x = hash3i(float3(n.x, n.y, 11)).x;
    
	return inv_error_function(x*2.0-1.0)*variance + mean;
}

float GaussianNoise(float2 uv, float mean, float variance)
{
    //return hash3i(float3(uv.x, uv.y, 11)).x;
    //return fbmT22(uv, float2(1,1)).x;
    return gaussian_rand(uv, mean, variance);
}