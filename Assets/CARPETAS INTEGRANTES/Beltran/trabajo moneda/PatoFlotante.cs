using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// Hace que un objeto (el pato) flote sobre el agua: sube y baja, se balancea
// y deriva un poco alrededor de su posición inicial.
// Con "verEnEditor" activado también se mueve en la vista de escena sin darle Play;
// al guardar la escena o apagar el script vuelve a su posición original.
[ExecuteAlways]
public class PatoFlotante : MonoBehaviour
{
    [Header("Agua")]
    [Tooltip("Plano de agua. Si se mueve en Y, el pato lo sigue manteniendo su altura relativa.")]
    public Transform agua;

    [Header("Editor")]
    [Tooltip("Mostrar el movimiento en la vista de escena sin darle Play.")]
    public bool verEnEditor = true;

    [Header("Subir y bajar")]
    [Tooltip("Cuánto sube y baja (metros).")]
    public float alturaOla = 0.2f;
    [Tooltip("Velocidad del vaivén vertical.")]
    public float velocidadOla = 0.8f;

    [Header("Balanceo")]
    [Tooltip("Inclinación máxima adelante/atrás y a los lados (grados).")]
    public float inclinacion = 12f;
    [Tooltip("Velocidad del balanceo.")]
    public float velocidadBalanceo = 0.6f;
    [Tooltip("Cuánto gira lentamente sobre sí mismo (grados).")]
    public float giro = 20f;

    [Header("Deriva")]
    [Tooltip("Radio máximo que se aleja de su posición inicial (metros).")]
    public float radioDeriva = 0.5f;
    [Tooltip("Velocidad de la deriva.")]
    public float velocidadDeriva = 0.2f;

    [Header("Espuma en el agua")]
    [Tooltip("Radio de la espuma alrededor del pato (metros). En 0 se calcula del tamaño del pato.")]
    public float radioEspuma = 0f;

    // El shader Water Cartoon lee hasta 4 patos desde las variables globales _PatoPos0.._PatoPos3
    // (xyz = posición, w = radio; w = 0 significa "sin pato").
    const int MaxPatos = 4;
    static readonly System.Collections.Generic.List<PatoFlotante> activos = new System.Collections.Generic.List<PatoFlotante>();
    static int[] idsPatos;
    float radioAuto;

    [Tooltip("Altura de la base del pato sobre el agua (metros). Se toma sola la primera vez y queda guardada.")]
    public float alturaSobreAgua;
    [SerializeField, HideInInspector] bool alturaGuardada;

    Vector3 posicionInicial;
    Quaternion rotacionInicial;
    float alturaRelativa => alturaSobreAgua;
    float semilla;

    // Pose base guardada y última pose aplicada, para poder restaurarla en el editor.
    bool tieneBase;
    bool aplicado;
    Vector3 ultimaPosicion;
    Quaternion ultimaRotacion, ultimaGuinada, ultimaInclinacion;

