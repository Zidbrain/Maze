﻿#ifndef STANDART_DEF
#define STANDART_DEF

#include "Data.hlsl"

PARAMETER(float4 _color);
PARAMETER(bool _onlyColor);
PARAMETER(float4x4 _transform);
PARAMETER(float4x4 _matrix);

PARAMETER(bool _normalEnabled);
PARAMETER(Texture2D _normalTexture);

PARAMETER(float _gamma);

Pixel StandartVS(in Vertex input)
{
    Pixel pixel = (Pixel) 0;
    
    pixel.WorldPosition = mul(input.Position, _transform).xyz;
    pixel.Position = mul(float4(pixel.WorldPosition, 1), _matrix);
    pixel.TextureCoordinate = input.TextureCoordinate;
    pixel.TBN = ConstructTBN(pixel.WorldPosition, input.Normal, input.Tangent, (float3x3) _transform);
    
    return pixel;
}

PARAMETER(texture2D tex);

PSOutput StandartPS(in Pixel input)
{
    PSOutput output = (PSOutput) 0;
    
    if (_onlyColor)
        output.Color = _color;
    else
        output.Color = _texture.Sample(anisotropicSampler, input.TextureCoordinate) * _color;
    
    output.Depth = float4(input.Position.z, 0, 0, 1);
    
    if (_normalEnabled && !_onlyColor)
    {
        float3 normal = _normalTexture.Sample(wrapSampler, input.TextureCoordinate).rgb * 2 - float3(1, 1, 1);
        output.Normal = float4((normalize(mul(normal, input.TBN)) + float3(1, 1, 1)) / 2, 1);
    }
    else
        output.Normal = float4((normalize(input.TBN[2]) + float3(1, 1, 1)) / 2, 1);
    
    output.Position = float4(input.WorldPosition, 1);
    
    return output;
}

DefferedPixel RasterizeVS(float4 position : SV_Position, float2 textureCoordinate : TEXCOORD)
{
    DefferedPixel ret = (DefferedPixel) 0;
    
    ret.Position = position;
    ret.TextureCoordinate = textureCoordinate;
    
    return ret;
}

float4 RasterizePS(in DefferedPixel input) : SV_Target
{
    return _texture.Sample(anisotropicSampler, input.TextureCoordinate);
}

PARAMETER(Texture2D _mask);

float4 MaskPS(in DefferedPixel input) : SV_Target
{
    float4 mask = _mask.Sample(clampSampler, input.TextureCoordinate);
    return RasterizePS(input) * _color * mask.r;
}

float4 GammaPS(in DefferedPixel input) : SV_Target
{
    return pow(RasterizePS(input), 1 / _gamma);
}

TECHNIQUE(Standart, StandartVS, StandartPS);
TECHNIQUE(Rasterize, RasterizeVS, RasterizePS);
TECHNIQUE(Gamma, RasterizeVS, GammaPS);
TECHNIQUE(Mask, RasterizeVS, MaskPS);

#endif