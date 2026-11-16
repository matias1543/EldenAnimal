
using UnityEngine;

/// <summary>
/// Misión secundaria: eliminar a un grupo de enemigos.
///
/// Escucha Health.OnDeath de cada objetivo. Cuenta las muertes aunque
/// la misión todavía no se haya aceptado: si ya están todos muertos
/// al aceptarla, se completa en el momento.
/// </summary>
public class KillQuest : Quest
{
    [SerializeField] private Health[] _targets = new Health[0];

    private int _killed;

    public int Killed => _killed;
    public int Total => _targets.Length;
    public override string ProgressText => $"{_killed}/{Total}";

    private void OnEnable()
    {
        foreach (Health target in _targets)
        {
            if (target != null)
            {
                target.OnDeath += HandleTargetDeath;
            }
        }
    }

    private void OnDisable()
    {
        foreach (Health target in _targets)
        {
            if (target != null)
            {
                target.OnDeath -= HandleTargetDeath;
            }
        }
    }

    protected override void OnBegin()
    {
        TryComplete();
    }

    private void HandleTargetDeath()
    {
        _killed++;
        NotifyProgress();
        TryComplete();
    }

    private void TryComplete()
    {
        if (State == QuestState.Active && _killed >= Total)
        {
            Complete();
        }
    }
}
