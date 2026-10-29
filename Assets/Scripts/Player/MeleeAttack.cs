using UnityEngine;

public class MeleeAttack : AttackBase
{
    [Header("Melee")]
    [SerializeField] private float _damage = 25f;
    [SerializeField] private float _range = 1.2f;
    [SerializeField] private float _radius = 1f;
    [SerializeField] private LayerMask _targetLayers;

    private Vector3 GetHitCenter()
    {
        Vector3 offset = transform.forward * _range;
        return transform.position + offset;
    }

    protected override void PerformAttack()
    {
        Vector3 center = GetHitCenter();
        Collider[] enemiesColliders = Physics.OverlapSphere(center, _radius, _targetLayers);

        foreach (var item in enemiesColliders)
        {
            IDamageable damageable = item.GetComponentInParent<IDamageable>();

            if (damageable == null) continue;

            damageable.TakeDamage(_damage);
        }

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetHitCenter(), _radius);
    }

}
