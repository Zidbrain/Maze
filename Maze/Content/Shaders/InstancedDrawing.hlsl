#ifndef INSTANCED_DEF
#define INSTANCED_DEF

#include "Standart.hlsl"

#define MAX_POLYGONES 255

PARAMETER(float4x4 _matrices[MAX_POLYGONES]);

struct InstancedVertexInput
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    float3 Normal : NORMAL0;
    float3 Tangent : TANGENT0;
    uint Instance : SV_InstanceID;
};

Pixel InstancedVS(in InstancedVertexInput input)
{
    Pixel output = (Pixel) 0;
    
    output.WorldPosition = mul(input.Position, _matrices[input.Instance]).xyz;
    output.TextureCoordinate = input.TextureCoordinate;
    output.Position = mul(float4(output.WorldPosition, 1), _matrix);
    output.TBN = ConstructTBN(input.Normal, input.Tangent, (float3x4) _matrices[input.Instance]);
    
    return output;
}

TECHNIQUE(Instanced, InstancedVS, StandartPS);

#endif