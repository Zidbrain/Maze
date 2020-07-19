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
    float3 Normal : NORMAL;
};

struct Pixel
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    float3 WorldPosition : TEXCOORD1;
    float3 Normal : NORMAL;
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
PARAMETER(texture _depthTexture);
PARAMETER(texture _normalTexture);
PARAMETER(texture _positionTexture);

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
    Texture = <_depthTexture>;
};

sampler2D normalSampler = sampler_state
{
    Texture = <_normalTexture>;
};

sampler2D positionSampler = sampler_state
{
    Texture = <_positionTexture>;
};

float Distance(float3 value, float4 plane)
{
    return dot(value, plane.xyz) + plane.w;
}

#endif