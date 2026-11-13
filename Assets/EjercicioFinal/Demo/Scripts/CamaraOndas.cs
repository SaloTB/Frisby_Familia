using UnityEngine;

// Cámara ortográfica que mira el agua desde arriba y solo dibuja la capa "Ondas"
// (los halos del pato) en una Render Texture. El shader Water Toon lee esa textura
// para poner espuma donde están los halos (técnica de Minions Art).
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CamaraOndas : MonoBehaviour
{
    [Tooltip("La cámara sigue a este objeto en XZ (el pato jugable).")]
    public Transform objetivo;
    [Tooltip("Altura de la cámara sobre el objetivo.")]
    public float altura = 10f;
    public RenderTexture textura;

    Camera cam;
    static int idTex = -1, idArea = -1;

    void OnEnable()
    {
        if (idTex < 0) { idTex = Shader.PropertyToID("_OndasRT"); idArea = Shader.PropertyToID("_OndasArea"); }
        cam = GetComponent<Camera>();
        if (textura != null) cam.targetTexture = textura;
        Publicar();
    }

    void OnDisable()
    {
        // ancho 0: el agua deja de leer la textura
        Shader.SetGlobalVector(idArea, Vector4.zero);
    }

    void LateUpdate()
    {
        if (objetivo != null)
        {
            var p = objetivo.position;
            transform.position = new Vector3(p.x, p.y + altura, p.z);
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        Publicar();
    }

    void Publicar()
    {
        if (cam == null) return;
        if (textura != null) Shader.SetGlobalTexture(idTex, textura);
        var p = transform.position;
        // x, z = centro del área que ve la cámara; w = ancho en metros (2 × tamaño ortográfico)
        Shader.SetGlobalVector(idArea, new Vector4(p.x, 0f, p.z, cam.orthographicSize * 2f));
    }
}
