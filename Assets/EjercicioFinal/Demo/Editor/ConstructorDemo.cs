using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Arma la escena de la demo final a partir de la escena del agua toon:
//  - copia la escena a Assets/EjercicioFinal/Demo/Escenas y la abre
//  - borra todo lo que no es el agua, la isla o el pato (más cámara, luces y volumen)
//  - extiende el agua de la isla hasta el horizonte (océano) y pone un límite al pato
//  - crea materiales, perfiles de post-procesado, la estatua de Calabaza, las cajas de
//    pescado, los tres efectos, la UI y el gestor de la demo
// Se puede volver a ejecutar: reutiliza y actualiza lo que ya existe.
public static class ConstructorDemo
{
    const string Raiz = "Assets/EjercicioFinal/Demo";
    // Solo se usa si la escena de la demo todavía no existe (para crearla por primera vez).
    const string EscenaOrigen = "Assets/CARPETAS INTEGRANTES/Beltran/trabajo moneda/Agua Toon/Agua Toon Pruebas.unity";
    const string EscenaDemo = Raiz + "/Escenas/Demo Final.unity";
    const string DiscoOriginal = Raiz + "/Modelos/Agua Isla Disco.asset";
    const string MaterialRoca = Raiz + "/Materiales/Isla/Roca.mat";

    static readonly string[] ABorrar =
    {
        "Floor", "Cube", "Cube (1)", "Cube (2)", "Cube (3)", "Sphere", "Cylinder",
        "RubberDuck", "RubberDuck (1)", "RubberDuck (2)", "RubberDuck (3)",
        "SM_VFX_WaterPlane_G2", "Point Light", "Point Light (1)"
    };

    [MenuItem("Herramientas/Demo Final/Construir escena")]
    public static string ConstruirTodo()
    {
        var log = new System.Text.StringBuilder();
        CrearCarpetas();
        AbrirEscena(log);

        var mats = CrearMateriales();
        var perfiles = CrearPerfiles();

        var pato = Object.FindFirstObjectByType<PatoJugador>();
        var isla = GameObject.Find("island/island");
        var agua = GameObject.Find("island/Plane001");
        if (pato == null || isla == null || agua == null)
            throw new System.Exception("No se encontró el pato, la isla o el agua en la escena.");

        LimpiarEscena(log);
        var centroIsla = isla.GetComponent<Renderer>().bounds.center;
        AjustarCamaraYVolumen(perfiles["Global"], centroIsla);
        CrearOceano(agua.GetComponent<MeshFilter>(), log);
        AjustarPato(pato, centroIsla);
        CrearEstatua(isla, centroIsla, mats["Piedra Estatua"], log);
        CrearCajas(centroIsla, mats, log);
        var efectos = CrearEfectos(mats, perfiles);
        CrearUI(pato, efectos);

        var escena = pato.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        AgregarABuild();
        AssetDatabase.SaveAssets();
        log.AppendLine("Escena guardada: " + EscenaDemo);
        Debug.Log("[Demo Final] " + log);
        return log.ToString();
    }

    // ─── Escena ──────────────────────────────────────────────────────────────

    static void CrearCarpetas()
    {
        Carpeta("Assets", "EjercicioFinal");
        Carpeta("Assets/EjercicioFinal", "Demo");
        foreach (var c in new[] { "Escenas", "Materiales", "Modelos", "PostProcesado" }) Carpeta(Raiz, c);
    }

    static void Carpeta(string padre, string nombre)
    {
        if (!AssetDatabase.IsValidFolder(padre + "/" + nombre)) AssetDatabase.CreateFolder(padre, nombre);
    }

