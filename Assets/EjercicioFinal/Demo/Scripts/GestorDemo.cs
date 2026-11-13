using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Controla la demo técnica:
//  - Teclas 1, 2, 3: reproducen al instante el efecto correspondiente
//    (si había otro en curso, lo corta y empieza el nuevo).
//  - Contador de pescados recogidos de las cajas.
// La UI se crea en la escena con el constructor de la demo (Herramientas > Demo Final).
public class GestorDemo : MonoBehaviour
{
    public static GestorDemo Instancia { get; private set; }

    public PatoJugador pato;
    public EfectoPato[] efectos;

    [Header("UI")]
    public Text textoPescados;

    int pescados;
    float pulsoPescados;

    void Awake()
    {
        Instancia = this;
        if (pato == null) pato = FindFirstObjectByType<PatoJugador>();
        foreach (var e in efectos) if (e != null) e.Configurar(pato);
        ActualizarPescados();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Reproducir(0);
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Reproducir(1);
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Reproducir(2);
            // soltar la tecla: la burbuja (1) se lanza al soltar
            if (kb.digit1Key.wasReleasedThisFrame || kb.numpad1Key.wasReleasedThisFrame) Soltar(0);
            if (kb.digit2Key.wasReleasedThisFrame || kb.numpad2Key.wasReleasedThisFrame) Soltar(1);
            if (kb.digit3Key.wasReleasedThisFrame || kb.numpad3Key.wasReleasedThisFrame) Soltar(2);
        }

        if (textoPescados != null)
        {
            pulsoPescados = Mathf.MoveTowards(pulsoPescados, 0f, Time.deltaTime * 3f);
            textoPescados.transform.localScale = Vector3.one * (1f + 0.25f * pulsoPescados);
        }
    }

    // Reproduce el efecto desde el inicio, cortando el que estuviera en curso.
    public void Reproducir(int indice)
    {
        if (indice < 0 || indice >= efectos.Length || efectos[indice] == null) return;
        foreach (var e in efectos) if (e != null) e.Detener();
        efectos[indice].Reproducir();
    }

    public void Soltar(int indice)
    {
        if (indice >= 0 && indice < efectos.Length && efectos[indice] != null) efectos[indice].SoltarTecla();
    }

    public void SumarPescado()
    {
        pescados++;
        pulsoPescados = 1f;
        ActualizarPescados();
    }

    void ActualizarPescados()
    {
        if (textoPescados != null) textoPescados.text = "Pescados recogidos: " + pescados;
    }
}
