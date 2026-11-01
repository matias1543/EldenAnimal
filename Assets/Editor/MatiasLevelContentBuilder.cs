
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using G = MatiasGemLevelBuilder;

/// <summary>
/// Herramienta de editor que completa el nivel TestMatias.
///
/// Menú: Tools > Elden Animal > Completar nivel
///
/// 1. Genera un NavMesh propio de TestMatias (el compartido de SampleScene no se toca).
/// 2. Vuelve a ubicar cofres y enemigos de cada cofre sobre el NavMesh nuevo.
/// 3. Crea el enemigo Ave (ataque a distancia) y el prefab de la Tortuga.
/// 4. Agrega grupos de enemigos a lo largo del recorrido.
/// 5. Arma el minimapa (cámara cenital + íconos) con mapa completo en la tecla M.
/// 6. Comprueba que se pueda llegar caminando a cada cofre y al portal.
/// </summary>
public static class MatiasLevelContentBuilder
{
    private const string ScenePath = "Assets/Scenes/TestMatias.unity";
    private const string NavMeshFolder = "Assets/Scenes/TestMatias";
    private const string NavMeshPath = NavMeshFolder + "/NavMesh-TestMatias.asset";

    private const string SlimePrefabPath = "Assets/Prefabs/Slime.prefab";
    private const string CrabPrefabPath = "Assets/Prefabs/CRAB.prefab";
    private const string TurtlePrefabPath = "Assets/Prefabs/Turtle.prefab";
    private const string BirdPrefabPath = "Assets/Prefabs/Bird.prefab";
    private const string ProjectilePrefabPath = "Assets/Prefabs/BirdProjectile.prefab";

    private const string BirdModelPath = "Assets/Assets importados/TinyBirdDuo/Prefab/Character/C05.prefab";
    private const string BirdAnimationFolder = "Assets/Assets importados/TinyBirdDuo/Animation/";
    private const string BirdControllerPath = "Assets/Animation/BirdTp.controller";

    private const string MinimapTexturePath = "Assets/Materials/Level/Minimap.renderTexture";
    private const string MinimapLayerName = "Minimap";

    private const string DataFolder = "Assets/Data";
    private const string SmallPotionPath = DataFolder + "/Potion_Small.asset";
    private const string BigPotionPath = DataFolder + "/Potion_Big.asset";
    private const string PotionPrefabPath = "Assets/Prefabs/Potion.prefab";
    private const string DefendClipPath = "Assets/Assets importados/DogKnight/Animations/Defend.anim";

    // Pociones sueltas a lo largo del recorrido (true = poción grande).
    private static readonly (Vector3 position, bool big)[] PotionSpots =
    {
        (new Vector3(55f,  -2.5f, -36f), false),
        (new Vector3(40f,  -2.5f, -52f), false),
        (new Vector3(35f,   0f,   -12f), false),
        (new Vector3(37f,   0f,    22f), true),
        (new Vector3(4f,    5f,    23f), false),
        (new Vector3(-12f,  7.5f,  24f), true),
    };

    private enum EnemyKind { Slime, Crab, Turtle, Bird }

    // Grupos de enemigos a lo largo del recorrido: Playa -> Camino del Este -> Puente -> Mesetas -> Cima.
    private static readonly (string name, Vector3 position, EnemyKind[] enemies)[] PathGroups =
    {
        ("Playa_Escalera", new Vector3(52f, -2.5f, -44f), new[] { EnemyKind.Crab, EnemyKind.Slime }),
        ("Camino_Sur",     new Vector3(36f,  0f,   -22f), new[] { EnemyKind.Slime, EnemyKind.Slime, EnemyKind.Bird }),
        ("Camino_Norte",   new Vector3(38f,  0f,    14f), new[] { EnemyKind.Turtle, EnemyKind.Crab, EnemyKind.Bird }),
        ("Meseta",         new Vector3(8f,   5f,    20f), new[] { EnemyKind.Slime, EnemyKind.Slime, EnemyKind.Bird }),
        ("Meseta_Alta",    new Vector3(-15f, 7.5f,  28f), new[] { EnemyKind.Turtle, EnemyKind.Slime, EnemyKind.Bird }),
        ("Cima",           new Vector3(-30f, 10f,   30f), new[] { EnemyKind.Bird, EnemyKind.Bird, EnemyKind.Turtle }),
    };

