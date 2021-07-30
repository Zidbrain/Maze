#ifndef LIGHTING_DEF
#define LIGHTING_DEF

#include "Data.hlsl"
#include "Standart.hlsl"
#include "InstancedDrawing.hlsl"

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
    
    PARAMETER(Texture2DArray _shadowMaps);
    PARAMETER(int _shadowsEnabled[MAX_LIGHTS]);
};

    PARAMETER(Texture2D _SSAOMap);
    PARAMETER(float _diversionAngle[MAX_LIGHTS]);
    PARAMETER(float3 _direction[MAX_LIGHTS]);
    PARAMETER(matrix _directionMatrix[MAX_LIGHTS]);

    PARAMETER(float4x4 _lightViewMatrices[6]);
    PARAMETER(int _lightViewLength);
    PARAMETER(Texture2DArray _lightShadows);
    PARAMETER(float3 _lightPosition);

float sqr(in float value)
{
    return value * value;
}

float4 SpotLightPS(in DefferedPixel input) : COLOR
{
    float4 color = _texture.Sample(anisotropicSampler, input.TextureCoordinate);
    float3 position = _positionBuffer.Sample(clampSampler, input.TextureCoordinate).rgb;
    float3 normal = GetNormal(input.TextureCoordinate);
    
    [unroll(MAX_LIGHTS)]
    for (int i = 0; i < _lightsCount; i++)
    {
        float shadowValue = (_shadowsEnabled[i] ? _shadowMaps.Sample(wrapSampler, float3(input.TextureCoordinate, i)).r : 1);
        
        float3 lightDir = _lightingPosition[i] - position;
        float dist = length(lightDir);
        float csa = cos(_diversionAngle[i]);   
        
        lightDir /= dist;
        
        float3 x0 = mul(float4(position, 1), _directionMatrix[i]).xyz;
        float3 s = mul(float4(_cameraPosition - position, 1), _directionMatrix[i]).xyz;
                        
        float angle = angleCos(_direction[i], lightDir);
        bool isIn = false;
        if (angle > csa && dist <= _lightingRadius[i])
        {
            float diffuseValue = saturate(dot(normal, lightDir)) * pow(_diversionAngle[i] - acos(angle), 1 / _hardness[i]) / _diversionAngle[i];
            float3 halfVector = normalize(lightDir + _cameraPosition - position);
            float specularValue = pow(saturate(dot(normal, halfVector)), _specularHardness[i]);
    
            color.rgb += color.rgb * lerp(diffuseValue * _diffusePower[i] + specularValue * _specularPower[i], float3(0, 0, 0), pow(dist / _lightingRadius[i], _hardness[i])) * _lightingColor[i].rgb * shadowValue;
            isIn = true;
        }
        
        float a = s.z * s.z - dot(s, s) * csa * csa;
        float b = x0.z * s.z - dot(x0, s) * csa * csa;
        float c = x0.z * x0.z - dot(x0, x0) * csa * csa;
        
        float d = b * b - a * c;
        if (d >= 0)
        {
            float t1 = (-b + sqrt(d)) / a;
            float t2 = (-b - sqrt(d)) / a;
            float3 s1 = x0 + s * t1;
            float3 s2 = x0 + s * t2;          
            
            if ((t1 >= 0 && t1 <= 1 && s1.z < 0 && length(s1) <= _lightingRadius[i]) ||
                 (t2 >= 0 && t2 <= 1 && s2.z < 0 && length(s2) <= _lightingRadius[i]) || isIn)
            {
                //float v = abs(t2 - t1);
                ////if (isIn)
                ////    v = max(abs(t1), abs(t2));
                //v = 2 * atan(v) / PI;
                
                float v =  1 - length(x0 - s * (dot(x0, s) / lengthSqr(s))) / _lightingRadius[i];
                
                float value =  pow(v, 1 / _hardness[i]) * _diffusePower[i];
                
                //if (isIn)
                //    value = lerp(value, float3(0, 0, 0), pow(dist / _lightingRadius[i], _hardness[i]));
                
                color.rgb += color.rgb * value * _lightingColor[i].rgb * shadowValue;
            }
        }
    }
    
    return color;
}

