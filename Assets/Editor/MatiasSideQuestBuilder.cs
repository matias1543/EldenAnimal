
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using D = MatiasLevelDesignBuilder;
using G = MatiasGemLevelBuilder;

/// <summary>
/// Zonas opcionales con misiones secundarias para TestMatias.
///
/// - Usa los caminos que estaban vacíos: Bosque Suroeste, Meseta Oeste y Llanura Sur.
/// - Enemigos nuevos con assets sin usar: Monstruo Embestidor (PartyMonster) y Ave de élite (variante del Ave).
/// - NPCs Lila y Tito con misiones de eliminar monstruos (KillQuest).
/// - Recompensas: +50% de daño (Lila) y pociones grandes + curación (Tito).
///
/// Lo usa MatiasLevelContentBuilder (menú "Completar nivel").
/// </summary>
public static class MatiasSideQuestBuilder
{
    private const string PlayerPowerPath = "Assets/Data/PlayerPower.asset";
    private const string DamageRewardPath = "Assets/Data/Rewards/Reward_Lila_Damage.asset";
    private const string PotionRewardPath = "Assets/Data/Rewards/Reward_Tito_Potions.asset";
    private const string ShieldRewardPath = "Assets/Data/Rewards/Reward_Roco_Shield.asset";
    private const string PearlModelPath = "Assets/Assets importados/PixitGames/Pixit3D/PixitUnderseaCollection/LowPolyFishFree/Prefabs/PearlSeashell_Rigged_Unity.prefab";

    // Puertas (cuevas) del suroeste que llevan a la zona oeste, que no estaba conectada.
    private static readonly string[] WestGateNames = { "Gate03_1", "Gate01_1" };

    // Puente sur: lo custodia Kiko hasta eliminar a los monstruos de la llanura.
    private const string SouthBridgeName = "Bridge06_1";
    private const string TinyBirdFolder = "Assets/Assets importados/TinyBirdDuo/";
    private const string KikoControllerPath = "Assets/Animation/NpcKikoTp.controller";

    // Zona oeste grande: lugares de referencia según el mapa de caminos (x, z).
    private static readonly Vector3[] WestAnchorSpots =
    {
        new Vector3(-46f, 0f, -8f), new Vector3(-48f, 0f, 4f), new Vector3(-48f, 0f, 16f),
        new Vector3(-46f, 0f, 28f), new Vector3(-50f, 0f, 38f),
    };

    // Datos de la cueva calculados en BuildWestGate.
    private static bool _westReady;
    private static Vector3 _westNear;
    private static Vector3 _westFar;

    // Un punto bien adentro de la zona oeste (para la gema, los enemigos y la perla).
    private static Vector3 _westDeep;
    private const string BigPotionPath = "Assets/Data/Potion_Big.asset";

    private const string PartyMonsterFolder = "Assets/Assets importados/PartyMonsterDuo/";
    private const string ChargePrefabPath = "Assets/Prefabs/ChargeMonster.prefab";
    private const string ChargeControllerPath = "Assets/Animation/ChargeMonsterTp.controller";
    private const string EliteBirdPrefabPath = "Assets/Prefabs/BirdElite.prefab";
    private const string NpcControllerPath = "Assets/Animation/NpcTp.controller";

    private const string SlimePrefabPath = "Assets/Prefabs/Slime.prefab";
    private const string CrabPrefabPath = "Assets/Prefabs/CRAB.prefab";
    private const string TurtlePrefabPath = "Assets/Prefabs/Turtle.prefab";
    private const string BirdPrefabPath = "Assets/Prefabs/Bird.prefab";

    private enum Kind { Slime, Crab, Turtle, Bird, EliteBird, Charge }

    // Zonas que antes no se usaban.
    private static readonly (string name, Vector3 position, Kind[] enemies)[] SideGroups =
    {
        ("Bosque_Suroeste", new Vector3(-36f, 0f, -38f), new[] { Kind.Charge, Kind.Charge, Kind.Slime, Kind.Slime, Kind.Crab, Kind.Crab }),
        ("Meseta_Oeste",    new Vector3(-30f, 5f,  -6f), new[] { Kind.EliteBird, Kind.EliteBird, Kind.Charge, Kind.Turtle }),
        ("Llanura_Sur",     new Vector3(0f,   0f, -40f), new[] { Kind.Charge, Kind.Slime, Kind.Bird }),
    };

    // ---------- Cueva del Oeste ----------

