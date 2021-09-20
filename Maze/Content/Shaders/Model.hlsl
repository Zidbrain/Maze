#ifndef MODEL_DEF
#define MODEL_DEF

#include "Data.hlsl"
#include "Standart.hlsl"
#include "Lighting.hlsl"

#define MAX_BONES 50

PARAMETER(float4x4 _bones[MAX_BONES]);

struct ModelVertexInput
{
	float3 Position : SV_Position;
	float3 Normal : NORMAL0;
	float3 Tangent : TANGENT0;
	float3 Binormal : BINORMAL0;
	float2 TextureCoordinate : TEXCOORD0;
	uint4 BlendIndex : BLENDINDICES0;
	float4 BlendWeight : BLENDWEIGHT0;
};

float4x4 Skin(in uint4 blendIndex, in float4 blendWeight)
{
	float4x4 mat = 0;

	[unroll]
	for (int i = 0; i < 4; i++)
		mat += _bones[blendIndex[i]] * blendWeight[i];

	return mat;
}

Pixel ModelVS(in ModelVertexInput input)
{
	float4x4 mat = Skin(input.BlendIndex, input.BlendWeight);

	Pixel ret = (Pixel)0;

    ret.WorldPosition = mul(float4(input.Position, 1), mat).xyz;
	ret.TextureCoordinate = input.TextureCoordinate;
	ret.Position = mul(float4(ret.WorldPosition, 1), _matrix);

	ret.TBN = mul(float3x3(input.Tangent, input.Binormal, input.Normal), (float3x3)mat);

	return ret;
}

float4 WriteShadowModelVS(in float3 Position : SV_Position, in uint4 BlendIndex : BLENDINDICES0, in float4 BlendWeight : BLENDWEIGHT0, in float3 normal : NORMAL0) : SV_Position
{
	return WriteShadowVS(float4(Position, 1), Skin(BlendIndex, BlendWeight), normal);
}

TECHNIQUE(ModelStandart, ModelVS, StandartPS);
TECHNIQUE(ModelWriteDepth, WriteShadowModelVS, WriteShadow);

#endif