float4 LightingPS(in DefferedPixel input) : COLOR
{
    float4 color = _texture.Sample(anisotropicSampler, input.TextureCoordinate);
    float3 position = _positionBuffer.Sample(clampSampler, input.TextureCoordinate).rgb;
    float3 normal = GetNormal(input.TextureCoordinate);
    
    [unroll(MAX_LIGHTS)]
    for (int i = 0; i < _lightsCount; i++)
    {
        float shadowValue = (_shadowsEnabled[i] ? _shadowMaps.Sample(wrapSampler, float3(input.TextureCoordinate, i)).r : 1);
        
        float3 lightDir = _lightingPosition[i] - position;
        float dist = length(lightDir);
        
        lightDir /= dist;
        
        if (dist > _lightingRadius[i])
            continue;
    
        float diffuseValue = saturate(dot(normal, lightDir));
        float3 halfVector = normalize(lightDir + _cameraPosition - position);
        float specularValue = pow(saturate(dot(normal, halfVector)), _specularHardness[i]);
    
        color.rgb += color.rgb * lerp(diffuseValue * _diffusePower[i] + specularValue * _specularPower[i], float3(0, 0, 0), pow(dist / _lightingRadius[i], _hardness[i])) * _lightingColor[i].rgb * shadowValue;
    }
    
    return color;
}

Pixel WriteShadowVS(in float4 position, in float4x4 transform, in float3 normal)
{
    Pixel ret = (Pixel) 0;
    
    ret.WorldPosition = mul(position, transform).xyz;
  
    //normal = normalize(mul(normal, (float3x4) transform).xyz);
    //OrientVector(normal, _lightPosition, ret.WorldPosition);
    //ret.WorldPosition -= normal * 0.001;
    
    ret.Position = mul(float4(ret.WorldPosition, 1), _matrix);
    
    return ret;
}

Pixel WriteShadowVSInstanced(in InstancedVertexInput input)
{
    return WriteShadowVS(input.Position, _matrices[input.Instance], input.Normal);
}

Pixel WriteShadowVSStandart(in Vertex input)
{
    return WriteShadowVS(input.Position, _transform, input.Normal);
}

float4 WriteShadow(in Pixel input) : SV_Target
{
    //return float4(1 / length(input.WorldPosition - _lightPosition), 0, 0, 1);
    return float4(input.Position.z, 0, 0, 1);
}

uint GetCubemapFace(in float3 vec)
{
    float maxValue = max(max(abs(vec.x), abs(vec.y)), abs(vec.z));

    if (vec.x == maxValue)
        return 0;
    else if (-vec.x == maxValue)
        return 1;
    else if (vec.y == maxValue)
        return 2;
    else if (-vec.y == maxValue)
        return 3;
    else if (vec.z == maxValue)
        return 4;
    else
        return 5;
}

float GetShadowValue(in float3 vecToPixel, in float4 position)
{
    uint sizeX, sizeY, elements;
    _lightShadows.GetDimensions(sizeX, sizeY, elements);
    
    float2 step = float2(1.0 / sizeX, 1.0 / sizeY);
    
    uint face = 0;
    if (_lightViewLength == 6)
        face = GetCubemapFace(vecToPixel);
    
    float4 lightViewCoordinates = mul(position, _lightViewMatrices[face]);
    
    if (lightViewCoordinates.z < 0)
        return 1;
    
    float3 projCoords = lightViewCoordinates.xyz / lightViewCoordinates.w;
    
    float2 uv = float2(0.5 * projCoords.x + 0.5, -0.5 * projCoords.y + 0.5);
    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
        return 1;
    
   float depthPixel = projCoords.z;
    
    float value = 0;
    
    [unroll(3)]
    for (int y = -1; y <= 1; y++)
    {
        [unroll(3)]
        for (int x = -1; x <= 1; x++)
        {
            float2 offsets = float2(x, y) * step;
            float2 newUV = uv + offsets;
            float depthLight = _lightShadows.Sample(borderSampler, float3(newUV, face)).r;
            
            if (depthLight - depthPixel > -0.0003)
                value += 1;
        }
    }
 
    return value / 9;
}

float4 ShadowToCamera(in DefferedPixel input) : SV_Target
{
    float4 position = _positionBuffer.Sample(clampSampler, input.TextureCoordinate);
    float3 vec = position.xyz - _lightPosition;
    
    float value = GetShadowValue(vec, position);
    return float4(value, 0, 0, 1);
}

TECHNIQUE(GenerateShadowMap, RasterizeVS, ShadowToCamera);

TECHNIQUE(WriteDepthInstanced, WriteShadowVSInstanced, WriteShadow);
TECHNIQUE(WriteDepth, WriteShadowVSStandart, WriteShadow);

TECHNIQUE(Lighting, RasterizeVS, LightingPS);

TECHNIQUE(Spotlight, RasterizeVS, SpotLightPS);

#endif