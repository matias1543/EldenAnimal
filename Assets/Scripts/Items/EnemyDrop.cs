
using UnityEngine;

/// <summary>
/// Al morir el enemigo, puede soltar una poción.
///
/// Escucha Health.OnDeath (patrón Observer): no modifica EnemyTemplate.
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyDrop : MonoBehaviour
{
    [SerializeField] private PotionPickup _dropPrefab;
    [SerializeField, Range(0f, 1f)] private float _dropChance = 0.35f;

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
        if (_dropPrefab == null || Random.value > _dropChance)
            return;

        Instantiate(_dropPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);
    }
}
