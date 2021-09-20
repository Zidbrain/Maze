#ifndef LIGHTING_DEF
#define LIGHTING_DEF

#include "Data.hlsl"
#include "Standart.hlsl"
#include "InstancedDrawing.hlsl"

#define MAX_LIGHTS 5

PARAMETER(float3 _lightingPosition[MAX_LIGHTS]);
PARAMETER(float _lightingRadius[MAX_LIGHTS]);
PARAMETER(float4 _lightingColor[MAX_LIGHTS]);
PARAMETER(float _diffusePower[MAX_LIGHTS]);
PARAMETER(float _hardness[MAX_LIGHTS]);
PARAMETER(float _specularHardness[MAX_LIGHTS]);
PARAMETER(float _specularPower[MAX_LIGHTS]);

PARAMETER(int _lightsCount);

PARAMETER(Texture2DArray _shadowMaps);
PARAMETER(int _shadowsEnabled[MAX_LIGHTS]);

PARAMETER(Texture2D _SSAOMap);
PARAMETER(float _diversionAngle[MAX_LIGHTS]);
PARAMETER(float3 _direction[MAX_LIGHTS]);
//PARAMETER(matrix _directionMatrix[MAX_LIGHTS]);

PARAMETER(TextureCube _lightShadows);
PARAMETER(float3 _lightPosition);

float sqr(in float value)
{
	return value * value;
}

float4 LightingPS(in DefferedPixel input) : SV_Target
{
	float4 color = float4(0,0,0,1);
	float3 position = _positionBuffer.Sample(clampSampler, input.TextureCoordinate).rgb;
	float3 normal = GetNormal(input.TextureCoordinate);

	[unroll(MAX_LIGHTS)]
	for (int i = 0; i < _lightsCount; i++)
	{
		float3 lightDir = _lightingPosition[i] - position;
		float dist = length(lightDir);

		if (dist > _lightingRadius[i])
			continue;

		lightDir /= dist;

		float diffuseValue = saturate(dot(normal, lightDir));
		float3 halfVector = normalize(lightDir + _cameraPosition - position);
		float specularValue = pow(saturate(dot(normal, halfVector)), _specularHardness[i]);

		float shadowValue = (_shadowsEnabled[i] ? _shadowMaps.Sample(wrapSampler, float3(input.TextureCoordinate, i)).r : 1);

		color.rgb += (diffuseValue * _diffusePower[i] + specularValue * _specularPower[i]) * (1 - pow(dist / _lightingRadius[i], _hardness[i])) * _lightingColor[i].rgb * shadowValue;
	}

	return color;
}

float4 WriteShadowVS(in float4 position, in float4x4 transform, in float3 normal)
{
	float3 world = mul(position, transform).xyz;

	//normal = normalize(mul(normal, (float3x3) transform).xyz);
	//OrientVector(normal, _lightPosition, world);
	//world -= normal * 0.00;

	return mul(float4(world, 1), _matrix);
}

float4 WriteShadowVSInstanced(in InstancedVertexInput input) : SV_Position
{
	return WriteShadowVS(input.Position, _matrices[input.Instance], input.Normal);
}

float4 WriteShadowVSStandart(in Vertex input) : SV_Position
{
	return WriteShadowVS(input.Position, _transform, input.Normal);
}

PARAMETER(bool _static);

float4 WriteShadow(in float4 input : SV_Position) : SV_Target
{
	float result = input.z;

	//if (_static) 
	//{
	//	float2 uv = input.xy / 1024;
	//	uv = float2(uv.x, uv.y);
	//	float depth = _texture.Sample(wrapSampler, uv).r;

	//	if (depth < result)
	//		result = depth;
	//}

	return float4(result, 0, 0, 1);
}

PARAMETER(float _farPlane);
PARAMETER(float _nearPlane);

