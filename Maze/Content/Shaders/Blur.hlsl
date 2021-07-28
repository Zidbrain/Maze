#ifndef BLUR_DEF
#define BLUR_DEF

#define BLUR_PASSES 3

#include "Data.hlsl"
#include "Standart.hlsl"

static const float _offset[] = { 0.0, 1.3846153846, 3.2307692308 };
static const float _weight[] = { 0.2270270270 / 0.6135134593 / 2, 0.3162162162 / 0.6135134593 / 2, 0.0702702703 / 0.6135134593 / 2 };

PARAMETER(bool _vertical);

float4 BlurPS(in DefferedPixel input) : SV_Target
{
    float2 screenSize;
    _texture.GetDimensions(screenSize.x, screenSize.y);
    
    float3 color = _texture.Sample(anisotropicSampler, input.TextureCoordinate).rgb * _weight[0];
    if (_vertical)
    {
        for (int i = 0; i < BLUR_PASSES; i++)
        {
            color += _texture.Sample(anisotropicSampler, input.TextureCoordinate + float2(0.0, _offset[i] / screenSize.y)).rgb * _weight[i];
            color += _texture.Sample(anisotropicSampler, input.TextureCoordinate - float2(0.0, _offset[i] / screenSize.y)).rgb * _weight[i];
        }
    }
    else
    {
        for (int i = 0; i < BLUR_PASSES; i++)
        {
            color += _texture.Sample(anisotropicSampler, input.TextureCoordinate + float2(_offset[i] / screenSize.x, 0.0)).rgb * _weight[i];
            color += _texture.Sample(anisotropicSampler, input.TextureCoordinate - float2(_offset[i] / screenSize.x, 0.0)).rgb * _weight[i];
        }
    }
    
   //color =  clamp(color, float3(0, 0, 0), float3(1, 1, 1));

    return float4(color, 1);
}

TECHNIQUE(Blur, RasterizeVS, BlurPS);

#endif