    static void AbrirEscena(System.Text.StringBuilder log)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EscenaDemo) == null)
        {
            if (!AssetDatabase.CopyAsset(EscenaOrigen, EscenaDemo)) throw new System.Exception("No se pudo copiar la escena.");
            log.AppendLine("Escena copiada de " + EscenaOrigen);
        }
        var activa = EditorSceneManager.GetActiveScene();
        if (activa.path != EscenaDemo) EditorSceneManager.OpenScene(EscenaDemo, OpenSceneMode.Single);
    }

    static void LimpiarEscena(System.Text.StringBuilder log)
    {
        var escena = EditorSceneManager.GetActiveScene();
        foreach (var raiz in escena.GetRootGameObjects())
            if (ABorrar.Contains(raiz.name)) { log.AppendLine("Borrado: " + raiz.name); Object.DestroyImmediate(raiz); }
    }

    static void AjustarCamaraYVolumen(VolumeProfile global, Vector3 centroIsla)
    {
        var cam = Camera.main;
        cam.backgroundColor = new Color(0.56f, 0.79f, 0.93f, 1f);
        cam.farClipPlane = 1500f;
        var datos = cam.GetUniversalAdditionalCameraData();
        datos.renderPostProcessing = true;

        var volGo = GameObject.Find("Global Volume");
        if (volGo != null) volGo.GetComponent<Volume>().sharedProfile = global;

        var sonda = GameObject.Find("Reflection Probe");
        if (sonda != null) sonda.transform.position = new Vector3(centroIsla.x, 2f, centroIsla.z);
    }

    static void AjustarPato(PatoJugador pato, Vector3 centroIsla)
    {
        pato.usarLimite = true;
        pato.centroLimite = new Vector3(centroIsla.x, 0f, centroIsla.z);
        pato.radioLimite = 42f;
        EditorUtility.SetDirty(pato);
    }

    static void AgregarABuild()
    {
        var lista = EditorBuildSettings.scenes.Where(s => s.path != EscenaDemo).ToList();
        lista.Insert(0, new EditorBuildSettingsScene(EscenaDemo, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }

    // ─── Materiales y post-procesado ─────────────────────────────────────────

    static Dictionary<string, Material> CrearMateriales()
    {
        var d = new Dictionary<string, Material>();
        d["Burbuja"] = Mat("FX Burbuja", "Demo/FX Burbuja", null);
        d["Gotas Burbuja"] = Mat("FX Gotas Burbuja", "Demo/FX Brillo", m => { m.SetColor("_Color", new Color(1.2f, 2f, 3.2f, 1f)); m.SetFloat("_Dureza", 2.2f); m.SetFloat("_Nucleo", 0.9f); });
        d["Disolver"] = Mat("FX Disolver Pato", "Demo/FX Disolver", null);
        d["Circulo"] = Mat("FX Circulo Magico", "Demo/FX Circulo Magico", null);
        d["Motas"] = Mat("FX Motas Arcanas", "Demo/FX Brillo", m => { m.SetColor("_Color", new Color(1.6f, 0.6f, 3.2f, 1f)); m.SetFloat("_Dureza", 2.2f); m.SetFloat("_Nucleo", 0.8f); });
        // fuego azul de Calabaza: núcleo azul claro, cuerpo azul y puntas índigo oscuro.
        // Valores HDR moderados para que el bloom no los queme a blanco.
        d["Fuego"] = Mat("FX Fuego", "Demo/FX Fuego", m =>
        {
            m.SetColor("_ColorNucleo", new Color(0.7f, 1.2f, 1.9f, 1f));
            m.SetColor("_ColorBase", new Color(0.25f, 0.5f, 1.6f, 1f));
            m.SetColor("_ColorPunta", new Color(0.25f, 0.18f, 0.85f, 1f));
            m.SetFloat("_PotenciaForma", 1f);
            m.SetFloat("_Intensidad", 1f);
        });
        d["Brasas"] = Mat("FX Brasas", "Demo/FX Brillo", m => { m.SetColor("_Color", new Color(0.45f, 0.75f, 1.6f, 1f)); m.SetFloat("_Dureza", 1.6f); m.SetFloat("_Nucleo", 0.35f); });
        d["Estallido"] = Mat("FX Estallido Caja", "Demo/FX Brillo", m => { m.SetColor("_Color", new Color(1.6f, 1.6f, 1.6f, 1f)); m.SetFloat("_Dureza", 2f); m.SetFloat("_Nucleo", 0.9f); });
        d["Caja"] = Mat("FX Caja Pescado", "Demo/FX Caja Pescado", null);

        // estatua y pescados: mismo shader estilizado que la isla (copias del material Roca)
        d["Piedra Estatua"] = CopiaRoca("Piedra Estatua", new Color(0.66f, 0.64f, 0.6f), new Color(0.47f, 0.45f, 0.43f));
        d["Pescado"] = CopiaRoca("Pescado", new Color(1f, 0.56f, 0.18f), new Color(0.95f, 0.36f, 0.1f));
        d["Pescado Ojos"] = CopiaRoca("Pescado Ojos", new Color(0.06f, 0.06f, 0.08f), new Color(0.04f, 0.04f, 0.05f));
        return d;
    }

    static Material Mat(string nombre, string shader, System.Action<Material> configurar)
    {
        var sh = Shader.Find(shader);
        if (sh == null) throw new System.Exception("Falta el shader " + shader);
        var ruta = Raiz + "/Materiales/" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, ruta); }
        else m.shader = sh;
        configurar?.Invoke(m);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material CopiaRoca(string nombre, Color colorBase, Color colorBajo)
    {
        var ruta = Raiz + "/Materiales/" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            AssetDatabase.CopyAsset(MaterialRoca, ruta);
            m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        }
        if (m.HasProperty("_Color_Base")) m.SetColor("_Color_Base", colorBase);
        if (m.HasProperty("_Color_Bajo")) m.SetColor("_Color_Bajo", colorBajo);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Dictionary<string, VolumeProfile> CrearPerfiles()
    {
        var d = new Dictionary<string, VolumeProfile>();

        var global = Perfil("PP Global Demo");
        var bloom = Comp<Bloom>(global);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(0.9f);
        bloom.scatter.Override(0.65f);
        d["Global"] = global;

        var burbuja = Perfil("PP Burbuja");
        Comp<ChromaticAberration>(burbuja).intensity.Override(1f);
        d["Burbuja"] = burbuja;

        var tele = Perfil("PP Teletransporte");
        var lente = Comp<LensDistortion>(tele);
        lente.intensity.Override(-0.6f);
        lente.scale.Override(1f);
        d["Teletransporte"] = tele;

        var nova = Perfil("PP Nova de Calabaza");
        var vin = Comp<Vignette>(nova);
        vin.color.Override(new Color(0.15f, 0.25f, 0.9f));
        vin.intensity.Override(0.48f);
        vin.smoothness.Override(0.55f);
        var ajuste = Comp<ColorAdjustments>(nova);
        ajuste.postExposure.Override(0.05f);
        ajuste.contrast.Override(20f);
        ajuste.saturation.Override(25f);
        ajuste.colorFilter.Override(new Color(0.78f, 0.88f, 1f));
        d["Nova"] = nova;

        foreach (var p in d.Values) EditorUtility.SetDirty(p);
        return d;
    }

    static VolumeProfile Perfil(string nombre)
    {
        var ruta = Raiz + "/PostProcesado/" + nombre + ".asset";
        var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, ruta);
        }
        return p;
    }

    static T Comp<T>(VolumeProfile p) where T : VolumeComponent
    {
        if (!p.TryGet(out T c))
        {
            c = p.Add<T>(false);
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, p);
        }
        c.active = true;
        EditorUtility.SetDirty(c);
        return c;
    }

    // ─── Océano ──────────────────────────────────────────────────────────────

    // Toma el disco de agua de la isla y le agrega anillos hacia afuera, pegados a su borde,
    // hasta ~35 veces su radio: así el agua llega al horizonte sin costuras ni capas dobles.
    // Los anillos tienen color de vértice blanco (= lejos de la orilla) y UV que siguen el
    // mismo mapeo plano del disco.
    static void CrearOceano(MeshFilter mf, System.Text.StringBuilder log)
    {
        var disco = AssetDatabase.LoadAssetAtPath<Mesh>(DiscoOriginal);
        if (disco == null) throw new System.Exception("No se encontró " + DiscoOriginal);
        var v = disco.vertices; var uv = disco.uv; var col = disco.colors; var nor = disco.normals; var tri = disco.triangles;

        // bordes que solo usa un triángulo = contorno del disco
        var usos = new Dictionary<long, int>();
        long Clave(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        for (int i = 0; i < tri.Length; i += 3)
            for (int k = 0; k < 3; k++)
            {
                long c = Clave(tri[i + k], tri[i + (k + 1) % 3]);
                usos[c] = usos.TryGetValue(c, out int n) ? n + 1 : 1;
            }
        var borde = new HashSet<int>();
        foreach (var kv in usos) if (kv.Value == 1) { borde.Add((int)(kv.Key >> 32)); borde.Add((int)(kv.Key & 0xffffffff)); }

        Vector3 centro = Vector3.zero;
        foreach (int i in borde) centro += v[i];
        centro /= borde.Count;
        var orden = borde.OrderBy(i => Mathf.Atan2(v[i].z - centro.z, v[i].x - centro.x)).ToArray();

        // UV = a·x + b·z + c (mínimos cuadrados sobre el disco)
        var (au, bu, cu) = AjustePlano(v, uv, 0);
        var (av, bv, cv) = AjustePlano(v, uv, 1);

        float[] factores = { 1.12f, 1.3f, 1.55f, 1.9f, 2.4f, 3.2f, 4.4f, 6.2f, 9f, 13.5f, 20f, 35f };
        int nb = orden.Length;
        var nv = new List<Vector3>(v);
        var nuv = new List<Vector2>(uv);
        var ncol = new List<Color>(col.Length == v.Length ? col : Enumerable.Repeat(Color.white, v.Length).ToArray());
        var nnor = new List<Vector3>(nor.Length == v.Length ? nor : Enumerable.Repeat(Vector3.up, v.Length).ToArray());
        var ntri = new List<int>(tri);

        // sentido de los triángulos del disco (para que los nuevos miren igual)
        var n0 = Vector3.Cross(v[tri[1]] - v[tri[0]], v[tri[2]] - v[tri[0]]);
        bool haciaArriba = n0.y > 0f;

        int[] anterior = orden;
        foreach (float f in factores)
        {
            var actual = new int[nb];
            for (int j = 0; j < nb; j++)
            {
                var p = v[orden[j]];
                var q = new Vector3(centro.x + (p.x - centro.x) * f, p.y, centro.z + (p.z - centro.z) * f);
                actual[j] = nv.Count;
                nv.Add(q);
                nuv.Add(new Vector2(au * q.x + bu * q.z + cu, av * q.x + bv * q.z + cv));
                ncol.Add(Color.white);
                nnor.Add(Vector3.up);
            }
            for (int j = 0; j < nb; j++)
            {
                int a = anterior[j], b = anterior[(j + 1) % nb], c = actual[j], d = actual[(j + 1) % nb];
                // (a, b, d) y (a, d, c) dan normal +Y con el ángulo creciente de Atan2(z, x)
                if (haciaArriba) { ntri.Add(a); ntri.Add(b); ntri.Add(d); ntri.Add(a); ntri.Add(d); ntri.Add(c); }
                else { ntri.Add(a); ntri.Add(d); ntri.Add(b); ntri.Add(a); ntri.Add(c); ntri.Add(d); }
            }
            anterior = actual;
        }

        var ruta = Raiz + "/Modelos/Agua Oceano.asset";
        var malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        bool nueva = malla == null;
        if (nueva) malla = new Mesh();
        malla.Clear();
        malla.name = "Agua Oceano";
        malla.indexFormat = IndexFormat.UInt32;
        malla.SetVertices(nv);
        malla.SetUVs(0, nuv);
        malla.SetColors(ncol);
        malla.SetNormals(nnor);
        malla.SetTriangles(ntri, 0);
        malla.RecalculateBounds();
        malla.RecalculateTangents();
        if (nueva) AssetDatabase.CreateAsset(malla, ruta);
        EditorUtility.SetDirty(malla);

        mf.sharedMesh = malla;
        EditorUtility.SetDirty(mf);
        log.AppendLine($"Océano: {nb} vértices de borde, {nv.Count} vértices en total, radio ×{factores[factores.Length - 1]}");
    }

    static (float, float, float) AjustePlano(Vector3[] v, Vector2[] uv, int eje)
    {
        // resuelve [Σxx Σxz Σx; Σxz Σzz Σz; Σx Σz n]·[a b c] = [Σxu Σzu Σu]
        double sxx = 0, sxz = 0, sx = 0, szz = 0, sz = 0, n = v.Length, sxu = 0, szu = 0, su = 0;
        for (int i = 0; i < v.Length; i++)
        {
            double x = v[i].x, z = v[i].z, u = eje == 0 ? uv[i].x : uv[i].y;
            sxx += x * x; sxz += x * z; sx += x; szz += z * z; sz += z; sxu += x * u; szu += z * u; su += u;
        }
        double[,] m = { { sxx, sxz, sx }, { sxz, szz, sz }, { sx, sz, n } };
        double[] r = { sxu, szu, su };
        double Det(double[,] a) =>
            a[0, 0] * (a[1, 1] * a[2, 2] - a[1, 2] * a[2, 1]) - a[0, 1] * (a[1, 0] * a[2, 2] - a[1, 2] * a[2, 0]) + a[0, 2] * (a[1, 0] * a[2, 1] - a[1, 1] * a[2, 0]);
        double det = Det(m);
        if (System.Math.Abs(det) < 1e-12) return (0f, 0f, 0f);
        var res = new float[3];
        for (int c = 0; c < 3; c++)
        {
            var mc = (double[,])m.Clone();
            for (int f = 0; f < 3; f++) mc[f, c] = r[f];
            res[c] = (float)(Det(mc) / det);
        }
        return (res[0], res[1], res[2]);
    }

    // ─── Estatua de Calabaza ─────────────────────────────────────────────────

    // Calabaza: gata naranja con sombrero de bruja que sostiene una poción. Se arma con
    // formas redondeadas (esferas deformadas, tubos curvos) y se combina en una sola malla
    // de piedra con el mismo shader estilizado de las rocas de la isla.
    static void CrearEstatua(GameObject isla, Vector3 centroIsla, Material piedra, System.Text.StringBuilder log)
    {
        var m = new MallaCompuesta();
        var esfera = Primitiva(PrimitiveType.Sphere);
        var cilindro = Primitiva(PrimitiveType.Cylinder);

        // pedestal
        m.Agregar(cilindro, new Vector3(0f, 0.25f, 0f), Vector3.zero, new Vector3(2.0f, 0.25f, 2.0f));
        m.Agregar(cilindro, new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(1.75f, 0.06f, 1.75f));
        // patas
        m.Agregar(esfera, new Vector3(0.24f, 0.7f, 0.14f), Vector3.zero, new Vector3(0.36f, 0.24f, 0.48f));
        m.Agregar(esfera, new Vector3(-0.24f, 0.7f, 0.14f), Vector3.zero, new Vector3(0.36f, 0.24f, 0.48f));
        // cuerpo (panza y pecho)
        m.Agregar(esfera, new Vector3(0f, 1.05f, 0f), Vector3.zero, new Vector3(0.95f, 0.85f, 0.8f));
        m.Agregar(esfera, new Vector3(0f, 1.45f, 0.02f), Vector3.zero, new Vector3(0.72f, 0.62f, 0.62f));
        // cabeza ancha con mechones en los cachetes
        m.Agregar(esfera, Cabeza.Centro, Vector3.zero, Cabeza.Radios * 2f);
        foreach (float lado in new[] { 1f, -1f })
        {
            m.Tubo(new[] { new Vector3(0.5f * lado, 2.0f, 0.05f), new Vector3(0.62f * lado, 1.95f, 0.07f), new Vector3(0.74f * lado, 1.9f, 0.08f) }, new[] { 0.11f, 0.06f, 0f }, 10);
            m.Tubo(new[] { new Vector3(0.47f * lado, 1.9f, 0.06f), new Vector3(0.56f * lado, 1.84f, 0.07f), new Vector3(0.65f * lado, 1.78f, 0.07f) }, new[] { 0.09f, 0.05f, 0f }, 10);
        }
        CaraCalabaza(m, esfera);
        // orejas grandes y puntiagudas que asoman bajo el ala del sombrero
        m.Tubo(new[] { new Vector3(0.33f, 2.34f, 0f), new Vector3(0.5f, 2.64f, 0.02f), new Vector3(0.62f, 2.92f, 0.03f) }, new[] { 0.23f, 0.13f, 0f }, 12);
        m.Tubo(new[] { new Vector3(-0.33f, 2.34f, 0f), new Vector3(-0.5f, 2.64f, 0.02f), new Vector3(-0.62f, 2.92f, 0.03f) }, new[] { 0.23f, 0.13f, 0f }, 12);
        // sombrero de bruja: ala inclinada y copa con la punta doblada
        m.Agregar(cilindro, new Vector3(0f, 2.5f, -0.02f), new Vector3(-6f, 0f, 7f), new Vector3(1.5f, 0.035f, 1.5f));
        m.Tubo(new[] { new Vector3(0f, 2.5f, -0.02f), new Vector3(0.02f, 2.78f, -0.05f), new Vector3(0.07f, 3.04f, -0.1f), new Vector3(0.2f, 3.24f, -0.17f), new Vector3(0.42f, 3.32f, -0.22f) },
               new[] { 0.43f, 0.34f, 0.23f, 0.12f, 0f }, 20);
        m.Tubo(new[] { new Vector3(0f, 2.52f, -0.02f), new Vector3(0.005f, 2.6f, -0.03f) }, new[] { 0.44f, 0.42f }, 20, tapas: true); // cinta
        // brazo extendido hacia el costado sosteniendo el farol/poción con un cordel
        m.Tubo(new[] { new Vector3(0.34f, 1.62f, 0.04f), new Vector3(0.62f, 1.5f, 0.12f), new Vector3(0.84f, 1.36f, 0.18f) }, new[] { 0.13f, 0.115f, 0.1f }, 12, tapas: true);
        m.Agregar(esfera, new Vector3(0.86f, 1.34f, 0.18f), Vector3.zero, Vector3.one * 0.17f);
        m.Tubo(new[] { new Vector3(0.86f, 1.27f, 0.18f), new Vector3(0.86f, 1.07f, 0.18f) }, new[] { 0.018f, 0.018f }, 6, tapas: true);
        m.Arco(new Vector3(0.86f, 1.07f, 0.18f), Vector3.right, Vector3.up, 0.04f, 0.012f, 0f, 360f);
        m.Agregar(cilindro, new Vector3(0.86f, 1.03f, 0.18f), Vector3.zero, new Vector3(0.1f, 0.04f, 0.1f));
        m.Agregar(esfera, new Vector3(0.86f, 0.86f, 0.18f), Vector3.zero, new Vector3(0.3f, 0.3f, 0.3f));
        // el otro brazo cuelga a un lado del cuerpo
        m.Tubo(new[] { new Vector3(-0.34f, 1.62f, 0.04f), new Vector3(-0.5f, 1.4f, 0.06f), new Vector3(-0.54f, 1.16f, 0.1f) }, new[] { 0.13f, 0.115f, 0.1f }, 12, tapas: true);
        m.Agregar(esfera, new Vector3(-0.55f, 1.12f, 0.11f), Vector3.zero, Vector3.one * 0.16f);
        // cola en curva hacia arriba
        m.Tubo(new[] { new Vector3(0.12f, 0.85f, -0.36f), new Vector3(0.42f, 0.88f, -0.6f), new Vector3(0.64f, 1.2f, -0.64f), new Vector3(0.62f, 1.56f, -0.52f), new Vector3(0.46f, 1.74f, -0.44f) },
               new[] { 0.11f, 0.1f, 0.09f, 0.075f, 0.04f }, 10, tapas: true);

        var ruta = Raiz + "/Modelos/Estatua Calabaza.asset";
        var malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        bool nueva = malla == null;
        if (nueva) malla = new Mesh();
        m.Volcar(malla, "Estatua Calabaza");
        if (nueva) AssetDatabase.CreateAsset(malla, ruta);
        EditorUtility.SetDirty(malla);

        var go = GameObject.Find("Estatua Calabaza") ?? new GameObject("Estatua Calabaza");
        if (go.GetComponent<MeshFilter>() == null) go.AddComponent<MeshFilter>();
        if (go.GetComponent<MeshRenderer>() == null) go.AddComponent<MeshRenderer>();
        go.GetComponent<MeshFilter>().sharedMesh = malla;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = piedra;
        r.shadowCastingMode = ShadowCastingMode.On;

        var (pos, libre) = BuscarLugar(isla, centroIsla);
        go.transform.SetPositionAndRotation(pos - Vector3.up * 0.15f, Quaternion.Euler(0f, 180f, 0f)); // mira hacia la cámara (−Z)
        go.transform.localScale = Vector3.one * 1.25f;
        log.AppendLine($"Estatua en {pos} (espacio libre {libre:F1} m)");
    }

    // Cabeza de la estatua: elipsoide sobre el que se apoyan los rasgos de la cara.
    static class Cabeza
    {
        public static readonly Vector3 Centro = new Vector3(0f, 2.08f, 0f);
        public static readonly Vector3 Radios = new Vector3(0.6f, 0.46f, 0.475f);

        // Punto de la superficie frontal en (x, y) y su normal.
        public static Vector3 Punto(float x, float y, out Vector3 normal)
        {
            float dx = x / Radios.x, dy = (y - Centro.y) / Radios.y;
            float z = Radios.z * Mathf.Sqrt(Mathf.Max(0.02f, 1f - dx * dx - dy * dy));
            var p = new Vector3(x, y, Centro.z + z);
            normal = new Vector3(x / (Radios.x * Radios.x), (y - Centro.y) / (Radios.y * Radios.y), z / (Radios.z * Radios.z)).normalized;
            return p;
        }
    }

    // Cara como el diseño de Calabaza: ojos de calabaza tallada (triángulos de esquinas
    // redondeadas, sin pupila), nariz chiquita, boca en "w" y dos colmillos.
    static void CaraCalabaza(MallaCompuesta m, Mesh esfera)
    {
        OjoCalabaza(m, 0.22f, 2.14f);
        OjoCalabaza(m, -0.22f, 2.14f);

        // nariz: triangulito con la punta hacia abajo
        var pn = Cabeza.Punto(0f, 2.02f, out var nn);
        m.Agregar(esfera, pn + nn * 0.015f, Quaternion.LookRotation(nn).eulerAngles, new Vector3(0.11f, 0.06f, 0.06f));
        var pn2 = Cabeza.Punto(0f, 1.995f, out var nn2);
        m.Tubo(new[] { pn2 + nn2 * 0.02f, Cabeza.Punto(0f, 1.965f, out var nn3) + nn3 * 0.012f }, new[] { 0.012f, 0.012f }, 6, tapas: true);

        // boca en "w"
        foreach (float lado in new[] { 1f, -1f })
        {
            var pb = Cabeza.Punto(0.045f * lado, 1.97f, out var nb);
            var der = Vector3.Cross(Vector3.up, nb).normalized;
            var arr = Vector3.Cross(nb, der);
            m.Arco(pb + nb * 0.01f, der, arr, 0.045f, 0.013f, 195f, 345f, 10);
        }

        // colmillos
        foreach (float lado in new[] { 1f, -1f })
        {
            var pc = Cabeza.Punto(0.075f * lado, 1.925f, out var nc);
            m.Tubo(new[] { pc + nc * 0.01f, pc + nc * 0.025f - Vector3.up * 0.09f }, new[] { 0.03f, 0f }, 8);
        }
    }

    // Ojo de calabaza: triángulo con la punta hacia arriba (un poco inclinada hacia afuera) y
    // las esquinas bien redondeadas. Un borde en relieve más alto que el relleno hace que se lea
    // como tallado en la piedra.
    static void OjoCalabaza(MallaCompuesta m, float cx, float cy)
    {
        float lado = Mathf.Sign(cx);
        float inclina = -14f * lado * Mathf.Deg2Rad; // punta hacia afuera (girar en positivo la lleva al centro)
        var esquinas = new[] { new Vector2(0f, 0.15f), new Vector2(-0.15f, -0.085f), new Vector2(0.15f, -0.085f) };

        // contorno del triángulo con muchos puntos, redondeado con Chaikin
        var contorno = new List<Vector2>();
        for (int k = 0; k < 3; k++)
        {
            var a = esquinas[k]; var b = esquinas[(k + 1) % 3];
            for (int i = 0; i < 2; i++) contorno.Add(Vector2.Lerp(a, b, i / 2f)); // pocos puntos = esquinas más redondas
        }
        for (int it = 0; it < 4; it++)
        {
            var suave = new List<Vector2>();
            for (int i = 0; i < contorno.Count; i++)
            {
                var a = contorno[i]; var b = contorno[(i + 1) % contorno.Count];
                suave.Add(Vector2.Lerp(a, b, 0.25f));
                suave.Add(Vector2.Lerp(a, b, 0.75f));
            }
            contorno = suave;
        }

        // al plano de la cara: girar, ubicar y apoyar sobre la superficie de la cabeza
        Vector3 Sobre(Vector2 q, float alto)
        {
            float c = Mathf.Cos(inclina), s = Mathf.Sin(inclina);
            var r = new Vector2(q.x * c - q.y * s, q.x * s + q.y * c);
            var p = Cabeza.Punto(cx + r.x, cy + r.y, out var n);
            return p + n * alto;
        }

        var centro = Cabeza.Punto(cx, cy, out var normal);
        var relleno = contorno.Select(q => Sobre(q * 0.94f, 0.012f)).ToArray();
        m.Poligono(relleno, centro + normal * 0.012f, normal);

        var borde = contorno.Where((_, i) => i % 2 == 0).Select(q => Sobre(q, 0.022f)).ToList();
        borde.Add(borde[0]);
        borde.Add(borde[1]);
        m.Tubo(borde.ToArray(), Enumerable.Repeat(0.022f, borde.Count).ToArray(), 8);
    }

    // Punto alto y despejado de la arena (lejos de palmeras, rocas y barriles).
    static (Vector3, float) BuscarLugar(GameObject isla, Vector3 centro)
    {
        var col = isla.AddComponent<MeshCollider>();
        var obstaculos = isla.transform.parent.GetComponentsInChildren<Renderer>()
            .Where(x => x.gameObject != isla && x.name != "Plane001").Select(x => x.bounds).ToList();
        Vector3 mejor = centro; float mejorPuntaje = float.MinValue, mejorLibre = 0f;
        for (float dx = -8f; dx <= 8f; dx += 0.5f)
            for (float dz = -8f; dz <= 8f; dz += 0.5f)
            {
                var o = new Vector3(centro.x + dx, 50f, centro.z + dz);
                if (!col.Raycast(new Ray(o, Vector3.down), out var hit, 100f)) continue;
                if (hit.point.y < 1.9f || hit.normal.y < 0.92f) continue; // arena seca y casi plana
                float libre = float.MaxValue;
                foreach (var b in obstaculos)
                {
                    var c = b.center; float ext = Mathf.Max(b.extents.x, b.extents.z);
                    libre = Mathf.Min(libre, new Vector2(c.x - hit.point.x, c.z - hit.point.z).magnitude - ext);
                }
                float puntaje = Mathf.Min(libre, 3.5f) * 2f - new Vector2(dx, dz).magnitude * 0.25f;
                if (puntaje > mejorPuntaje) { mejorPuntaje = puntaje; mejor = hit.point; mejorLibre = libre; }
            }
        Object.DestroyImmediate(col);
        return (mejor, mejorLibre);
    }

    // ─── Cajas de pescado ────────────────────────────────────────────────────

    static void CrearCajas(Vector3 centroIsla, Dictionary<string, Material> mats, System.Text.StringBuilder log)
    {
        var pescado = CrearMallaPescado(out var ojos);
        var cubo = Primitiva(PrimitiveType.Cube);

        var viejo = GameObject.Find("Cajas de Pescado");
        if (viejo != null) Object.DestroyImmediate(viejo);
        var padre = new GameObject("Cajas de Pescado").transform;

        // puntos cerca de la orilla, donde el pato todavía puede nadar (distancia a la arena 0.33–0.42)
        var disco = AssetDatabase.LoadAssetAtPath<Mesh>(DiscoOriginal);
        var aguaT = GameObject.Find("island/Plane001").transform;
        var v = disco.vertices; var c = disco.colors;
        const int Cantidad = 8;
        var elegidos = new Vector3[Cantidad];
        var mejor = Enumerable.Repeat(float.MaxValue, Cantidad).ToArray();
        for (int i = 0; i < v.Length; i++)
        {
            float r = c[i].r;
            if (r < 0.33f || r > 0.42f) continue;
            var p = aguaT.TransformPoint(v[i]);
            float ang = Mathf.Atan2(p.z - centroIsla.z, p.x - centroIsla.x) / (2f * Mathf.PI) + 0.5f;
            float pos = ang * Cantidad;
            int sector = Mathf.Clamp(Mathf.FloorToInt(pos), 0, Cantidad - 1);
            float error = Mathf.Abs(pos - sector - 0.5f) + Mathf.Abs(r - 0.37f) * 3f;
            if (error < mejor[sector]) { mejor[sector] = error; elegidos[sector] = p; }
        }

        int creadas = 0;
        for (int i = 0; i < Cantidad; i++)
        {
            if (mejor[i] == float.MaxValue) continue;
            var caja = new GameObject("Caja Pescado " + (++creadas));
            caja.transform.SetParent(padre, false);
            caja.transform.position = elegidos[i] + Vector3.up * 0.95f;
            caja.transform.localScale = Vector3.one * 0.9f;

            var cuboGo = new GameObject("Cubo");
            cuboGo.transform.SetParent(caja.transform, false);
            cuboGo.AddComponent<MeshFilter>().sharedMesh = cubo;
            var rc = cuboGo.AddComponent<MeshRenderer>();
            rc.sharedMaterial = mats["Caja"];
            rc.shadowCastingMode = ShadowCastingMode.Off;

            var pez = new GameObject("Pescado");
            pez.transform.SetParent(caja.transform, false);
            var cuerpo = new GameObject("Cuerpo");
            cuerpo.transform.SetParent(pez.transform, false);
            cuerpo.AddComponent<MeshFilter>().sharedMesh = pescado;
            cuerpo.AddComponent<MeshRenderer>().sharedMaterial = mats["Pescado"];
            var ojosGo = new GameObject("Ojos");
            ojosGo.transform.SetParent(pez.transform, false);
            ojosGo.AddComponent<MeshFilter>().sharedMesh = ojos;
            ojosGo.AddComponent<MeshRenderer>().sharedMaterial = mats["Pescado Ojos"];

            var comp = caja.AddComponent<CajaPescado>();
            comp.cubo = cuboGo.transform;
            comp.pescado = pez.transform;
            comp.materialEstallido = mats["Estallido"];
        }
        log.AppendLine("Cajas de pescado: " + creadas);
    }

    static Mesh CrearMallaPescado(out Mesh ojos)
    {
        var esfera = Primitiva(PrimitiveType.Sphere);
        var m = new MallaCompuesta();
        m.Agregar(esfera, Vector3.zero, Vector3.zero, new Vector3(0.6f, 0.34f, 0.22f));                          // cuerpo
        m.Agregar(esfera, new Vector3(-0.36f, 0.08f, 0f), new Vector3(0f, 0f, 38f), new Vector3(0.24f, 0.09f, 0.05f)); // cola
        m.Agregar(esfera, new Vector3(-0.36f, -0.08f, 0f), new Vector3(0f, 0f, -38f), new Vector3(0.24f, 0.09f, 0.05f));
        m.Agregar(esfera, new Vector3(0.02f, 0.16f, 0f), new Vector3(0f, 0f, -18f), new Vector3(0.22f, 0.12f, 0.035f)); // aleta
        m.Agregar(esfera, new Vector3(0.05f, -0.07f, 0.1f), new Vector3(20f, 0f, -30f), new Vector3(0.12f, 0.06f, 0.03f));
        m.Agregar(esfera, new Vector3(0.05f, -0.07f, -0.1f), new Vector3(-20f, 0f, -30f), new Vector3(0.12f, 0.06f, 0.03f));
        var cuerpo = Guardar(m, "Pescado");

        var mo = new MallaCompuesta();
        mo.Agregar(esfera, new Vector3(0.19f, 0.05f, 0.085f), Vector3.zero, Vector3.one * 0.065f);
        mo.Agregar(esfera, new Vector3(0.19f, 0.05f, -0.085f), Vector3.zero, Vector3.one * 0.065f);
        ojos = Guardar(mo, "Pescado Ojos");
        return cuerpo;
    }

    static Mesh Guardar(MallaCompuesta m, string nombre)
    {
        var ruta = Raiz + "/Modelos/" + nombre + ".asset";
        var malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        bool nueva = malla == null;
        if (nueva) malla = new Mesh();
        m.Volcar(malla, nombre);
        if (nueva) AssetDatabase.CreateAsset(malla, ruta);
        EditorUtility.SetDirty(malla);
        return malla;
    }

    static Mesh Primitiva(PrimitiveType tipo)
    {
        var go = GameObject.CreatePrimitive(tipo);
        var malla = go.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(go);
        return malla;
    }

    // ─── Efectos ─────────────────────────────────────────────────────────────

    static EfectoPato[] CrearEfectos(Dictionary<string, Material> mats, Dictionary<string, VolumeProfile> perfiles)
    {
        var viejo = GameObject.Find("Efectos Demo");
        if (viejo != null) Object.DestroyImmediate(viejo);
        var padre = new GameObject("Efectos Demo").transform;

        var burbuja = Efecto<EfectoBurbuja>(padre, "Burbuja", 30f, "Shader: FX Burbuja  ·  Post-procesado: Aberración cromática", perfiles["Burbuja"]);
        burbuja.materialBurbuja = mats["Burbuja"];
        burbuja.materialGotas = mats["Gotas Burbuja"];

        var tele = Efecto<EfectoTeletransporte>(padre, "Teletransporte", 4.2f, "Shaders: FX Disolver + FX Círculo Mágico  ·  Post-procesado: Distorsión de lente", perfiles["Teletransporte"]);
        tele.materialDisolver = mats["Disolver"];
        tele.materialCirculo = mats["Circulo"];
        tele.materialMotas = mats["Motas"];

        var nova = Efecto<EfectoNovaCalabaza>(padre, "Nova de Calabaza", 3.8f, "Shader: FX Fuego  ·  Post-procesado: Viñeta + Ajustes de color", perfiles["Nova"]);
        nova.materialFuego = mats["Fuego"];
        nova.materialBrasas = mats["Brasas"];

        return new EfectoPato[] { burbuja, tele, nova };
    }

    static T Efecto<T>(Transform padre, string nombre, float duracion, string descripcion, VolumeProfile perfil) where T : EfectoPato
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var e = go.AddComponent<T>();
        e.nombre = nombre;
        e.duracion = duracion;
        e.descripcion = descripcion;

        var volGo = new GameObject("Post-procesado " + nombre);
        volGo.transform.SetParent(go.transform, false);
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10;
        vol.weight = 0f;
        vol.sharedProfile = perfil;
        e.volumen = vol;
        return e;
    }

    // ─── UI ──────────────────────────────────────────────────────────────────

    static void CrearUI(PatoJugador pato, EfectoPato[] efectos)
    {
        foreach (var n in new[] { "UI Demo", "Gestor Demo" })
        {
            var viejo = GameObject.Find(n);
            if (viejo != null) Object.DestroyImmediate(viejo);
        }
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetAsLastSibling();
        }

        var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var canvasGo = new GameObject("UI Demo", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escala = canvasGo.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;

        // ── controles (arriba a la izquierda) ──
        var panelControles = Panel(canvasGo.transform, "Controles", sprite, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -28), new Vector2(440, 300));
        Texto(panelControles, "Texto", fuente, 26, TextAnchor.UpperLeft, new Vector2(22, -18), new Vector2(400, 270),
            "<b><color=#FFB347>CONTROLES</color></b>\n" +
            "<b>W A S D</b>   Moverse\n" +
            "<b>Espacio</b>   Saltar\n" +
            "<b>E</b>   Giro\n" +
            "<b>1</b>   " + efectos[0].nombre + " (mantén y suelta)\n" +
            "<b>2</b>   " + efectos[1].nombre + "\n" +
            "<b>3</b>   " + efectos[2].nombre);
        var pescados = Texto(canvasGo.transform, "Pescados", fuente, 30, TextAnchor.MiddleLeft, new Vector2(34, -342), new Vector2(440, 50), "Pescados recogidos: 0");
        pescados.rectTransform.anchorMin = pescados.rectTransform.anchorMax = new Vector2(0, 1);
        pescados.rectTransform.pivot = new Vector2(0, 1);
        pescados.color = new Color(1f, 0.85f, 0.4f);
        pescados.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.75f);

        // ── gestor (las teclas 1, 2 y 3 reproducen cada efecto) ──
        var gestorGo = new GameObject("Gestor Demo");
        var gestor = gestorGo.AddComponent<GestorDemo>();
        gestor.pato = pato;
        gestor.efectos = efectos;
        gestor.textoPescados = pescados;
    }

    static RectTransform Panel(Transform padre, string nombre, Sprite sprite, Vector2 ancla, Vector2 pivote, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.05f, 0.08f, 0.12f, 0.62f);
        return rt;
    }

    static Text Texto(Transform padre, string nombre, Font fuente, int tam, TextAnchor alineado, Vector2 pos, Vector2 caja, string texto)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = caja;
        var t = go.GetComponent<Text>();
        t.font = fuente;
        t.fontSize = tam;
        t.alignment = alineado;
        t.supportRichText = true;
        t.lineSpacing = 1.12f;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.text = texto;
        return t;
    }

    static Button Boton(Transform padre, string nombre, Font fuente, Sprite sprite, Vector2 pos, Vector2 tam, string etiqueta, out Text texto)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.12f, 0.16f, 0.22f, 0.85f);
        var b = go.GetComponent<Button>();
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        var colores = b.colors;
        colores.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colores.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        b.colors = colores;
        texto = Texto(go.transform, "Texto", fuente, 24, TextAnchor.MiddleCenter, Vector2.zero, tam, "<b>" + etiqueta + "</b>");
        return b;
    }
}

