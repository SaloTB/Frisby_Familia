using UnityEngine;
using UnityEngine.InputSystem;

// Pato que el jugador mueve con WASD sobre el agua.
// Usa PatoFlotante (en el mismo objeto) para seguir flotando.
// Los halos que deja al nadar son un sistema de partículas hijo ("Halos"), no parte del agua.
[RequireComponent(typeof(PatoFlotante))]
public class PatoJugador : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad máxima (metros por segundo).")]
    public float velocidad = 3.5f;
    [Tooltip("Qué tan rápido acelera y frena (m/s²). Bajo = se siente más 'en el agua'.")]
    public float aceleracion = 4f;
    [Tooltip("Qué tan rápido gira hacia donde nada.")]
    public float giro = 4f;
    [Tooltip("Corrige hacia dónde mira el modelo del pato (grados).")]
    public float giroModelo = 180f;

    [Header("Límites")]
    [Tooltip("Agua por la que puede nadar. Su mesh debe tener la distancia a la orilla pintada en el color de vértice (rojo).")]
    public MeshFilter agua;
    [Tooltip("No se acerca más a la arena que esto (0 = arena, 1 = lejos de la orilla).")]
    [Range(0f, 1f)] public float limiteOrilla = 0.3f;

    [Header("Cámara")]
    public bool seguirCamara = true;
    public Camera camara;
    public float suavidadCamara = 4f;

    PatoFlotante flotar;
    Vector3 vel, offsetCamara;
    Quaternion rotBase;
    System.Collections.Generic.Dictionary<long, float> mapaAgua;
    const float Celda = 0.75f;

    void OnEnable()
    {
        flotar = GetComponent<PatoFlotante>();
        rotBase = transform.rotation;
        vel = Vector3.zero;
        mapaAgua = null;
        if (seguirCamara)
        {
            if (camara == null) camara = Camera.main;
            if (camara != null) offsetCamara = camara.transform.position - transform.position;
        }
    }

    void Update()
    {
        Nadar(Time.deltaTime);
    }

    void Nadar(float dt)
    {
        var kb = Keyboard.current;
        Vector2 entrada = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed) entrada.y += 1f;
            if (kb.sKey.isPressed) entrada.y -= 1f;
            if (kb.dKey.isPressed) entrada.x += 1f;
            if (kb.aKey.isPressed) entrada.x -= 1f;
        }
        entrada = Vector2.ClampMagnitude(entrada, 1f);

        // relativo a la cámara: W siempre es "hacia adelante en la pantalla"
        Vector3 adelante = Vector3.forward, derecha = Vector3.right;
        var cam = camara != null ? camara : Camera.main;
        if (cam != null)
        {
            adelante = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            derecha = Vector3.Cross(Vector3.up, adelante);
        }
        Vector3 deseada = (adelante * entrada.y + derecha * entrada.x) * velocidad;
        vel = Vector3.MoveTowards(vel, deseada, aceleracion * dt);

        Vector3 pos = flotar.PosicionBase;
        Vector3 siguiente = pos + vel * dt;
        if (!Permitido(siguiente))
        {
            // deslizar por la orilla en vez de quedarse pegado
            var soloX = pos + new Vector3(vel.x, 0f, 0f) * dt;
            var soloZ = pos + new Vector3(0f, 0f, vel.z) * dt;
            if (Permitido(soloX)) { siguiente = soloX; vel.z = 0f; }
            else if (Permitido(soloZ)) { siguiente = soloZ; vel.x = 0f; }
            else { siguiente = pos; vel = Vector3.zero; }
        }

        if (vel.sqrMagnitude > 0.04f)
        {
            var objetivo = Quaternion.LookRotation(new Vector3(vel.x, 0f, vel.z)) * Quaternion.Euler(0f, giroModelo, 0f);
            rotBase = Quaternion.Slerp(rotBase, objetivo, 1f - Mathf.Exp(-giro * dt));
        }
        flotar.MoverBase(siguiente, rotBase);
    }

    void LateUpdate()
    {
        if (!seguirCamara || camara == null) return;
        var objetivo = transform.position + offsetCamara;
        camara.transform.position = Vector3.Lerp(camara.transform.position, objetivo, 1f - Mathf.Exp(-suavidadCamara * Time.deltaTime));
    }

    // ¿Hay agua (y lejos de la arena) en este punto? Se lee del color de vértice del mesh del agua.
    bool Permitido(Vector3 p)
    {
        if (agua == null || agua.sharedMesh == null) return true;
        if (mapaAgua == null) ConstruirMapa();
        float r;
        return mapaAgua.TryGetValue(Clave(p), out r) && r >= limiteOrilla;
    }

    static long Clave(Vector3 p) => ((long)Mathf.FloorToInt(p.x / Celda) << 32) ^ (uint)Mathf.FloorToInt(p.z / Celda);

    void ConstruirMapa()
    {
        mapaAgua = new System.Collections.Generic.Dictionary<long, float>();
        var m = agua.sharedMesh; var v = m.vertices; var c = m.colors; var t = agua.transform;
        bool hayColor = c != null && c.Length == v.Length;
        for (int i = 0; i < v.Length; i++)
        {
            long k = Clave(t.TransformPoint(v[i]));
            float r = hayColor ? c[i].r : 1f;
            float actual;
            mapaAgua[k] = mapaAgua.TryGetValue(k, out actual) ? Mathf.Min(actual, r) : r;
        }
    }
}
