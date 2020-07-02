#ifndef SHADER_DEF
#define SHADER_DEF

struct Vertex
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD;
};

struct Pixel
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    float4 WorldPosition : TEXCOORD1;
};

uniform extern float4 _color;
uniform extern float4x4 _matrix;
uniform extern float4x4 _transform;
uniform extern texture _texture;

sampler2D textureSampler = sampler_state
{
    Texture = <_texture>;
    AddressU = WRAP;
    AddressV = WRAP;
    AddressW = WRAP;
};

Pixel StandartVS(in Vertex input)
{
    Pixel pixel = (Pixel) 0;
    
    pixel.WorldPosition = mul(input.Position, _transform);
    pixel.Position = mul(pixel.WorldPosition, _matrix);
    pixel.TextureCoordinate = input.TextureCoordinate;
    
    return pixel;
}

float4 StandartPS(in Pixel input) : SV_Target
{
    return tex2D(textureSampler, input.TextureCoordinate) * _color;
}

float4 ColorPS(in Pixel input) : SV_Target
{
    return _color;
}

technique Standart
{
    pass P0
    {
        VertexShader = compile vs_5_0 StandartVS();
        PixelShader = compile ps_5_0 StandartPS();
    }
}

technique Color
{
    pass P0
    {
        VertexShader = compile vs_5_0 StandartVS();
        PixelShader = compile ps_5_0 ColorPS();
    }
}

#endif