using UnityEngine;

// EFECTO 2 — TELETRANSPORTE ARCANO
// Animación del pato: aparece un círculo mágico bajo él y empieza a girar cada vez más rápido
// mientras se eleva (0–0.7 s); se disuelve de abajo hacia arriba con un borde brillante
// (0.7–1.5 s); reaparece en otro punto del agua materializándose de arriba hacia abajo
// mientras frena el giro y baja (1.5–2.5 s); los círculos se desvanecen (2.5–4.2 s).
// Shaders propios: Demo/FX Disolver (en el pato), Demo/FX Circulo Magico y Demo/FX Brillo.
// Post-procesado: Distorsión de lente (volumen propio) que deforma la imagen en el salto.
[DefaultExecutionOrder(-20)]
public class EfectoTeletransporte : EfectoPato
{
    [Header("Materiales")]
    public Material materialDisolver;
    public Material materialCirculo;
    public Material materialMotas;

    [Header("Teletransporte")]
    public float distancia = 7f;
    public float diametroCirculo = 3.6f;
    public Color colorMotas = new Color(0.8f, 0.45f, 1f, 1f);

    const float FinGiro = 2.5f, Desaparece0 = 0.7f, Desaparece1 = 1.5f, Aparece1 = 2.5f;
    Renderer render;
    Material matPatoOriginal, matDisolver;
    GameObject circuloOrigen, circuloDestino;
    Material matOrigen, matDestino;
    ParticleSystem motas;
    Light luz;
    Vector3 origen, destino;
    bool salto, materialPuesto;

    void Reset() { nombre = "Teletransporte"; duracion = 4.2f; }

    protected override void Preparar()
    {
        render = pato.GetComponent<Renderer>();
        matPatoOriginal = render.sharedMaterial;

        matDisolver = Registrar(new Material(materialDisolver) { hideFlags = HideFlags.DontSave });
        if (matPatoOriginal != null)
        {
            if (matPatoOriginal.HasProperty("_BaseMap")) matDisolver.SetTexture("_BaseMap", matPatoOriginal.GetTexture("_BaseMap"));
            if (matPatoOriginal.HasProperty("_BaseColor")) matDisolver.SetColor("_BaseColor", matPatoOriginal.GetColor("_BaseColor"));
        }
        var mf = pato.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var b = mf.sharedMesh.bounds;
            matDisolver.SetFloat("_MinY", b.min.y);
            matDisolver.SetFloat("_MaxY", b.max.y);
        }

        var quad = Registrar(FXUtil.MallaQuad());
        matOrigen = MaterialConTiempo(materialCirculo);
        matDestino = MaterialConTiempo(materialCirculo);
        circuloOrigen = FXUtil.CrearMalla(transform, "Círculo origen", quad, matOrigen);
        circuloDestino = FXUtil.CrearMalla(transform, "Círculo destino", quad, matDestino);

        motas = FXUtil.CrearParticulas(transform, "Motas", MaterialConTiempo(materialMotas), 500, -0.15f);

