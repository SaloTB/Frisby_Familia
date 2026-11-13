// Efecto 3 (Nova de Calabaza): fuego procedural para la onda expansiva (anillo) y la
// columna de llamas (cilindro abierto). uv.x rodea la malla, uv.y va de la base (0) a la
// punta (1). Ruido fractal que sube con el tiempo, recortado con un umbral que crece con
// _Disolver para que las llamas se deshagan al apagarse. Tres colores: núcleo, base y punta.
Shader "Demo/FX Fuego"
{
    Properties
    {
        [HDR] _ColorNucleo ("Color del núcleo", Color) = (4, 2.6, 0.9, 1)
        [HDR] _ColorBase ("Color de la base", Color) = (4, 1.1, 0.15, 1)
        [HDR] _ColorPunta ("Color de la punta", Color) = (1.2, 0.15, 0.05, 1)
        _EscalaX ("Escala del ruido (alrededor)", Float) = 10
        _EscalaY ("Escala del ruido (alto)", Float) = 3
        _Velocidad ("Velocidad de subida", Float) = 2.2
        _PotenciaForma ("Forma (bajo = llega fuerte hasta arriba)", Range(0.1, 3)) = 1
        _Disolver ("Disolver", Range(0, 1)) = 0
        _Intensidad ("Intensidad", Range(0, 3)) = 1
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
            Name "FXFuego"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FXComun.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorNucleo;
                half4 _ColorBase;
                half4 _ColorPunta;
                float _EscalaX;
                float _EscalaY;
                float _Velocidad;
                half _PotenciaForma;
                half _Disolver;
                half _Intensidad;
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

            half4 frag(Varyings i) : SV_Target
            {
                float t = _TiempoFX;
                // uv.x da la vuelta completa: se usa seno/coseno para que el ruido no tenga costura
                float a = i.uv.x * 2.0 * PI;
                float2 alrededor = float2(cos(a), sin(a)) * (_EscalaX / (2.0 * PI));
                float2 q = alrededor + float2(0.0, i.uv.y * _EscalaY - t * _Velocidad);
                float n = FxFbm(q + FxFbm(q * 0.5 + t * 0.3) * 0.8);

                float forma = pow(saturate(1.0 - i.uv.y), _PotenciaForma); // más fuego abajo
                forma *= smoothstep(1.0, 0.82, i.uv.y);                    // la punta se deshace en lenguas
                float llama = n * 1.35 * forma + forma * 0.25;
                float umbral = lerp(0.28, 1.1, _Disolver);
                half masa = smoothstep(umbral, umbral + 0.18, llama);

                half3 color = lerp(_ColorPunta.rgb, _ColorBase.rgb, saturate(forma * 1.3));
                color = lerp(color, _ColorNucleo.rgb, smoothstep(umbral + 0.25, umbral + 0.55, llama));

                half base = smoothstep(0.0, 0.06, i.uv.y);         // sin corte duro en la base
                return half4(color * masa * base * _Intensidad * i.color.rgb * i.color.a, 0);
            }
            ENDHLSL
        }
    }
}
