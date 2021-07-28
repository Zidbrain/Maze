#ifndef SSAO_DEF
#define SSAO_DEF

#define KERNELS_COUNT 16

#include "Data.hlsl"
#include "Standart.hlsl"

const float2 _noiseScale = float2(1920 / 4.0, 1080 / 4.0); // dependency on screen dimensions - bad

PARAMETER(Texture2D _noiseTexture);
PARAMETER(float3 _kernels[KERNELS_COUNT]);
PARAMETER(float _SSAORadius);

float4 SSAOPS(in DefferedPixel input) : SV_Target
{
    float3 position = _positionBuffer.Sample(borderSampler, input.TextureCoordinate).xyz;
    float3 normal = GetNormal(input.TextureCoordinate);
    
    float3 noise = _noiseTexture.Sample(wrapSampler, input.TextureCoordinate * _noiseScale).xyz;
    
    float3 tangent = normalize(noise - normal * dot(noise, normal));
    float3x3 TBN = float3x3(tangent, cross(normal, tangent), normal);
    
    float occlusion = 0.0;
    
    [unroll(KERNELS_COUNT)]
    for (int i = 0; i < KERNELS_COUNT; i++)
    {
        float3 worldKernelPos = mul(_kernels[i], TBN);
        worldKernelPos = position + worldKernelPos * _SSAORadius;
        
        float4 csKernelPos = mul(float4(worldKernelPos, 1.0), _matrix);
        csKernelPos.xyz /= csKernelPos.w;
        csKernelPos.xy = csKernelPos.xy * float2(0.5, -0.5) + float2(0.5, 0.5);
        csKernelPos.xy = clamp(csKernelPos.xy, float2(0, 0), float2(1, 1));
        
        float sampleDepth = _depthBuffer.Sample(clampSampler, csKernelPos.xy).r;
        occlusion += (sampleDepth <= csKernelPos.z ? 1.0 : 0.0) * smoothstep(0.0, 1.0, _SSAORadius / distance(position, _positionBuffer.Sample(borderSampler, csKernelPos.xy).xyz)); // works wrong on edges
    }
    occlusion = 1.0 - (occlusion / KERNELS_COUNT);
    occlusion = pow(occlusion, 1);
    
    return float4(occlusion, 0, 0, 1.0);
}

TECHNIQUE(SSAO, RasterizeVS, SSAOPS);

#endif