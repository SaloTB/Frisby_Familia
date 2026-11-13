using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Base de los tres efectos de la demo. Cada efecto es una línea de tiempo de "duracion"
// segundos que anima al pato (a través de PatoFlotante) y a sus propias piezas visuales.
//
// Play/Pausa: el tiempo del efecto solo avanza cuando está reproduciéndose. En pausa se
// congelan sus partículas, el tiempo de sus shaders (_TiempoFX) y la pose del pato.
// Mientras dura, el jugador no puede mover al pato ni saltar/hacer slash.
public abstract class EfectoPato : MonoBehaviour
{
    [Header("Efecto")]
    public string nombre = "Efecto";
    [TextArea] public string descripcion;
    public float duracion = 3f;
    [Tooltip("Post-procesado propio del efecto: su peso se anima durante el efecto.")]
    public Volume volumen;

    protected PatoJugador jugador;
    protected PatoFlotante flotar;
    protected PatoHabilidades habilidades;
    protected Transform pato;
    protected float tiempo;

    public bool Reproduciendo { get; private set; }
    public bool Pausado { get; private set; }
    public float Progreso => duracion > 0f ? Mathf.Clamp01(tiempo / duracion) : 0f;

    static readonly int IdTiempo = Shader.PropertyToID("_TiempoFX");
    readonly List<Material> materialesConTiempo = new List<Material>();
    readonly List<Object> creados = new List<Object>();
    ParticleSystem[] sistemas = new ParticleSystem[0];
    bool configurado;

    public void Configurar(PatoJugador objetivo)
    {
        if (configurado) return;
        jugador = objetivo;
        pato = objetivo.transform;
        flotar = objetivo.GetComponent<PatoFlotante>();
        habilidades = objetivo.GetComponent<PatoHabilidades>();
        Preparar();
        sistemas = GetComponentsInChildren<ParticleSystem>(true);
        if (volumen != null) volumen.weight = 0f;
        configurado = true;
    }

    // Crea las piezas visuales del efecto (una sola vez).
    protected abstract void Preparar();
    protected abstract void AlIniciar();
    // t = segundos desde que empezó el efecto (no avanza en pausa).
    protected abstract void AlActualizar(float t, float dt);
    // Deja todo como estaba (el pato ya se restablece solo).
    protected abstract void AlTerminar();

    // Se llama cuando se suelta la tecla del efecto (la burbuja la usa para dispararse).
    public virtual void SoltarTecla() { }

    // Copia del material cuyo _TiempoFX lleva el reloj del efecto.
    protected Material MaterialConTiempo(Material original)
    {
        var m = new Material(original) { hideFlags = HideFlags.DontSave };
        materialesConTiempo.Add(m);
        creados.Add(m);
        return m;
    }

    protected T Registrar<T>(T obj) where T : Object { creados.Add(obj); return obj; }

    public void Reproducir()
    {
        if (!configurado) return;
        if (Reproduciendo) Detener();
        tiempo = 0f;
        Reproduciendo = true;
        Pausado = false;
        BloquearPato(true);
        RestablecerPato();
        foreach (var s in sistemas) if (s != null) s.Play(true);
        ActualizarMateriales();
        AlIniciar();
    }

    public void Pausar()
    {
        if (!Reproduciendo || Pausado) return;
        Pausado = true;
        foreach (var s in sistemas) if (s != null) s.Pause(true);
    }

    public void Reanudar()
    {
        if (!Reproduciendo || !Pausado) return;
        Pausado = false;
        foreach (var s in sistemas) if (s != null) s.Play(true);
    }

    public void Detener()
    {
        if (!Reproduciendo) return;
        Reproduciendo = false;
        Pausado = false;
        foreach (var s in sistemas) if (s != null) { s.Play(true); }
        AlTerminar();
        RestablecerPato();
        if (volumen != null) volumen.weight = 0f;
        BloquearPato(false);
    }

    void Update()
    {
        if (!Reproduciendo || Pausado) return;
        float dt = Time.deltaTime;
        tiempo += dt;
        ActualizarMateriales();
        AlActualizar(tiempo, dt);
        if (tiempo >= duracion) Detener();
    }

    void ActualizarMateriales()
    {
        foreach (var m in materialesConTiempo) if (m != null) m.SetFloat(IdTiempo, tiempo);
    }

    void BloquearPato(bool bloquear)
    {
        if (jugador != null) jugador.controlBloqueado = bloquear;
        if (habilidades != null) habilidades.Bloqueado = bloquear;
    }

    protected void RestablecerPato()
    {
        if (flotar == null) return;
        flotar.alturaExtra = 0f;
        flotar.giroExtra = 0f;
        flotar.escalaExtra = Vector3.zero;
        flotar.inclinacionExtra = Vector3.zero;
    }

    protected void Espuma(float tamano)
    {
        if (habilidades != null) habilidades.EmitirEspuma(tamano);
    }

    protected void PesoVolumen(float peso)
    {
        if (volumen != null) volumen.weight = Mathf.Clamp01(peso);
    }

    protected virtual void OnDisable() => Detener();

    protected virtual void OnDestroy()
    {
        foreach (var o in creados) if (o != null) Destroy(o);
    }
}
