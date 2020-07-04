#ifndef FOG_DEF
#define FOG_DEF

#include "Standart.hlsl"

PARAMETER(float4 _cameraPlane);
PARAMETER(float _fogStart);
PARAMETER(float _fogEnd);
PARAMETER(float4 _fogColor);

PSOutput FogPS(in Pixel input)
{
    PSOutput output = StandartPS(input);
    
    float dist = Distance(input.WorldPosition, _cameraPlane);
    if (dist >= _fogStart)
    {
        if (dist > _fogEnd)
            dist = _fogEnd;
        output.Color = lerp(output.Color, _fogColor, (dist - _fogStart) / (_fogEnd - _fogStart));
    }
    
    return output;
}

TECHNIQUE(Fog, StandartVS, FogPS);

#endif