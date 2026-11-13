using UnityEngine;

// EFECTO 1 — BURBUJA
// Mientras se mantiene la tecla 1, el pato sopla una burbuja en el pico que va creciendo:
// infla los cachetes, se echa hacia atrás y, cerca del tamaño máximo, tiembla. Al soltar la
// tecla (o al llegar al máximo, sola) la lanza hacia adelante con un culatazo; la burbuja
// vuela tambaleándose y revienta en gotas, dejando espuma en el agua.
// Shader propio: Demo/FX Burbuja (película iridiscente) + Demo/FX Brillo (gotas y aire).
// Post-procesado: Aberración cromática, que crece con la burbuja y da un golpe al reventar.
[DefaultExecutionOrder(-20)]
public class EfectoBurbuja : EfectoPato
{
    [Header("Materiales")]
    public Material materialBurbuja;
    public Material materialGotas;

    [Header("Burbuja")]
    [Tooltip("Diámetro inicial y máximo (metros).")]
    public float tamanoMinimo = 0.25f;
    public float tamanoMaximo = 2.2f;
    [Tooltip("Segundos sosteniendo la tecla para llegar al tamaño máximo.")]
    public float tiempoCarga = 2.5f;
    [Tooltip("Velocidad de una burbuja pequeña; las grandes van más lento.")]
    public float velocidad = 9f;
    [Tooltip("Segundos de vuelo antes de reventar (las grandes duran más).")]
    public float vidaVuelo = 1.6f;
    public Color colorGotas = new Color(0.85f, 0.95f, 1f, 1f);

    enum Fase { Cargando, Volando, Reventada }
    Fase fase;
    float carga, tamano, inicioFase;
    Vector3 posBurbuja, velBurbuja, frente;
    GameObject burbuja;
    Material matBurbuja;
    ParticleSystem gotas;
    Vector3 picoLocal;

    void Reset() { nombre = "Burbuja"; duracion = 30f; }

