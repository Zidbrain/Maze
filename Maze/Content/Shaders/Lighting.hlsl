#ifndef LIGHTING_DEF
#define LIGHTING_DEF

#include "Data.hlsl"
#include "Standart.hlsl"

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
}

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
        float3 lightDir = _lightingPosition[i] - position;      
        float dist = length(lightDir);
        
        lightDir /= dist;
        
        if (dist > _lightingRadius[i])
            continue;
    
        float diffuseValue = abs(dot(normal, lightDir));
        float3 halfVector = normalize(lightDir + _cameraPosition - position);
        float specularValue = pow(abs(dot(normal, halfVector)), _specularHardness[i]);
    
        color.rgb += color.rgb * lerp(diffuseValue * _diffusePower[i] + specularValue * _specularPower[i], float3(0, 0, 0), pow(dist / _lightingRadius[i], _hardness[i])) * _lightingColor[i].rgb;
    }
    
    return color;
}

TECHNIQUE(Lighting, RasterizeVS, LightingPS);

#endif