float VectorToDepth(in float3 vec, out float3 normal) {
	float3 AbsVec = abs(vec);
	float LocalZcomp = max(AbsVec.x, max(AbsVec.y, AbsVec.z));

	if (LocalZcomp == AbsVec.x)
		normal = float3(1, 0, 0);
	else if (LocalZcomp == AbsVec.y)
		normal = float3(0, 1, 0);
	else normal = float3(0, 0, 1);

	float f = _farPlane;
	float n = _nearPlane;

	float NormZComp = (f + n) / (f - n) - (2 * f * n) / (f - n) / LocalZcomp;
	return (NormZComp + 1.0) * 0.5;
}

SamplerComparisonState shadowSampler : register(s1);

float GetShadowValue(in float3 vecToPixel)
{
	const float3 sampleOffsetDirections[20] =
	{
	   float3(1,  1,  1), float3(1, -1,  1), float3(-1, -1,  1), float3(-1,  1,  1),
	   float3(1,  1, -1), float3(1, -1, -1), float3(-1, -1, -1), float3(-1,  1, -1),
	   float3(1,  1,  0), float3(1, -1,  0), float3(-1, -1,  0), float3(-1,  1,  0),
	   float3(1,  0,  1), float3(-1,  0,  1), float3(1,  0, -1), float3(-1,  0, -1),
	   float3(0,  1,  1), float3(0, -1,  1), float3(0, -1, -1), float3(0,  1, -1)
	}; // Cant make static because monogame sets this array 0 for some reason

	float3 normal = float3(0, 0, 0);
	float depthPixel = VectorToDepth(vecToPixel, normal);
	float3 tangent = float3(0, 0, 0);
	[unroll]
	for (int m = 0; m < 3; m++)
		if (normal[m] != 0) {
			int n = 0;
			if (m != 2) n++;
			tangent[n] = normal[m];
			tangent[m] = -normal[n];
			tangent = normalize(tangent);
			break;
		}
	float3 binormal = cross(normal, tangent);
	float3x3 TBN = float3x3(tangent, binormal, normal);

	//return _lightShadows.SampleCmpLevelZero(shadowSampler, vecToPixel, depthPixel).r;
	float value = 0;
	float radius = depthPixel * 0.05;

	for (int i = 0; i < 20; i++)
		value += _lightShadows.SampleCmpLevelZero(shadowSampler, vecToPixel + mul(float3(sampleOffsetDirections[i].xz, 0), TBN) * radius, depthPixel).r;

	return saturate(value / 20);
}

float4 ShadowToCamera(in DefferedPixel input) : SV_Target
{
	float4 position = _positionBuffer.Sample(clampSampler, input.TextureCoordinate);
	float3 vec = position.xyz - _lightPosition;

	float value = GetShadowValue(vec);
	return float4(value, 0, 0, 1);
}

PARAMETER(Texture2D _spotLightDepthMap);
PARAMETER(float3 _lightDirection);
PARAMETER(float _lightAngle);
PARAMETER(float _lightReach);
PARAMETER(float _lightHardness);
PARAMETER(float _lightSpecularHardness);
PARAMETER(float _lightDiffusePower);
PARAMETER(float _lightSpecularPower);

struct Ray {
	float3 Direction;
	float3 Position;
};

#define RAY_STEPS 50
#define G_SCATTERING 0.8

float ComputeScattering(float lightDotView)
{
	float result = 1.0f - G_SCATTERING * G_SCATTERING;
	result /= (4.0f * PI * pow(1.0f + G_SCATTERING * G_SCATTERING - (2.0f * G_SCATTERING) * lightDotView, 1.5f));
	return result;
}

