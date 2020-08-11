#ifndef DATA_DEF
#define DATA_DEF

#define TECHNIQUE(techniqueName, VS, PS)\
    technique techniqueName\
    {\
        pass P0\
        {\
            VertexShader = compile vs_5_0 VS();\
            PixelShader = compile ps_5_0 PS();\
        }\
    }\

#define PARAMETER(parameterDefinition) uniform extern parameterDefinition

struct Vertex
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    float3 Normal : NORMAL0;
    float3 Tangent : TANGENT0;
};

struct Pixel
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    float3 WorldPosition : TEXCOORD1;
    nointerpolation float3x3 TBN : NORMAL0;
};

struct DefferedPixel
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD;
};

struct PSOutput
{
    float4 Color : SV_Target0;
    float Depth : SV_Target1;
    float4 Normal : SV_Target2;
    float4 Position : SV_Target3;
};

PARAMETER(texture _texture);
PARAMETER(texture _depthBuffer);
PARAMETER(texture _normalBuffer);
PARAMETER(texture _positionBuffer);

PARAMETER(float3 _cameraPosition);

SamplerState wrapSampler = sampler_state
{
    AddressU = WRAP;
    AddressV = WRAP;
    AddressW = WRAP;
};

SamplerState borderSampler = sampler_state
{
    AddressU = BORDER;
    AddressV = BORDER;
    BorderColor = 0x00000000;
};

sampler2D textureSampler = sampler_state
{
    Texture = <_texture>;
    AddressU = WRAP;
    AddressV = WRAP;
    AddressW = WRAP;
    Filter = ANISOTROPIC;
    MaxAnisotropy = 16;
};

sampler2D depthSampler = sampler_state
{
    Texture = <_depthBuffer>;
};

sampler2D normalSampler = sampler_state
{
    Texture = <_normalBuffer>;
};

sampler2D positionSampler = sampler_state
{
    Texture = <_positionBuffer>;
};

float Distance(float3 value, float4 plane)
{
    return dot(value, plane.xyz) + plane.w;
}

float3 GetNormal(in float2 texCoord)
{
    return (tex2D(normalSampler, texCoord).rgb - float3(0.5, 0.5, 0.5)) * 2;
}

void OrientVector(inout float3 vec, in float3 onPosition, in float3 vectorPosition)
{
    vec = normalize(vec * dot(onPosition - vectorPosition, vec));
}

float3x3 ConstructTBN(in float3 position, in float3 normal, in float3 tangent, in float3x4 transform)
{          
    float3 N = mul(normal, transform).xyz;
    OrientVector(N, _cameraPosition, position);
    
    float3 T = normalize(mul(tangent, transform).xyz);
    float3 B = cross(N, T);
    
    return float3x3(T, B, N);
}

#endif