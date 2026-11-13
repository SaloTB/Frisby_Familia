// Partícula de luz aditiva: punto suave con núcleo blanco. Color HDR × color de la partícula.
// Lo usan las chispas del rayo, las motas mágicas, las brasas y los estallidos de las cajas.
Shader "Demo/FX Brillo"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
        _Dureza ("Caída del brillo", Range(0.5, 8)) = 2.5
        _Nucleo ("Núcleo blanco", Range(0, 2)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FXBrillo"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Dureza;
                half _Nucleo;
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
                float d = length(i.uv * 2.0 - 1.0);
                half brillo = pow(saturate(1.0 - d), _Dureza);
                half nucleo = smoothstep(0.35, 0.0, d) * _Nucleo;
                half3 c = _Color.rgb * i.color.rgb * brillo + nucleo * i.color.rgb;
                return half4(c * i.color.a * _Color.a, 0);
            }
            ENDHLSL
        }
    }
}
