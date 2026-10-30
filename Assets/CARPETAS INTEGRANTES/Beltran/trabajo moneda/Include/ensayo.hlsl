//include guard
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
#endif