    [MenuItem("Tools/Elden Animal/Completar nivel (NavMesh, enemigos, aves, minimapa)")]
    private static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int minimapLayer = EnsureLayer(MinimapLayerName);

        if (!BakeNavMesh(scene, minimapLayer))
            return;

        // Cofres, enemigos de cada cofre, portal y UI sobre el NavMesh nuevo.
        if (!G.Build(scene))
            return;

        Dictionary<EnemyKind, GameObject> prefabs = new Dictionary<EnemyKind, GameObject>
        {
            [EnemyKind.Slime] = AssetDatabase.LoadAssetAtPath<GameObject>(SlimePrefabPath),
            [EnemyKind.Crab] = AssetDatabase.LoadAssetAtPath<GameObject>(CrabPrefabPath),
            [EnemyKind.Turtle] = EnsureTurtlePrefab(scene),
            [EnemyKind.Bird] = BuildBirdPrefab(),
        };

        SpawnPathEnemies(scene, prefabs);
        BuildPotionsAndBlock(scene);
        BuildMinimap(scene, minimapLayer);
        ValidatePaths(scene);
        WriteReachabilityMap(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Nivel completo: NavMesh propio, enemigos en el recorrido, aves y minimapa.");
    }

    // ---------- NavMesh ----------

    private static bool BakeNavMesh(Scene scene, int minimapLayer)
    {
        NavMeshSurface surface = G.Find<NavMeshSurface>(scene);

        if (surface == null)
        {
            Debug.LogError("No hay NavMeshSurface en TestMatias.");
            return false;
        }

        // Los íconos del minimapa no deben generar piso caminable.
        surface.layerMask &= ~(1 << minimapLayer);

        try
        {
            EditorUtility.DisplayProgressBar("Elden Animal", "Generando el NavMesh de TestMatias...", 0.5f);
            surface.BuildNavMesh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (!AssetDatabase.IsValidFolder(NavMeshFolder))
        {
            AssetDatabase.CreateFolder("Assets/Scenes", "TestMatias");
        }

        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshPath) != null)
        {
            AssetDatabase.DeleteAsset(NavMeshPath);
        }

