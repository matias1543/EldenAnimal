
using System;
using UnityEngine;

/// <summary>
/// Al morir el enemigo, puede soltar una poción elegida al azar.
///
/// Cada poción posible tiene un peso: cuanto más peso, más seguido sale
/// (por ejemplo, la chica es común y la grande es rara).
/// Escucha Health.OnDeath (patrón Observer): no modifica EnemyTemplate.
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyDrop : MonoBehaviour
{
    [Serializable]
    private struct DropEntry
    {
        public PotionData Potion;
        [Min(0f)] public float Weight;
    }

    [SerializeField] private PotionPickup _dropPrefab;
    [SerializeField, Range(0f, 1f)] private float _dropChance = 0.4f;
    [SerializeField] private DropEntry[] _possibleDrops = new DropEntry[0];

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        _health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        _health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (_dropPrefab == null || UnityEngine.Random.value > _dropChance)
            return;

        PotionData potion = PickRandomPotion();

        if (potion == null)
            return;

        PotionPickup pickup = Instantiate(_dropPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);
        pickup.Init(potion);
    }

    // Elige una poción según su peso (sorteo ponderado).
    private PotionData PickRandomPotion()
    {
        float totalWeight = 0f;

        foreach (DropEntry entry in _possibleDrops)
        {
            totalWeight += entry.Weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.value * totalWeight;

        foreach (DropEntry entry in _possibleDrops)
        {
            roll -= entry.Weight;

            if (roll <= 0f)
                return entry.Potion;
        }

        return _possibleDrops[_possibleDrops.Length - 1].Potion;
    }
}