// Junta varias mallas (con su posición, rotación y escala) en una sola.
class MallaCompuesta
{
    readonly List<Vector3> v = new List<Vector3>();
    readonly List<Vector3> n = new List<Vector3>();
    readonly List<Vector2> uv = new List<Vector2>();
    readonly List<int> t = new List<int>();

    public void Agregar(Mesh malla, Vector3 pos, Vector3 rot, Vector3 escala)
    {
        var m = Matrix4x4.TRS(pos, Quaternion.Euler(rot), escala);
        var mn = m.inverse.transpose;
        int b = v.Count;
        var mv = malla.vertices; var mnor = malla.normals; var muv = malla.uv;
        for (int i = 0; i < mv.Length; i++)
        {
            v.Add(m.MultiplyPoint3x4(mv[i]));
            n.Add(mn.MultiplyVector(mnor[i]).normalized);
            uv.Add(i < muv.Length ? muv[i] : Vector2.zero);
        }
        foreach (var i in malla.triangles) t.Add(b + i);
    }

    // Tubo que sigue una curva (interpolada con Catmull-Rom) con radio variable.
    // Un radio final de 0 cierra en punta; "tapas" cierra los extremos abiertos.
    public void Tubo(Vector3[] puntos, float[] radios, int lados, bool tapas = false)
    {
        const int PorTramo = 6;
        var esp = new List<Vector3>();
        var rad = new List<float>();
        for (int s = 0; s < puntos.Length - 1; s++)
            for (int k = 0; k < PorTramo; k++)
            {
                float u = k / (float)PorTramo;
                var p0 = puntos[Mathf.Max(s - 1, 0)]; var p1 = puntos[s]; var p2 = puntos[s + 1]; var p3 = puntos[Mathf.Min(s + 2, puntos.Length - 1)];
                esp.Add(0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u));
                rad.Add(Mathf.Lerp(radios[s], radios[s + 1], u));
            }
        esp.Add(puntos[puntos.Length - 1]);
        rad.Add(radios[radios.Length - 1]);

