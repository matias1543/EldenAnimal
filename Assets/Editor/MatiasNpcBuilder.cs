
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using G = MatiasGemLevelBuilder;

/// <summary>
/// Herramienta de editor que agrega el NPC guardián y la misión de las gemas a TestMatias.
///
/// Menú: Tools > Elden Animal > Agregar NPC y misión
///
/// - Crea los diálogos como assets (Assets/Data/Dialogues), editables desde el Inspector.
/// - Crea la misión (Quest), la caja de diálogo (DialogueUI) y el NPC cerca del inicio.
/// - Los cofres quedan cerrados hasta aceptar la misión.
/// </summary>
public static class MatiasNpcBuilder
{
    private const string ScenePath = "Assets/Scenes/TestMatias.unity";
    private const string DialogueFolder = "Assets/Data/Dialogues";
    private const string NpcPrefabPath = "Assets/Prefabs/Npc_Guardian.prefab";
    private const string NpcModelPath = "Assets/Assets importados/PartyMonsterDuo/Prefab/P01.prefab";
    private const string NpcAnimationFolder = "Assets/Assets importados/PartyMonsterDuo/Animation/";
    private const string NpcControllerPath = "Assets/Animation/NpcTp.controller";
    private const string NpcName = "Pipo";

    // Distancia del NPC respecto del punto de inicio del jugador.
    private static readonly Vector3 NpcOffset = new Vector3(-3f, 0f, -2.5f);

    [MenuItem("Tools/Elden Animal/Agregar NPC y misión")]
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
    /// Arma NPC, misión y diálogos en la escena abierta.
    /// La usa también MatiasLevelContentBuilder, porque al reconstruir la UI se pierden las referencias.
    /// </summary>
    internal static bool Build(Scene scene)
    {
        GameObject player = G.Find<PlayerMovement>(scene)?.gameObject;
        LevelUI levelUI = G.Find<LevelUI>(scene);

        if (player == null || levelUI == null)
        {
            Debug.LogError("Falta el Player o la LevelUI. Corré antes 'Armar nivel de gemas'.");
            return false;
        }

        // 1. Diálogos.
        DialogueData intro = GetDialogue("Npc_Intro", new[]
        {
            $"¡Hola, viajero! Soy {NpcName}, el guardián de esta isla.",
            "Unas criaturas robaron las tres gemas sagradas y las escondieron en cofres por toda la isla.",
            "Sin las gemas, el portal de la cima no se abre... y nadie puede salir de acá.",
            "¿Me ayudás? Recuperá las 3 gemas. Apretá M para ver el mapa: los cofres están marcados en amarillo.",
            "Cuidado con los enemigos: clic izquierdo para atacar, clic derecho para defenderte y Q para tomar pociones. ¡Suerte!",
        });

        DialogueData inProgress = GetDialogue("Npc_InProgress", new[]
        {
            "¡Todavía te faltan gemas! Buscá los cofres amarillos en el mapa (M).",
            "Si te lastiman, tomá una poción con Q.",
        });

        DialogueData completed = GetDialogue("Npc_Completed", new[]
        {
            "¡Lo lograste! Las tres gemas brillan otra vez.",
            "El portal de la cima ya está abierto. Seguí la luz celeste y volvé a casa. ¡Gracias, héroe!",
        });

        // 2. Misión.
        GameObject questObject = G.FindByName(scene, "Quest_Gemas") ?? new GameObject("Quest_Gemas");
        Quest quest = G.GetOrAdd<Quest>(questObject);
        G.SetReference(quest, "_gemCollector", player.GetComponent<GemCollector>());

        // 3. Caja de diálogo.
        DialogueUI dialogueUI = BuildDialogueUI(scene);

        // 4. NPC cerca del inicio, mirando al jugador.
        GameObject npc = G.FindByName(scene, "Npc_Guardian");

        if (npc == null)
        {
            npc = (GameObject)PrefabUtility.InstantiatePrefab(BuildNpcPrefab(intro, inProgress, completed), scene);
            npc.name = "Npc_Guardian";
        }

        Vector3 spawn = player.transform.position;
        G.ReachableOrigin = G.SampleNavMesh(spawn);
        Vector3 npcPosition = G.SampleReachable(spawn + NpcOffset);

        Vector3 toPlayer = spawn - npcPosition;
        toPlayer.y = 0f;
        npc.transform.SetPositionAndRotation(npcPosition, Quaternion.LookRotation(toPlayer.normalized));

        NpcDialogue npcDialogue = npc.GetComponent<NpcDialogue>();
        G.SetReference(npcDialogue, "_quest", quest);
        G.SetReference(npcDialogue, "_dialogueUI", dialogueUI);

        // 5. Los cofres necesitan la misión activa.
        foreach (Chest chest in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Chest>(true)))
        {
            G.SetReference(chest, "_requiredQuest", quest);
        }

        // 6. UI del nivel.
        G.SetReference(levelUI, "_quest", quest);
        G.SetReference(levelUI, "_dialogueUI", dialogueUI);