    /// <summary>
    /// Conecta la zona oeste (caminable pero aislada) a través de una de las cuevas del suroeste,
    /// con un NavMeshLink (enemigos) y una pasarela invisible (jugador), igual que el puente norte.
    /// Se llama antes de ubicar cosas del otro lado.
    /// </summary>
    internal static void BuildWestGate(Scene scene)
    {
        _westReady = false;
        DestroyByName(scene, "WestGateLink");
        DestroyByName(scene, "WestGateWalkway");

        GameObject player = G.Find<PlayerMovement>(scene)?.gameObject;

        if (player == null)
            return;

        Vector3 origin = G.SampleNavMesh(player.transform.position);
        G.ReachableOrigin = origin;

        // Puntos de referencia dentro de la zona oeste grande (según el mapa de caminos).
        List<Vector3> westAnchors = FindWestAnchors(origin);

        if (westAnchors.Count == 0)
        {
            Debug.LogWarning("No se encontró la zona oeste en el NavMesh.");
            return;
        }

        NavMeshPath path = new NavMeshPath();

        foreach (string gateName in WestGateNames)
        {
            GameObject gate = G.FindByName(scene, gateName);

            if (gate == null)
                continue;

            // Probamos las mejores combinaciones entrada/salida hasta que una conecte de verdad la zona oeste.
            foreach ((Vector3 near, Vector3 far) in FindCrossings(D.RendererBounds(gate).center, origin, westAnchors).Take(15))
            {
                GameObject linkObject = CreateWestLink(scene, near, far);

                Vector3 anchor = westAnchors.FirstOrDefault(a => G.IsReachable(origin, a, path));

                if (anchor == Vector3.zero)
                {
                    Object.DestroyImmediate(linkObject);
                    continue;
                }

                GameObject walkway = new GameObject("WestGateWalkway");
                SceneManager.MoveGameObjectToScene(walkway, scene);
                walkway.transform.SetPositionAndRotation((near + far) * 0.5f - Vector3.up * 0.1f, Quaternion.LookRotation(far - near));
                walkway.AddComponent<BoxCollider>().size = new Vector3(2.5f, 0.2f, Vector3.Distance(near, far));

                _westNear = near;
                _westFar = far;
                _westDeep = anchor;
                _westReady = true;

                Debug.Log($"Cueva del Oeste ({gateName}): {near} -> {far}. Zona oeste alcanzable: SÍ (hasta {anchor}).");
                return;
            }
        }

        Debug.LogWarning("No se pudo conectar la zona oeste por ninguna cueva.");
    }

    private static GameObject CreateWestLink(Scene scene, Vector3 near, Vector3 far)
    {
        Vector3 middle = (near + far) * 0.5f;

        GameObject linkObject = new GameObject("WestGateLink");
        SceneManager.MoveGameObjectToScene(linkObject, scene);
        linkObject.transform.position = middle;

        NavMeshLink link = linkObject.AddComponent<NavMeshLink>();
        link.startPoint = near - middle;
        link.endPoint = far - middle;
        link.width = 2f;
        link.bidirectional = true;
        link.UpdateLink();

        return linkObject;
    }

    // Puntos caminables de la zona oeste que hoy NO se alcanzan desde el inicio.
    private static List<Vector3> FindWestAnchors(Vector3 origin)
    {
        List<Vector3> anchors = new List<Vector3>();
        NavMeshPath path = new NavMeshPath();

        foreach (Vector3 spot in WestAnchorSpots)
        {
            foreach (float y in new[] { 0f, 2.5f, 5f, 7.5f, -2.5f })
            {
                if (NavMesh.SamplePosition(new Vector3(spot.x, y + 1f, spot.z), out NavMeshHit hit, 2f, NavMesh.AllAreas) &&
                    hit.position.y > -4f && !G.IsReachable(origin, hit.position, path))
                {
                    anchors.Add(hit.position);
                    break;
                }
            }
        }

        return anchors;
    }

    // Pares (punto alcanzable, punto aislado conectado a la zona oeste) cerca de la cueva, ordenados del mejor al peor.
    private static IEnumerable<(Vector3 near, Vector3 far)> FindCrossings(Vector3 center, Vector3 origin, List<Vector3> westAnchors)
    {
        List<Vector3> reachable = new List<Vector3>();
        List<Vector3> isolated = new List<Vector3>();
        NavMeshPath path = new NavMeshPath();

        for (float radius = 1.5f; radius <= 16f; radius += 1.5f)
        {
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16f;
                Vector3 probe = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

                foreach (float dy in new[] { 0f, 2.5f, -2.5f, 5f })
                {
                    if (!NavMesh.SamplePosition(probe + Vector3.up * (dy + 1f), out NavMeshHit hit, 1.2f, NavMesh.AllAreas))
                        continue;

                    // Ignoramos el fondo del mar.
                    if (hit.position.y < -4f)
                        continue;

                    if (G.IsReachable(origin, hit.position, path))
                    {
                        reachable.Add(hit.position);
                    }
                    else if (westAnchors.Any(a => G.IsReachable(hit.position, a, path)))
                    {
                        // Solo sirven los puntos aislados que pertenecen a la zona oeste grande.
                        isolated.Add(hit.position);
                    }
                }
            }
        }