        int b = v.Count;
        Vector3 normalPrev = Vector3.zero;
        for (int i = 0; i < esp.Count; i++)
        {
            var tan = (esp[Mathf.Min(i + 1, esp.Count - 1)] - esp[Mathf.Max(i - 1, 0)]).normalized;
            var normal = i == 0 ? Vector3.Cross(tan, Mathf.Abs(tan.y) < 0.9f ? Vector3.up : Vector3.right).normalized
                                : Vector3.Cross(Vector3.Cross(tan, normalPrev), tan).normalized; // transporte paralelo
            normalPrev = normal;
            var binormal = Vector3.Cross(tan, normal);
            for (int l = 0; l <= lados; l++)
            {
                float a = l / (float)lados * Mathf.PI * 2f;
                var dir = normal * Mathf.Cos(a) + binormal * Mathf.Sin(a);
                v.Add(esp[i] + dir * rad[i]);
                n.Add(dir);
                uv.Add(new Vector2(l / (float)lados, i / (float)(esp.Count - 1)));
            }
        }
        int fila = lados + 1;
        for (int i = 0; i < esp.Count - 1; i++)
            for (int l = 0; l < lados; l++)
            {
                // caras hacia afuera: Cross(lado, avance) apunta en la dirección radial
                int a = b + i * fila + l;
                t.Add(a); t.Add(a + 1); t.Add(a + fila);
                t.Add(a + 1); t.Add(a + fila + 1); t.Add(a + fila);
            }

