
using System;
using UnityEngine;

/// <summary>
/// Poder del jugador compartido entre sistemas (ScriptableObject).
///
/// Las recompensas lo suben y los enemigos lo leen al recibir daño
/// (PlayerPowerModifier). Así el jugador pega más fuerte sin modificar
/// los ataques (MeleeAttack, RangedAttack, Spear).
/// </summary>
[CreateAssetMenu(fileName = "PlayerPower", menuName = "Elden Animal/Player Power")]
public class PlayerPowerData : ScriptableObject
{
    // Valor de partida: no se guarda en el asset. LevelFlow lo reinicia al empezar el nivel.
    [NonSerialized] private float _damageBonus;

    public float DamageMultiplier => 1f + _damageBonus;

    public event Action<float> OnChanged;

    public void AddDamageBonus(float bonus)
    {
        _damageBonus += bonus;
        OnChanged?.Invoke(DamageMultiplier);
    }

    public void ResetRuntime()
    {
        _damageBonus = 0f;
        OnChanged?.Invoke(DamageMultiplier);
    }
}
