//Include Guard
#ifndef LIGHTING_CORE_G2
#define LIGHTING_CORE_G2

//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

void GetMainLight_float(float3 positionWS, out float3 directionWS, out float3 color, out float shadowAttenuation)
{
    #if defined(SHADERGRAPH_PREVIEW)
    directionWS = normalize(float3(1,1,-1));
    color = 1;
    shadowAttenuation = 1;
    #else
    Light l = GetMainLight();
    directionWS = l.direction;
    color = l.color;
    shadowAttenuation = l.shadowAttenuation;
    #endif
}

void SimpleLightingAdditional_float(float2 UVSS, float3 normalWS, float3 viewDirWS, float3 positionWS, float roughness, 
    out float3 lit)
{
    #if defined(SHADERGRAPH_PREVIEW)
    lit = 0;
    #else
    uint additionalLightCount = GetAdditionalLightsCount();
    
    #ifdef USE_CLUSTER_LIGHT_LOOP
    InputData inputData = (InputData)0;
    inputData.normalizedScreenSpaceUV = UVSS;
    inputData.positionWS = positionWS;
    inputData.normalWS = normalWS;
    #endif
    
    lit = 0;
    
    LIGHT_LOOP_BEGIN(additionalLightCount)
    Light light = GetAdditionalLight(lightIndex, positionWS, 1);
    //TODO Get Additional Light Index
    
    float lambert = max(dot(light.direction, normalWS),0);
    
    float3 h = normalize(viewDirWS + light.direction);
    float blinnPhong = max(dot(h, normalWS), 0);
    float specularPower = lerp(200, 1, roughness);
    float specularCoefficient = pow(1 - roughness, 2);
    
    float3 diffuse = lambert * light.color * light.distanceAttenuation * light.shadowAttenuation;
    float3 specular = pow(blinnPhong, specularPower) 
    * specularCoefficient 
    * light.color 
    * light.distanceAttenuation 
    * light.shadowAttenuation;
    lit += diffuse + specular;
    LIGHT_LOOP_END
    #endif
}

void BlinnPhongAdditional_float(float2 UVSS, float3 normalWS, float3 viewDirWS, float3 positionWS, float roughness, 
    out float3 lit)
{
    #if defined(SHADERGRAPH_PREVIEW)
    lit = 0;
    #else
    uint additionalLightCount = GetAdditionalLightsCount();
    
    #ifdef USE_CLUSTER_LIGHT_LOOP
    InputData inputData = (InputData)0;
    inputData.normalizedScreenSpaceUV = UVSS;
    inputData.positionWS = positionWS;
    inputData.normalWS = normalWS;
    #endif
    
    lit = 0;
    
    LIGHT_LOOP_BEGIN(additionalLightCount)
    Light light = GetAdditionalLight(lightIndex, positionWS, 1);
    //TODO Get Additional Light Index
    
    float3 h = normalize(viewDirWS + light.direction);
    float blinnPhong = max(dot(h, normalWS), 0);
    float specularPower = lerp(200, 1, roughness);
    float specularCoefficient = pow(1 - roughness, 2);
    
    float3 specular = pow(blinnPhong, specularPower) 
    * specularCoefficient 
    * light.color 
    * light.distanceAttenuation 
    * light.shadowAttenuation;
    lit += specular;
    LIGHT_LOOP_END
    #endif
}
#endif

