
using System;
using UnityEngine;

public enum QuestState
{
    NotStarted,
    Active,
    Completed
}

/// <summary>
/// Misión del nivel: recuperar todas las gemas.
///
/// La activa el NPC al terminar su diálogo y se completa cuando
/// GemCollector avisa que se juntaron todas las gemas.
/// </summary>
public class Quest : MonoBehaviour
{
    [SerializeField] private string _title = "Recuperá las 3 gemas";
    [SerializeField] private GemCollector _gemCollector;

    public string Title => _title;
    public QuestState State { get; private set; } = QuestState.NotStarted;

    public event Action<Quest> OnStateChanged;

    private void OnEnable()
    {
        if (_gemCollector != null)
        {
            _gemCollector.OnAllGemsCollected += Complete;
        }
    }

    private void OnDisable()
    {
        if (_gemCollector != null)
        {
            _gemCollector.OnAllGemsCollected -= Complete;
        }
    }

    public void Begin()
    {
        if (State != QuestState.NotStarted)
            return;

        ChangeState(QuestState.Active);
    }

    private void Complete()
    {
        if (State != QuestState.Active)
            return;

        ChangeState(QuestState.Completed);
    }

    private void ChangeState(QuestState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(this);
    }
}