        SerializedObject ui = new SerializedObject(levelUI);
        ui.FindProperty("_startMessage").stringValue = $"Hablá con {NpcName}, el guardián que está a tu lado (E)";
        ui.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"NPC {NpcName} y misión de las gemas agregados en {npcPosition}.");
        return true;
    }

    // Crea el diálogo solo si no existe: si después lo editás en el Inspector, no se pisa.
    private static DialogueData GetDialogue(string name, string[] lines)
    {
        string path = $"{DialogueFolder}/{name}.asset";
        DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(path);

        if (dialogue != null)
            return dialogue;

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        if (!AssetDatabase.IsValidFolder(DialogueFolder))
            AssetDatabase.CreateFolder("Assets/Data", "Dialogues");

        dialogue = ScriptableObject.CreateInstance<DialogueData>();
        AssetDatabase.CreateAsset(dialogue, path);

        SerializedObject so = new SerializedObject(dialogue);
        so.FindProperty("_speakerName").stringValue = NpcName;

        SerializedProperty linesProperty = so.FindProperty("_lines");
        linesProperty.arraySize = lines.Length;

        for (int i = 0; i < lines.Length; i++)
        {
            linesProperty.GetArrayElementAtIndex(i).stringValue = lines[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        return dialogue;
    }

    // ---------- Caja de diálogo ----------

    private static DialogueUI BuildDialogueUI(Scene scene)
    {
        GameObject existing = G.FindByName(scene, "DialogueUI");

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject canvasObject = new GameObject("DialogueUI", typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 40f);
        panelRect.sizeDelta = new Vector2(1300f, 240f);
        panel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Text speaker = G.CreateText("Speaker", panel.transform, font, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(900f, 50f), 38, TextAnchor.UpperLeft);
        speaker.color = new Color(1f, 0.85f, 0.3f);

        Text body = G.CreateText("Body", panel.transform, font, new Vector2(0f, 1f), new Vector2(30f, -80f), new Vector2(1240f, 140f), 32, TextAnchor.UpperLeft);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;

        Text hint = G.CreateText("Hint", panel.transform, font, new Vector2(1f, 0f), new Vector2(-25f, 15f), new Vector2(400f, 40f), 24, TextAnchor.LowerRight);
        hint.color = new Color(0.8f, 0.8f, 0.8f);

        DialogueUI dialogueUI = canvasObject.AddComponent<DialogueUI>();
        G.SetReference(dialogueUI, "_panel", panel);
        G.SetReference(dialogueUI, "_speakerText", speaker);
        G.SetReference(dialogueUI, "_bodyText", body);
        G.SetReference(dialogueUI, "_hintText", hint);

        panel.SetActive(false);
        return dialogueUI;
    }

    // ---------- NPC ----------

    private static GameObject BuildNpcPrefab(DialogueData intro, DialogueData inProgress, DialogueData completed)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath);

        if (existing != null)
            return existing;

        GameObject root = new GameObject("Npc_Guardian");

        // Cuerpo sólido (no se lo atraviesa) + zona para hablar.
        CapsuleCollider body = root.AddComponent<CapsuleCollider>();
        body.center = new Vector3(0f, 0.8f, 0f);
        body.height = 1.6f;
        body.radius = 0.5f;

        SphereCollider talkZone = root.AddComponent<SphereCollider>();
        talkZone.isTrigger = true;
        talkZone.radius = 2.5f;
        talkZone.center = new Vector3(0f, 1f, 0f);

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NpcModelPath));
        model.transform.SetParent(root.transform, false);
        G.FitToSize(model, 1.6f);

        Animator animator = model.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.runtimeAnimatorController = BuildNpcController();
            animator.applyRootMotion = false;

            // Sigue animándose aunque el diálogo pause el juego.
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        // "!" amarillo sobre la cabeza.
        GameObject marker = new GameObject("QuestMarker", typeof(TextMesh), typeof(Billboard));
        marker.transform.SetParent(root.transform, false);
        marker.transform.localPosition = new Vector3(0f, 2.3f, 0f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        TextMesh text = marker.GetComponent<TextMesh>();
        text.text = "!";
        text.font = font;
        text.fontSize = 64;
        text.characterSize = 0.08f;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.85f, 0.1f);
        marker.GetComponent<MeshRenderer>().sharedMaterial = font.material;

        NpcDialogue npc = root.AddComponent<NpcDialogue>();
        SerializedObject so = new SerializedObject(npc);
        so.FindProperty("_npcName").stringValue = NpcName;
        so.FindProperty("_questIntro").objectReferenceValue = intro;
        so.FindProperty("_questInProgress").objectReferenceValue = inProgress;
        so.FindProperty("_questCompleted").objectReferenceValue = completed;
        so.FindProperty("_questMarker").objectReferenceValue = marker;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, NpcPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // Idle en loop y una animación de festejo al hablar (trigger Talk).
    private static AnimatorController BuildNpcController()
    {
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(NpcControllerPath);

        if (existing != null)
            return existing;

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(NpcControllerPath);
        controller.AddParameter("Talk", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idle = sm.AddState("Idle");
        idle.motion = LoadClip("Idle03.fbx");
        sm.defaultState = idle;

        AnimatorState talk = sm.AddState("Talk");
        talk.motion = LoadClip("Victory01.fbx");

        AnimatorStateTransition toTalk = idle.AddTransition(talk);
        toTalk.AddCondition(AnimatorConditionMode.If, 0f, "Talk");
        toTalk.hasExitTime = false;
        toTalk.duration = 0.1f;

        AnimatorStateTransition toIdle = talk.AddTransition(idle);
        toIdle.hasExitTime = true;
        toIdle.exitTime = 0.95f;
        toIdle.duration = 0.2f;

        return controller;
    }

    private static AnimationClip LoadClip(string file)
    {
        return AssetDatabase.LoadAllAssetsAtPath(NpcAnimationFolder + file)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }
}
