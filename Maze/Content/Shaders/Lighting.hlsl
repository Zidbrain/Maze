#ifndef LIGHTING_DEF
#define LIGHTING_DEF

#include "Data.hlsl"
#include "Standart.hlsl"
#include "InstancedDrawing.hlsl"

#define MAX_LIGHTS 5

cbuffer LightData : register(b1)
{
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
};

    PARAMETER(float4x4 _lightViewMatrices[6]);
    PARAMETER(Texture2DArray _lightShadows);
    PARAMETER(float3 _lightPosition);

float sqr(in float value)
{
    return value * value;
}

float4 LightingPS(in DefferedPixel input) : COLOR
{
    float4 color = tex2D(textureSampler, input.TextureCoordinate);
    float3 position = tex2D(positionSampler, input.TextureCoordinate).rgb;
    float3 normal = GetNormal(input.TextureCoordinate);
    
    [unroll(MAX_LIGHTS)]
    for (int i = 0; i < _lightsCount; i++)
    {
        float shadowValue = _shadowsEnabled[i] ? _shadowMaps.Sample(wrapSampler, float3(input.TextureCoordinate, i)).r : 1;
        
        float3 lightDir = _lightingPosition[i] - position;
        float dist = length(lightDir);
        
        lightDir /= dist;
        
        if (dist > _lightingRadius[i])
            continue;
    
        float diffuseValue = saturate(dot(normal, lightDir));
        float3 halfVector = normalize(lightDir + _cameraPosition - position);
        float specularValue = pow(saturate(dot(normal, halfVector)), _specularHardness[i]);
    
        color.rgb += color.rgb * lerp(diffuseValue * _diffusePower[i] + specularValue * _specularPower[i], float3(0, 0, 0), pow(dist / _lightingRadius[i], _hardness[i])) * _lightingColor[i].rgb * shadowValue;
    }
    
    return color;
}

Pixel WriteShadowVS(in float4 position, in float4x4 transform, in float3 normal)
{
    Pixel ret = (Pixel) 0;
    
    ret.WorldPosition = mul(position, transform).xyz;
  
    //normal = normalize(mul(normal, (float3x4) transform).xyz);
    //OrientVector(normal, _lightPosition, ret.WorldPosition);
    //ret.WorldPosition -= normal * 0.001;
    
    ret.Position = mul(float4(ret.WorldPosition, 1), _matrix);
    
    return ret;
}

Pixel WriteShadowVSInstanced(in InstancedVertexInput input)
{
    return WriteShadowVS(input.Position, _matrices[input.Instance], input.Normal);
}

Pixel WriteShadowVSStandart(in Vertex input)
{
    return WriteShadowVS(input.Position, _transform, input.Normal);
}

float4 WriteShadow(in Pixel input) : SV_Target
{
    //return float4(1 / length(input.WorldPosition - _lightPosition), 0, 0, 1);
    return float4(input.Position.z, 0, 0, 1);
}

uint GetCubemapFace(in float3 vec)
{
    float maxValue = max(max(abs(vec.x), abs(vec.y)), abs(vec.z));

    if (vec.x == maxValue)
        return 0;
    else if (-vec.x == maxValue)
        return 1;
    else if (vec.y == maxValue)
        return 2;
    else if (-vec.y == maxValue)
        return 3;
    else if (vec.z == maxValue)
        return 4;
    else
        return 5;
}

float GetShadowValue(in float3 vecToPixel, in float4 position)
{
    uint sizeX, sizeY, elements;
    _lightShadows.GetDimensions(sizeX, sizeY, elements);
    
    float2 step = float2(1.0 / sizeX, 1.0 / sizeY);
    
    uint face = GetCubemapFace(vecToPixel);
    
    float4 lightViewCoordinates = mul(position, _lightViewMatrices[face]);
    float3 projCoords = lightViewCoordinates.xyz / lightViewCoordinates.w;
    
    float2 uv = float2(0.5 * projCoords.x + 0.5, -0.5 * projCoords.y + 0.5);
    float depthPixel = projCoords.z;
    
    float value = 0;
    
    [unroll(3)]
    for (int y = -1; y <= 1; y++)
    {
        [unroll(3)]
        for (int x = -1; x <= 1; x++)
        {
            float2 offsets = float2(x, y) * step;
            float2 newUV = uv + offsets;
            float depthLight = _lightShadows.Sample(borderSampler, float3(newUV, face)).r;
            
            if (depthLight - depthPixel > -0.0003)
                value += 1;
        }
    }
 
    return value / 9;
}

float4 ShadowToCamera(in DefferedPixel input) : SV_Target
{
    float4 position = tex2D(positionSampler, input.TextureCoordinate);
    float3 vec = position.xyz - _lightPosition;
    
    float value = GetShadowValue(vec, position);
    return float4(value, 0, 0, 1);
}

TECHNIQUE(GenerateShadowMap, RasterizeVS, ShadowToCamera);

TECHNIQUE(WriteDepthInstanced, WriteShadowVSInstanced, WriteShadow);
TECHNIQUE(WriteDepth, WriteShadowVSStandart, WriteShadow);

TECHNIQUE(Lighting, RasterizeVS, LightingPS);

#endif