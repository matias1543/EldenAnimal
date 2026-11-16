
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ejecuta una sola vez "Actualizar Player de Marcos" cuando existe el archivo
/// Logs/elden_build.request (lo crea el asistente para no depender del menú).
///
/// Corre al recargar Unity o al salir del modo Play, y borra el archivo
/// antes de empezar, así no se repite.
/// </summary>
[InitializeOnLoad]
public static class MatiasAutoBuild
{
    private const string RequestFile = "Logs/elden_build.request";

    static MatiasAutoBuild()
    {
        EditorApplication.delayCall += TryRun;
        EditorApplication.playModeStateChanged += HandlePlayModeChanged;
    }

    private static void HandlePlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += TryRun;
        }
    }

    private static void TryRun()
    {
        if (!File.Exists(RequestFile))
            return;

        // En Play o compilando no se puede editar la escena: se intenta al volver al editor.
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        File.Delete(RequestFile);
        Debug.Log("Elden Animal: ejecutando 'Actualizar Player de Marcos' automáticamente...");
        MatiasLevelSetup.UpdatePlayerFromMarcos();
    }
}
