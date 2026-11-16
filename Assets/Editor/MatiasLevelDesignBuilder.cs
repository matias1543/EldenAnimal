
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using G = MatiasGemLevelBuilder;

/// <summary>
/// Diseño de nivel "Las tres llaves de la isla" para TestMatias.
///
/// - Conecta el puente norte en el NavMesh (NavMeshLink) y agrega una pasarela invisible.
/// - Brutus bloquea el puente hasta tener 2 gemas (GemGatekeeper).
/// - La Puerta de la Cima bloquea la última rampa hasta tener todas las gemas (GemDoor).
/// - El Rey Slime custodia el portal (BossSlime) y tiene su barra de vida.
///
/// Lo usa MatiasLevelContentBuilder (menú "Completar nivel").
/// </summary>
public static class MatiasLevelDesignBuilder
{
    private const string BridgeName = "Bridge06";
    private const string SummitRampName = "Ramp01_3";
    private const string MaterialsFolder = "Assets/Materials/Level";

    private const string SlimeModelPath = "Assets/Assets importados/RPG Monster DUO PBR Polyart/Prefabs/PolyartDefault/SlimePolyart.prefab";
    private const string TurtleModelPath = "Assets/Assets importados/RPG Monster DUO PBR Polyart/Prefabs/PolyartDefault/TurtleShellPolyart.prefab";
    private const string SlimeAnimationFolder = "Assets/Assets importados/RPG Monster DUO PBR Polyart/Animations/Slime/";
    private const string TurtleAnimationFolder = "Assets/Assets importados/RPG Monster DUO PBR Polyart/Animations/TurtleShell/";
    private const string MinionPrefabPath = "Assets/Prefabs/Slime.prefab";

    private const string BrutusName = "Brutus";
    private const string BrutusPrefabPath = "Assets/Prefabs/Npc_Brutus.prefab";
    private const string BrutusControllerPath = "Assets/Animation/NpcBrutusTp.controller";

    private const string BossPrefabPath = "Assets/Prefabs/ReySlime.prefab";
    private const string BossControllerPath = "Assets/Animation/ReySlimeTp.controller";
    private const string BossStatsPath = "Assets/Data/BossStats_ReySlime.asset";

    // Cima, cerca del portal.
    private static readonly Vector3 BossSpot = new Vector3(-30f, 10f, 30f);

    // Datos del puente calculados en BuildBridgeLink y usados después para Brutus.
    private static bool _bridgeReady;
    private static Vector3 _bridgeNear;
    private static Vector3 _bridgeAxis;
    private static float _bridgeWidth;

    // ---------- Puente ----------

