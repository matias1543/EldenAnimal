
using UnityEngine;

/// <summary>
/// Cerradura que pide cierta cantidad de gemas.
///
/// Agrega a PathLock el conteo de gemas (GemCollector).
/// Las concretas deciden cómo se abre:
/// - GemGatekeeper: al hablar con el NPC teniendo las gemas.
/// - GemDoor: sola, apenas se juntan las gemas.
/// </summary>
public abstract class GemLock : PathLock
{
    [Header("Gems")]
    [SerializeField] protected GemCollector _gemCollector;
    [SerializeField] private int _requiredGems = 2;

    public int RequiredGems => _requiredGems;

    protected int CurrentGems => _gemCollector != null ? _gemCollector.Gems : 0;
    protected bool HasEnoughGems => CurrentGems >= _requiredGems;
}
