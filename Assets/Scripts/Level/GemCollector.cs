
using System;
using UnityEngine;

/// <summary>
/// Cuenta las gemas que juntó el jugador.
///
/// No sabe nada de la UI ni del portal: solo avisa mediante eventos
/// cuando cambia la cantidad y cuando se juntaron todas (patrón Observer).
/// </summary>
public class GemCollector : MonoBehaviour
{
    [SerializeField] private int _requiredGems = 3;

    private int _gems;

    public int Gems => _gems;
    public int RequiredGems => _requiredGems;
    public bool HasAllGems => _gems >= _requiredGems;

    // Envía: gemas actuales y gemas necesarias.
    public event Action<int, int> OnGemsChanged;

    // Se emite una sola vez, al juntar la última gema.
    public event Action OnAllGemsCollected;

    public void AddGem()
    {
        if (HasAllGems)
            return;

        _gems++;

        OnGemsChanged?.Invoke(_gems, _requiredGems);

        if (HasAllGems)
        {
            OnAllGemsCollected?.Invoke();
        }
    }
}
