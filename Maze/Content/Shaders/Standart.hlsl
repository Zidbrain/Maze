#ifndef STANDART_DEF
#define STANDART_DEF

#include "Data.hlsl"

PARAMETER(float4 _color);
PARAMETER(bool _onlyColor);
PARAMETER(float4x4 _matrix);
PARAMETER(float4x4 _transform);

Pixel StandartVS(in Vertex input)
{
    Pixel pixel = (Pixel) 0;
    
    pixel.WorldPosition = mul(input.Position, _transform).xyz;
    pixel.Position = mul(input.Position, mul(_transform, _matrix));
    pixel.TextureCoordinate = input.TextureCoordinate;
    pixel.Normal = input.Normal;
    
    return pixel;
}

PSOutput StandartPS(in Pixel input)
{
    PSOutput output = (PSOutput) 0;
    
    if (_onlyColor)
        output.Color = _color;
    else
        output.Color = tex2D(textureSampler, input.TextureCoordinate) * _color;
    
    output.Depth = input.Position.z / input.Position.w;
    output.Normal = float4(input.Normal, 1);
    output.Position = float4(input.WorldPosition, 1);
    
    return output;
}

RasterizePixel RasterizeVS(float4 position : SV_Position, float2 textureCoordinate : TEXCOORD)
{
    RasterizePixel ret = (RasterizePixel) 0;
    
    ret.Position = position;
    ret.TextureCoordinate = textureCoordinate;
    
    return ret;
}

float4 RasterizePS(in RasterizePixel input) : COLOR
{
    return tex2D(textureSampler, input.TextureCoordinate);
}

TECHNIQUE(Standart, StandartVS, StandartPS);
TECHNIQUE(Rasterize, RasterizeVS, RasterizePS);

#endif