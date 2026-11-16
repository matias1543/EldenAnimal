
using UnityEngine;

/// <summary>
/// Recompensa: escudo reforzado (bloquea más daño y gasta menos stamina).
/// </summary>
[CreateAssetMenu(fileName = "ShieldUpgradeReward", menuName = "Elden Animal/Rewards/Shield Upgrade")]
public class ShieldUpgradeReward : QuestReward
{
    [SerializeField, Range(0f, 1f)] private float _damageReduction = 0.95f;
    [SerializeField] private float _staminaPerBlock = 8f;

    public override void Apply(GameObject player)
    {
        if (player.TryGetComponent(out PlayerBlock block))
        {
            block.Upgrade(_damageReduction, _staminaPerBlock);
        }
    }
}