float SpotlightShadowValue(in DefferedPixel input) 
{
	float4 position = _positionBuffer.Sample(borderSampler, input.TextureCoordinate);

	Ray ray;
	ray.Direction = _cameraPosition - position.xyz;
	ray.Position = position.xyz;
	float value = 0;

	[unroll(RAY_STEPS)]
	for (int i = 0; i < RAY_STEPS; i++)
	{
		position = float4(ray.Position + ray.Direction / RAY_STEPS * i, 1);
		float3 vec = position.xyz - _lightPosition;
		float len = length(vec);

		if (len > _lightReach)
			continue;

		vec.xyz /= len;

		float angle = dot(vec, _lightDirection) / length(_lightDirection);
		float lightCos = cos(_lightAngle);
		float diff = angle - lightCos;

		if (diff < 0)
			continue;

		if (i == 0) {
			float3 normal = GetNormal(input.TextureCoordinate);

			float diffuseValue = saturate(dot(normal, -vec));
			float3 halfVector = normalize(vec + _cameraPosition - position.xyz);
			float specularValue = pow(saturate(dot(normal, halfVector)), _lightSpecularHardness);

			position = mul(position, _matrix);
			position.xyz /= position.w;

			float2 uv = float2(0.5 * position.x + 0.5, -0.5 * position.y + 0.5);

			value += (diffuseValue * _lightDiffusePower + specularValue * _lightSpecularPower) * (1 - pow(1 - diff / (1 - lightCos), _lightHardness)) * _spotLightDepthMap.SampleCmpLevelZero(shadowSampler, uv, position.z).r;
			continue;
		}

		position = mul(position, _matrix);
		position.xyz /= position.w;

		float2 uv = float2(0.5 * position.x + 0.5, -0.5 * position.y + 0.5);

		float radius = 1.0 / 1024.0 * position.z;

		//for (int x = -2; x <= 2; x++)
		//	for (int y = -2; y <= 2; y++) {
		//		value += _spotLightDepthMap.SampleCmpLevelZero(shadowSampler, uv + float2(x, y) * radius, position.z).r;
		//	}
		value += _spotLightDepthMap.SampleCmpLevelZero(shadowSampler, uv, position.z).r * ComputeScattering(angle) * (1 - pow(1 - diff / (1 - lightCos), _lightHardness)) * 5;
	}
	
	return value / RAY_STEPS;
}

float4 SpotLightPS(in DefferedPixel input) : SV_Target
{
	float4 color = float4(0,0,0,1);
	float3 position = _positionBuffer.Sample(clampSampler, input.TextureCoordinate).rgb;
	float3 normal = GetNormal(input.TextureCoordinate);

	[unroll(MAX_LIGHTS)]
	for (int i = 0; i < _lightsCount; i++)
	{
		float shadowValue = (_shadowsEnabled[i] ? _shadowMaps.Sample(wrapSampler, float3(input.TextureCoordinate, i)).r : 1);

		if (shadowValue == 0.0)
			continue;

		//float3 lightDir = _lightingPosition[i] - position;
		//float dist = length(lightDir);

		//if (dist > _lightingRadius[i]) {
		//	color.rgb += _lightingColor[i].rgb * shadowValue;
		//	continue;
		//}

		//lightDir /= dist;

		//float angle = dot(-lightDir, _direction[i]) / length(_direction[i]);
		//float lightCos = cos(_diversionAngle[i]);
		//float diff = angle - lightCos;

		//if (diff < 0) {
		//	color.rgb += _lightingColor[i].rgb * shadowValue;
		//	continue;
		//}

		//float diffuseValue = saturate(dot(normal, lightDir));
		//float3 halfVector = normalize(lightDir + _cameraPosition - position);
		//float specularValue = pow(saturate(dot(normal, halfVector)), _specularHardness[i]);

		//color.rgb += (diffuseValue * _diffusePower[i] + specularValue * _specularPower[i]) /** (1 - pow(1 - diff / (1 - lightCos), _hardness[i]))*/
		//	* _lightingColor[i].rgb * shadowValue;
		color.rgb += shadowValue * _lightingColor[i].rgb;
	}

	return color;
}

float4 SpotlightShadowPS(in DefferedPixel input) : SV_Target
{
	return float4(SpotlightShadowValue(input), 0, 0, 1);
}

TECHNIQUE(GenerateShadowMap, RasterizeVS, ShadowToCamera);
TECHNIQUE(SpotLightShadowMap, RasterizeVS, SpotlightShadowPS);

TECHNIQUE(WriteDepthInstanced, WriteShadowVSInstanced, WriteShadow);
TECHNIQUE(WriteDepth, WriteShadowVSStandart, WriteShadow);

TECHNIQUE(Lighting, RasterizeVS, LightingPS);

TECHNIQUE(Spotlight, RasterizeVS, SpotLightPS);

#endif