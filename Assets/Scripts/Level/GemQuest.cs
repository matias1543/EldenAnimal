
using UnityEngine;

/// <summary>
/// Misión principal: recuperar todas las gemas.
///
/// La activa Pipo al terminar su diálogo y se completa cuando
/// GemCollector avisa que se juntaron todas las gemas.
/// </summary>
public class GemQuest : Quest
{
    [SerializeField] private GemCollector _gemCollector;

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
}
