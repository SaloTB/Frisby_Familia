using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// Acciones del pato jugable:
//   Espacio -> salta y al caer salpica.
//   E       -> "slash": gira sobre sí mismo y lanza agua en espiral.
// No mueve el transform directamente: le pasa a PatoFlotante una altura y un giro extra
// que se suman al flote. La espuma en el agua se hace emitiendo halos grandes en el
// sistema "Halos" del pato (capa Ondas), que la Camara Ondas pinta en el agua.
[RequireComponent(typeof(PatoFlotante))]
[DefaultExecutionOrder(-10)] // antes que PatoFlotante, para que use la altura y el giro de este frame
public class PatoHabilidades : MonoBehaviour
{
    [Header("Salto (Espacio)")]
    [Tooltip("Altura máxima del salto (metros).")]
    public float alturaSalto = 1.5f;
    [Tooltip("Gravedad del salto (m/s²). Más alto = salto más rápido y con más peso.")]
    public float gravedad = 22f;
    [Tooltip("Gotas que salen al despegar y al caer.")]
    public int gotasSalpicadura = 30;

    [Header("Giro (E)")]
    [Tooltip("Cuánto dura el giro (segundos).")]
    public float duracionSlash = 0.45f;
    [Tooltip("Vueltas completas por slash.")]
    public int vueltas = 1;
    [Tooltip("Tiempo de espera entre slashes (segundos), contado desde que termina el giro.")]
    public float esperaSlash = 0.15f;
    [Tooltip("Gotas por segundo mientras gira (se reparten entre los dos chorros).")]
    public float gotasPorSegundo = 220f;
    [Tooltip("Distancia desde el centro del pato desde la que sale el agua (metros).")]
    public float radioChorro = 0.55f;
    [Tooltip("Velocidad del agua en la dirección del giro (m/s).")]
    public float velocidadTangencial = 6f;
    [Tooltip("Velocidad del agua hacia afuera (m/s).")]
    public float velocidadRadial = 2.5f;
    [Tooltip("Velocidad del agua hacia arriba (m/s).")]
    public float velocidadSubida = 2.2f;

    [Header("Espuma en el agua")]
    [Tooltip("Sistema de halos del pato (capa Ondas). Si está vacío busca el hijo \"Halos\".")]
    public ParticleSystem halos;
    public float tamanoHaloSlash = 6f;
    public float tamanoHaloAterrizaje = 4.5f;

    [Header("Gotas")]
    [Tooltip("Material de las gotas. Si está vacío se crea uno con el shader de partículas de URP.")]
    public Material materialGotas;
    public Color colorGotas = new Color(0.62f, 0.88f, 1f, 0.95f);

    PatoFlotante flotar;
    ParticleSystem gotas;
    float velY;
    bool enAire;
    float tiempoSlash = -1f, finSlash = -999f;
    Vector3 dirSlash;
    float acumulado;
    bool emisionHalosOriginal = true;

    public bool EnAire => enAire;
    public bool Girando => tiempoSlash >= 0f;

    bool bloqueado;
    // Lo activan los efectos de la demo: corta el salto/slash en curso, ignora Espacio y E
    // y deja la altura y el giro del pato libres para que los anime el efecto.
    public bool Bloqueado
    {
        get => bloqueado;
        set
        {
            if (bloqueado == value) return;
            bloqueado = value;
            velY = 0f; enAire = false; tiempoSlash = -1f;
            if (flotar != null) { flotar.alturaExtra = 0f; flotar.giroExtra = 0f; }
            PonerEmisionHalos(emisionHalosOriginal);
        }
    }

    // Espuma en el agua bajo el pato (anillo del tamaño dado). La usan también los efectos.
    public void EmitirEspuma(float tamano) => EmitirHalo(tamano);

    // Activa o apaga la estela de espuma que deja el pato al nadar.
    public void EstelaEspuma(bool activa) => PonerEmisionHalos(activa && emisionHalosOriginal);

    void OnEnable()
    {
        flotar = GetComponent<PatoFlotante>();
        if (halos == null)
        {
            var h = transform.Find("Halos");
            if (h != null) halos = h.GetComponent<ParticleSystem>();
        }
        if (halos != null) emisionHalosOriginal = halos.emission.enabled;
        if (gotas == null) gotas = CrearGotas();
        velY = 0f; enAire = false; tiempoSlash = -1f;
    }

    void OnDisable()
    {
        if (flotar != null) { flotar.alturaExtra = 0f; flotar.giroExtra = 0f; }
        PonerEmisionHalos(emisionHalosOriginal);
    }

    void OnDestroy()
    {
        if (gotas != null) Destroy(gotas.gameObject);
    }

