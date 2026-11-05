using UnityEngine;

public class RangedAttack : AttackBase
{

    [Header("Ranged")]
    [SerializeField] private Spear _spearPrefab;
    [SerializeField] private Transform _spawnPoint;

    protected override void PerformAttack()
    {
        if (_spearPrefab == null)
        {
            Debug.LogError($"{GetType().Name}: falta asignar Spear Prefab en el Inspector");
            return;
        }
        if (_spawnPoint == null)
        {
            Debug.LogError($"{GetType().Name}: falta asignar Spawn Point en el Inspector");
            return;
        }

        Instantiate(_spearPrefab, _spawnPoint.position, _spawnPoint.rotation);

    }
}