        // Guardamos el NavMesh como asset propio de esta escena.
        AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
        EditorUtility.SetDirty(surface);
        return true;
    }

    private static void ValidatePaths(Scene scene)
    {
        Transform player = G.Find<PlayerMovement>(scene).transform;
        NavMeshPath path = new NavMeshPath();

        IEnumerable<Transform> targets = G.Find<Portal>(scene) is Portal portal
            ? new[] { "Chest_Playa", "Chest_Camino", "Chest_Puente" }
                .Select(n => G.FindByName(scene, n)?.transform)
                .Append(portal.transform)
            : Enumerable.Empty<Transform>();

        Vector3 start = G.SampleNavMesh(player.position);

        foreach (Transform target in targets.Where(t => t != null))
        {
            NavMesh.CalculatePath(start, G.SampleNavMesh(target.position), NavMesh.AllAreas, path);
            string result = path.status == NavMeshPathStatus.PathComplete ? "OK, se llega caminando" : $"PROBLEMA: {path.status}";
            Debug.Log($"Camino Player -> {target.name}: {result}");
        }
    }

    /// <summary>
    /// Escribe en Logs/TestMatias_MapaCaminos.txt un mapa visto desde arriba (norte arriba)
    /// que muestra qué zonas se pueden recorrer caminando desde el inicio y a qué altura.
    /// </summary>
    private static void WriteReachabilityMap(Scene scene)
    {
        const float min = -56f, max = 56f, step = 2f;
        float[] levels = { -2.5f, 0f, 2.5f, 5f, 7.5f, 10f, 12.5f };

        Vector3 origin = G.SampleNavMesh(G.Find<PlayerMovement>(scene).transform.position);
        NavMeshPath path = new NavMeshPath();

        // Marcas de objetos importantes (se dibujan encima del terreno).
        Dictionary<(int, int), char> marks = new Dictionary<(int, int), char>();

        void Mark(Vector3 p, char c) => marks[(Mathf.RoundToInt((p.x - min) / step), Mathf.RoundToInt((p.z - min) / step))] = c;

        foreach (Chest chest in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Chest>(true))) Mark(chest.transform.position, 'C');
        foreach (PotionPickup potion in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PotionPickup>(true))) Mark(potion.transform.position, 'o');
        if (G.Find<Portal>(scene) is Portal portalMark) Mark(portalMark.transform.position, 'P');
        Mark(origin, 'S');

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Mapa de caminos de TestMatias (norte arriba, cada caracter = 2 m)");
        sb.AppendLine("Alcanzable por altura: b=playa  0=llanura  2  5  7=mesetas  T=cima | x=caminable pero NO alcanzable | ~=fondo del mar | (espacio)=sin piso");
        sb.AppendLine("S=inicio C=cofre o=pocion P=portal");
        sb.AppendLine();

        int size = Mathf.RoundToInt((max - min) / step);

        for (int zi = size; zi >= 0; zi--)
        {
            for (int xi = 0; xi <= size; xi++)
            {
                if (marks.TryGetValue((xi, zi), out char mark))
                {
                    sb.Append(mark);
                    continue;
                }

                float x = min + xi * step, z = min + zi * step;
                char cell = ' ';

                foreach (float level in levels)
                {
                    if (!NavMesh.SamplePosition(new Vector3(x, level + 0.5f, z), out NavMeshHit hit, 1.2f, NavMesh.AllAreas))
                        continue;

                    if (hit.position.y < -4f)
                    {
                        cell = cell == ' ' ? '~' : cell;
                        continue;
                    }

                    if (G.IsReachable(origin, hit.position, path))
                    {
                        float y = hit.position.y;
                        cell = y < -1f ? 'b' : y < 1.5f ? '0' : y < 4f ? '2' : y < 6.5f ? '5' : y < 9f ? '7' : 'T';
                    }
                    else if (cell == ' ' || cell == '~')
                    {
                        cell = 'x';
                    }
                }

                sb.Append(cell);
            }

            sb.AppendLine($"  z={min + zi * step}");
        }

        string file = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Logs/TestMatias_MapaCaminos.txt"));
        System.IO.File.WriteAllText(file, sb.ToString());
        Debug.Log($"Mapa de caminos guardado en {file}");
    }

    // ---------- Enemigos ----------

    private static GameObject EnsureTurtlePrefab(Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurtlePrefabPath);

        if (prefab != null)
            return prefab;

        TurtleEnemy turtle = G.Find<TurtleEnemy>(scene);

        if (turtle == null)
        {
            Debug.LogWarning("No hay Tortuga en la escena para crear su prefab.");
            return null;
        }

        GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(turtle.gameObject);

        if (root == null)
        {
            root = turtle.gameObject;
        }

        return PrefabUtility.SaveAsPrefabAssetAndConnect(root, TurtlePrefabPath, InteractionMode.AutomatedAction);
    }

    private static void SpawnPathEnemies(Scene scene, Dictionary<EnemyKind, GameObject> prefabs)
    {
        GameObject existing = G.FindByName(scene, "PathEnemies");

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject parent = new GameObject("PathEnemies");
        SceneManager.MoveGameObjectToScene(parent, scene);

        int enemyLayer = LayerMask.NameToLayer("Enemy");

        foreach ((string groupName, Vector3 approx, EnemyKind[] enemies) in PathGroups)
        {
            Vector3 center = G.SampleReachable(approx);

            for (int i = 0; i < enemies.Length; i++)
            {
                GameObject prefab = prefabs[enemies[i]];

                if (prefab == null)
                    continue;

                GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                enemy.name = $"{enemies[i]}_{groupName}_{i}";
                enemy.transform.SetParent(parent.transform, true);

                float angle = i * Mathf.PI * 2f / enemies.Length;
                enemy.transform.position = G.SampleReachable(center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 3f);

                SetLayerRecursive(enemy, enemyLayer);
                G.GetOrAdd<EnemyHitFeedback>(enemy);
            }
        }
    }

    // ---------- Pociones y escudo ----------

    private static void BuildPotionsAndBlock(Scene scene)
    {
        PotionData small = GetPotionData(SmallPotionPath, "Poción", 25f, new Color(1f, 0.15f, 0.2f));
        PotionData big = GetPotionData(BigPotionPath, "Poción grande", 50f, new Color(0.6f, 0.2f, 1f));
        PotionPickup prefab = BuildPotionPrefab(small);

        // Pociones en el recorrido.
        GameObject existing = G.FindByName(scene, "Potions");

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject parent = new GameObject("Potions");
        SceneManager.MoveGameObjectToScene(parent, scene);

        foreach ((Vector3 position, bool isBig) in PotionSpots)
        {
            GameObject potion = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, scene);
            potion.transform.SetParent(parent.transform, true);
            potion.transform.position = G.SampleReachable(position);

            if (isBig)
            {
                potion.name = "Potion_Big";
                G.SetReference(potion.GetComponent<PotionPickup>(), "_data", big);
            }
        }

        // Todos los enemigos pueden soltar una poción al morir.
        foreach (EnemyTemplate enemy in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemyTemplate>(true)))
        {
            G.SetReference(G.GetOrAdd<EnemyDrop>(enemy.gameObject), "_dropPrefab", prefab);
        }

        // Jugador: bolsa de pociones + escudo (en el mismo objeto que Health).
        GameObject player = G.Find<PlayerMovement>(scene).gameObject;
        PotionBag bag = G.GetOrAdd<PotionBag>(player);
        G.GetOrAdd<PlayerBlock>(player);
        RemoveOldShieldVisual(player.transform);
        AddDefendToPlayerController(player.GetComponent<Animator>());

        // UI: contador de pociones y ayuda de controles.
        LevelUI ui = G.Find<LevelUI>(scene);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Text potionsText = G.CreateText("PotionsText", ui.transform, font, new Vector2(1f, 1f), new Vector2(-40f, -100f), new Vector2(400f, 60f), 36, TextAnchor.UpperRight);
        G.SetReference(ui, "_potionBag", bag);
        G.SetReference(ui, "_potionsText", potionsText);

        G.CreateText("ControlsHint", ui.transform, font, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(1200f, 40f), 24, TextAnchor.LowerLeft)
            .text = "WASD mover · Clic izq atacar · Clic der escudo · E abrir · Q poción · M mapa";
    }

    private static PotionData GetPotionData(string path, string displayName, float heal, Color color)
    {
        PotionData data = AssetDatabase.LoadAssetAtPath<PotionData>(path);

        if (data != null)
            return data;

        if (!AssetDatabase.IsValidFolder(DataFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }

        data = ScriptableObject.CreateInstance<PotionData>();
        AssetDatabase.CreateAsset(data, path);

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("_displayName").stringValue = displayName;
        so.FindProperty("_healAmount").floatValue = heal;
        so.FindProperty("_color").colorValue = color;
        so.ApplyModifiedPropertiesWithoutUndo();

        return data;
    }

    private static PotionPickup BuildPotionPrefab(PotionData defaultData)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PotionPrefabPath);

        if (existing != null)
            return existing.GetComponent<PotionPickup>();

        GameObject root = new GameObject("Potion");

        SphereCollider pickupZone = root.AddComponent<SphereCollider>();
        pickupZone.isTrigger = true;
        pickupZone.radius = 0.7f;
        pickupZone.center = new Vector3(0f, 0.5f, 0f);

        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);
        visual.localPosition = new Vector3(0f, 0.4f, 0f);

        // El color del líquido lo pone PotionPickup según su PotionData.
        Renderer liquid = CreatePrimitiveChild(PrimitiveType.Sphere, "Liquid", visual, Vector3.zero, Vector3.one * 0.4f, G.GetMaterial("PotionLiquid", Color.white));
        CreatePrimitiveChild(PrimitiveType.Cylinder, "Neck", visual, new Vector3(0f, 0.25f, 0f), new Vector3(0.12f, 0.1f, 0.12f), G.GetMaterial("PotionGlass", new Color(0.85f, 0.95f, 1f)));
        CreatePrimitiveChild(PrimitiveType.Cylinder, "Cork", visual, new Vector3(0f, 0.37f, 0f), new Vector3(0.14f, 0.04f, 0.14f), G.GetMaterial("ChestWood", new Color(0.45f, 0.27f, 0.12f)));

        PotionPickup pickup = root.AddComponent<PotionPickup>();
        G.SetReference(pickup, "_data", defaultData);
        G.SetReference(pickup, "_visual", visual);
        G.SetReference(pickup, "_liquidRenderer", liquid);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PotionPrefabPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<PotionPickup>();
    }

    // El escudo flotante de la versión anterior: ahora se usa la animación Defend.
    private static void RemoveOldShieldVisual(Transform player)
    {
        Transform old = player.Find("ShieldVisual");

        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }
    }

    /// <summary>
    /// Agrega al Animator del jugador (Player.controller, de Marcos) el bool IsBlocking
    /// y el estado Defend, con transiciones de ida y vuelta desde Locomotion.
    /// Si ya existen, no hace nada.
    /// </summary>
    private static void AddDefendToPlayerController(Animator animator)
    {
        AnimatorController controller = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;

        if (controller == null)
        {
            Debug.LogWarning("El Player no tiene un Animator Controller editable: no se agregó la animación Defend.");
            return;
        }

        if (controller.parameters.All(p => p.name != "IsBlocking"))
        {
            controller.AddParameter("IsBlocking", AnimatorControllerParameterType.Bool);
        }

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        if (sm.states.Any(s => s.state.name == "Defend"))
            return;

        AnimatorState locomotion = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Locomotion") ?? sm.defaultState;

        AnimatorState defend = sm.AddState("Defend");
        defend.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(DefendClipPath);

        AnimatorStateTransition toDefend = locomotion.AddTransition(defend);
        toDefend.AddCondition(AnimatorConditionMode.If, 0f, "IsBlocking");
        toDefend.hasExitTime = false;
        toDefend.duration = 0.1f;

        AnimatorStateTransition toLocomotion = defend.AddTransition(locomotion);
        toLocomotion.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsBlocking");
        toLocomotion.hasExitTime = false;
        toLocomotion.duration = 0.15f;

        EditorUtility.SetDirty(controller);
        Debug.Log($"Estado Defend agregado a {AssetDatabase.GetAssetPath(controller)}.");
    }

    private static Renderer CreatePrimitiveChild(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;

        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        return renderer;
    }

    // ---------- Ave ----------

    private static GameObject BuildBirdPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BirdPrefabPath);

        if (existing != null)
            return existing;

        GameObject root = new GameObject("Bird");

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BirdModelPath));
        model.transform.SetParent(root.transform, false);
        G.FitToSize(model, 1.4f);

        Animator animator = model.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.runtimeAnimatorController = BuildBirdController();
            animator.applyRootMotion = false;
        }

        CapsuleCollider body = root.AddComponent<CapsuleCollider>();
        body.center = new Vector3(0f, 0.7f, 0f);
        body.height = 1.4f;
        body.radius = 0.45f;

        Transform shootPoint = new GameObject("ShootPoint").transform;
        shootPoint.SetParent(root.transform, false);
        shootPoint.localPosition = new Vector3(0f, 0.8f, 0.6f);

        // RequireComponent agrega Health y NavMeshAgent.
        BirdEnemy bird = root.AddComponent<BirdEnemy>();
        root.AddComponent<EnemyHitFeedback>();

        NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 1.4f;

        SerializedObject so = new SerializedObject(bird);
        so.FindProperty("_projectilePrefab").objectReferenceValue = BuildProjectilePrefab();
        so.FindProperty("_shootPoint").objectReferenceValue = shootPoint;
        so.FindProperty("_detectionRange").floatValue = 12f;
        so.FindProperty("_attackRange").floatValue = 7f;
        so.FindProperty("_attackCooldown").floatValue = 1.8f;
        so.FindProperty("_moveSpeed").floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject health = new SerializedObject(root.GetComponent<Health>());
        health.FindProperty("_maxHealth").floatValue = 30f;
        health.ApplyModifiedPropertiesWithoutUndo();

        SetLayerRecursive(root, LayerMask.NameToLayer("Enemy"));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BirdPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Projectile BuildProjectilePrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);

        if (existing != null)
            return existing.GetComponent<Projectile>();

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "BirdProjectile";
        sphere.transform.localScale = Vector3.one * 0.35f;
        sphere.GetComponent<SphereCollider>().isTrigger = true;
        sphere.GetComponent<Renderer>().sharedMaterial = G.GetMaterial("BirdProjectile", new Color(1f, 0.5f, 0.1f), unlit: true);

        Rigidbody rb = sphere.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        sphere.AddComponent<Projectile>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sphere, ProjectilePrefabPath);
        Object.DestroyImmediate(sphere);
        return prefab.GetComponent<Projectile>();
    }

    // Mismos parámetros que el resto de los enemigos: IsMoving, Attack y Death.
    private static AnimatorController BuildBirdController()
    {
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(BirdControllerPath);

        if (existing != null)
            return existing;

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(BirdControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idle = sm.AddState("Idle");
        idle.motion = LoadClip("Idle/IdleOneWeapon.fbx");

        AnimatorState walk = sm.AddState("Walk");
        walk.motion = LoadClip("Movement/InPlace/WalkFWDOneWeapon_IP.fbx");

        AnimatorState attack = sm.AddState("Attack");
        attack.motion = LoadClip("Attack/ShootSingleshotOneWeapon.fbx");

        AnimatorState die = sm.AddState("Die");
        die.motion = LoadClip("MISC/DieOneWeapon.fbx");

        sm.defaultState = idle;

        AddTransition(idle, walk, false).AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
        AddTransition(walk, idle, false).AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
        AddTransition(idle, attack, false).AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        AddTransition(walk, attack, false).AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        AddTransition(attack, idle, true);

        AnimatorStateTransition toDie = sm.AddAnyStateTransition(die);
        toDie.AddCondition(AnimatorConditionMode.If, 0f, "Death");
        toDie.hasExitTime = false;
        toDie.canTransitionToSelf = false;
        toDie.duration = 0.1f;

        return controller;
    }

    private static AnimatorStateTransition AddTransition(AnimatorState from, AnimatorState to, bool hasExitTime)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = 0.9f;
        transition.duration = 0.1f;
        return transition;
    }

    private static AnimationClip LoadClip(string relativePath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(BirdAnimationFolder + relativePath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    // ---------- Minimapa ----------

    private static void BuildMinimap(Scene scene, int minimapLayer)
    {
        Transform player = G.Find<PlayerMovement>(scene).transform;
        Camera mainCamera = G.Find<IsometricCamera>(scene)?.GetComponent<Camera>();

        // La cámara principal no dibuja los íconos.
        if (mainCamera != null)
        {
            mainCamera.cullingMask &= ~(1 << minimapLayer);
        }

        RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(MinimapTexturePath);

        if (texture == null)
        {
            texture = new RenderTexture(512, 512, 16);
            AssetDatabase.CreateAsset(texture, MinimapTexturePath);
        }

        foreach (string old in new[] { "MinimapCamera", "MinimapUI" })
        {
            GameObject found = G.FindByName(scene, old);

            if (found != null)
            {
                Object.DestroyImmediate(found);
            }
        }

        // UI: marco + imagen de la cámara + ayuda.
        GameObject canvasObject = new GameObject("MinimapUI", typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.GetComponent<Canvas>().sortingOrder = 5;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject frame = new GameObject("MapFrame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(canvasObject.transform, false);
        frame.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        frame.GetComponent<Image>().raycastTarget = false;

        GameObject view = new GameObject("MapView", typeof(RectTransform), typeof(RawImage));
        view.transform.SetParent(frame.transform, false);
        RectTransform viewRect = view.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = new Vector2(6f, 6f);
        viewRect.offsetMax = new Vector2(-6f, -6f);
        view.GetComponent<RawImage>().texture = texture;
        view.GetComponent<RawImage>().raycastTarget = false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        G.CreateText("Hint", frame.transform, font, new Vector2(0.5f, 1f), new Vector2(0f, 34f), new Vector2(300f, 30f), 24, TextAnchor.MiddleCenter)
            .text = "M: ver mapa";

        // Cámara cenital ortográfica que dibuja en la textura.
        GameObject cameraObject = new GameObject("MinimapCamera", typeof(Camera));
        SceneManager.MoveGameObjectToScene(cameraObject, scene);

        Camera minimapCamera = cameraObject.GetComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.08f, 0.2f, 0.35f);
        minimapCamera.cullingMask = ~((1 << LayerMask.NameToLayer("UI")) | (1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Enemy")));
        minimapCamera.targetTexture = texture;
        minimapCamera.nearClipPlane = 1f;
        minimapCamera.farClipPlane = 200f;
        minimapCamera.depth = -2f;

        Minimap minimap = cameraObject.AddComponent<Minimap>();
        G.SetReference(minimap, "_target", player);
        G.SetReference(minimap, "_viewReference", mainCamera != null ? mainCamera.transform : null);
        G.SetReference(minimap, "_mapFrame", frame.GetComponent<RectTransform>());

        // Íconos.
        Material playerIcon = G.GetMaterial("MarkerPlayer", new Color(0.2f, 1f, 0.3f), unlit: true);
        Material enemyIcon = G.GetMaterial("MarkerEnemy", new Color(1f, 0.2f, 0.2f), unlit: true);
        Material chestIcon = G.GetMaterial("MarkerChest", new Color(1f, 0.85f, 0.1f), unlit: true);
        Material portalIcon = G.GetMaterial("MarkerPortal", new Color(0.3f, 0.9f, 1f), unlit: true);

        AddMarker(player, playerIcon, 3f, minimapLayer);

        foreach (EnemyTemplate enemy in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemyTemplate>(true)))
        {
            AddMarker(enemy.transform, enemyIcon, 2f, minimapLayer);
        }

        Transform portalActive = G.FindByName(scene, "PortalActive")?.transform;

        if (portalActive != null)
        {
            AddMarker(portalActive, portalIcon, 6f, minimapLayer);
        }

        AddChestMarkerToPrefab(chestIcon, minimapLayer);
    }

    private static void AddChestMarkerToPrefab(Material icon, int layer)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(G.ChestPrefabPath);

        try
        {
            GameObject marker = AddMarker(contents.transform, icon, 5f, layer);
            G.SetReference(contents.GetComponent<Chest>(), "_minimapMarker", marker);
            PrefabUtility.SaveAsPrefabAsset(contents, G.ChestPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // Cuadrado plano mirando hacia arriba, por encima del objeto: solo lo ve la cámara del minimapa.
    private static GameObject AddMarker(Transform parent, Material material, float size, int layer)
    {
        Transform old = parent.Find("MinimapMarker");

        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        marker.name = "MinimapMarker";
        marker.layer = layer;
        Object.DestroyImmediate(marker.GetComponent<Collider>());

        marker.transform.SetParent(parent, false);
        marker.transform.position = parent.position + Vector3.up * 8f;
        marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        float parentScale = Mathf.Max(parent.lossyScale.x, 0.0001f);
        marker.transform.localScale = Vector3.one * (size / parentScale);

        Renderer renderer = marker.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return marker;
    }

    // ---------- Utilidades ----------

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);

        if (existing >= 0)
            return existing;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);

            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }

        Debug.LogError("No hay layers libres para el minimapa.");
        return 0;
    }

    private static void SetLayerRecursive(GameObject root, int layer)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = layer;
        }
    }
}
