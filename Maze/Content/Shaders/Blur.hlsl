#ifndef BLUR_DEF
#define BLUR_DEF

#define BLUR_PASSES 3

#include "Data.hlsl"
#include "Standart.hlsl"

#define samples 5u
#define LOD 0u        // gaussian done on MIPmap at scale LOD

SamplerState blurSampler = sampler_state 
{
    Filter = Anisotropic;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

float Gaussian(float2 i) {
    const float sigma = float(samples) * 0.25;
    return exp(-0.5 * dot(i /= sigma, i)) / (6.28 * sigma * sigma);
}

float4 blur(float2 U, float2 scale) 
{
    const uint sLOD = 1 << LOD; // tile size = 2^LOD

    float4 ret = float4(0, 0, 0, 0);
    uint s = samples / sLOD;

    for (uint i = 0; i < s * s; i++) {
        float2 d = float2(i % s, i / s) * float(sLOD) - float2(samples, samples) / 2.0;
        ret += Gaussian(d) * _texture.SampleLevel(blurSampler, U + scale * d, LOD);
    }

    return ret / ret.a;
}

float4 BlurPS(in DefferedPixel pixel) : COLOR
{
    float sizeX, sizeY;
    _texture.GetDimensions(sizeX, sizeY);
    return blur(pixel.TextureCoordinate, 1 / float2(sizeX, sizeY));
}

TECHNIQUE(Blur, RasterizeVS, BlurPS);

#endif