        if (tapas)
        {
            Tapa(b, esp[0], -(esp[1] - esp[0]).normalized, lados, true);
            Tapa(b + (esp.Count - 1) * fila, esp[esp.Count - 1], (esp[esp.Count - 1] - esp[esp.Count - 2]).normalized, lados, false);
        }
    }

    // Polígono relleno (abanico desde el centro) que mira hacia "normal".
    public void Poligono(Vector3[] borde, Vector3 centro, Vector3 normal)
    {
        int c = v.Count;
        v.Add(centro); n.Add(normal); uv.Add(new Vector2(0.5f, 0.5f));
        int b = v.Count;
        foreach (var p in borde) { v.Add(p); n.Add(normal); uv.Add(Vector2.zero); }
        bool invertir = Vector3.Dot(Vector3.Cross(borde[0] - centro, borde[1] - centro), normal) < 0f;
        for (int i = 0; i < borde.Length; i++)
        {
            int a = b + i, d = b + (i + 1) % borde.Length;
            if (invertir) { t.Add(c); t.Add(d); t.Add(a); }
            else { t.Add(c); t.Add(a); t.Add(d); }
        }
    }

    // Arco en relieve (tubo fino) sobre el plano derecha/arriba, de a0 a a1 grados.
    public void Arco(Vector3 centro, Vector3 derecha, Vector3 arriba, float radio, float grosor, float a0, float a1, int pasos = 12)
    {
        var pts = new Vector3[pasos + 1];
        var rad = new float[pasos + 1];
        for (int i = 0; i <= pasos; i++)
        {
            float a = Mathf.Lerp(a0, a1, i / (float)pasos) * Mathf.Deg2Rad;
            pts[i] = centro + (derecha * Mathf.Cos(a) + arriba * Mathf.Sin(a)) * radio;
            rad[i] = grosor;
        }
        Tubo(pts, rad, 8, tapas: true);
    }

    void Tapa(int inicioAnillo, Vector3 centro, Vector3 normal, int lados, bool inicio)
    {
        int c = v.Count;
        v.Add(centro); n.Add(normal); uv.Add(new Vector2(0.5f, 0.5f));
        int b = v.Count;
        for (int l = 0; l <= lados; l++) { v.Add(v[inicioAnillo + l]); n.Add(normal); uv.Add(Vector2.zero); }
        for (int l = 0; l < lados; l++)
        {
            // el anillo gira antihorario alrededor de la tangente: el abanico (c, l, l+1) mira hacia +tangente
            if (inicio) { t.Add(c); t.Add(b + l + 1); t.Add(b + l); }
            else { t.Add(c); t.Add(b + l); t.Add(b + l + 1); }
        }
    }

    public void Volcar(Mesh malla, string nombre)
    {
        malla.Clear();
        malla.name = nombre;
        malla.indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        malla.SetVertices(v);
        malla.SetNormals(n);
        malla.SetUVs(0, uv);
        malla.SetTriangles(t, 0);
        malla.RecalculateBounds();
    }
}
