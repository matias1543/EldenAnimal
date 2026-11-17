
using UnityEngine;

/// <summary>
/// Misión secundaria: encontrar un objeto (QuestItem).
///
/// Si el objeto se recoge antes de aceptar la misión, se completa
/// apenas se acepta.
/// </summary>
public class FetchQuest : Quest
{
    [SerializeField] private QuestItem _item;

    private bool _collected;

    public override string ProgressText => _collected ? "1/1" : "0/1";

    private void OnEnable()
    {
        if (_item != null)
        {
            _item.OnCollected += HandleCollected;
        }
    }

    private void OnDisable()
    {
        if (_item != null)
        {
            _item.OnCollected -= HandleCollected;
        }
    }

    protected override void OnBegin()
    {
        TryComplete();
    }

    private void HandleCollected(QuestItem item)
    {
        _collected = true;
        NotifyProgress();
        TryComplete();
    }

    private void TryComplete()
    {
        if (State == QuestState.Active && _collected)
        {
            Complete();
        }
    }
}