    /// <summary>
    /// Une las dos orillas del puente norte con un NavMeshLink (para los enemigos)
    /// y una pasarela invisible (para el jugador). Se llama antes de ubicar cosas del otro lado.
    /// </summary>
    internal static void BuildBridgeLink(Scene scene)
    {
        _bridgeReady = false;
        DestroyByName(scene, "BridgeLink");
        DestroyByName(scene, "BridgeWalkway");

        GameObject bridge = G.FindByName(scene, BridgeName);
        GameObject player = G.Find<PlayerMovement>(scene)?.gameObject;

        if (bridge == null || player == null)
        {
            Debug.LogWarning($"No se encontró '{BridgeName}' o el Player: el puente no se conectó.");
            return;
        }

        Bounds bounds = RendererBounds(bridge);
        bool alongX = bounds.size.x >= bounds.size.z;
        Vector3 axis = alongX ? Vector3.right : Vector3.forward;
        float halfLength = (alongX ? bounds.size.x : bounds.size.z) * 0.5f;
        float width = alongX ? bounds.size.z : bounds.size.x;

        if (!TryFindBridgeEnd(bounds, axis, halfLength, out Vector3 endA) ||
            !TryFindBridgeEnd(bounds, -axis, halfLength, out Vector3 endB))
        {
            Debug.LogWarning("No se encontró piso caminable en los extremos del puente.");
            return;
        }

        // El extremo "cercano" es el que ya se alcanza desde el inicio.
        Vector3 origin = G.SampleNavMesh(player.transform.position);
        NavMeshPath path = new NavMeshPath();
        bool aReachable = G.IsReachable(origin, endA, path);

        Vector3 near = aReachable ? endA : endB;
        Vector3 far = aReachable ? endB : endA;

        GameObject linkObject = new GameObject("BridgeLink");
        SceneManager.MoveGameObjectToScene(linkObject, scene);
        linkObject.transform.position = (near + far) * 0.5f;

        NavMeshLink link = linkObject.AddComponent<NavMeshLink>();
        link.startPoint = near - linkObject.transform.position;
        link.endPoint = far - linkObject.transform.position;
        link.width = Mathf.Max(1.5f, width * 0.6f);
        link.bidirectional = true;
        link.UpdateLink();

        // Pasarela invisible: el jugador (CharacterController) no usa el NavMesh.
        GameObject walkway = new GameObject("BridgeWalkway");
        SceneManager.MoveGameObjectToScene(walkway, scene);
        walkway.transform.SetPositionAndRotation((near + far) * 0.5f - Vector3.up * 0.1f, Quaternion.LookRotation(far - near));
        walkway.AddComponent<BoxCollider>().size = new Vector3(width * 0.8f, 0.2f, Vector3.Distance(near, far));

        Vector3 flatAxis = far - near;
        flatAxis.y = 0f;

        _bridgeNear = near;
        _bridgeAxis = flatAxis.normalized;
        _bridgeWidth = width;
        _bridgeReady = true;

        G.ReachableOrigin = origin;
        bool connected = G.IsReachable(origin, far, path);
        Debug.Log($"Puente norte: {near} -> {far}. Otra orilla alcanzable: {(connected ? "SÍ" : "NO")}.");
    }