    void OnEnable()
    {
        GuardarBase();
        semilla = Random.Range(0f, 100f);
        var r = GetComponent<Renderer>();
        radioAuto = r != null ? (r.bounds.extents.x + r.bounds.extents.z) * 0.5f * 0.8f : 0.5f;
        if (!activos.Contains(this)) activos.Add(this);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.update += RefrescarEditor;
            EditorSceneManager.sceneSaving += AlGuardarEscena;
        }
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= RefrescarEditor;
        EditorSceneManager.sceneSaving -= AlGuardarEscena;
        if (!Application.isPlaying) Restaurar();
#endif
        activos.Remove(this);
        PublicarPatos();
    }

    static void PublicarPatos()
    {
        if (idsPatos == null)
        {
            idsPatos = new int[MaxPatos];
            for (int i = 0; i < MaxPatos; i++) idsPatos[i] = Shader.PropertyToID("_PatoPos" + i);
        }
        for (int i = 0; i < MaxPatos; i++)
        {
            Vector4 v = Vector4.zero;
            if (i < activos.Count && activos[i] != null)
            {
                var p = activos[i];
                Vector3 pos = p.transform.position;
                v = new Vector4(pos.x, pos.y, pos.z, p.radioEspuma > 0f ? p.radioEspuma : p.radioAuto);
            }
            Shader.SetGlobalVector(idsPatos[i], v);
        }
    }

    void GuardarBase()
    {
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
        // la altura se guarda una sola vez: recalcularla en cada recarga la tomaba a mitad
        // del vaivén y los patos iban subiendo poco a poco
        if (!alturaGuardada && agua != null)
        {
            alturaSobreAgua = posicionInicial.y - agua.position.y;
            alturaGuardada = true;
        }
        tieneBase = true;
        aplicado = false;
    }

    // Para PatoJugador: mueve el punto (en XZ) y la orientación alrededor de los cuales flota el pato.
    public Vector3 PosicionBase => posicionInicial;
    public void MoverBase(Vector3 posicion, Quaternion rotacion)
    {
        posicionInicial = new Vector3(posicion.x, posicionInicial.y, posicion.z);
        rotacionInicial = rotacion;
    }

    void Restaurar()
    {
        if (!tieneBase || !aplicado) return;
        transform.SetPositionAndRotation(posicionInicial, rotacionInicial);
        aplicado = false;
    }

    void Update()
    {
        if (Application.isPlaying) Mover();
    }

    void Mover()
    {
        if (!Application.isPlaying)
        {
            if (!verEnEditor) { Restaurar(); PublicarPatos(); return; }

            // Si lo moviste o rotaste con las herramientas del editor, esa pasa a ser su nueva base.
            if (aplicado && (transform.position != ultimaPosicion || transform.rotation != ultimaRotacion))
            {
                posicionInicial += transform.position - ultimaPosicion;
                rotacionInicial = Quaternion.Inverse(ultimaGuinada) * transform.rotation * Quaternion.Inverse(ultimaInclinacion);
            }
        }

#if UNITY_EDITOR
        float t = Application.isPlaying ? Time.time : (float)EditorApplication.timeSinceStartup;
#else
        float t = Time.time;
#endif

        // Ruido Perlin centrado en 0 (-1..1) para que el movimiento no se vea mecánico.
        float Ruido(float velocidad, float desfase) =>
            Mathf.PerlinNoise(semilla + desfase, t * velocidad) * 2f - 1f;

        // Vaivén vertical: seno principal más un poco de ruido.
        float y = Mathf.Sin(t * velocidadOla * Mathf.PI * 2f * 0.5f) * 0.7f + Ruido(velocidadOla, 1f) * 0.3f;

        float baseY = agua != null ? agua.position.y + alturaRelativa : posicionInicial.y;
        Vector3 deriva = new Vector3(Ruido(velocidadDeriva, 10f), 0f, Ruido(velocidadDeriva, 20f)) * radioDeriva;

        Vector3 posicion = new Vector3(posicionInicial.x, baseY, posicionInicial.z)
                           + deriva
                           + Vector3.up * y * alturaOla;

        // Balanceo: cabeceo, alabeo y un giro lento.
        float cabeceo = Ruido(velocidadBalanceo, 30f) * inclinacion;
        float alabeo = Ruido(velocidadBalanceo, 40f) * inclinacion;
        float guinada = Ruido(velocidadDeriva, 50f) * giro;

        ultimaGuinada = Quaternion.Euler(0f, guinada, 0f);
        ultimaInclinacion = Quaternion.Euler(cabeceo, 0f, alabeo);
        transform.SetPositionAndRotation(posicion, ultimaGuinada * rotacionInicial * ultimaInclinacion);

        ultimaPosicion = transform.position;
        ultimaRotacion = transform.rotation;
        aplicado = true;
        PublicarPatos();
    }

#if UNITY_EDITOR
    // En el editor Update solo corre cuando algo cambia, así que el movimiento
    // lo lleva el ciclo del editor y se repinta la vista de escena.
    // Un solo repintado compartido por todos los patos y como mucho 30 por segundo:
    // repintar en cada tick con varios patos dejaba el editor muy lento en escenas pesadas.
    const double IntervaloRepintado = 1.0 / 30.0;
    static double ultimoRepintado;

    void RefrescarEditor()
    {
        if (this == null || Application.isPlaying) return;
        double ahora = EditorApplication.timeSinceStartup;
        if (ahora - ultimoMovimiento < IntervaloRepintado) return;
        ultimoMovimiento = ahora;
        Mover();
        if (verEnEditor && ahora - ultimoRepintado >= IntervaloRepintado)
        {
            ultimoRepintado = ahora;
            SceneView.RepaintAll();
        }
    }
    double ultimoMovimiento;

    // Antes de guardar, devuelve el pato a su pose original para no guardar una pose a mitad de ola.
    void AlGuardarEscena(UnityEngine.SceneManagement.Scene escena, string ruta)
    {
        if (escena == gameObject.scene) Restaurar();
    }
#endif
}
