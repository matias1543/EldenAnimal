
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Herramienta de editor para armar el nivel de gemas en TestMatias.
///
/// Menú: Tools > Elden Animal > Armar nivel de gemas
///
/// - Crea el prefab del cofre (con su gema) y coloca 3 cofres.
/// - Reparte los enemigos alrededor de cada cofre.
/// - Configura el portal (haz de luz + trigger) y la lógica de victoria/derrota.
/// - Crea la UI del nivel (gemas, mensajes, pantalla final).
/// </summary>
public static class MatiasGemLevelBuilder
{
    private const string ScenePath = "Assets/Scenes/TestMatias.unity";
    internal const string ChestPrefabPath = "Assets/Prefabs/Chest.prefab";
    private const string MaterialsFolder = "Assets/Materials/Level";
    private const string GemPrefabPath = "Assets/Assets importados/Low poly Gems/Prefabs/Stone1.prefab";
    private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

    // Zona: nombre del cofre, posición aproximada (x, altura esperada, z) y enemigos que lo cuidan.
    private static readonly (string chest, Vector3 position, string[] guards)[] Zones =
    {
        ("Chest_Playa",  new Vector3(34f, -2.5f, -56f), new[] { "CRAB (4)", "CRAB (5)", "CRAB (6)", "Slime" }),
        ("Chest_Camino", new Vector3(37f,  0f,   -2f),  new[] { "CRAB", "CRAB (1)", "TurtleShellPolyart" }),
        ("Chest_Puente", new Vector3(25f,  0f,   31f),  new[] { "CRAB (2)", "CRAB (3)", "Octopus" }),
    };

    [MenuItem("Tools/Elden Animal/Armar nivel de gemas")]
    private static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        if (Build(scene))
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>
    /// Arma cofres, enemigos de cada cofre, portal, flujo y UI en la escena abierta.
    /// La usa también MatiasLevelContentBuilder después de regenerar el NavMesh.
    /// </summary>
    internal static bool Build(Scene scene)
    {
        GameObject player = Find<PlayerMovement>(scene)?.gameObject;
        GameObject portalObject = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == "Portal01")?.gameObject;

        if (player == null || portalObject == null)
        {
            Debug.LogError("Falta el Player (PlayerMovement) o el Portal01. Corré antes 'Preparar nivel Matías'.");
            return false;
        }

        // 1. Jugador: contador de gemas + interacción.
        GemCollector collector = GetOrAdd<GemCollector>(player);
        PlayerInteractor interactor = GetOrAdd<PlayerInteractor>(player);
        SetReference(interactor, "_interactAction", FindInputAction("Player", "Interact"));

        // 2. Cofres y enemigos.
        GameObject chestPrefab = BuildChestPrefab();
        Vector3 spawn = player.transform.position;

        // Todo lo que se coloque tiene que ser alcanzable caminando desde el inicio.
        ReachableOrigin = SampleNavMesh(spawn);

        foreach ((string chestName, Vector3 approx, string[] guards) in Zones)
        {
            Vector3 chestPosition = SampleReachable(approx);
            GameObject chest = FindByName(scene, chestName);

            if (chest == null)
            {
                chest = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab, scene);
                chest.name = chestName;
            }

            Vector3 toSpawn = spawn - chestPosition;
            toSpawn.y = 0f;
            chest.transform.SetPositionAndRotation(chestPosition, Quaternion.LookRotation(toSpawn.normalized));

            for (int i = 0; i < guards.Length; i++)
            {
                GameObject guard = FindByName(scene, guards[i]);

                if (guard == null)
                {
                    Debug.LogWarning($"No se encontró el enemigo '{guards[i]}'.");
                    continue;
                }

                float angle = i * Mathf.PI * 2f / guards.Length;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 4f;
                guard.transform.position = SampleReachable(chestPosition + offset);
            }