    internal static bool TryFindBridgeEnd(Bounds bounds, Vector3 direction, float halfLength, out Vector3 end)
    {
        foreach (float margin in new[] { 1.5f, 3f, 4.5f })
        {
            Vector3 probe = bounds.center + direction * (halfLength + margin);
            probe.y = bounds.min.y + 1f;

            if (NavMesh.SamplePosition(probe, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                end = hit.position;
                return true;
            }
        }

        end = Vector3.zero;
        return false;
    }

    // ---------- Cerraduras y jefe ----------

    internal static void BuildLocksAndBoss(Scene scene)
    {
        GameObject player = G.Find<PlayerMovement>(scene)?.gameObject;
        DialogueUI dialogueUI = G.Find<DialogueUI>(scene);
        LevelUI levelUI = G.Find<LevelUI>(scene);
        Portal portal = G.Find<Portal>(scene);

        if (player == null || dialogueUI == null || levelUI == null || portal == null)
        {
            Debug.LogError("Falta Player, DialogueUI, LevelUI o Portal: corré el menú 'Completar nivel'.");
            return;
        }

        GemCollector collector = player.GetComponent<GemCollector>();
        List<PathLock> locks = new List<PathLock>();

        GemGatekeeper brutus = BuildBrutus(scene, collector, dialogueUI);
        if (brutus != null) locks.Add(brutus);

        GemDoor door = BuildSummitDoor(scene, collector);
        if (door != null) locks.Add(door);

        BossSlime boss = BuildBoss(scene);

        // El portal se enciende con todas las gemas y el jefe muerto.
        G.SetReference(portal, "_guardian", boss != null ? boss.GetComponent<Health>() : null);

        SerializedObject ui = new SerializedObject(levelUI);
        SerializedProperty locksProperty = ui.FindProperty("_pathLocks");
        locksProperty.arraySize = locks.Count;

        for (int i = 0; i < locks.Count; i++)
        {
            locksProperty.GetArrayElementAtIndex(i).objectReferenceValue = locks[i];
        }

        ui.FindProperty("_boss").objectReferenceValue = boss;
        ui.ApplyModifiedPropertiesWithoutUndo();

        BuildBossBar(scene, boss);
    }

    // ---------- Brutus ----------

    private static GemGatekeeper BuildBrutus(Scene scene, GemCollector collector, DialogueUI dialogueUI)
    {
        DestroyByName(scene, "Npc_Brutus");
        DestroyByName(scene, "BridgeBlocker");

        if (!_bridgeReady)
        {
            Debug.LogWarning("El puente no se pudo conectar: Brutus no se colocó.");
            return null;
        }

        DialogueData locked = MatiasNpcBuilder.GetDialogue("Brutus_Locked", BrutusName, new[]
        {
            $"¡Alto ahí! Soy {BrutusName}, y nadie cruza mi puente sin al menos 2 gemas sagradas.",
            "Volvé cuando las tengas. Grrr.",
        });

        DialogueData open = MatiasNpcBuilder.GetDialogue("Brutus_Open", BrutusName, new[]
        {
            "Hmm... esas gemas brillan de verdad.",
            "Está bien, podés pasar. Cuidado con las aves del otro lado: tiran desde lejos.",
        });

        // Barrera invisible que cierra la entrada del puente.
        GameObject blocker = new GameObject("BridgeBlocker");
        SceneManager.MoveGameObjectToScene(blocker, scene);
        blocker.transform.SetPositionAndRotation(_bridgeNear + _bridgeAxis * 0.8f + Vector3.up * 1.5f, Quaternion.LookRotation(_bridgeAxis));

        BoxCollider wall = blocker.AddComponent<BoxCollider>();
        wall.size = new Vector3(_bridgeWidth + 1.5f, 4f, 0.6f);
        AddCarvingObstacle(blocker, wall.size);

        GameObject npc = (GameObject)PrefabUtility.InstantiatePrefab(BuildBrutusPrefab(), scene);
        npc.name = "Npc_Brutus";
        npc.transform.SetPositionAndRotation(G.SampleNavMesh(_bridgeNear - _bridgeAxis), Quaternion.LookRotation(-_bridgeAxis));

        Vector3 side = Vector3.Cross(Vector3.up, _bridgeAxis).normalized * (_bridgeWidth * 0.5f + 2f);

        GemGatekeeper gatekeeper = npc.GetComponent<GemGatekeeper>();
        SerializedObject so = new SerializedObject(gatekeeper);
        so.FindProperty("_gemCollector").objectReferenceValue = collector;
        so.FindProperty("_requiredGems").intValue = 2;
        SetArray(so.FindProperty("_blockers"), blocker);
        so.FindProperty("_navMeshLink").objectReferenceValue = G.FindByName(scene, "BridgeLink")?.GetComponent<NavMeshLink>();
        so.FindProperty("_openedMessage").stringValue = $"{BrutusName} te dejó pasar: ¡el puente norte está abierto!";
        so.FindProperty("_npcName").stringValue = BrutusName;
        so.FindProperty("_lockedDialogue").objectReferenceValue = locked;
        so.FindProperty("_openDialogue").objectReferenceValue = open;
        so.FindProperty("_dialogueUI").objectReferenceValue = dialogueUI;
        so.FindProperty("_stepAsideOffset").vector3Value = side;
        so.ApplyModifiedPropertiesWithoutUndo();

        return gatekeeper;
    }

    private static GameObject BuildBrutusPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BrutusPrefabPath);

        if (existing != null)
            return existing;

        GameObject root = new GameObject("Npc_Brutus");

        CapsuleCollider body = root.AddComponent<CapsuleCollider>();
        body.center = new Vector3(0f, 0.8f, 0f);
        body.height = 1.6f;
        body.radius = 0.6f;

