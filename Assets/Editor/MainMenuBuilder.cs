using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Herramienta de Editor: arma la escena del menú principal (Assets/Scenes/MainMenu.unity)
/// y la pone primera en la lista de escenas del build, seguida del nivel.
/// Menú: Tools > Elden Animal > Crear Menú Principal.
/// </summary>
public static class MainMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string LevelScenePath = "Assets/Scenes/TestMatias.unity";
    private const string TitlePath = "Assets/Images/Menu/MenuTitle.png";
    private const string BackgroundPath = "Assets/Images/Menu/MenuBackground.png";
    private const string MusicPath = "Assets/Audio/Music/darren_hirst-gamer-music-140-bpm-355954.mp3";

    private static readonly Color Gold = new Color(0.98f, 0.81f, 0.36f);
    private static readonly Color Cream = new Color(0.96f, 0.92f, 0.82f);
    private static readonly Color ButtonColor = new Color(0.16f, 0.11f, 0.06f, 0.92f);
    private static readonly Color PanelColor = new Color(0.05f, 0.08f, 0.09f, 0.88f);

    private const string CreditsText =
        "<size=60><color=#FACE5C>CRÉDITOS</color></size>\n\n" +
        "<color=#FACE5C>Integrantes</color>\n" +
        "Cristian Pallante\n" +
        "Matias Pallante\n" +
        "Marcos Vizio\n\n" +
        "<size=30><color=#FACE5C>Recursos de terceros</color>\n" +
        "Modelos y animaciones: Unity Asset Store\n" +
        "Música y efectos de sonido: Pixabay y Freesound (licencias libres)\n\n" +
        "Programación de Videojuegos II</size>";

    [MenuItem("Tools/Elden Animal/Crear Menú Principal")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Sprite title = ImportAsSprite(TitlePath);
        Sprite background = ImportAsSprite(BackgroundPath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Cámara (el menú es solo UI, pero el juego necesita una cámara y un AudioListener).
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var cam = cameraGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cameraGo.AddComponent<AudioListener>();

        // Música del menú.
        var music = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        if (music != null)
        {
            var musicGo = new GameObject("MenuMusic");
            var source = musicGo.AddComponent<AudioSource>();
            source.clip = music;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = 0.6f;
        }

        // EventSystem con el módulo del Input System (el proyecto usa el Input System).
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();

        // Canvas.
        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var bg = CreateImage("Background", canvasGo.transform, background, Color.white);
        Stretch(bg.rectTransform);
        bg.raycastTarget = false;

        var menu = canvasGo.AddComponent<MainMenu>();

        // ---------- Panel principal ----------
        var mainPanel = CreateRect("MainPanel", canvasGo.transform);
        Stretch(mainPanel);

        var titleImage = CreateImage("Title", mainPanel, title, Color.white);
        titleImage.preserveAspect = true;
        titleImage.raycastTarget = false;
        SetAnchored(titleImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(1300, 405));

        var buttons = CreateRect("Buttons", mainPanel);
        SetAnchored(buttons, new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(460, 470));
        var layout = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 22;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        var play = CreateButton("PlayButton", buttons, "JUGAR");
        var credits = CreateButton("CreditsButton", buttons, "CRÉDITOS");
        var options = CreateButton("OptionsButton", buttons, "OPCIONES");
        var exit = CreateButton("ExitButton", buttons, "SALIR");

        // ---------- Panel de créditos ----------
        var creditsPanel = CreatePanel("CreditsPanel", canvasGo.transform);
        var creditsText = CreateText("CreditsText", creditsPanel, CreditsText, 40, Cream);
        creditsText.alignment = TextAlignmentOptions.Center;
        SetAnchored(creditsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1000, 640));
        var creditsBack = CreateButton("BackButton", creditsPanel, "VOLVER");
        SetAnchored((RectTransform)creditsBack.transform, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(380, 90));

        // ---------- Panel de opciones ----------
        var optionsPanel = CreatePanel("OptionsPanel", canvasGo.transform);
        var optionsTitle = CreateText("OptionsTitle", optionsPanel, "OPCIONES", 60, Gold);
        optionsTitle.alignment = TextAlignmentOptions.Center;
        SetAnchored(optionsTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(800, 90));

        var volumeLabel = CreateText("VolumeLabel", optionsPanel, "Volumen general", 40, Cream);
        volumeLabel.alignment = TextAlignmentOptions.Center;
        SetAnchored(volumeLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(800, 60));

        var slider = CreateSlider(optionsPanel);
        SetAnchored((RectTransform)slider.transform, new Vector2(0.5f, 0.5f), new Vector2(-40, 0), new Vector2(700, 40));

        var valueLabel = CreateText("VolumeValue", optionsPanel, "100%", 40, Gold);
        valueLabel.alignment = TextAlignmentOptions.Left;
        SetAnchored(valueLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(410, 0), new Vector2(130, 60));

        var volumeSlider = slider.gameObject.AddComponent<VolumeSlider>();
        var volumeSo = new SerializedObject(volumeSlider);
        volumeSo.FindProperty("_valueLabel").objectReferenceValue = valueLabel;
        volumeSo.ApplyModifiedPropertiesWithoutUndo();

        var optionsBack = CreateButton("BackButton", optionsPanel, "VOLVER");
        SetAnchored((RectTransform)optionsBack.transform, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(380, 90));

        // ---------- Referencias del MainMenu ----------
        var menuSo = new SerializedObject(menu);
        menuSo.FindProperty("_levelSceneName").stringValue = "TestMatias";
        menuSo.FindProperty("_mainPanel").objectReferenceValue = mainPanel.gameObject;
        menuSo.FindProperty("_creditsPanel").objectReferenceValue = creditsPanel.gameObject;
        menuSo.FindProperty("_optionsPanel").objectReferenceValue = optionsPanel.gameObject;
        menuSo.ApplyModifiedPropertiesWithoutUndo();

        // OnClick de cada botón (quedan visibles en el Inspector como si se hubieran asignado a mano).
        UnityEventTools.AddPersistentListener(play.onClick, menu.Play);
        UnityEventTools.AddPersistentListener(credits.onClick, menu.ShowCredits);
        UnityEventTools.AddPersistentListener(options.onClick, menu.ShowOptions);
        UnityEventTools.AddPersistentListener(exit.onClick, menu.Exit);
        UnityEventTools.AddPersistentListener(creditsBack.onClick, menu.BackToMain);
        UnityEventTools.AddPersistentListener(optionsBack.onClick, menu.BackToMain);

        // Los tres paneles quedan activos en la escena: así el VolumeSlider ejecuta Awake
        // al cargar y aplica el volumen guardado. MainMenu.Start apaga los que no van.

        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(LevelScenePath, true),
        };

        Debug.Log("Menú principal creado en " + ScenePath + " y agregado primero al build.");
    }

    private static Sprite ImportAsSprite(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private static RectTransform CreatePanel(string name, Transform parent)
    {
        var image = CreateImage(name, parent, null, PanelColor);
        Stretch(image.rectTransform);
        return image.rectTransform;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color)
    {
        var rect = CreateRect(name, parent);
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        var image = CreateImage(name, parent, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"), Color.white);
        image.type = Image.Type.Sliced;

        var button = image.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = new Color(0.45f, 0.30f, 0.08f, 1f);
        colors.pressedColor = new Color(0.62f, 0.42f, 0.10f, 1f);
        colors.selectedColor = ButtonColor;
        button.colors = colors;

        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(3, -3);

        var text = CreateText("Text", image.transform, label, 46, Cream);
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform);
        return button;
    }

    private static Slider CreateSlider(Transform parent)
    {
        var root = CreateRect("VolumeSlider", parent);
        var slider = root.gameObject.AddComponent<Slider>();

        var track = CreateImage("Background", root, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"), new Color(0.2f, 0.15f, 0.1f));
        track.type = Image.Type.Sliced;
        Stretch(track.rectTransform);

        var fillArea = CreateRect("Fill Area", root);
        Stretch(fillArea);
        fillArea.offsetMin = new Vector2(10, 0);
        fillArea.offsetMax = new Vector2(-10, 0);
        var fill = CreateImage("Fill", fillArea, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"), Gold);
        fill.type = Image.Type.Sliced;
        Stretch(fill.rectTransform);

        var handleArea = CreateRect("Handle Slide Area", root);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(20, 0);
        handleArea.offsetMax = new Vector2(-20, 0);
        var handle = CreateImage("Handle", handleArea, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"), Cream);
        handle.rectTransform.sizeDelta = new Vector2(56, 0);
        handle.rectTransform.anchorMin = new Vector2(0, 0);
        handle.rectTransform.anchorMax = new Vector2(0, 1);
        handle.rectTransform.offsetMin = new Vector2(-28, -14);
        handle.rectTransform.offsetMax = new Vector2(28, 14);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, anchor.y == 1f ? 1f : 0.5f);
        if (anchor.y == 0f)
            rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
