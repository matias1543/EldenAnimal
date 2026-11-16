
using System;
using UnityEngine;

/// <summary>
/// NPC con el que se habla presionando E.
///
/// Implementa IInteractable (igual que el cofre), así PlayerInteractor
/// lo usa sin conocer esta clase. Elige el diálogo según el estado
/// de la misión y, al terminar el primero, la activa.
/// </summary>
public class NpcDialogue : MonoBehaviour, IInteractable
{
    private static readonly int TalkId = Animator.StringToHash("Talk");

    [SerializeField] private string _npcName = "Pipo";

    [Header("Dialogues")]
    [SerializeField] private DialogueData _questIntro;
    [SerializeField] private DialogueData _questInProgress;
    [SerializeField] private DialogueData _questCompleted;

    [Header("References")]
    [SerializeField] private Quest _quest;
    [SerializeField] private DialogueUI _dialogueUI;

    // Signo "!" sobre la cabeza: indica que tiene una misión para dar.
    [SerializeField] private GameObject _questMarker;

    private Animator _animator;

    public string Prompt => $"E: Hablar con {_npcName}";
    public bool CanInteract => _dialogueUI != null && !_dialogueUI.IsBusy;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (_quest != null)
        {
            _quest.OnStateChanged += HandleQuestChanged;
        }
    }

    private void OnDisable()
    {
        if (_quest != null)
        {
            _quest.OnStateChanged -= HandleQuestChanged;
        }
    }

    private void Start()
    {
        UpdateMarker();
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract)
            return;

        FaceTowards(interactor.transform.position);

        if (_animator != null)
        {
            _animator.SetTrigger(TalkId);
        }

        QuestState state = _quest != null ? _quest.State : QuestState.NotStarted;

        DialogueData dialogue = state switch
        {
            QuestState.NotStarted => _questIntro,
            QuestState.Active => _questInProgress,
            _ => _questCompleted
        };

        // Solo el primer diálogo activa la misión al terminar.
        Action onFinished = state == QuestState.NotStarted && _quest != null ? _quest.Begin : null;

        _dialogueUI.Show(dialogue, onFinished);
    }

    private void HandleQuestChanged(Quest quest) => UpdateMarker();

    private void UpdateMarker()
    {
        if (_questMarker != null)
        {
            _questMarker.SetActive(_quest == null || _quest.State == QuestState.NotStarted);
        }
    }

    private void FaceTowards(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
