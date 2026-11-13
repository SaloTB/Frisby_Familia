// Caja de objeto con pescado (jugabilidad): cubo translúcido con borde y reflejo arcoíris
// que cambia de tono con la altura y el tiempo, más brillante en los bordes (fresnel) y en
// las aristas de cada cara. El pescado opaco de adentro se ve a través.
Shader "Demo/FX Caja Pescado"
{
    Properties
    {
        _Opacidad ("Opacidad del centro", Range(0, 1)) = 0.18
        _Fresnel ("Potencia del borde", Range(0.5, 8)) = 2.5
        _Arista ("Grosor de las aristas", Range(0.01, 0.3)) = 0.08
        _Brillo ("Brillo", Range(0, 4)) = 1.6
        _VelocidadTono ("Velocidad del arcoíris", Float) = 0.25
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "FXCaja"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FXComun.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Opacidad;
                half _Fresnel;
                half _Arista;
                half _Brillo;
                float _VelocidadTono;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionHCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                half fres = pow(1.0 - saturate(dot(n, v)), _Fresnel);

                float2 d = abs(i.uv - 0.5) * 2.0;
                half arista = smoothstep(1.0 - _Arista, 1.0 - _Arista * 0.4, max(d.x, d.y));

                float tono = frac(i.positionWS.y * 0.35 + _Time.y * _VelocidadTono + fres * 0.25 + (i.uv.x + i.uv.y) * 0.15);
                half3 arcoiris = lerp(FxTono(tono), 1.0, 0.35);

                half3 color = arcoiris * (0.35 + fres * 1.2 + arista * 1.5) * _Brillo;
                half alfa = saturate(_Opacidad + fres * 0.55 + arista * 0.85);
                return half4(color, alfa);
            }
            ENDHLSL
        }
    }
}