        return reachable
            .SelectMany(a => isolated.Select(b => (near: a, far: b)))
            .Where(p => Vector3.Distance(p.near, p.far) <= 14f && Mathf.Abs(p.near.y - p.far.y) <= 3f)
            .OrderBy(p => Vector3.Distance(p.near, center) + Vector3.Distance(p.far, center) + Vector3.Distance(p.near, p.far));
    }

    // Dirección horizontal desde la entrada de la cueva hacia la zona oeste.
    private static Vector3 WestDirection()
    {
        Vector3 direction = _westFar - _westNear;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.left;
    }

    /// <summary>
    /// Lugar del cofre de la 4.ª gema, en la zona oeste (si la cueva quedó conectada).
    /// Lo usa MatiasGemLevelBuilder al ubicar los cofres.
    /// </summary>
    internal static bool TryGetWestChestSpot(out Vector3 spot)
    {
        spot = _westReady ? _westDeep : Vector3.zero;
        return _westReady;
    }

    // ---------- Enemigos ----------

    internal static void SpawnSideEnemies(Scene scene)
    {
        DestroyByName(scene, "SideEnemies");

        Dictionary<Kind, GameObject> prefabs = new Dictionary<Kind, GameObject>
        {
            [Kind.Slime] = AssetDatabase.LoadAssetAtPath<GameObject>(SlimePrefabPath),
            [Kind.Crab] = AssetDatabase.LoadAssetAtPath<GameObject>(CrabPrefabPath),
            [Kind.Turtle] = AssetDatabase.LoadAssetAtPath<GameObject>(TurtlePrefabPath),
            [Kind.Bird] = AssetDatabase.LoadAssetAtPath<GameObject>(BirdPrefabPath),
            [Kind.EliteBird] = BuildEliteBirdPrefab(),
            [Kind.Charge] = BuildChargePrefab(),
        };

        GameObject root = new GameObject("SideEnemies");
        SceneManager.MoveGameObjectToScene(root, scene);
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        List<(string name, Vector3 position, Kind[] enemies)> groups = SideGroups.ToList();

        // Zona oeste: solo si la cueva quedó conectada.
        if (_westReady)
        {
            groups.Add(("Bosque_Oeste", Vector3.Lerp(_westFar, _westDeep, 0.5f),
                new[] { Kind.Charge, Kind.Charge, Kind.EliteBird, Kind.Turtle, Kind.Slime }));
        }

        foreach ((string groupName, Vector3 approx, Kind[] enemies) in groups)
        {
            Transform group = new GameObject(groupName).transform;
            group.SetParent(root.transform, false);

            Vector3 center = G.SampleReachable(approx);

            for (int i = 0; i < enemies.Length; i++)
            {
                GameObject prefab = prefabs[enemies[i]];

                if (prefab == null)
                    continue;

                GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                enemy.name = $"{enemies[i]}_{groupName}_{i}";
                enemy.transform.SetParent(group, true);

                float angle = i * Mathf.PI * 2f / enemies.Length;
                enemy.transform.position = G.SampleReachable(center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 3.5f);

                foreach (Transform t in enemy.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = enemyLayer;
                }

                G.GetOrAdd<EnemyHitFeedback>(enemy);
            }

            Debug.Log($"Zona {groupName}: {enemies.Length} enemigos en {center}.");
        }
    }

    // ---------- Misiones ----------

    internal static void BuildSideQuests(Scene scene)
    {
        GameObject player = G.Find<PlayerMovement>(scene)?.gameObject;
        DialogueUI dialogueUI = G.Find<DialogueUI>(scene);
        LevelUI levelUI = G.Find<LevelUI>(scene);
        LevelFlow levelFlow = G.Find<LevelFlow>(scene);

        if (player == null || dialogueUI == null || levelUI == null || levelFlow == null)
        {
            Debug.LogError("Falta Player, DialogueUI, LevelUI o LevelFlow: no se armaron las misiones secundarias.");
            return;
        }

        // Poder del jugador: lo suben las recompensas y lo leen los enemigos.
        PlayerPowerData power = GetAsset<PlayerPowerData>(PlayerPowerPath);
        G.SetReference(levelFlow, "_playerPower", power);

        foreach (EnemyTemplate enemy in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemyTemplate>(true)))
        {
            G.SetReference(G.GetOrAdd<PlayerPowerModifier>(enemy.gameObject), "_playerPower", power);
        }

        // Los slimes que invoca el jefe salen del prefab.
        AddPowerModifierToPrefab(SlimePrefabPath, power);

        // Recompensas (ScriptableObjects).
        DamageUpgradeReward damageReward = GetAsset<DamageUpgradeReward>(DamageRewardPath);
        SetFields(damageReward, so =>
        {
            so.FindProperty("_description").stringValue = "Tu arma está afilada: +50% de daño";
            so.FindProperty("_playerPower").objectReferenceValue = power;
            so.FindProperty("_damageBonus").floatValue = 0.5f;
        });

        PotionReward potionReward = GetAsset<PotionReward>(PotionRewardPath);
        SetFields(potionReward, so =>
        {
            so.FindProperty("_description").stringValue = "Recibiste 2 pociones grandes y te curaste por completo";
            so.FindProperty("_potion").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PotionData>(BigPotionPath);
            so.FindProperty("_amount").intValue = 2;
            so.FindProperty("_healFully").boolValue = true;
        });

        KillQuest lilaQuest = BuildKillQuest(scene, "Quest_Lila", "Limpiá el bosque suroeste", "Bosque_Suroeste", damageReward, player);
        KillQuest titoQuest = BuildKillQuest(scene, "Quest_Tito", "Echá a los monstruos de la meseta oeste", "Meseta_Oeste", potionReward, player);

        PlaceQuestNpc(scene, "Npc_Lila", "Lila", PartyMonsterFolder + "Prefab/P02.prefab", null, new Vector3(10f, 0f, -36f), lilaQuest, dialogueUI, new[]
        {
            "¡Ey! Soy Lila, cazadora de esta isla.",
            "Unos monstruos embestidores invadieron el bosque del suroeste. ¡Te persiguen y te embisten a toda velocidad!",
            "Si los eliminás a todos, te afilo el arma: vas a pegar un 50% más fuerte. Contra el Rey Slime te va a venir bárbaro.",
        }, new[]
        {
            "Todavía quedan monstruos en el bosque del suroeste. ¡Fijate en el mapa con M!",
        }, new[]
        {
            "¡Bien hecho! Tu arma ya está afilada. Andá a darle su merecido al Rey Slime.",
        });

        PlaceQuestNpc(scene, "Npc_Tito", "Tito", PartyMonsterFolder + "Prefab/P01.prefab", new Color(0.55f, 1f, 0.55f), new Vector3(-14f, 0f, -24f), titoQuest, dialogueUI, new[]
        {
            "Hola... soy Tito, el alquimista de la isla.",
            "Unas aves salvajes y un monstruo tomaron mi laboratorio en la meseta del oeste.",
            "Si los echás, te doy dos pociones grandes y te curo por completo.",
        }, new[]
        {
            "Siguen en la meseta del oeste... ¡ojo con las aves rojas, disparan rápido!",
        }, new[]
        {
            "¡Gracias! Ya tenés tus pociones. ¡Mucha suerte allá arriba!",
        });

        List<Quest> sideQuests = new List<Quest> { lilaQuest, titoQuest };
        List<PathLock> newLocks = new List<PathLock>();

        FetchQuest rocoQuest = BuildRocoQuest(scene, player, dialogueUI);

        if (rocoQuest != null)
        {
            sideQuests.Add(rocoQuest);
        }

        // Puente sur: Kiko lo abre al eliminar a los monstruos de la llanura.
        (KillQuest kikoQuest, QuestLock southLock) = BuildSouthBridgeGate(scene, dialogueUI);

        if (kikoQuest != null)
        {
            sideQuests.Insert(0, kikoQuest);
            newLocks.Add(southLock);
        }

        // Cueva del oeste: barrera que se abre con 1 gema.
        GemDoor caveDoor = BuildCaveDoor(scene, player.GetComponent<GemCollector>());

        if (caveDoor != null)
        {
            newLocks.Add(caveDoor);
        }

        // UI: lista de misiones secundarias debajo de las pociones.
        Text sideQuestsText = G.CreateText("SideQuestsText", levelUI.transform, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
            new Vector2(1f, 1f), new Vector2(-40f, -160f), new Vector2(700f, 150f), 28, TextAnchor.UpperRight);

        SerializedObject ui = new SerializedObject(levelUI);
        SerializedProperty quests = ui.FindProperty("_sideQuests");
        quests.arraySize = sideQuests.Count;

        for (int i = 0; i < sideQuests.Count; i++)
        {
            quests.GetArrayElementAtIndex(i).objectReferenceValue = sideQuests[i];
        }

        ui.FindProperty("_sideQuestsText").objectReferenceValue = sideQuestsText;

        // Se suman a las cerraduras que ya cargó MatiasLevelDesignBuilder (Brutus y la Puerta de la Cima).
        SerializedProperty locks = ui.FindProperty("_pathLocks");

        foreach (PathLock pathLock in newLocks)
        {
            locks.arraySize++;
            locks.GetArrayElementAtIndex(locks.arraySize - 1).objectReferenceValue = pathLock;
        }

        ui.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Puente sur (Kiko) ----------

    private static (KillQuest, QuestLock) BuildSouthBridgeGate(Scene scene, DialogueUI dialogueUI)
    {
        DestroyByName(scene, "SouthBridgeLock");
        DestroyByName(scene, "SouthBridgeBlocker");
        DestroyByName(scene, "Quest_Kiko");
        DestroyByName(scene, "Npc_Kiko");

        GameObject bridge = G.FindByName(scene, SouthBridgeName);

        if (bridge == null || G.ReachableOrigin == null)
        {
            Debug.LogWarning($"No se encontró '{SouthBridgeName}': el puente sur no se bloqueó.");
            return (null, null);
        }

        Bounds bounds = D.RendererBounds(bridge);
        bool alongX = bounds.size.x >= bounds.size.z;
        Vector3 axis = alongX ? Vector3.right : Vector3.forward;
        float halfLength = (alongX ? bounds.size.x : bounds.size.z) * 0.5f;
        float width = alongX ? bounds.size.z : bounds.size.x;

        if (!D.TryFindBridgeEnd(bounds, axis, halfLength, out Vector3 endA) ||
            !D.TryFindBridgeEnd(bounds, -axis, halfLength, out Vector3 endB))
        {
            Debug.LogWarning("No se encontró piso en los extremos del puente sur.");
            return (null, null);
        }

        // El extremo "cercano" es el del lado del jugador (camino más corto desde el inicio).
        Vector3 origin = G.ReachableOrigin.Value;
        Vector3 near = PathLength(origin, endA) <= PathLength(origin, endB) ? endA : endB;
        Vector3 far = near == endA ? endB : endA;

        Vector3 direction = far - near;
        direction.y = 0f;
        direction.Normalize();

        // Barrera invisible en la entrada del puente.
        GameObject blocker = new GameObject("SouthBridgeBlocker");
        SceneManager.MoveGameObjectToScene(blocker, scene);
        blocker.transform.SetPositionAndRotation(near + direction * 0.8f + Vector3.up * 1.5f, Quaternion.LookRotation(direction));
        BoxCollider wall = blocker.AddComponent<BoxCollider>();
        wall.size = new Vector3(width + 1.5f, 4f, 0.6f);
        D.AddCarvingObstacle(blocker, wall.size);

        // Misión de Kiko: eliminar a los monstruos de la llanura sur (del lado del jugador).
        Health[] targets = G.FindByName(scene, "Llanura_Sur")?.GetComponentsInChildren<Health>(true) ?? new Health[0];

        GameObject questObject = new GameObject("Quest_Kiko");
        SceneManager.MoveGameObjectToScene(questObject, scene);
        KillQuest quest = questObject.AddComponent<KillQuest>();

        SetFields(quest, so =>
        {
            so.FindProperty("_title").stringValue = "Eliminá a los monstruos de la llanura sur";
            D.SetArray(so.FindProperty("_targets"), targets.Cast<Object>().ToArray());
        });

        GameObject lockObject = new GameObject("SouthBridgeLock");
        SceneManager.MoveGameObjectToScene(lockObject, scene);
        QuestLock questLock = lockObject.AddComponent<QuestLock>();

        SetFields(questLock, so =>
        {
            so.FindProperty("_quest").objectReferenceValue = quest;
            D.SetArray(so.FindProperty("_blockers"), blocker);
            so.FindProperty("_openedMessage").stringValue = "Kiko abrió el puente sur: ¡podés cruzar al bosque!";
        });

        // Kiko espera al costado de la entrada.
        Vector3 side = Vector3.Cross(Vector3.up, direction) * (width * 0.5f + 1.8f);
        RuntimeAnimatorController kikoController = D.BuildTalkController(KikoControllerPath, TinyBirdFolder + "Animation/",
            "Idle/IdleOneWeapon.fbx", "Idle/LookAroundOneWeapon.fbx");

        PlaceQuestNpc(scene, "Npc_Kiko", "Kiko", TinyBirdFolder + "Prefab/Character/C03.prefab", new Color(0.6f, 0.8f, 1f),
            near - direction * 1.2f + side, quest, dialogueUI, new[]
            {
                "¡Alto! Soy Kiko, guardia del puente sur.",
                $"Unos monstruos rondan la llanura. Eliminá a los {targets.Length} y te abro el paso al bosque.",
            }, new[]
            {
                "Todavía quedan monstruos en la llanura. ¡Volvé cuando no quede ninguno!",
            }, new[]
            {
                "¡Buen trabajo! El puente sur está abierto. Del otro lado te espera Lila.",
            }, kikoController);

        // Si el NavMesh encuentra otro camino al otro lado, el bloqueo se puede esquivar: lo avisamos.
        NavMeshPath path = new NavMeshPath();
        bool usesBridge = NavMesh.CalculatePath(origin, far, NavMesh.AllAreas, path) &&
                          path.corners.Any(c => bounds.Contains(new Vector3(c.x, bounds.center.y, c.z)));

        Debug.Log($"Puente sur bloqueado por Kiko ({targets.Length} monstruos). " +
                  (usesBridge ? "El camino al bosque pasa por el puente." : "ATENCIÓN: hay otro camino que esquiva el puente."));

        return (quest, questLock);
    }

    private static float PathLength(Vector3 from, Vector3 to)
    {
        NavMeshPath path = new NavMeshPath();

        if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
            return float.MaxValue;

        float length = 0f;

        for (int i = 1; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return length;
    }

    // ---------- Cueva del oeste ----------

    private static GemDoor BuildCaveDoor(Scene scene, GemCollector collector)
    {
        DestroyByName(scene, "CaveDoor");

        if (!_westReady)
            return null;

        Vector3 direction = WestDirection();

        GameObject root = new GameObject("CaveDoor");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.SetPositionAndRotation(_westNear + direction, Quaternion.LookRotation(direction));
        root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        // Barrera de energía en la entrada de la cueva.
        GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrier.name = "Barrier";
        barrier.transform.SetParent(root.transform, false);
        barrier.transform.localPosition = new Vector3(0f, 2f, 0f);
        barrier.transform.localScale = new Vector3(4f, 4f, 0.8f);

        Renderer barrierRenderer = barrier.GetComponent<Renderer>();
        barrierRenderer.sharedMaterial = D.GetEnergyMaterial();
        barrierRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        D.AddCarvingObstacle(barrier, Vector3.one);

        // Un hueco: se enciende con la primera gema.
        GameObject socket = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        socket.name = "Socket_1";
        Object.DestroyImmediate(socket.GetComponent<Collider>());
        socket.transform.SetParent(root.transform, false);
        socket.transform.localPosition = new Vector3(0f, 4.8f, 0f);
        socket.transform.localScale = Vector3.one * 0.8f;
        socket.GetComponent<Renderer>().sharedMaterial = G.GetMaterial("GemSocket", Color.white, unlit: true);

        GemDoor door = root.AddComponent<GemDoor>();
        SetFields(door, so =>
        {
            so.FindProperty("_gemCollector").objectReferenceValue = collector;
            so.FindProperty("_requiredGems").intValue = 1;
            D.SetArray(so.FindProperty("_blockers"), barrier);
            D.SetArray(so.FindProperty("_sockets"), socket.GetComponent<Renderer>());
            so.FindProperty("_navMeshLink").objectReferenceValue = G.FindByName(scene, "WestGateLink")?.GetComponent<NavMeshLink>();
            so.FindProperty("_openedMessage").stringValue = "¡La cueva del oeste se abrió con el brillo de tu gema!";
        });

        Debug.Log($"Cueva del oeste bloqueada (1 gema) en {root.transform.position}.");
        return door;
    }

    // Roco, en la entrada de la cueva, pide la Perla del Oeste (misión de búsqueda).
    private static FetchQuest BuildRocoQuest(Scene scene, GameObject player, DialogueUI dialogueUI)
    {
        DestroyByName(scene, "Perla_Oeste");
        DestroyByName(scene, "Quest_Roco");
        DestroyByName(scene, "Npc_Roco");

        if (!_westReady)
        {
            Debug.LogWarning("La zona oeste no está conectada: no se creó la misión de Roco.");
            return null;
        }

        ShieldUpgradeReward reward = GetAsset<ShieldUpgradeReward>(ShieldRewardPath);
        SetFields(reward, so =>
        {
            so.FindProperty("_description").stringValue = "Escudo reforzado: bloquea 95% del daño y gasta menos stamina";
            so.FindProperty("_damageReduction").floatValue = 0.95f;
            so.FindProperty("_staminaPerBlock").floatValue = 8f;
        });

        QuestItem pearl = BuildPearl(scene, G.SampleReachable(_westDeep + Vector3.forward * 10f));

        GameObject questObject = new GameObject("Quest_Roco");
        SceneManager.MoveGameObjectToScene(questObject, scene);
        FetchQuest quest = questObject.AddComponent<FetchQuest>();

        SetFields(quest, so =>
        {
            so.FindProperty("_title").stringValue = "Recuperá la Perla del Oeste";
            so.FindProperty("_reward").objectReferenceValue = reward;
            so.FindProperty("_rewardReceiver").objectReferenceValue = player;
            so.FindProperty("_item").objectReferenceValue = pearl;
        });

        // Roco espera al costado de la entrada, del lado alcanzable.
        Vector3 side = Vector3.Cross(Vector3.up, WestDirection()) * 2.5f;

        PlaceQuestNpc(scene, "Npc_Roco", "Roco", PartyMonsterFolder + "Prefab/C01.prefab", new Color(0.55f, 0.75f, 1f),
            _westNear + side - WestDirection() * 1.5f, quest, dialogueUI, new[]
            {
                "¡Hola! Soy Roco, el minero. Esta cueva lleva al bosque del oeste.",
                "Ahí perdí mi Perla del Oeste cuando me atacaron unos monstruos embestidores.",
                "Si me la traés, te refuerzo el escudo: vas a bloquear casi todo el daño y cansarte mucho menos.",
            }, new[]
            {
                "La perla brilla: la vas a ver del otro lado de la cueva. ¡Cuidado con los monstruos!",
            }, new[]
            {
                "¡Mi perla! Gracias, amigo. Tu escudo ya está reforzado: probalo contra el Rey Slime.",
            });

        return quest;
    }

    private static QuestItem BuildPearl(Scene scene, Vector3 position)
    {
        GameObject root = new GameObject("Perla_Oeste");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = position;

        SphereCollider pickupZone = root.AddComponent<SphereCollider>();
        pickupZone.isTrigger = true;
        pickupZone.radius = 1.2f;
        pickupZone.center = new Vector3(0f, 0.8f, 0f);

        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);
        visual.localPosition = new Vector3(0f, 0.8f, 0f);

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PearlModelPath));
        model.transform.SetParent(visual, false);
        G.FitToSize(model, 1f);

        // Material propio de URP, para que se vea bien sin depender del material original.
        Material pearlMaterial = G.GetMaterial("Pearl", new Color(1f, 0.9f, 0.95f));

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterials = Enumerable.Repeat(pearlMaterial, renderer.sharedMaterials.Length).ToArray();
        }

        foreach (Collider collider in model.GetComponentsInChildren<Collider>())
        {
            Object.DestroyImmediate(collider);
        }

        // Brillo para encontrarla.
        Light glow = new GameObject("Glow").AddComponent<Light>();
        glow.transform.SetParent(visual, false);
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.85f, 0.95f);
        glow.range = 6f;
        glow.intensity = 3f;

        QuestItem item = root.AddComponent<QuestItem>();
        SetFields(item, so =>
        {
            so.FindProperty("_displayName").stringValue = "Perla del Oeste";
            so.FindProperty("_visual").objectReferenceValue = visual;
        });

        // Ícono en el minimapa.
        int minimapLayer = LayerMask.NameToLayer("Minimap");

        if (minimapLayer >= 0)
        {
            MatiasLevelContentBuilder.AddMarker(root.transform, G.GetMaterial("MarkerQuestItem", new Color(1f, 0.5f, 0.9f), unlit: true), 4f, minimapLayer);
        }

        Debug.Log($"Perla del Oeste en {position}.");
        return item;
    }

    private static KillQuest BuildKillQuest(Scene scene, string objectName, string title, string groupName, QuestReward reward, GameObject player)
    {
        DestroyByName(scene, objectName);

        GameObject questObject = new GameObject(objectName);
        SceneManager.MoveGameObjectToScene(questObject, scene);
        KillQuest quest = questObject.AddComponent<KillQuest>();

        Health[] targets = G.FindByName(scene, groupName)?.GetComponentsInChildren<Health>(true) ?? new Health[0];

        SetFields(quest, so =>
        {
            so.FindProperty("_title").stringValue = title;
            so.FindProperty("_reward").objectReferenceValue = reward;
            so.FindProperty("_rewardReceiver").objectReferenceValue = player;

            SerializedProperty targetsProperty = so.FindProperty("_targets");
            targetsProperty.arraySize = targets.Length;

            for (int i = 0; i < targets.Length; i++)
            {
                targetsProperty.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            }
        });

        return quest;
    }

    private static void PlaceQuestNpc(Scene scene, string objectName, string npcName, string model, Color? tint, Vector3 approx,
        Quest quest, DialogueUI dialogueUI, string[] intro, string[] inProgress, string[] completed, RuntimeAnimatorController controller = null)
    {
        DestroyByName(scene, objectName);

        GameObject prefab = BuildQuestNpcPrefab($"Assets/Prefabs/{objectName}.prefab", objectName, npcName, model, tint, controller);
        GameObject npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        npc.name = objectName;

        Vector3 position = G.SampleReachable(approx);
        Vector3 toOrigin = (G.ReachableOrigin ?? position) - position;
        toOrigin.y = 0f;
        npc.transform.SetPositionAndRotation(position, toOrigin.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toOrigin) : Quaternion.identity);

        SetFields(npc.GetComponent<NpcDialogue>(), so =>
        {
            so.FindProperty("_npcName").stringValue = npcName;
            so.FindProperty("_questIntro").objectReferenceValue = MatiasNpcBuilder.GetDialogue($"{npcName}_Intro", npcName, intro);
            so.FindProperty("_questInProgress").objectReferenceValue = MatiasNpcBuilder.GetDialogue($"{npcName}_InProgress", npcName, inProgress);
            so.FindProperty("_questCompleted").objectReferenceValue = MatiasNpcBuilder.GetDialogue($"{npcName}_Completed", npcName, completed);
            so.FindProperty("_quest").objectReferenceValue = quest;
            so.FindProperty("_dialogueUI").objectReferenceValue = dialogueUI;
        });

        Debug.Log($"NPC {npcName} en {position}.");
    }

    private static GameObject BuildQuestNpcPrefab(string path, string rootName, string displayName, string modelPath, Color? tint, RuntimeAnimatorController controller)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (existing != null)
            return existing;

        GameObject root = new GameObject(rootName);

        CapsuleCollider body = root.AddComponent<CapsuleCollider>();
        body.center = new Vector3(0f, 0.8f, 0f);
        body.height = 1.6f;
        body.radius = 0.5f;

        SphereCollider talkZone = root.AddComponent<SphereCollider>();
        talkZone.isTrigger = true;
        talkZone.radius = 2.5f;
        talkZone.center = new Vector3(0f, 1f, 0f);

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
        model.transform.SetParent(root.transform, false);
        G.FitToSize(model, 1.6f);

        if (tint.HasValue)
        {
            D.Tint(model, rootName, tint.Value);
        }

        Animator animator = model.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.runtimeAnimatorController = controller != null ? controller : AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(NpcControllerPath);
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        D.AddNameLabel(root.transform, displayName, 1.95f);

        // "!" amarillo: tiene una misión para dar.
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject marker = new GameObject("QuestMarker", typeof(TextMesh), typeof(Billboard));
        marker.transform.SetParent(root.transform, false);
        marker.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        TextMesh text = marker.GetComponent<TextMesh>();
        text.text = "!";
        text.font = font;
        text.fontSize = 64;
        text.characterSize = 0.08f;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.85f, 0.1f);
        marker.GetComponent<MeshRenderer>().sharedMaterial = font.material;

        root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        NpcDialogue npc = root.AddComponent<NpcDialogue>();
        G.SetReference(npc, "_questMarker", marker);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---------- Enemigos nuevos ----------

    // Monstruo Embestidor: modelo PartyMonster C02 con ChargeEnemy.
    private static GameObject BuildChargePrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ChargePrefabPath);

        if (existing != null)
            return existing;

        GameObject root = new GameObject("ChargeMonster");

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PartyMonsterFolder + "Prefab/C02.prefab"));
        model.transform.SetParent(root.transform, false);
        G.FitToSize(model, 1.5f);
        D.Tint(model, "ChargeMonster", new Color(1f, 0.55f, 0.5f));

        Animator animator = model.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.runtimeAnimatorController = BuildChargeController();
            animator.applyRootMotion = false;
        }

        CapsuleCollider body = root.AddComponent<CapsuleCollider>();
        body.center = new Vector3(0f, 0.75f, 0f);
        body.height = 1.5f;
        body.radius = 0.5f;

        // RequireComponent agrega Health y NavMeshAgent.
        ChargeEnemy charge = root.AddComponent<ChargeEnemy>();
        root.AddComponent<EnemyHitFeedback>();

        NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
        agent.radius = 0.5f;
        agent.height = 1.5f;

        SetFields(charge, so =>
        {
            so.FindProperty("_detectionRange").floatValue = 11f;
            so.FindProperty("_attackRange").floatValue = 1.8f;
            so.FindProperty("_attackCooldown").floatValue = 1.4f;
            so.FindProperty("_moveSpeed").floatValue = 3f;
            so.FindProperty("_attackDamage").floatValue = 10f;
        });

        SetFields(root.GetComponent<Health>(), so => so.FindProperty("_maxHealth").floatValue = 60f);

        int enemyLayer = LayerMask.NameToLayer("Enemy");

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = enemyLayer;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ChargePrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static AnimatorController BuildChargeController()
    {
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ChargeControllerPath);

        if (existing != null)
            return existing;

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ChargeControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Charge", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(sm, "Idle", "Idle03.fbx");
        AnimatorState run = AddState(sm, "Run", "Run01FWD.fbx");
        AnimatorState attack = AddState(sm, "Attack", "Attack01.fbx");
        AnimatorState charge = AddState(sm, "Charge", "Slide.fbx");
        AnimatorState die = AddState(sm, "Die", "Die01.fbx");
        sm.defaultState = idle;

        AddTransition(idle, run, AnimatorConditionMode.If, "IsMoving");
        AddTransition(run, idle, AnimatorConditionMode.IfNot, "IsMoving");

        foreach (AnimatorState from in new[] { idle, run })
        {
            AddTransition(from, attack, AnimatorConditionMode.If, "Attack");
            AddTransition(from, charge, AnimatorConditionMode.If, "Charge");
        }

        foreach (AnimatorState from in new[] { attack, charge })
        {
            AnimatorStateTransition back = from.AddTransition(idle);
            back.hasExitTime = true;
            back.exitTime = 0.9f;
            back.duration = 0.1f;
        }

        AnimatorStateTransition toDie = sm.AddAnyStateTransition(die);
        toDie.AddCondition(AnimatorConditionMode.If, 0f, "Death");
        toDie.hasExitTime = false;
        toDie.canTransitionToSelf = false;
        toDie.duration = 0.1f;

        return controller;
    }

    // Ave de élite: variante del prefab Ave (Prefab Variant) más grande, roja, con más vida y que dispara más seguido.
    private static GameObject BuildEliteBirdPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(EliteBirdPrefabPath);

        if (existing != null)
            return existing;

        GameObject baseBird = AssetDatabase.LoadAssetAtPath<GameObject>(BirdPrefabPath);

        if (baseBird == null)
            return null;

        GameObject elite = (GameObject)PrefabUtility.InstantiatePrefab(baseBird);
        elite.name = "BirdElite";
        elite.transform.localScale = Vector3.one * 1.25f;
        D.Tint(elite, "BirdElite", new Color(1f, 0.35f, 0.35f));

        SetFields(elite.GetComponent<BirdEnemy>(), so =>
        {
            so.FindProperty("_attackCooldown").floatValue = 1.1f;
            so.FindProperty("_detectionRange").floatValue = 14f;
            so.FindProperty("_projectileSpeed").floatValue = 13f;
        });

        SetFields(elite.GetComponent<Health>(), so => so.FindProperty("_maxHealth").floatValue = 55f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(elite, EliteBirdPrefabPath);
        Object.DestroyImmediate(elite);
        return prefab;
    }

    // ---------- Utilidades ----------

    private static void AddPowerModifierToPrefab(string path, PlayerPowerData power)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            return;

        GameObject contents = PrefabUtility.LoadPrefabContents(path);

        try
        {
            G.SetReference(G.GetOrAdd<PlayerPowerModifier>(contents), "_playerPower", power);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static T GetAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset != null)
            return asset;

        string folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        EnsureFolder(folder);

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            return;

        string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
    }

    private static void SetFields(Object target, System.Action<SerializedObject> apply)
    {
        if (target == null)
            return;

        SerializedObject so = new SerializedObject(target);
        apply(so);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string name, string clipFile)
    {
        AnimatorState state = sm.AddState(name);
        state.motion = AssetDatabase.LoadAllAssetsAtPath(PartyMonsterFolder + "Animation/" + clipFile)
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

    private static void DestroyByName(Scene scene, string name)
    {
        GameObject found = G.FindByName(scene, name);

        if (found != null)
        {
            Object.DestroyImmediate(found);
        }
    }
}
