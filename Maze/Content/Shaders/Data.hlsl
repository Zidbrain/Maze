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

#define PI 3.14159265359

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
    float3x3 TBN : NORMAL0;
};

struct DefferedPixel
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD;
};

struct PSOutput
{
    float4 Color : SV_Target0;
    float4 Depth : SV_Target1;
    float4 Normal : SV_Target2;
    float4 Position : SV_Target3;
};

tbuffer Textures : register(t1)
{
    PARAMETER(Texture2D _texture);
    PARAMETER(Texture2D _depthBuffer);
    PARAMETER(Texture2D _normalBuffer);
    PARAMETER(Texture2D _positionBuffer);
}

PARAMETER(float3 _cameraPosition);

SamplerState wrapSampler : register(s0)
{
    AddressU = WRAP;
    AddressV = WRAP;
    AddressW = WRAP;
};

SamplerState clampSampler = sampler_state
{
    AddressU = clamp;
    AddressV = clamp;
    AddressW = clamp;
};

SamplerState borderSampler = sampler_state
{
    AddressU = BORDER;
    AddressV = BORDER;
    AddressW = BORDER;
    BorderColor = 0xFFFFFF;
};

SamplerState anisotropicSampler = sampler_state
{
    Texture = <_texture>;
    AddressU = WRAP;
    AddressV = WRAP;
    AddressW = WRAP;
    Filter = ANISOTROPIC;
    MaxAnisotropy = 16;
};

float Distance(float3 value, float4 plane)
{
    return dot(value, plane.xyz) + plane.w;
}

float3 GetNormal(in float2 texCoord)
{
    return (_normalBuffer.Sample(borderSampler, texCoord).xyz - float3(0.5, 0.5, 0.5)) * 2;
}

void OrientVector(inout float3 vec, in float3 onPosition, in float3 vectorPosition)
{
    if (dot(onPosition - vectorPosition, vec) < 0)
        vec = -vec;
}

float3x3 ConstructTBN(in float3 position, in float3 normal, in float3 tangent, in float3x3 transform)
{          
    float3 N = normalize(mul(normal, transform).xyz);
    float3 T = normalize(mul(tangent, transform).xyz);

    OrientVector(N, _cameraPosition, position);

    float3 B = cross(N, T);
    
    return float3x3(T, B, N);
}

float lengthSqr(in float3 vec)
{
    return vec.x * vec.x + vec.y * vec.y + vec.z * vec.z;
}

float angleCos(in float3 vec1, in float3 vec2)
{
    return dot(vec1, vec2) / length(vec1) / length(vec2);
}

#endif