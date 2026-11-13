using UnityEngine;
using UnityEngine.Rendering;

// Ayudas para construir las piezas de los efectos en tiempo de ejecución:
// sistemas de partículas que se emiten a mano, mallas simples (anillo, cilindro, quad)
// y rayos con LineRenderer. Así cada efecto controla exactamente cuándo y dónde sale cada cosa.
public static class FXUtil
{
    // Sistema de partículas sin emisión propia: las partículas se crean con Emitir().
    public static ParticleSystem CrearParticulas(Transform padre, string nombre, Material material,
        int maximo, float gravedad, bool estirado = false, float encoger = 0.2f)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = maximo;
        main.gravityModifier = gravedad;
        main.startSpeed = 0f;
        main.startLifetime = 1f;

        var em = ps.emission; em.enabled = false;
        var shape = ps.shape; shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, encoger)));

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (estirado)
        {
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.05f;
            r.lengthScale = 1.5f;
        }
        ps.Play();
        return ps;
    }

    public static void Emitir(ParticleSystem ps, Vector3 posicion, Vector3 velocidad, float tamano, float vida, Color color)
    {
        if (ps == null) return;
        var p = new ParticleSystem.EmitParams
        {
            position = posicion,
            velocity = velocidad,
            startSize = tamano,
            startLifetime = vida,
            startColor = color,
            applyShapeToPosition = false
        };
        ps.Emit(p, 1);
    }

    public static GameObject CrearMalla(Transform padre, string nombre, Mesh malla, Material material)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        go.SetActive(false);
        return go;
    }

    // Anillo plano en XZ. uv.x da la vuelta (0..1), uv.y va del borde interior (0) al exterior (1).
    public static Mesh MallaAnillo(int segmentos, float radioInterior, float radioExterior)
    {
        var v = new Vector3[(segmentos + 1) * 2];
        var uv = new Vector2[v.Length];
        var tri = new int[segmentos * 6];
        for (int i = 0; i <= segmentos; i++)
        {
            float a = i / (float)segmentos * Mathf.PI * 2f;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            v[i * 2] = d * radioInterior;
            v[i * 2 + 1] = d * radioExterior;
            uv[i * 2] = new Vector2(i / (float)segmentos, 0f);
            uv[i * 2 + 1] = new Vector2(i / (float)segmentos, 1f);
            if (i == segmentos) continue;
            int k = i * 6, b = i * 2;
            tri[k] = b; tri[k + 1] = b + 1; tri[k + 2] = b + 3;
            tri[k + 3] = b; tri[k + 4] = b + 3; tri[k + 5] = b + 2;
        }
        return Armar("Anillo FX", v, uv, tri);
    }

    // Cilindro abierto (sin tapas) de alto 1 sobre el eje Y. uv.y = altura (0 abajo, 1 arriba).
    public static Mesh MallaCilindro(int segmentos, int pisos, float radioAbajo, float radioArriba)
    {
        int fila = segmentos + 1;
        var v = new Vector3[fila * (pisos + 1)];
        var uv = new Vector2[v.Length];
        var tri = new int[segmentos * pisos * 6];
        int t = 0;
        for (int y = 0; y <= pisos; y++)
        {
            float h = y / (float)pisos;
            float r = Mathf.Lerp(radioAbajo, radioArriba, h);
            for (int i = 0; i <= segmentos; i++)
            {
                float a = i / (float)segmentos * Mathf.PI * 2f;
                v[y * fila + i] = new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a) * r);
                uv[y * fila + i] = new Vector2(i / (float)segmentos, h);
                if (y == pisos || i == segmentos) continue;
                int b = y * fila + i;
                tri[t++] = b; tri[t++] = b + fila; tri[t++] = b + fila + 1;
                tri[t++] = b; tri[t++] = b + fila + 1; tri[t++] = b + 1;
            }
        }
        return Armar("Cilindro FX", v, uv, tri);
    }

    // Quad horizontal centrado de lado 1.
    public static Mesh MallaQuad()
    {
        var v = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f) };
        var uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        var tri = new[] { 0, 2, 1, 2, 3, 1 };
        return Armar("Quad FX", v, uv, tri);
    }

    static Mesh Armar(string nombre, Vector3[] v, Vector2[] uv, int[] tri)
    {
        var m = new Mesh { name = nombre, hideFlags = HideFlags.DontSave };
        m.vertices = v;
        m.uv = uv;
        m.triangles = tri;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    public static LineRenderer CrearRayo(Transform padre, string nombre, Material material, float ancho, int puntos)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = material;
        lr.useWorldSpace = true;
        lr.positionCount = puntos;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.widthMultiplier = ancho;
        lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.6f));
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.numCapVertices = 2;
        lr.enabled = false;
        return lr;
    }

    // Llena un LineRenderer con un trazo quebrado de a a b (desvío máximo en metros).
    public static void TrazarRayo(LineRenderer lr, Vector3 a, Vector3 b, float desvio)
    {
        int n = lr.positionCount;
        var eje = b - a;
        var lado1 = Vector3.Cross(eje, Vector3.up);
        if (lado1.sqrMagnitude < 1e-4f) lado1 = Vector3.right;
        lado1.Normalize();
        var lado2 = Vector3.Cross(eje.normalized, lado1);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            float forma = Mathf.Sin(t * Mathf.PI); // fijo en los extremos
            var off = (lado1 * Random.Range(-1f, 1f) + lado2 * Random.Range(-1f, 1f)) * desvio * forma;
            lr.SetPosition(i, a + eje * t + off);
        }
    }

    public static void Alfa(LineRenderer lr, float alfa)
    {
        var c = new Color(1f, 1f, 1f, Mathf.Clamp01(alfa));
        lr.startColor = c;
        lr.endColor = c;
    }

    // Curvas de suavizado.
    public static float SalidaSuave(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }
    public static float EntradaSalida(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    public static float Rango(float t, float desde, float hasta) => Mathf.Clamp01((t - desde) / Mathf.Max(hasta - desde, 1e-4f));
    // Rebote elástico que arranca en 1 y se calma hacia 0.
    public static float Elastico(float t, float frecuencia = 14f, float amortiguacion = 5f) =>
        t < 0f ? 0f : Mathf.Exp(-amortiguacion * t) * Mathf.Cos(frecuencia * t);
}
