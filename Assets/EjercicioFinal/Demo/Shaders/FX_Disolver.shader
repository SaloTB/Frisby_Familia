// Efecto 2 (Teletransporte): se pone en el pato mientras se teletransporta.
// Ilumina la textura del pato con un sombreado de dos tonos (como el resto de la escena)
// y lo va borrando según su altura más ruido, con un borde que brilla.
// _Disolver: 0 = pato entero, 1 = pato desaparecido. _Direccion: 1 se borra de abajo hacia
// arriba, -1 de arriba hacia abajo. _MinY/_MaxY: alto del mesh en espacio de objeto.
Shader "Demo/FX Disolver"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        [HDR] _ColorBorde ("Color del borde", Color) = (3, 1.2, 6, 1)
        _Disolver ("Disolver", Range(0, 1)) = 0
        _AnchoBorde ("Ancho del borde", Range(0.01, 0.3)) = 0.08
        _EscalaRuido ("Escala del ruido", Float) = 9
        _MinY ("Altura mínima (objeto)", Float) = 0
        _MaxY ("Altura máxima (objeto)", Float) = 1
        _Direccion ("Dirección", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "FXComun.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ColorBorde;
            half _Disolver;
            half _AnchoBorde;
            float _EscalaRuido;
            float _MinY;
            float _MaxY;
            float _Direccion;
        CBUFFER_END

        // valor > 0: visible; entre 0 y _AnchoBorde: borde brillante
        float ValorDisolver(float alturaOS, float2 uv)
        {
            float h = saturate((alturaOS - _MinY) / max(_MaxY - _MinY, 1e-4));
            if (_Direccion < 0) h = 1.0 - h;
            float n = FxFbm(uv * _EscalaRuido);
            float valor = h * 0.65 + n * 0.35;
            float umbral = lerp(-0.05, 1.05, _Disolver);
            return valor - umbral;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float alturaOS : TEXCOORD3;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionHCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.alturaOS = i.positionOS.y;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float v = ValorDisolver(i.alturaOS, i.uv);
                clip(v);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 n = normalize(i.normalWS);
                Light luz = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(n, luz.direction)) * luz.shadowAttenuation;
                half tono = lerp(0.55, 1.0, smoothstep(0.05, 0.15, ndl)); // dos tonos, como la isla
                half3 ambiente = SampleSH(n) * 0.45;
                half3 color = albedo.rgb * (luz.color * tono + ambiente);

                half borde = 1.0 - smoothstep(0.0, _AnchoBorde, v);
                color += _ColorBorde.rgb * borde * step(0.001, _Disolver);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float alturaOS : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.alturaOS = i.positionOS.y;
                return o;
            }

            half frag(Varyings i) : SV_Target
            {
                clip(ValorDisolver(i.alturaOS, i.uv));
                return 0;
            }
            ENDHLSL
        }
    }
}
