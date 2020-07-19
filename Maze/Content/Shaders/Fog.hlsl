#ifndef FOG_DEF
#define FOG_DEF

#include "Standart.hlsl"

PARAMETER(float4 _cameraPlane);
PARAMETER(float _fogStart);
PARAMETER(float _fogEnd);
PARAMETER(float4 _fogColor);

float4 FogPS(in DefferedPixel input) : COLOR0
{
    float4 color = tex2D(textureSampler, input.TextureCoordinate);
    float3 position = tex2D(positionSampler, input.TextureCoordinate).rgb;

    float dist = Distance(position, _cameraPlane);
    if (dist >= _fogStart)
    {
        if (dist > _fogEnd)
            dist = _fogEnd;
        return lerp(color, _fogColor, (dist - _fogStart) / (_fogEnd - _fogStart));
    }
    
    return color;
}

TECHNIQUE(Fog, RasterizeVS, FogPS);

#endif