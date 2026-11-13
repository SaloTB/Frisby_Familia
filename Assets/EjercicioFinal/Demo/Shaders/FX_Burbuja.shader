// Efecto 1 (Burbuja): pompa de jabón para una esfera.
// Casi transparente en el centro y visible en el borde (fresnel). El color del borde es una
// película iridiscente: el tono cambia con el ángulo de vista y con un remolino de ruido que
// se mueve. Tiene un reflejo especular de la luz principal y una ventana de reflejo fija, y
// la superficie tiembla (_Tambaleo) desplazando los vértices. _Alfa la apaga al reventar.
Shader "Demo/FX Burbuja"
{
    Properties
    {
        _ColorBase ("Color base", Color) = (0.6, 0.85, 1, 1)
        _Opacidad ("Opacidad del centro", Range(0, 1)) = 0.06
        _Fresnel ("Potencia del borde", Range(0.5, 8)) = 2.2
        _Iridiscencia ("Iridiscencia", Range(0, 1)) = 0.8
        _Brillo ("Brillo", Range(0, 4)) = 1.6
        _Tambaleo ("Tambaleo", Range(0, 0.2)) = 0.03
        _Alfa ("Alfa", Range(0, 1)) = 1
        _TiempoFX ("Tiempo del efecto", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "FXBurbuja"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "FXComun.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorBase;
                half _Opacidad;
                half _Fresnel;
                half _Iridiscencia;
                half _Brillo;
                half _Tambaleo;
                half _Alfa;
                float _TiempoFX;
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
                float t = _TiempoFX;
                float3 p = i.positionOS.xyz;
                float onda = sin(p.x * 7.0 + t * 7.0) * sin(p.y * 6.0 - t * 5.0) + sin(p.z * 8.0 + t * 6.0) * 0.5;
                p += i.normalOS * onda * _Tambaleo;
                VertexPositionInputs pos = GetVertexPositionInputs(p);
                o.positionHCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _TiempoFX;
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                half ndv = saturate(dot(n, v));
                half fres = pow(1.0 - ndv, _Fresnel);

                // película iridiscente: tono según el ángulo + remolino de ruido
                float remolino = FxFbm(i.uv * float2(6.0, 3.0) + float2(t * 0.15, -t * 0.25));
                half3 pelicula = FxTono(frac(ndv * 1.4 + remolino * 0.8 + t * 0.05));
                half3 color = lerp(_ColorBase.rgb, pelicula, _Iridiscencia) * (0.25 + fres * 1.6) * _Brillo;

                // reflejos: luz principal y una "ventana" fija arriba a un lado
                Light luz = GetMainLight();
                float3 r = reflect(-v, n);
                half especular = pow(saturate(dot(r, luz.direction)), 80.0) * 3.0;
                half ventana = smoothstep(0.93, 0.97, dot(r, normalize(float3(-0.4, 0.8, 0.45)))) * 0.9;
                color += (especular + ventana);

                half alfa = saturate(_Opacidad + fres * 0.85 + especular + ventana) * _Alfa;
                return half4(color, alfa);
            }
            ENDHLSL
        }
    }
}
