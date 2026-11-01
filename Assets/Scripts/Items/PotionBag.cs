
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pociones que lleva el jugador.
///
/// Las guarda en una lista y, con la tecla Q, toma la última
/// curando con Health.Heal(). No contiene lógica de vida propia.
/// </summary>
[RequireComponent(typeof(Health))]
public class PotionBag : MonoBehaviour
{
    [SerializeField] private int _maxPotions = 5;

    private readonly List<PotionData> _potions = new List<PotionData>();
    private Health _health;

    public int Count => _potions.Count;
    public int MaxPotions => _maxPotions;

    // Envía la cantidad actual de pociones.
    public event Action<int> OnPotionsChanged;

    public event Action<PotionData> OnPotionUsed;

    // Se intentó tomar una poción con la vida llena.
    public event Action OnHealthAlreadyFull;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            UsePotion();
        }
    }

    /// <summary>
    /// Guarda una poción. Devuelve false si la bolsa está llena.
    /// </summary>
    public bool TryAdd(PotionData potion)
    {
        if (potion == null || _potions.Count >= _maxPotions)
            return false;

        _potions.Add(potion);
        OnPotionsChanged?.Invoke(_potions.Count);
        return true;
    }

    private void UsePotion()
    {
        if (_potions.Count == 0 || _health.IsDead)
            return;

        if (_health.CurrentHealth >= _health.MaxHealth)
        {
            OnHealthAlreadyFull?.Invoke();
            return;
        }

        PotionData potion = _potions[_potions.Count - 1];
        _potions.RemoveAt(_potions.Count - 1);

        _health.Heal(potion.HealAmount);

        OnPotionUsed?.Invoke(potion);
        OnPotionsChanged?.Invoke(_potions.Count);
    }
}
