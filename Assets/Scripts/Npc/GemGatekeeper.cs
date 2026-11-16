
using System.Collections;
using UnityEngine;

/// <summary>
/// NPC que bloquea un camino (por ejemplo, un puente) hasta que el jugador
/// tenga suficientes gemas. Al hablarle con las gemas necesarias, se corre a un costado.
///
/// Es una GemLock (cerradura de gemas) que además se puede hablar (IInteractable).
/// </summary>
public class GemGatekeeper : GemLock, IInteractable
{
    private static readonly int TalkId = Animator.StringToHash("Talk");

    [Header("Gatekeeper")]
    [SerializeField] private string _npcName = "Brutus";
    [SerializeField] private DialogueData _lockedDialogue;
    [SerializeField] private DialogueData _openDialogue;
    [SerializeField] private DialogueUI _dialogueUI;

    // Hacia dónde se corre al abrir el paso (en espacio del mundo).
    [SerializeField] private Vector3 _stepAsideOffset = new Vector3(2.5f, 0f, 0f);
    [SerializeField] private float _stepAsideDuration = 1f;

    private Animator _animator;

    public string Prompt => $"E: Hablar con {_npcName} ({CurrentGems}/{RequiredGems} gemas)";
    public bool CanInteract => !IsOpen && _dialogueUI != null && !_dialogueUI.IsBusy;

    protected override void Awake()
    {
        base.Awake();
        _animator = GetComponentInChildren<Animator>();
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

        if (HasEnoughGems)
        {
            // Abre el paso recién al terminar de hablar.
            _dialogueUI.Show(_openDialogue, Open);
        }
        else
        {
            _dialogueUI.Show(_lockedDialogue);
        }
    }

    protected override void HandleOpened()
    {
        StartCoroutine(StepAsideRoutine());
    }

    private IEnumerator StepAsideRoutine()
    {
        Vector3 start = transform.position;
        Vector3 end = start + _stepAsideOffset;

        for (float t = 0f; t < _stepAsideDuration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, end, t / _stepAsideDuration);
            yield return null;
        }

        transform.position = end;
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
