using UnityEngine;

// Caja de objeto (estilo carreras) con un pescado adentro. Gira y flota; cuando el pato
// la toca se rompe en un estallido de luces, suma un pescado al contador y reaparece
// después de unos segundos.
public class CajaPescado : MonoBehaviour
{
    public Transform cubo;
    public Transform pescado;
    public Material materialEstallido;

    [Header("Recoger")]
    [Tooltip("Distancia en el plano (metros) a la que el pato la recoge.")]
    public float radioRecoger = 1.3f;
    [Tooltip("Diferencia de altura máxima para recogerla (el pato puede saltar hacia ella).")]
    public float altoRecoger = 2.2f;
    public float tiempoReaparecer = 5f;

    [Header("Animación")]
    public float velocidadGiro = 70f;
    public float alturaFlote = 0.15f;

    Transform objetivo;
    Vector3 posicionBase;
    float fase, escala = 1f, reaparecer;
    bool activa = true;
    ParticleSystem estallido;

    void Start()
    {
        var pato = FindFirstObjectByType<PatoJugador>();
        if (pato != null) objetivo = pato.transform;
        posicionBase = transform.position;
        fase = Random.Range(0f, 10f);
        if (materialEstallido != null)
            estallido = FXUtil.CrearParticulas(transform, "Estallido", materialEstallido, 120, 0.8f, estirado: false, encoger: 0f);
    }

    void Update()
    {
        float t = Time.time + fase;
        transform.position = posicionBase + Vector3.up * Mathf.Sin(t * 2f) * alturaFlote;
        if (cubo != null) cubo.localRotation = Quaternion.Euler(t * velocidadGiro * 0.6f, t * velocidadGiro, t * velocidadGiro * 0.35f);
        if (pescado != null)
        {
            pescado.localRotation = Quaternion.Euler(0f, -t * velocidadGiro * 1.5f, Mathf.Sin(t * 6f) * 12f);
            pescado.localPosition = Vector3.up * Mathf.Sin(t * 3f) * 0.05f;
        }

        if (activa)
        {
            if (objetivo != null)
            {
                Vector3 d = objetivo.position - transform.position;
                float plano = new Vector2(d.x, d.z).magnitude;
                if (plano < radioRecoger && Mathf.Abs(d.y) < altoRecoger) Recoger();
            }
        }
        else
        {
            reaparecer -= Time.deltaTime;
            if (reaparecer <= 0f) activa = true;
        }

        // aparece con un rebote, desaparece de golpe
        float meta = activa ? 1f : 0f;
        escala = activa ? Mathf.MoveTowards(escala, meta, Time.deltaTime * 2.5f) : Mathf.MoveTowards(escala, meta, Time.deltaTime * 10f);
        float rebote = activa && escala < 1f ? Mathf.Sin(escala * Mathf.PI) * 0.25f : 0f;
        float s = escala + rebote;
        if (cubo != null) cubo.localScale = Vector3.one * s;
        if (pescado != null) pescado.localScale = Vector3.one * s;
    }

    void Recoger()
    {
        activa = false;
        reaparecer = tiempoReaparecer;
        if (GestorDemo.Instancia != null) GestorDemo.Instancia.SumarPescado();

        if (estallido != null)
        {
            for (int i = 0; i < 45; i++)
            {
                var d = Random.onUnitSphere;
                var color = Color.HSVToRGB(Random.value, 0.55f, 1f);
                FXUtil.Emitir(estallido, transform.position + d * 0.3f, d * Random.Range(2f, 5f) + Vector3.up * 1.5f,
                    Random.Range(0.12f, 0.28f), Random.Range(0.5f, 0.9f), color);
            }
        }
    }
}
