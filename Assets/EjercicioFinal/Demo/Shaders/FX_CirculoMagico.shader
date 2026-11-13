// Efecto 2 (Teletransporte): círculo mágico procedural para un quad horizontal.
// Coordenadas polares: tres anillos, una banda de runas generadas con ruido, un hexagrama
// y un brillo central. Los anillos giran en sentidos opuestos. _Aparicion (0..1) lo va
// dibujando en barrido circular y _Intensidad lo apaga al final.
Shader "Demo/FX Circulo Magico"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1.6, 0.5, 3.2, 1)
        [HDR] _ColorRunas ("Color de las runas", Color) = (2.5, 1.6, 4, 1)
        _Aparicion ("Aparición", Range(0, 1)) = 1
        _Intensidad ("Intensidad", Range(0, 2)) = 1
        _Giro ("Velocidad de giro", Float) = 0.6
        _Runas ("Cantidad de runas", Float) = 28
        _TiempoFX ("Tiempo del efecto", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FXCirculo"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FXComun.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _ColorRunas;
                half _Aparicion;
                half _Intensidad;
                float _Giro;
                float _Runas;
                float _TiempoFX;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            half Anillo(float r, float radio, float grosor)
            {
                return smoothstep(grosor, 0.0, abs(r - radio));
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float r = length(p);
                if (r > 1.0) return 0;

                float t = _TiempoFX;
                float2 pFuera = FxRotar(p, t * _Giro);
                float2 pDentro = FxRotar(p, -t * _Giro * 1.6);
                float ang = atan2(pFuera.y, pFuera.x) / (2.0 * PI) + 0.5; // 0..1

                // anillos
                half lineas = Anillo(r, 0.95, 0.018) + Anillo(r, 0.80, 0.012) + Anillo(r, 0.58, 0.012) * 0.8;

                // runas: celdas alrededor de la banda entre los dos anillos exteriores
                float celda = floor(ang * _Runas);
                float2 local = float2(frac(ang * _Runas), (r - 0.82) / 0.11);
                half enBanda = step(0.0, local.y) * step(local.y, 1.0);
                float2 trazo = floor(local * float2(3.0, 4.0));
                half runa = step(0.45, FxHash21(trazo + celda * 7.31));
                half margen = step(0.12, local.x) * step(local.x, 0.88) * step(0.1, local.y) * step(local.y, 0.9);
                half runas = enBanda * runa * margen;

                // hexagrama: seis rectas tangentes a un círculo interior, recortadas por el anillo
                half estrella = 0;
                [unroll]
                for (int k = 0; k < 6; k++)
                {
                    float a = k * PI / 3.0;
                    float2 n = float2(cos(a), sin(a));
                    estrella += smoothstep(0.012, 0.0, abs(dot(pDentro, n) - 0.29));
                }
                estrella *= step(r, 0.58);

                // brillo central que late
                half centro = exp(-r * 6.0) * (0.6 + 0.4 * sin(t * 6.0));

                // barrido de aparición
                half visible = step(ang, _Aparicion) * smoothstep(0.0, 0.1, _Aparicion);
                half borde = smoothstep(1.0, 0.9, r);

                half3 c = (_Color.rgb * (lineas + estrella * 0.8 + centro) + _ColorRunas.rgb * runas) * visible * borde;
                return half4(c * _Intensidad * i.color.rgb * i.color.a, 0);
            }
            ENDHLSL
        }
    }
}
