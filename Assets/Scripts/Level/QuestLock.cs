
using UnityEngine;

/// <summary>
/// Cerradura que se abre al completar una misión
/// (por ejemplo, "eliminá a los monstruos de la llanura" para cruzar el puente sur).
///
/// Escucha Quest.OnStateChanged: no sabe qué tipo de misión es.
/// </summary>
public class QuestLock : PathLock
{
    [SerializeField] private Quest _quest;

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

    private void HandleQuestChanged(Quest quest)
    {
        if (quest.State == QuestState.Completed)
        {
            Open();
        }
    }
}
