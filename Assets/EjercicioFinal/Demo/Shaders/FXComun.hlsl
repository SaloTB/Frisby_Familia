#ifndef FX_COMUN_INCLUDED
#define FX_COMUN_INCLUDED

// Funciones compartidas por los shaders de efectos de la demo (ruido procedural, color).
// Todo es procedural: los efectos no dependen de texturas.

float FxHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// Ruido de valor suave (0..1).
float FxRuido(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float a = FxHash21(i);
    float b = FxHash21(i + float2(1, 0));
    float c = FxHash21(i + float2(0, 1));
    float d = FxHash21(i + float2(1, 1));
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Ruido fractal de 4 octavas (0..~1).
float FxFbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        v += a * FxRuido(p);
        p = p * 2.03 + 17.1;
        a *= 0.5;
    }
    return v;
}

// Tono (0..1) a color RGB saturado.
float3 FxTono(float h)
{
    return saturate(abs(frac(h + float3(0.0, 2.0, 1.0) / 3.0) * 6.0 - 3.0) - 1.0);
}

float2 FxRotar(float2 p, float a)
{
    float s = sin(a), c = cos(a);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

#endif