        SphereCollider talkZone = root.AddComponent<SphereCollider>();
        talkZone.isTrigger = true;
        talkZone.radius = 2.5f;
        talkZone.center = new Vector3(0f, 1f, 0f);

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TurtleModelPath));
        model.transform.SetParent(root.transform, false);
        G.FitToSize(model, 1.6f);

        // Color distinto al de las tortugas enemigas, para que no se confunda.
        Tint(model, "Brutus", new Color(1f, 0.8f, 0.45f));

        Animator animator = model.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.runtimeAnimatorController = BuildTalkController(BrutusControllerPath, TurtleAnimationFolder,
                "IdleNormal_TurtleShell_Anim.fbx", "Taunt_TurtleShell_Anim.fbx");
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        AddNameLabel(root.transform, BrutusName, 2.2f);

        // No genera piso caminable al regenerar el NavMesh.
        root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        root.AddComponent<GemGatekeeper>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BrutusPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---------- Puerta de la Cima ----------

    private static GemDoor BuildSummitDoor(Scene scene, GemCollector collector)
    {
        DestroyByName(scene, "SummitDoor");

        GameObject ramp = G.FindByName(scene, SummitRampName);

        if (ramp == null)
        {
            Debug.LogWarning($"No se encontró la rampa '{SummitRampName}': no se creó la Puerta de la Cima.");
            return null;
        }

        Bounds bounds = RendererBounds(ramp);

        GameObject root = new GameObject("SummitDoor");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = bounds.center;
        root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        // Barrera de energía que cubre toda la rampa: bloquea al jugador y a los enemigos.
        GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrier.name = "Barrier";
        barrier.transform.SetParent(root.transform, true);
        barrier.transform.position = bounds.center + Vector3.up * 1.5f;
        barrier.transform.localScale = bounds.size + new Vector3(0.6f, 3f, 0.6f);

        Renderer barrierRenderer = barrier.GetComponent<Renderer>();
        barrierRenderer.sharedMaterial = GetEnergyMaterial();
        barrierRenderer.shadowCastingMode = ShadowCastingMode.Off;
        AddCarvingObstacle(barrier, Vector3.one);

        // Un hueco por gema, flotando sobre la rampa.
        Material socketMaterial = G.GetMaterial("GemSocket", Color.white, unlit: true);
        List<Renderer> sockets = new List<Renderer>();

        int gemCount = collector != null ? collector.RequiredGems : 3;

        for (int i = 0; i < gemCount; i++)
        {
            GameObject socket = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            socket.name = $"Socket_{i + 1}";
            Object.DestroyImmediate(socket.GetComponent<Collider>());
            socket.transform.SetParent(root.transform, true);
            socket.transform.position = bounds.center + Vector3.up * (bounds.extents.y + 4f) + Vector3.right * ((i - (gemCount - 1) * 0.5f) * 1.4f);
            socket.transform.localScale = Vector3.one * 0.8f;
            socket.GetComponent<Renderer>().sharedMaterial = socketMaterial;
            sockets.Add(socket.GetComponent<Renderer>());
        }

        GemDoor door = root.AddComponent<GemDoor>();
        SerializedObject so = new SerializedObject(door);
        so.FindProperty("_gemCollector").objectReferenceValue = collector;
        so.FindProperty("_requiredGems").intValue = gemCount;
        SetArray(so.FindProperty("_blockers"), barrier);
        SetArray(so.FindProperty("_sockets"), sockets.Cast<Object>().ToArray());
        so.FindProperty("_openedMessage").stringValue = "¡La Puerta de la Cima se abrió! Subí a enfrentar al Rey Slime";
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"Puerta de la Cima sobre la rampa {SummitRampName} en {bounds.center}.");
        return door;
    }

    internal static Material GetEnergyMaterial()
    {
        string path = $"{MaterialsFolder}/EnergyBarrier.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
            return material;

        EnsureMaterialsFolder();

        // Unlit transparente: se ve la rampa a través de la barrera.
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetColor("_BaseColor", new Color(0.3f, 0.9f, 1f, 0.35f));

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // ---------- Rey Slime ----------

    private static BossSlime BuildBoss(Scene scene)
    {
        DestroyByName(scene, "ReySlime");

        GameObject prefab = BuildBossPrefab(GetBossStats());
        GameObject boss = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        boss.name = "ReySlime";
        boss.transform.position = G.SampleReachable(BossSpot);

        // Balance: menos vida y ataques más espaciados que la primera versión (era demasiado difícil).
        SerializedObject bossData = new SerializedObject(boss.GetComponent<BossSlime>());
        bossData.FindProperty("_attackCooldown").floatValue = 4.5f;
        bossData.FindProperty("_moveSpeed").floatValue = 2f;
        bossData.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject health = new SerializedObject(boss.GetComponent<Health>());
        health.FindProperty("_maxHealth").floatValue = 250f;
        health.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"Rey Slime en {boss.transform.position}.");
        return boss.GetComponent<BossSlime>();
    }

    private static BossStats GetBossStats()
    {
        BossStats stats = AssetDatabase.LoadAssetAtPath<BossStats>(BossStatsPath);

        if (stats == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            stats = ScriptableObject.CreateInstance<BossStats>();
            AssetDatabase.CreateAsset(stats, BossStatsPath);
        }

        // Balance: golpe más débil, mareo más largo y fase 2 más tarde.
        SerializedObject so = new SerializedObject(stats);
        so.FindProperty("_slamDamage").floatValue = 12f;
        so.FindProperty("_dizzyDuration").floatValue = 3f;
        so.FindProperty("_enrageThreshold").floatValue = 0.4f;
        so.FindProperty("_enragedSpeedMultiplier").floatValue = 1.3f;
        so.FindProperty("_minionCount").intValue = 2;
        so.ApplyModifiedPropertiesWithoutUndo();

        return stats;
    }

    private static GameObject BuildBossPrefab(BossStats stats)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);

        if (existing != null)
            return existing;

        GameObject root = new GameObject("ReySlime");

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SlimeModelPath));
        model.transform.SetParent(root.transform, false);
        G.FitToSize(model, 3.2f);
        Tint(model, "ReySlime", new Color(0.75f, 0.45f, 1f));

        Animator animator = model.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.runtimeAnimatorController = BuildBossController();
            animator.applyRootMotion = false;
        }

        CapsuleCollider body = root.AddComponent<CapsuleCollider>();
        body.center = new Vector3(0f, 1.4f, 0f);
        body.height = 2.8f;
        body.radius = 1.3f;

        // RequireComponent agrega Health y NavMeshAgent.
        BossSlime boss = root.AddComponent<BossSlime>();
        root.AddComponent<EnemyHitFeedback>();

        NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
        agent.radius = 1.2f;
        agent.height = 2.8f;

        SerializedObject so = new SerializedObject(boss);
        so.FindProperty("_stats").objectReferenceValue = stats;
        so.FindProperty("_minionPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(MinionPrefabPath);
        so.FindProperty("_bossName").stringValue = "Rey Slime";
        so.FindProperty("_detectionRange").floatValue = 14f;
        so.FindProperty("_attackRange").floatValue = 3.2f;
        so.FindProperty("_attackCooldown").floatValue = 3.5f;
        so.FindProperty("_moveSpeed").floatValue = 2.2f;
        so.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject health = new SerializedObject(root.GetComponent<Health>());
        health.FindProperty("_maxHealth").floatValue = 350f;
        health.ApplyModifiedPropertiesWithoutUndo();

        int enemyLayer = LayerMask.NameToLayer("Enemy");

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = enemyLayer;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // Mismos parámetros que el resto de los enemigos, más Dizzy y Taunt.
    private static AnimatorController BuildBossController()
    {
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(BossControllerPath);

        if (existing != null)
            return existing;

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(BossControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dizzy", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Taunt", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idle = AddState(sm, "Idle", SlimeAnimationFolder, "IdleNormal_Slime_Anim.fbx");
        AnimatorState walk = AddState(sm, "Walk", SlimeAnimationFolder, "Walk_Slime_Anim.fbx");
        AnimatorState attack = AddState(sm, "Attack", SlimeAnimationFolder, "Attack01_Slime_Anim.fbx");
        AnimatorState dizzy = AddState(sm, "Dizzy", SlimeAnimationFolder, "Dizzy_Slime_Anim.fbx");
        AnimatorState taunt = AddState(sm, "Taunt", SlimeAnimationFolder, "Taunt_Slime_Anim.fbx");
        AnimatorState die = AddState(sm, "Die", SlimeAnimationFolder, "Die_Slime_Anim.fbx");
        sm.defaultState = idle;

        AddTransition(idle, walk, AnimatorConditionMode.If, "IsMoving");
        AddTransition(walk, idle, AnimatorConditionMode.IfNot, "IsMoving");

        foreach (AnimatorState from in new[] { idle, walk })
        {
            AddTransition(from, attack, AnimatorConditionMode.If, "Attack");
            AddTransition(from, taunt, AnimatorConditionMode.If, "Taunt");
        }

        foreach (AnimatorState from in new[] { idle, walk, attack })
        {
            AddTransition(from, dizzy, AnimatorConditionMode.If, "Dizzy");
        }

        foreach (AnimatorState from in new[] { attack, dizzy, taunt })
        {
            AnimatorStateTransition back = from.AddTransition(idle);
            back.hasExitTime = true;
            back.exitTime = 0.95f;
            back.duration = 0.15f;
        }

        AnimatorStateTransition toDie = sm.AddAnyStateTransition(die);
        toDie.AddCondition(AnimatorConditionMode.If, 0f, "Death");
        toDie.hasExitTime = false;
        toDie.canTransitionToSelf = false;
        toDie.duration = 0.1f;

        return controller;
    }

    // Idle en loop y una animación al hablar (trigger Talk).
    internal static AnimatorController BuildTalkController(string path, string folder, string idleClip, string talkClip)
    {
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

        if (existing != null)
            return existing;

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Talk", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(sm, "Idle", folder, idleClip);
        AnimatorState talk = AddState(sm, "Talk", folder, talkClip);
        sm.defaultState = idle;

        AddTransition(idle, talk, AnimatorConditionMode.If, "Talk");

        AnimatorStateTransition back = talk.AddTransition(idle);
        back.hasExitTime = true;
        back.exitTime = 0.95f;
        back.duration = 0.2f;

        return controller;
    }

    // ---------- Barra de vida del jefe ----------

    private static void BuildBossBar(Scene scene, BossSlime boss)
    {
        DestroyByName(scene, "BossUI");

        if (boss == null)
            return;

        GameObject canvasObject = new GameObject("BossUI", typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject bar = new GameObject("BossBar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(canvasObject.transform, false);
        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -70f);
        barRect.sizeDelta = new Vector2(900f, 36f);
        bar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bar.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);
        fill.GetComponent<Image>().color = new Color(0.75f, 0.2f, 0.9f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text bossName = G.CreateText("BossName", bar.transform, font, new Vector2(0.5f, 1f), new Vector2(0f, 42f), new Vector2(900f, 40f), 32, TextAnchor.MiddleCenter);

        BossHealthBar healthBar = canvasObject.AddComponent<BossHealthBar>();
        G.SetReference(healthBar, "_boss", boss);
        G.SetReference(healthBar, "_root", bar);
        G.SetReference(healthBar, "_fill", fillRect);
        G.SetReference(healthBar, "_nameText", bossName);

        bar.SetActive(false);
    }

    // ---------- Utilidades ----------

    private static AnimatorState AddState(AnimatorStateMachine sm, string name, string folder, string clipFile)
    {
        AnimatorState state = sm.AddState(name);
        state.motion = AssetDatabase.LoadAllAssetsAtPath(folder + clipFile)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        return state;
    }

    private static void AddTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string parameter)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.AddCondition(mode, 0f, parameter);
        transition.hasExitTime = false;
        transition.duration = 0.1f;
    }

    // Copia el material del modelo con otro color y se lo asigna a todos sus renderers.
    internal static void Tint(GameObject model, string materialName, Color color)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return;

        string path = $"{MaterialsFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            EnsureMaterialsFolder();
            material = new Material(renderers[0].sharedMaterial);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
        }

        foreach (Renderer renderer in renderers)
        {
            renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
        }
    }

    internal static void AddNameLabel(Transform parent, string text, float height)
    {
        GameObject label = new GameObject("NameLabel", typeof(TextMesh), typeof(Billboard));
        label.transform.SetParent(parent, false);
        label.transform.localPosition = new Vector3(0f, height, 0f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        TextMesh textMesh = label.GetComponent<TextMesh>();
        textMesh.text = text;
        textMesh.font = font;
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.05f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = new Color(1f, 0.85f, 0.4f);
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }

    internal static void AddCarvingObstacle(GameObject go, Vector3 size)
    {
        NavMeshObstacle obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = size;
        obstacle.carving = true;
    }

    internal static Bounds RendererBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(go.transform.position, Vector3.one);

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    internal static void SetArray(SerializedProperty property, params Object[] values)
    {
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void EnsureMaterialsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "Level");
    }

    private static void DestroyByName(Scene scene, string name)
    {
        GameObject found = G.FindByName(scene, name);

        if (found != null)
        {
            Object.DestroyImmediate(found);
        }
    }
}
