
using UnityEngine;

/// <summary>
/// Recompensa: el jugador hace más daño (por ejemplo, +50%).
/// </summary>
[CreateAssetMenu(fileName = "DamageUpgradeReward", menuName = "Elden Animal/Rewards/Damage Upgrade")]
public class DamageUpgradeReward : QuestReward
{
    [SerializeField] private PlayerPowerData _playerPower;
    [SerializeField] private float _damageBonus = 0.5f;

    public override void Apply(GameObject player)
    {
        if (_playerPower != null)
        {
            _playerPower.AddDamageBonus(_damageBonus);
        }
    }
}
