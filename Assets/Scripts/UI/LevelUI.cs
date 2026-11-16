
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI del nivel: contador de gemas, texto de interacción,
/// notificaciones y pantalla final (victoria / derrota).
///
/// Solo escucha eventos de los demás sistemas; ninguno de ellos conoce a la UI.
/// </summary>
public class LevelUI : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private GemCollector _gemCollector;
    [SerializeField] private PlayerInteractor _interactor;
    [SerializeField] private Portal _portal;
    [SerializeField] private LevelFlow _levelFlow;
    [SerializeField] private PotionBag _potionBag;
    [SerializeField] private Quest _quest;
    [SerializeField] private DialogueUI _dialogueUI;
    [SerializeField] private PathLock[] _pathLocks = new PathLock[0];
    [SerializeField] private BossSlime _boss;
    [SerializeField] private Quest[] _sideQuests = new Quest[0];
    [SerializeField] private Text _sideQuestsText;

    [Header("Texts")]
    [SerializeField] private Text _gemsText;
    [SerializeField] private Text _potionsText;
    [SerializeField] private Text _promptText;
    [SerializeField] private Text _messageText;

    [Header("End Screen")]
    [SerializeField] private GameObject _endPanel;
    [SerializeField] private Text _endTitle;
    [SerializeField] private Button _restartButton;

    [Header("Messages")]
    [SerializeField] private float _messageDuration = 3f;
    [SerializeField] private string _startMessage = "Encontrá las gemas escondidas en los cofres";

    private Coroutine _messageRoutine;
    private WaitForSecondsRealtime _messageWait;

    // Cantidad anterior, para distinguir si se juntó o se usó una poción.
    private int _lastPotionCount;

    // Elemento al alcance del jugador, para volver a mostrar su texto al cerrar un diálogo.
    private IInteractable _focused;

    private void Awake()
    {
        _messageWait = new WaitForSecondsRealtime(_messageDuration);
    }

    private void OnEnable()
    {
        if (_gemCollector != null) _gemCollector.OnGemsChanged += HandleGemsChanged;
        if (_interactor != null) _interactor.OnFocusChanged += HandleFocusChanged;
        if (_portal != null) _portal.OnActivated += HandlePortalActivated;

        if (_potionBag != null)
        {
            _potionBag.OnPotionsChanged += HandlePotionsChanged;
            _potionBag.OnPotionUsed += HandlePotionUsed;
            _potionBag.OnHealthAlreadyFull += HandleHealthAlreadyFull;
        }

        if (_quest != null) _quest.OnStateChanged += HandleQuestChanged;

        if (_dialogueUI != null)
        {
            _dialogueUI.OnOpened += HandleDialogueOpened;
            _dialogueUI.OnClosed += HandleDialogueClosed;
        }

        foreach (PathLock pathLock in _pathLocks)
        {
            if (pathLock != null) pathLock.OnOpened += HandleLockOpened;
        }

        if (_boss != null) _boss.OnEnraged += HandleBossEnraged;

        foreach (Quest sideQuest in _sideQuests)
        {
            if (sideQuest == null) continue;
            sideQuest.OnStateChanged += HandleSideQuestStateChanged;
            sideQuest.OnProgressChanged += HandleSideQuestProgress;
        }

        if (_levelFlow != null)
        {
            _levelFlow.OnVictory += HandleVictory;
            _levelFlow.OnDefeat += HandleDefeat;
        }

        if (_restartButton != null && _levelFlow != null)
        {
            _restartButton.onClick.AddListener(_levelFlow.Restart);
        }
    }

    private void OnDisable()
    {
        if (_gemCollector != null) _gemCollector.OnGemsChanged -= HandleGemsChanged;
        if (_interactor != null) _interactor.OnFocusChanged -= HandleFocusChanged;
        if (_portal != null) _portal.OnActivated -= HandlePortalActivated;

        if (_potionBag != null)
        {
            _potionBag.OnPotionsChanged -= HandlePotionsChanged;
            _potionBag.OnPotionUsed -= HandlePotionUsed;
            _potionBag.OnHealthAlreadyFull -= HandleHealthAlreadyFull;
        }

        if (_quest != null) _quest.OnStateChanged -= HandleQuestChanged;

        if (_dialogueUI != null)
        {
            _dialogueUI.OnOpened -= HandleDialogueOpened;
            _dialogueUI.OnClosed -= HandleDialogueClosed;
        }

        foreach (PathLock pathLock in _pathLocks)
        {
            if (pathLock != null) pathLock.OnOpened -= HandleLockOpened;
        }

        if (_boss != null) _boss.OnEnraged -= HandleBossEnraged;

        foreach (Quest sideQuest in _sideQuests)
        {
            if (sideQuest == null) continue;
            sideQuest.OnStateChanged -= HandleSideQuestStateChanged;
            sideQuest.OnProgressChanged -= HandleSideQuestProgress;
        }

        if (_levelFlow != null)
        {
            _levelFlow.OnVictory -= HandleVictory;
            _levelFlow.OnDefeat -= HandleDefeat;
        }

        if (_restartButton != null && _levelFlow != null)
        {
            _restartButton.onClick.RemoveListener(_levelFlow.Restart);
        }
    }

    private void Start()
    {
        _endPanel.SetActive(false);
        _promptText.text = string.Empty;

        int required = _gemCollector != null ? _gemCollector.RequiredGems : 0;
        UpdateGemsText(0, required);
        UpdatePotionsText(_potionBag != null ? _potionBag.Count : 0);
        ShowMessage(_startMessage);
    }

    private void HandlePotionsChanged(int count)
    {
        if (count > _lastPotionCount)
        {
            ShowMessage("¡Poción obtenida! Tomala con Q");
        }

        UpdatePotionsText(count);
    }

    private void HandlePotionUsed(PotionData potion)
    {
        ShowMessage($"+{potion.HealAmount:0} de vida");
    }

    private void HandleHealthAlreadyFull()
    {
        ShowMessage("Tu vida ya está llena");
    }

    private void UpdatePotionsText(int count)
    {
        _lastPotionCount = count;

        if (_potionsText != null)
        {
            _potionsText.text = $"Pociones: {count} (Q)";
        }
    }

    private void HandleGemsChanged(int current, int required)
    {
        UpdateGemsText(current, required);

        if (current < required)
        {
            ShowMessage($"¡Gema obtenida! Te faltan {required - current}");
        }
    }

    private void HandleFocusChanged(IInteractable interactable)
    {
        _focused = interactable;
        _promptText.text = interactable != null ? interactable.Prompt : string.Empty;
    }

    private void HandleQuestChanged(Quest quest)
    {
        if (quest.State == QuestState.Active)
        {
            ShowMessage($"Nueva misión: {quest.Title}");
        }

        UpdateGemsText(_gemCollector != null ? _gemCollector.Gems : 0, _gemCollector != null ? _gemCollector.RequiredGems : 0);
    }

    private void HandleDialogueOpened()
    {
        _promptText.text = string.Empty;
    }

    private void HandleDialogueClosed()
    {
        _promptText.text = _focused != null ? _focused.Prompt : string.Empty;
    }

    private void HandlePortalActivated()
    {
        ShowMessage("¡El portal de la cima se abrió! Seguí la luz");
    }

    private void HandleLockOpened(PathLock pathLock)
    {
        ShowMessage(pathLock.OpenedMessage);
    }

    private void HandleBossEnraged()
    {
        ShowMessage($"¡El {_boss.BossName} se enfureció y llamó refuerzos!");
    }

    private void HandleSideQuestStateChanged(Quest quest)
    {
        if (quest.State == QuestState.Active)
        {
            ShowMessage($"Nueva misión: {quest.Title}");
        }
        else if (quest.State == QuestState.Completed)
        {
            string reward = quest.Reward != null ? quest.Reward.Description : string.Empty;
            ShowMessage($"¡Misión cumplida! {reward}");
        }

        RefreshSideQuestsText();
    }

    private void HandleSideQuestProgress(Quest quest)
    {
        if (quest.State == QuestState.Active)
        {
            ShowMessage($"{quest.Title}: {quest.ProgressText}");
        }

        RefreshSideQuestsText();
    }

    // Lista de misiones secundarias en curso, debajo de las pociones.
    private void RefreshSideQuestsText()
    {
        if (_sideQuestsText == null)
            return;

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        foreach (Quest quest in _sideQuests)
        {
            if (quest != null && quest.State == QuestState.Active)
            {
                builder.Append("• ").Append(quest.Title).Append(' ').AppendLine(quest.ProgressText);
            }
        }

        _sideQuestsText.text = builder.ToString();
    }

    private void HandleVictory() => ShowEndScreen("¡VICTORIA!");

    private void HandleDefeat() => ShowEndScreen("GAME OVER");

    private void ShowEndScreen(string title)
    {
        _promptText.text = string.Empty;
        _endTitle.text = title;
        _endPanel.SetActive(true);
    }

    private void UpdateGemsText(int current, int required)
    {
        // Antes de aceptar la misión, el objetivo es ir a hablar con el NPC.
        if (_quest != null && _quest.State == QuestState.NotStarted)
        {
            _gemsText.text = "Misión: hablá con el guardián";
            return;
        }

        _gemsText.text = $"Gemas: {current}/{required}";
    }

    private void ShowMessage(string message)
    {
        if (_messageRoutine != null)
        {
            StopCoroutine(_messageRoutine);
        }

        _messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    private IEnumerator MessageRoutine(string message)
    {
        _messageText.text = message;
        yield return _messageWait;
        _messageText.text = string.Empty;
    }
}
