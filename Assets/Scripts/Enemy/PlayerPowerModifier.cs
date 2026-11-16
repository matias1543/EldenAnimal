
using UnityEngine;

/// <summary>
/// Va en cada enemigo: multiplica el daño que recibe según el poder
/// actual del jugador (PlayerPowerData).
///
/// Usa IDamageModifier, el mismo mecanismo que el escudo del jugador:
/// Health le consulta el daño antes de descontarlo.
/// </summary>
public class PlayerPowerModifier : MonoBehaviour, IDamageModifier
{
    [SerializeField] private PlayerPowerData _playerPower;

    public float ModifyDamage(float damage)
    {
        return _playerPower != null ? damage * _playerPower.DamageMultiplier : damage;
    }
}