        var luzGo = new GameObject("Luz arcana");
        luzGo.transform.SetParent(transform, false);
        luz = luzGo.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.75f, 0.45f, 1f);
        luz.range = 8f;
        luz.enabled = false;
    }

    protected override void AlIniciar()
    {
        salto = false;
        origen = flotar.PosicionBase;
        destino = BuscarDestino(origen);
        PonerCirculo(circuloOrigen, matOrigen, origen, 0f, 0f);
        circuloOrigen.SetActive(true);
        circuloDestino.SetActive(false);
        luz.enabled = true;
    }

    protected override void AlActualizar(float t, float dt)
    {
        float agua = AlturaAgua();

        // ── pato: giro de 3 vueltas que acelera y frena, sube y baja ─────────
        flotar.giroExtra = 1080f * FXUtil.EntradaSalida(t / FinGiro);
        flotar.alturaExtra = t < Desaparece1
            ? 0.5f * FXUtil.EntradaSalida(t / Desaparece1)
            : 0.5f * (1f - FXUtil.EntradaSalida(FXUtil.Rango(t, Desaparece1, Aparece1)));
        float latido = Mathf.Sin(t * 18f) * 0.06f * (1f - FXUtil.Rango(t, Desaparece1, Aparece1));
        flotar.escalaExtra = new Vector3(latido, -latido, latido);

        // ── disolver y reaparecer ─────────────────────────────────────────────
        if (t < Desaparece0)
        {
            PonerDisolver(false, 0f, 1f);
        }
        else if (t < Desaparece1)
        {
            PonerDisolver(true, FXUtil.Rango(t, Desaparece0, Desaparece1), 1f);
        }
        else
        {
            if (!salto) Saltar();
            float aparece = FXUtil.Rango(t, Desaparece1 + 0.1f, Aparece1);
            if (aparece < 1f) PonerDisolver(true, 1f - aparece, -1f);
            else PonerDisolver(false, 0f, 1f);
        }

        // ── círculos mágicos ─────────────────────────────────────────────────
        float apagar = 1f - FXUtil.Rango(t, 2.6f, duracion);
        PonerCirculo(circuloOrigen, matOrigen, new Vector3(origen.x, agua, origen.z),
            FXUtil.SalidaSuave(t / 0.6f), apagar * (salto ? 1f - FXUtil.Rango(t, Desaparece1, 2.4f) * 0.6f : 1f));
        if (salto)
            PonerCirculo(circuloDestino, matDestino, new Vector3(destino.x, agua, destino.z),
                FXUtil.SalidaSuave(FXUtil.Rango(t, Desaparece1 - 0.1f, Desaparece1 + 0.4f)), apagar);

        // ── motas que suben desde el círculo activo ──────────────────────────
        Vector3 desde = salto ? destino : origen;
        int cantidad = Mathf.RoundToInt(dt * 90f * apagar);
        for (int i = 0; i < cantidad; i++)
        {
            var d = Random.insideUnitCircle.normalized * diametroCirculo * 0.45f * Random.Range(0.7f, 1f);
            FXUtil.Emitir(motas, new Vector3(desde.x + d.x, agua + 0.05f, desde.z + d.y),
                new Vector3(-d.y, 0f, d.x) * 0.6f + Vector3.up * Random.Range(1f, 2.4f),
                Random.Range(0.08f, 0.18f), Random.Range(0.8f, 1.4f), colorMotas);
        }

        // ── distorsión de lente: crece al desaparecer y se suelta al reaparecer ──
        float lente = t < Desaparece1 ? FXUtil.EntradaSalida(FXUtil.Rango(t, 0.4f, Desaparece1))
                                      : 1f - FXUtil.EntradaSalida(FXUtil.Rango(t, Desaparece1, Aparece1 + 0.3f));
        PesoVolumen(lente);

        luz.transform.position = pato.position + Vector3.up * 1f;
        luz.intensity = 3f * apagar + 4f * lente;
    }

    void Saltar()
    {
        salto = true;
        habilidades?.EstelaEspuma(false); // sin rastro de espuma en el salto
        EstallidoMotas(pato.position, 60);
        flotar.MoverBase(destino, RotacionBase());
        circuloDestino.SetActive(true);
        Espuma(5f);
    }

    Quaternion RotacionBase()
    {
        // Mantiene la orientación con la que nadaba el pato (sin el giro del efecto ni el balanceo).
        var f = Vector3.ProjectOnPlane(pato.rotation * Quaternion.Euler(0f, -jugador.giroModelo, 0f) * Vector3.forward, Vector3.up);
        if (f.sqrMagnitude < 1e-4f) f = Vector3.forward;
        return Quaternion.LookRotation(f.normalized) * Quaternion.Euler(0f, jugador.giroModelo, 0f);
    }

    void EstallidoMotas(Vector3 centro, int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            var d = Random.onUnitSphere;
            d.y = Mathf.Abs(d.y);
            FXUtil.Emitir(motas, centro + d * 0.3f, d * Random.Range(1.5f, 4f) + Vector3.up,
                Random.Range(0.1f, 0.22f), Random.Range(0.6f, 1.1f), colorMotas);
        }
    }

    // Busca un punto con agua delante del pato; si no hay, prueba otras direcciones.
    Vector3 BuscarDestino(Vector3 desde)
    {
        var frente = Vector3.ProjectOnPlane(pato.rotation * Quaternion.Euler(0f, -jugador.giroModelo, 0f) * Vector3.forward, Vector3.up);
        if (frente.sqrMagnitude < 1e-4f) frente = Vector3.forward;
        frente.Normalize();
        foreach (float dist in new[] { distancia, distancia * 0.6f })
            for (int i = 0; i < 12; i++)
            {
                float ang = (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 30f;
                var p = desde + Quaternion.Euler(0f, ang, 0f) * frente * dist;
                if (jugador.PuedeNadarEn(p)) return p;
            }
        return desde;
    }

    void PonerDisolver(bool activo, float valor, float direccion)
    {
        if (activo && !materialPuesto) { render.sharedMaterial = matDisolver; materialPuesto = true; }
        if (!activo && materialPuesto) { render.sharedMaterial = matPatoOriginal; materialPuesto = false; }
        if (activo)
        {
            matDisolver.SetFloat("_Disolver", valor);
            matDisolver.SetFloat("_Direccion", direccion);
        }
    }

    void PonerCirculo(GameObject circulo, Material mat, Vector3 pos, float aparicion, float intensidad)
    {
        circulo.transform.position = pos + Vector3.up * 0.04f;
        circulo.transform.localScale = Vector3.one * diametroCirculo * Mathf.Lerp(0.6f, 1f, aparicion);
        mat.SetFloat("_Aparicion", aparicion);
        mat.SetFloat("_Intensidad", intensidad);
        circulo.SetActive(intensidad > 0.01f);
    }

    float AlturaAgua()
    {
        var aguaT = flotar.agua;
        // la base del pato queda justo sobre la superficie del agua
        return aguaT != null ? aguaT.position.y + flotar.alturaSobreAgua + 0.03f : pato.position.y;
    }

    protected override void AlTerminar()
    {
        PonerDisolver(false, 0f, 1f);
        circuloOrigen.SetActive(false);
        circuloDestino.SetActive(false);
        luz.enabled = false;
        habilidades?.EstelaEspuma(true);
    }
}
