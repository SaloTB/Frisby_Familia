using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Build WebGL de la demo lista para subir a itch.io:
//  - solo la escena Demo Final
//  - compresión Gzip con descompresión de respaldo (itch.io no envía las cabeceras que
//    necesita Brotli, así que sin respaldo el juego no carga)
//  - calidad "PC" en WebGL: el agua necesita las texturas de profundidad y de opacos que el
//    perfil "Mobile" no genera
//  - deja Builds/WebGL Demo Final/ y Builds/Demo Final WebGL.zip (index.html en la raíz del zip)
public static class BuildWebGLDemo
{
    const string Escena = "Assets/EjercicioFinal/Demo/Escenas/Demo Final.unity";
    const string Carpeta = "Builds/WebGL Demo Final";
    const string Zip = "Builds/Demo Final WebGL.zip";

    [MenuItem("Herramientas/Demo Final/Build WebGL (itch.io)")]
    public static string Construir()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        CalidadPCEnWebGL();
        AssetDatabase.SaveAssets();

        if (Directory.Exists(Carpeta)) Directory.Delete(Carpeta, true);
        var opciones = new BuildPlayerOptions
        {
            scenes = new[] { Escena },
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            locationPathName = Carpeta,
            options = BuildOptions.None
        };
        var reporte = BuildPipeline.BuildPlayer(opciones);
        var r = reporte.summary;
        string texto = $"Resultado: {r.result}  ·  errores: {r.totalErrors}  ·  advertencias: {r.totalWarnings}  ·  " +
                       $"tamaño: {r.totalSize / (1024f * 1024f):F1} MB  ·  tiempo: {r.totalTime.TotalMinutes:F1} min";
        if (r.result != BuildResult.Succeeded)
        {
            foreach (var paso in reporte.steps)
                foreach (var m in paso.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception) texto += "\n  " + m.content;
            Debug.LogError("[Build WebGL] " + texto);
            return texto;
        }

        if (File.Exists(Zip)) File.Delete(Zip);
        ZipFile.CreateFromDirectory(Carpeta, Zip, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);
        texto += $"\nZip: {Path.GetFullPath(Zip)} ({new FileInfo(Zip).Length / (1024f * 1024f):F1} MB)";
        Debug.Log("[Build WebGL] " + texto);
        return texto;
    }

    static void CalidadPCEnWebGL()
    {
        int pc = System.Array.IndexOf(QualitySettings.names, "PC");
        if (pc < 0) return;
        var ajustes = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
        if (ajustes.Length == 0) return;
        var so = new SerializedObject(ajustes[0]);
        var mapa = so.FindProperty("m_PerPlatformDefaultQuality");
        for (int i = 0; i < mapa.arraySize; i++)
        {
            var par = mapa.GetArrayElementAtIndex(i);
            if (par.FindPropertyRelative("first").stringValue == "WebGL")
                par.FindPropertyRelative("second").intValue = pc;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
