
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramienta de editor de un solo uso para preparar el nivel TestMatias.
///
/// Menú: Tools > Elden Animal > Preparar nivel Matías
///
/// - Trae el Player, el HUD y el EventSystem de TestMarcos (TestMarcos no se guarda).
/// - Reemplaza el Player viejo (PlayerTest) y apunta la cámara al nuevo.
/// - Pone a los enemigos en la layer Enemy y les agrega EnemyHitFeedback.
/// - Guarda el Slime como prefab y agrega la escena a Build Settings.
/// </summary>
public static class MatiasLevelSetup
{
    private const string MatiasScenePath = "Assets/Scenes/TestMatias.unity";
    private const string MarcosScenePath = "Assets/Scenes/TestMarcos.unity";
    private const string SlimePrefabPath = "Assets/Prefabs/Slime.prefab";

    [MenuItem("Tools/Elden Animal/Preparar nivel Matías")]
    private static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene matiasScene = EditorSceneManager.OpenScene(MatiasScenePath, OpenSceneMode.Single);
        Scene marcosScene = EditorSceneManager.OpenScene(MarcosScenePath, OpenSceneMode.Additive);

        // 1. Objetos de Marcos.
        GameObject newPlayer = FindRoot<PlayerMovement>(marcosScene);
        GameObject hud = FindRoot<HealthBar>(marcosScene);
        GameObject eventSystem = FindRoot<UnityEngine.EventSystems.EventSystem>(marcosScene);

        if (newPlayer == null || hud == null || eventSystem == null)
        {
            Debug.LogError("No se encontró Player, HUD o EventSystem en TestMarcos.");
            EditorSceneManager.CloseScene(marcosScene, true);
            return;
        }

        // 2. Player viejo: guardamos su posición y lo borramos.
        GameObject oldPlayer = FindRoot<PlayerTest>(matiasScene);
        Vector3 spawnPosition = newPlayer.transform.position;
        Quaternion spawnRotation = newPlayer.transform.rotation;

        if (oldPlayer != null)
        {
            spawnPosition = oldPlayer.transform.position;
            spawnRotation = oldPlayer.transform.rotation;
            Object.DestroyImmediate(oldPlayer);
        }

        // 3. Movemos los objetos juntos para conservar las referencias HUD -> Player.
        foreach (GameObject go in new[] { newPlayer, hud, eventSystem })
        {
            SceneManager.MoveGameObjectToScene(go, matiasScene);
        }

        newPlayer.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        // Cerramos TestMarcos sin guardar: su archivo queda intacto.
        EditorSceneManager.CloseScene(marcosScene, true);

        // 4. La cámara sigue al nuevo Player.
        foreach (IsometricCamera cam in FindInScene<IsometricCamera>(matiasScene))
        {
            SerializedObject so = new SerializedObject(cam);
            so.FindProperty("_target").objectReferenceValue = newPlayer.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 5. Enemigos: layer Enemy (MeleeAttack solo golpea esa layer) + feedback de daño.
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        foreach (EnemyTemplate enemy in FindInScene<EnemyTemplate>(matiasScene))
        {
            foreach (Transform t in enemy.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = enemyLayer;
            }

            if (enemy.GetComponent<EnemyHitFeedback>() == null)
            {
                enemy.gameObject.AddComponent<EnemyHitFeedback>();
            }
        }

        // 6. Slime como prefab.
        SlimeEnemy slime = FindInScene<SlimeEnemy>(matiasScene).FirstOrDefault();

        if (slime != null && AssetDatabase.LoadAssetAtPath<GameObject>(SlimePrefabPath) == null)
        {
            GameObject slimeRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(slime.gameObject) ?? slime.gameObject;
            slimeRoot.name = "Slime";
            PrefabUtility.SaveAsPrefabAssetAndConnect(slimeRoot, SlimePrefabPath, InteractionMode.AutomatedAction);
        }

        // 7. Build Settings.
        List<EditorBuildSettingsScene> buildScenes = EditorBuildSettings.scenes.ToList();

        if (buildScenes.All(s => s.path != MatiasScenePath))
        {
            buildScenes.Add(new EditorBuildSettingsScene(MatiasScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        EditorSceneManager.MarkSceneDirty(matiasScene);
        EditorSceneManager.SaveScene(matiasScene);

        Debug.Log("Nivel Matías preparado: Player de Marcos + HUD, enemigos en layer Enemy con feedback, Slime prefab y escena en Build Settings.");
    }

    // Devuelve el objeto raíz que contiene el componente T dentro de la escena.
    private static GameObject FindRoot<T>(Scene scene) where T : Component
    {
        T component = FindInScene<T>(scene).FirstOrDefault();
        return component != null ? component.transform.root.gameObject : null;
    }

    private static IEnumerable<T> FindInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }
}
