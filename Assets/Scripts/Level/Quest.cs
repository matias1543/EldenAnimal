
using System;
using UnityEngine;

public enum QuestState
{
    NotStarted,
    Active,
    Completed
}

/// <summary>
/// Base de todas las misiones (máquina de estados NotStarted -> Active -> Completed).
///
/// Se encarga de lo común: título, estado, eventos y entregar la recompensa.
/// Cada misión concreta decide CUÁNDO se completa:
/// - GemQuest: al juntar todas las gemas.
/// - KillQuest: al eliminar a todos sus objetivos.
/// </summary>
public abstract class Quest : MonoBehaviour
{
    [SerializeField] private string _title = "Misión";
    [SerializeField] private QuestReward _reward;

    // Quién recibe la recompensa (el jugador).
    [SerializeField] private GameObject _rewardReceiver;

    public string Title => _title;
    public QuestReward Reward => _reward;
    public QuestState State { get; private set; } = QuestState.NotStarted;

    // Progreso para mostrar en pantalla (por ejemplo "3/6"). Vacío si la misión no lo usa.
    public virtual string ProgressText => string.Empty;

    public event Action<Quest> OnStateChanged;
    public event Action<Quest> OnProgressChanged;

    public void Begin()
    {
        if (State != QuestState.NotStarted)
            return;

        ChangeState(QuestState.Active);
        OnBegin();
    }

    // Por si la misión ya está cumplida al aceptarla.
    protected virtual void OnBegin() { }

    protected void Complete()
    {
        if (State != QuestState.Active)
            return;

        ChangeState(QuestState.Completed);

        if (_reward != null && _rewardReceiver != null)
        {
            _reward.Apply(_rewardReceiver);
        }
    }

    protected void NotifyProgress()
    {
        OnProgressChanged?.Invoke(this);
    }

    private void ChangeState(QuestState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(this);
    }
}
