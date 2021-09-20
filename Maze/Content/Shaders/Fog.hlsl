#ifndef FOG_DEF
#define FOG_DEF

#include "Standart.hlsl"
#include "Data.hlsl"

PARAMETER(float _fogStart);
PARAMETER(float _fogEnd);
PARAMETER(float4 _fogColor);

float4 FogPS(in DefferedPixel input) : COLOR0
{
    float4 position = _positionBuffer.Sample(clampSampler, input.TextureCoordinate);
    
    float4 color = float4(_fogColor.r, _fogColor.g, _fogColor.b, 0);//_texture.Sample(anisotropicSampler, input.TextureCoordinate);

    float dist = distance(position.xyz, _cameraPosition);
    if (dist >= _fogStart)
    {
        if (dist > _fogEnd)
            dist = _fogEnd;
        color.a = (dist - _fogStart) / (_fogEnd - _fogStart);
    }
    
    return color;
}

TECHNIQUE(Fog, RasterizeVS, FogPS);

#endif