
using UnityEngine;

/// <summary>
/// Recompensa: pociones para la bolsa y, opcionalmente, curación completa.
/// </summary>
[CreateAssetMenu(fileName = "PotionReward", menuName = "Elden Animal/Rewards/Potions")]
public class PotionReward : QuestReward
{
    [SerializeField] private PotionData _potion;
    [SerializeField] private int _amount = 2;
    [SerializeField] private bool _healFully = true;

    public override void Apply(GameObject player)
    {
        if (player.TryGetComponent(out PotionBag bag))
        {
            for (int i = 0; i < _amount; i++)
            {
                bag.TryAdd(_potion);
            }
        }

        if (_healFully && player.TryGetComponent(out Health health))
        {
            health.Heal(health.MaxHealth);
        }
    }
}