            Debug.Log($"{chestName} en {chestPosition}");
        }

        // 3. Portal: trigger + haz de luz que se enciende con todas las gemas.
        Portal portal = GetOrAdd<Portal>(portalObject);
        SetReference(portal, "_gemCollector", collector);
        SetReference(portal, "_activeVisual", BuildPortalVisual(portalObject.transform));

        SphereCollider portalTrigger = portalObject.GetComponents<SphereCollider>().FirstOrDefault(c => c.isTrigger)
                                       ?? portalObject.AddComponent<SphereCollider>();
        portalTrigger.isTrigger = true;
        portalTrigger.radius = 2f;
        portalTrigger.center = new Vector3(0f, 1.5f, 0f);

        // 4. Victoria / derrota.
        GameObject flowObject = FindByName(scene, "LevelFlow") ?? new GameObject("LevelFlow");
        LevelFlow flow = GetOrAdd<LevelFlow>(flowObject);
        SetReference(flow, "_playerHealth", player.GetComponent<Health>());
        SetReference(flow, "_portal", portal);

        // 5. UI del nivel.
        BuildLevelUI(scene, collector, interactor, portal, flow);

        Debug.Log("Nivel de gemas armado: 3 cofres, enemigos por zona, portal y UI.");
        return true;
    }

    // ---------- Cofre ----------

    private static GameObject BuildChestPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ChestPrefabPath);

        if (existing != null)
            return existing;

        Material wood = GetMaterial("ChestWood", new Color(0.45f, 0.27f, 0.12f));
        Material lidWood = GetMaterial("ChestLid", new Color(0.35f, 0.2f, 0.08f));
        Material gold = GetMaterial("ChestGold", new Color(1f, 0.8f, 0.2f));

        GameObject root = new GameObject("Chest");

        BoxCollider solid = root.AddComponent<BoxCollider>();
        solid.size = new Vector3(1.2f, 0.9f, 0.8f);
        solid.center = new Vector3(0f, 0.45f, 0f);

        // Zona en la que el jugador puede abrir el cofre.
        SphereCollider zone = root.AddComponent<SphereCollider>();
        zone.isTrigger = true;
        zone.radius = 2.2f;
        zone.center = new Vector3(0f, 0.5f, 0f);

        CreateCube("Body", root.transform, new Vector3(0f, 0.3f, 0f), new Vector3(1.2f, 0.6f, 0.8f), wood);
        CreateCube("Lock", root.transform, new Vector3(0f, 0.5f, 0.42f), new Vector3(0.2f, 0.25f, 0.06f), gold);

        // La tapa gira desde la bisagra trasera.
        Transform lidPivot = new GameObject("LidPivot").transform;
        lidPivot.SetParent(root.transform, false);
        lidPivot.localPosition = new Vector3(0f, 0.6f, -0.4f);
        CreateCube("Lid", lidPivot, new Vector3(0f, 0.11f, 0.4f), new Vector3(1.22f, 0.22f, 0.82f), lidWood);

        GameObject gem = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GemPrefabPath));
        gem.name = "Gem";
        gem.transform.SetParent(root.transform, false);
        gem.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        FitToSize(gem, 0.5f);

        foreach (Collider c in gem.GetComponentsInChildren<Collider>())
        {
            Object.DestroyImmediate(c);
        }

        Chest chest = root.AddComponent<Chest>();
        SetReference(chest, "_lid", lidPivot);
        SetReference(chest, "_gemVisual", gem);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ChestPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---------- Portal ----------

    private static GameObject BuildPortalVisual(Transform portal)
    {
        Transform existing = portal.Find("PortalActive");

        if (existing != null)
            return existing.gameObject;

        GameObject active = new GameObject("PortalActive");
        active.transform.SetParent(portal, false);

        // Haz de luz alto: es la pista visible desde todo el mapa.
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "LightBeam";
        Object.DestroyImmediate(beam.GetComponent<Collider>());
        beam.transform.SetParent(active.transform, false);
        beam.transform.localPosition = new Vector3(0f, 30f, 0f);
        beam.transform.localScale = new Vector3(0.8f, 30f, 0.8f);
        beam.GetComponent<Renderer>().sharedMaterial = GetMaterial("PortalBeam", new Color(0.3f, 0.9f, 1f), unlit: true);

        GameObject lightObject = new GameObject("PortalLight");
        lightObject.transform.SetParent(active.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 3f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.3f, 0.9f, 1f);
        light.range = 15f;
        light.intensity = 5f;

        return active;
    }

    // ---------- UI ----------

    private static void BuildLevelUI(Scene scene, GemCollector collector, PlayerInteractor interactor, Portal portal, LevelFlow flow)
    {
        GameObject existing = FindByName(scene, "LevelUI");

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("LevelUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Transform root = canvasObject.transform;

        Text gems = CreateText("GemsText", root, font, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(400f, 60f), 40, TextAnchor.UpperRight);
        Text message = CreateText("MessageText", root, font, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1500f, 80f), 36, TextAnchor.MiddleCenter);
        Text prompt = CreateText("PromptText", root, font, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(800f, 60f), 34, TextAnchor.MiddleCenter);

        // Pantalla final.
        GameObject panel = new GameObject("EndPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        Text title = CreateText("Title", panel.transform, font, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1400f, 160f), 96, TextAnchor.MiddleCenter);
        CreateText("Hint", panel.transform, font, new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(800f, 50f), 28, TextAnchor.MiddleCenter).text = "o presioná R";

        GameObject buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(0f, -60f);
        buttonRect.sizeDelta = new Vector2(380f, 90f);
        buttonObject.GetComponent<Image>().color = new Color(1f, 0.85f, 0.3f);

        Text buttonText = CreateText("Label", buttonObject.transform, font, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 90f), 40, TextAnchor.MiddleCenter);
        buttonText.text = "Reiniciar";
        buttonText.color = Color.black;
        Object.DestroyImmediate(buttonText.GetComponent<Outline>());

        LevelUI ui = canvasObject.AddComponent<LevelUI>();
        SetReference(ui, "_gemCollector", collector);
        SetReference(ui, "_interactor", interactor);
        SetReference(ui, "_portal", portal);
        SetReference(ui, "_levelFlow", flow);
        SetReference(ui, "_gemsText", gems);
        SetReference(ui, "_promptText", prompt);
        SetReference(ui, "_messageText", message);
        SetReference(ui, "_endPanel", panel);
        SetReference(ui, "_endTitle", title);
        SetReference(ui, "_restartButton", buttonObject.GetComponent<Button>());

        panel.SetActive(false);

        if (Find<EventSystem>(scene) == null)
        {
            Debug.LogWarning("No hay EventSystem en la escena: el botón Reiniciar no responderá al mouse (R sigue funcionando).");
        }
    }

    internal static Text CreateText(string name, Transform parent, Font font, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;

        go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        return text;
    }

    // ---------- Utilidades ----------

    internal static void CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
    }

    internal static Material GetMaterial(string name, Color color, bool unlit = false)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
            return material;

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "Level");

        string shaderName = unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit";
        material = new Material(Shader.Find(shaderName));
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // Escala el objeto para que su lado más grande mida "size" metros.
    internal static void FitToSize(GameObject go, float size)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;

        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        if (largest > 0.0001f)
        {
            go.transform.localScale *= size / largest;
        }
    }

    // Busca el punto caminable más cercano a la altura esperada.
    // Primero con radio chico, para no "caer" a otra altura (por ejemplo, el fondo del mar).
    internal static Vector3 SampleNavMesh(Vector3 position)
    {
        Vector3 probe = position + Vector3.up;

        if (NavMesh.SamplePosition(probe, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            return hit.position;

        if (NavMesh.SamplePosition(probe, out hit, 10f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"Sin NavMesh cerca de {position}: se usó {hit.position}.");
            return hit.position;
        }

        Debug.LogWarning($"Sin NavMesh cerca de {position}: se deja en esa posición.");
        return position;
    }

    // Punto de inicio del jugador sobre el NavMesh (lo fija Build).
    internal static Vector3? ReachableOrigin;

    private static readonly float[] HeightProbes = { 0f, -2.5f, 2.5f, -5f };

    /// <summary>
    /// Como SampleNavMesh, pero solo acepta puntos a los que se llega caminando
    /// desde el inicio. Si el punto pedido no es alcanzable, busca el más cercano que sí lo sea.
    /// </summary>
    internal static Vector3 SampleReachable(Vector3 approx)
    {
        if (ReachableOrigin == null)
            return SampleNavMesh(approx);

        NavMeshPath path = new NavMeshPath();

        for (float radius = 0f; radius <= 24f; radius += 3f)
        {
            int steps = radius == 0f ? 1 : 12;

            for (int i = 0; i < steps; i++)
            {
                float angle = i * Mathf.PI * 2f / steps;
                Vector3 candidate = approx + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

                foreach (float dy in HeightProbes)
                {
                    if (!NavMesh.SamplePosition(candidate + Vector3.up * (dy + 1f), out NavMeshHit hit, 2f, NavMesh.AllAreas))
                        continue;

                    if (!IsReachable(ReachableOrigin.Value, hit.position, path))
                        continue;

                    if (radius > 0f)
                    {
                        Debug.Log($"{approx} no era alcanzable: se movió a {hit.position}.");
                    }

                    return hit.position;
                }
            }
        }

        Debug.LogWarning($"No hay ningún punto alcanzable cerca de {approx}.");
        return SampleNavMesh(approx);
    }

    internal static bool IsReachable(Vector3 from, Vector3 to, NavMeshPath path)
    {
        return NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
    }

    private static InputActionReference FindInputAction(string map, string action)
    {
        return AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)
            .OfType<InputActionReference>()
            .FirstOrDefault(r => r.action != null && r.action.name == action && r.action.actionMap?.name == map);
    }

    internal static void SetReference(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static T GetOrAdd<T>(GameObject go) where T : Component
    {
        // Sin "??": en el editor GetComponent puede devolver un null "falso" de Unity.
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    internal static T Find<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).FirstOrDefault();
    }

    internal static GameObject FindByName(Scene scene, string name)
    {
        return scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == name)?.gameObject;
    }
}
