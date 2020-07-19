#ifndef INSTANCED_DEF
#define INSTANCED_DEF

#include "Standart.hlsl"

#define MAX_POLYGONES 255

PARAMETER(float4x4 _matrices[MAX_POLYGONES]);

struct InstancedVertexInput
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    uint Instance : SV_InstanceID;
};

Pixel InstancedVS(in InstancedVertexInput input)
{
    Pixel output = (Pixel) 0;
    
    output.WorldPosition = mul(input.Position, _matrices[input.Instance]).xyz;
    output.Normal = _matrices[input.Instance]._m21_m22_m23;
    output.TextureCoordinate = input.TextureCoordinate;
    output.Position = mul(float4(output.WorldPosition, 1), _matrix);
    
    return output;
}

TECHNIQUE(Instanced, InstancedVS, StandartPS);

#endif