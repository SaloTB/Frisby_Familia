using UnityEngine;

// EFECTO 3 — NOVA DE CALABAZA (fuego azul)
// Animación del pato: absorbe chispas que vienen hacia él mientras se infla, se echa hacia
// atrás y tiembla (0–1.1 s); se aplasta y suelta la energía: salta girando, sale una onda
// de fuego azul por el agua y una columna de llamas (1.1–2.3 s); vuelve a su forma con
// rebotes y quedan chispas flotando (2.3–3.8 s).
// Shaders propios: Demo/FX Fuego (onda y columna) + Demo/FX Brillo (chispas).
// Post-procesado: Viñeta azul + Ajustes de color (tinte frío, contraste, saturación).
[DefaultExecutionOrder(-20)]
public class EfectoNovaCalabaza : EfectoPato
{
    [Header("Materiales")]
    public Material materialFuego;
    public Material materialBrasas;

    [Header("Nova")]
    public float radioOnda = 8f;
    public float altoColumna = 4.5f;
    public Color colorBrasas = new Color(0.7f, 0.85f, 1f, 1f);

    const float Suelta = 1.1f;
    GameObject onda, columna;
    Material matOnda, matColumna;
    ParticleSystem brasas;
    Light luz;
    bool solto;

    void Reset() { nombre = "Nova de Calabaza"; duracion = 3.8f; }

    protected override void Preparar()
    {
        matOnda = MaterialConTiempo(materialFuego);
        matOnda.SetFloat("_EscalaX", 26f);
        matOnda.SetFloat("_EscalaY", 2f);
        matOnda.SetFloat("_Velocidad", 3.5f);
        matOnda.SetFloat("_Intensidad", 1.8f); // que el anillo se lea sobre la espuma
        matColumna = MaterialConTiempo(materialFuego);

        onda = FXUtil.CrearMalla(transform, "Onda de fuego", Registrar(FXUtil.MallaAnillo(96, 0.55f, 1f)), matOnda);
        columna = FXUtil.CrearMalla(transform, "Columna de fuego", Registrar(FXUtil.MallaCilindro(40, 8, 0.9f, 0.35f)), matColumna);

        brasas = FXUtil.CrearParticulas(transform, "Chispas", MaterialConTiempo(materialBrasas), 700, -0.08f, estirado: true, encoger: 0.1f);

        var luzGo = new GameObject("Luz azul");
        luzGo.transform.SetParent(transform, false);
        luz = luzGo.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.3f, 0.5f, 1f);
        luz.range = 11f;
        luz.enabled = false;
    }

    protected override void AlIniciar()
    {
        solto = false;
        onda.SetActive(false);
        columna.SetActive(false);
        luz.enabled = true;
    }

    protected override void AlActualizar(float t, float dt)
    {
        Vector3 centro = pato.position + Vector3.up * 0.3f;
        luz.transform.position = centro + Vector3.up * 1.5f;

        // ── carga: se infla absorbiendo chispas ──────────────────────────────
        if (t < Suelta)
        {
            float k = FXUtil.EntradaSalida(t / Suelta);
            float tiembla = Mathf.Sin(t * 45f) * 0.05f * k;
            flotar.escalaExtra = Vector3.one * (0.32f * k + tiembla);
            flotar.inclinacionExtra = new Vector3(-18f * k, 0f, Mathf.Sin(t * 30f) * 4f * k);
            int n = Mathf.RoundToInt(dt * 160f * (0.3f + k));
            for (int i = 0; i < n; i++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.7f;
                float r = Random.Range(2.5f, 3.5f);
                float vida = Random.Range(0.45f, 0.6f);
                FXUtil.Emitir(brasas, centro + d * r, -d * r / vida, Random.Range(0.08f, 0.16f), vida, colorBrasas);
            }
            PesoVolumen(0.45f * k);
            luz.intensity = 3f * k;
            return;
        }

        // ── suelta: onda, columna y estallido ────────────────────────────────
        if (!solto)
        {
            solto = true;
            onda.SetActive(true);
            columna.SetActive(true);
            Espuma(9f);
            for (int i = 0; i < 160; i++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 1.2f + 0.2f;
                FXUtil.Emitir(brasas, centro, d * Random.Range(3f, 9f), Random.Range(0.08f, 0.2f), Random.Range(0.6f, 1.4f), colorBrasas);
            }
        }

        float s = t - Suelta;

        // pato: aplastón, salto con giro, rebotes hasta su forma
        float rebote = FXUtil.Elastico(s, 12f, 3.5f);
        flotar.escalaExtra = new Vector3(0.35f * rebote, -0.45f * rebote, 0.35f * rebote);
        flotar.alturaExtra = 1.1f * Mathf.Sin(Mathf.PI * FXUtil.Rango(s, 0.05f, 0.8f));
        flotar.giroExtra = 360f * FXUtil.SalidaSuave(FXUtil.Rango(s, 0.05f, 0.9f));
        flotar.inclinacionExtra = new Vector3(-18f * Mathf.Exp(-s * 6f), 0f, 0f);

        // onda: crece rápido por el agua y se deshace
        float crece = FXUtil.SalidaSuave(s / 0.9f);
        float aguaY = flotar.agua != null ? flotar.agua.position.y + flotar.alturaSobreAgua + 0.05f : pato.position.y;
        onda.transform.position = new Vector3(pato.position.x, aguaY, pato.position.z);
        onda.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, radioOnda, crece);
        matOnda.SetFloat("_Disolver", FXUtil.Rango(s, 0.25f, 1.2f));
        onda.SetActive(s < 1.25f);

        // columna: sube de golpe y se apaga
        columna.transform.position = new Vector3(pato.position.x, aguaY - 0.1f, pato.position.z);
        float alto = altoColumna * FXUtil.SalidaSuave(s / 0.18f);
        float ancho = Mathf.Lerp(1.4f, 0.8f, FXUtil.Rango(s, 0f, 1f));
        columna.transform.localScale = new Vector3(ancho, Mathf.Max(alto, 0.01f), ancho);
        matColumna.SetFloat("_Disolver", FXUtil.Rango(s, 0.3f, 1.1f));
        columna.SetActive(s < 1.15f);

        // chispas que siguen saliendo un rato
        int extra = Mathf.RoundToInt(dt * 60f * (1f - FXUtil.Rango(s, 0f, 1.6f)));
        for (int i = 0; i < extra; i++)
        {
            var d = Random.insideUnitCircle * 1.2f;
            FXUtil.Emitir(brasas, centro + new Vector3(d.x, 0f, d.y), new Vector3(d.x, Random.Range(1.5f, 3.5f), d.y),
                Random.Range(0.06f, 0.14f), Random.Range(0.8f, 1.6f), colorBrasas);
        }

        // post-procesado y luz: golpe frío que se apaga
        float golpe = Mathf.Exp(-s * 1.6f);
        PesoVolumen(golpe);
        luz.intensity = 12f * Mathf.Exp(-s * 3f) * Random.Range(0.8f, 1f) + 1.5f * golpe;
    }

    protected override void AlTerminar()
    {
        onda.SetActive(false);
        columna.SetActive(false);
        luz.enabled = false;
    }
}