    protected override void Preparar()
    {
        matBurbuja = MaterialConTiempo(materialBurbuja);
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var esfera = tmp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tmp);
        burbuja = FXUtil.CrearMalla(transform, "Burbuja", esfera, matBurbuja);
        gotas = FXUtil.CrearParticulas(transform, "Gotas", MaterialConTiempo(materialGotas), 600, 0.9f, estirado: true, encoger: 0.3f);
        picoLocal = BuscarPico();
    }

    // Punta del pico: el vértice de la mitad de arriba que está más adelante.
    Vector3 BuscarPico()
    {
        var mf = pato.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return Vector3.zero;
        var v = mf.sharedMesh.vertices;
        var b = mf.sharedMesh.bounds;
        var adelante = Quaternion.Euler(0f, -jugador.giroModelo, 0f) * Vector3.forward;
        Vector3 mejor = b.center; float max = float.MinValue;
        foreach (var p in v)
        {
            if (p.y < b.center.y) continue;
            float d = Vector3.Dot(p - b.center, adelante);
            if (d > max) { max = d; mejor = p; }
        }
        return mejor;
    }

    Vector3 Pico => pato.TransformPoint(picoLocal);

    Vector3 Frente()
    {
        var f = Vector3.ProjectOnPlane(pato.rotation * Quaternion.Euler(0f, -jugador.giroModelo, 0f) * Vector3.forward, Vector3.up);
        return f.sqrMagnitude < 1e-4f ? Vector3.forward : f.normalized;
    }

    protected override void AlIniciar()
    {
        fase = Fase.Cargando;
        carga = 0f;
        inicioFase = 0f;
        tamano = tamanoMinimo;
        matBurbuja.SetFloat("_Alfa", 1f);
        burbuja.SetActive(true);
        frente = Frente();
        ColocarEnPico();
    }

    public override void SoltarTecla()
    {
        if (Reproduciendo && !Pausado && fase == Fase.Cargando) Lanzar();
    }

    protected override void AlActualizar(float t, float dt)
    {
        switch (fase)
        {
            case Fase.Cargando: Cargar(t, dt); break;
            case Fase.Volando: Volar(t, dt); break;
            case Fase.Reventada: Esperar(t); break;
        }
    }

    // ── soplar: crece mientras se sostiene la tecla ──────────────────────────
    void Cargar(float t, float dt)
    {
        carga = Mathf.Clamp01(carga + dt / Mathf.Max(tiempoCarga, 0.1f));
        float k = FXUtil.SalidaSuave(carga);
        tamano = Mathf.Lerp(tamanoMinimo, tamanoMaximo, k);

        float tiembla = carga > 0.75f ? (carga - 0.75f) * 4f : 0f;
        flotar.escalaExtra = new Vector3(0.14f * k, -0.06f * k, 0.14f * k) + Vector3.one * Mathf.Sin(t * 40f) * 0.03f * tiembla;
        flotar.inclinacionExtra = new Vector3(-14f * k, 0f, 0f) + Random.insideUnitSphere * 4f * tiembla;
        flotar.giroExtra = Mathf.Sin(t * 50f) * 4f * tiembla;

        matBurbuja.SetFloat("_Tambaleo", 0.02f + 0.08f * tiembla);
        frente = Frente();
        ColocarEnPico();

        // aire que entra al pico
        int n = Mathf.RoundToInt(dt * 40f);
        for (int i = 0; i < n; i++)
        {
            var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.5f;
            var desde = Pico + d * 1.6f;
            FXUtil.Emitir(gotas, desde, (Pico - desde) / 0.45f, Random.Range(0.03f, 0.06f), 0.45f, colorGotas);
        }

        PesoVolumen(0.4f * k);
        if (carga >= 1f) Lanzar(); // tamaño máximo: se tira sola
    }

    void ColocarEnPico()
    {
        posBurbuja = Pico + frente * tamano * 0.42f + Vector3.up * tamano * 0.08f;
        burbuja.transform.position = posBurbuja;
        burbuja.transform.localScale = Vector3.one * tamano;
    }

    // ── lanzar y volar ───────────────────────────────────────────────────────
    void Lanzar()
    {
        fase = Fase.Volando;
        inicioFase = tiempo;
        float grande = Mathf.InverseLerp(tamanoMinimo, tamanoMaximo, tamano);
        velBurbuja = frente * velocidad * Mathf.Lerp(1f, 0.45f, grande) + Vector3.up * 0.6f;
        matBurbuja.SetFloat("_Tambaleo", 0.05f);
        for (int i = 0; i < 12; i++)
            FXUtil.Emitir(gotas, Pico, (frente + Random.insideUnitSphere * 0.5f) * 3f, Random.Range(0.04f, 0.08f), 0.4f, colorGotas);
    }

    void Volar(float t, float dt)
    {
        float s = t - inicioFase;
        float grande = Mathf.InverseLerp(tamanoMinimo, tamanoMaximo, tamano);

        // culatazo del pato: se va hacia atrás y vuelve con rebote
        float culatazo = FXUtil.Elastico(s, 11f, 4.5f);
        flotar.inclinacionExtra = new Vector3(22f * culatazo * (0.4f + grande), 0f, 0f);
        flotar.escalaExtra = new Vector3(-0.12f, 0.15f, -0.12f) * culatazo;
        flotar.giroExtra = 0f;

        // la burbuja frena un poco, sube y baja y se deforma
        velBurbuja = Vector3.Lerp(velBurbuja, velBurbuja.normalized * velocidad * 0.25f, dt * 0.8f);
        posBurbuja += velBurbuja * dt;
        burbuja.transform.position = posBurbuja + Vector3.up * Mathf.Sin(s * 5f) * 0.15f;
        float deforma = Mathf.Sin(s * 9f) * 0.06f;
        burbuja.transform.localScale = new Vector3(tamano * (1f + deforma), tamano * (1f - deforma), tamano * (1f + deforma));

        PesoVolumen(0.4f * (1f - FXUtil.Rango(s, 0f, 0.5f)) + 0.15f);
        if (s >= vidaVuelo * Mathf.Lerp(0.8f, 1.4f, grande)) Reventar();
    }

    void Reventar()
    {
        fase = Fase.Reventada;
        inicioFase = tiempo;
        burbuja.SetActive(false);
        Vector3 c = burbuja.transform.position;
        int cantidad = Mathf.RoundToInt(Mathf.Lerp(30f, 140f, Mathf.InverseLerp(tamanoMinimo, tamanoMaximo, tamano)));
        for (int i = 0; i < cantidad; i++)
        {
            var d = Random.onUnitSphere;
            FXUtil.Emitir(gotas, c + d * tamano * 0.5f, d * Random.Range(1.5f, 4f) + Vector3.up * 1.2f,
                Random.Range(0.05f, 0.14f), Random.Range(0.5f, 1f), colorGotas);
        }
        // espuma donde cae (la burbuja se lanza a ras del agua)
        if (habilidades != null && habilidades.halos != null)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = new Vector3(c.x, habilidades.halos.transform.position.y, c.z),
                startSize = 2f + tamano * 2.5f,
                applyShapeToPosition = false
            };
            habilidades.halos.Emit(p, 1);
        }
    }

    void Esperar(float t)
    {
        float s = t - inicioFase;
        float golpe = Mathf.Exp(-s * 5f);
        PesoVolumen(golpe);
        flotar.inclinacionExtra *= 0.9f;
        flotar.escalaExtra *= 0.9f;
        if (s > 1f) Detener();
    }

    protected override void AlTerminar()
    {
        burbuja.SetActive(false);
    }
}