    void Update()
    {
        if (bloqueado) return;
        float dt = Time.deltaTime;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.spaceKey.wasPressedThisFrame && !enAire) Saltar();
            if (kb.eKey.wasPressedThisFrame && !Girando && Time.time >= finSlash + esperaSlash) EmpezarSlash();
        }
        ActualizarSalto(dt);
        ActualizarSlash(dt);
    }

    // ─── Salto ────────────────────────────────────────────────────────────────

    void Saltar()
    {
        velY = Mathf.Sqrt(2f * gravedad * Mathf.Max(alturaSalto, 0.01f));
        enAire = true;
        PonerEmisionHalos(false); // sin estela de espuma mientras vuela
        Salpicar(gotasSalpicadura / 2, 2.5f);
        EmitirHalo(tamanoHaloAterrizaje * 0.6f);
    }

    void ActualizarSalto(float dt)
    {
        if (!enAire) { flotar.alturaExtra = 0f; return; }
        velY -= gravedad * dt;
        flotar.alturaExtra += velY * dt;
        if (flotar.alturaExtra <= 0f && velY < 0f)
        {
            float fuerza = Mathf.Clamp01(-velY / Mathf.Sqrt(2f * gravedad * Mathf.Max(alturaSalto, 0.01f)));
            flotar.alturaExtra = 0f;
            velY = 0f;
            enAire = false;
            PonerEmisionHalos(emisionHalosOriginal);
            Salpicar(gotasSalpicadura, 2.5f + 2f * fuerza);
            EmitirHalo(tamanoHaloAterrizaje);
        }
    }

    // ─── Slash ────────────────────────────────────────────────────────────────

    void EmpezarSlash()
    {
        tiempoSlash = 0f;
        acumulado = 0f;
        dirSlash = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (dirSlash.sqrMagnitude < 1e-4f) dirSlash = Vector3.forward;
        dirSlash.Normalize();
        if (!enAire) EmitirHalo(tamanoHaloSlash);
    }

    void ActualizarSlash(float dt)
    {
        if (!Girando) return;
        tiempoSlash += dt;
        float t = Mathf.Clamp01(tiempoSlash / Mathf.Max(duracionSlash, 0.01f));
        float suave = 1f - (1f - t) * (1f - t); // arranca rápido y frena al final
        float angulo = 360f * Mathf.Max(vueltas, 1) * suave;
        flotar.giroExtra = angulo;

        // dos chorros opuestos que giran con el pato
        acumulado += gotasPorSegundo * dt * (1f - 0.6f * t);
        int n = Mathf.FloorToInt(acumulado);
        acumulado -= n;
        Vector3 centro = transform.position;
        for (int i = 0; i < n; i++)
        {
            float a = angulo + (i % 2 == 0 ? 0f : 180f) + Random.Range(-12f, 12f);
            Vector3 dir = Quaternion.Euler(0f, a, 0f) * dirSlash;
            Vector3 tangente = Vector3.Cross(Vector3.up, dir); // sentido del giro
            Vector3 vel = tangente * velocidadTangencial * Random.Range(0.75f, 1.15f)
                        + dir * velocidadRadial * Random.Range(0.6f, 1.2f)
                        + Vector3.up * velocidadSubida * Random.Range(0.6f, 1.3f);
            EmitirGota(centro + dir * radioChorro + Vector3.up * Random.Range(0.05f, 0.3f), vel,
                       Random.Range(0.1f, 0.22f), Random.Range(0.45f, 0.8f));
        }

        if (t >= 1f)
        {
            flotar.giroExtra = 0f; // 360° = 0°, queda mirando igual que antes
            tiempoSlash = -1f;
            finSlash = Time.time;
        }
    }

    // ─── Agua ─────────────────────────────────────────────────────────────────

    void Salpicar(int cantidad, float fuerza)
    {
        Vector3 centro = transform.position;
        for (int i = 0; i < cantidad; i++)
        {
            Vector2 c = Random.insideUnitCircle.normalized;
            Vector3 dir = new Vector3(c.x, 0f, c.y);
            Vector3 vel = dir * fuerza * Random.Range(0.4f, 0.9f) + Vector3.up * fuerza * Random.Range(0.8f, 1.4f);
            EmitirGota(centro + dir * radioChorro * 0.6f, vel, Random.Range(0.08f, 0.2f), Random.Range(0.4f, 0.7f));
        }
    }

    void EmitirGota(Vector3 pos, Vector3 vel, float tamano, float vida)
    {
        if (gotas == null) return;
        var p = new ParticleSystem.EmitParams
        {
            position = pos,
            velocity = vel,
            startSize = tamano,
            startLifetime = vida,
            applyShapeToPosition = false
        };
        gotas.Emit(p, 1);
    }

    void EmitirHalo(float tamano)
    {
        if (halos == null) return;
        float altura = flotar != null ? flotar.alturaExtra : 0f;
        var p = new ParticleSystem.EmitParams
        {
            position = new Vector3(transform.position.x, halos.transform.position.y - altura, transform.position.z),
            startSize = tamano,
            applyShapeToPosition = false
        };
        halos.Emit(p, 1);
    }

    void PonerEmisionHalos(bool activa)
    {
        if (halos == null) return;
        var e = halos.emission;
        e.enabled = activa;
    }

    // Sistema de partículas para las gotas, creado en tiempo de ejecución (no queda en la escena).
    ParticleSystem CrearGotas()
    {
        var go = new GameObject(name + " - Gotas") { hideFlags = HideFlags.DontSave };
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.gravityModifier = 1.6f;
        main.maxParticles = 1500;
        main.startColor = colorGotas;

        var em = ps.emission; em.enabled = false;
        var shape = ps.shape; shape.enabled = false;

        var tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0.25f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        // estelas cortas: dan la sensación de corte (slash) al girar
        var tr = ps.trails;
        tr.enabled = true;
        tr.ratio = 0.6f;
        tr.lifetime = 0.18f;
        tr.minVertexDistance = 0.05f;
        tr.dieWithParticles = true;
        tr.inheritParticleColor = true;
        tr.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(1f, 0f)));

        var mat = materialGotas != null ? materialGotas : CrearMaterial();
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = mat;
        r.trailMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;

        ps.Play();
        return ps;
    }

    static Material CrearMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var m = new Material(shader) { name = "Gotas Agua (auto)", hideFlags = HideFlags.DontSave };
        var tex = TexturaGota();
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        m.mainTexture = tex;
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        return m;
    }

    // Círculo suave con un brillo, para que cada gota se vea redonda.
    static Texture2D TexturaGota()
    {
        const int N = 64;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((1f - d) * 3f);
                float brillo = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(dx, dy), new Vector2(-0.3f, 0.3f)) * 2.5f);
                float v = Mathf.Lerp(0.85f, 1f, brillo);
                px[y * N + x] = new Color(v, v, v, a);
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }
}
