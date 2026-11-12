
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Enemigo tipo ave que ataca a distancia.
///
/// Hereda de EnemyTemplate la detección, persecución, estados y muerte.
/// Su Attack() dispara proyectiles reutilizados con un ObjectPool:
/// el ave dispara seguido, así evitamos crear y destruir objetos en cada ataque.
/// </summary>
public class BirdEnemy : EnemyTemplate
{
    [Header("Bird Attack")]
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private float _projectileSpeed = 10f;

    // Altura a la que apunta sobre la posición del jugador (el pecho, no los pies).
    [SerializeField] private float _aimHeight = 1f;

    private ObjectPool<Projectile> _pool;

    protected override void Awake()
    {
        base.Awake();

        _pool = new ObjectPool<Projectile>(
            createFunc: () => Instantiate(_projectilePrefab),
            actionOnGet: p => p.gameObject.SetActive(true),
            actionOnRelease: p => p.gameObject.SetActive(false),
            // Al cerrar la escena Unity puede destruir el proyectil antes que al ave.
            actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
            collectionCheck: false,
            defaultCapacity: 5,
            maxSize: 10);
    }

    protected override void Attack()
    {
        if (_player == null || _projectilePrefab == null || _shootPoint == null)
            return;

        Vector3 target = _player.position + Vector3.up * _aimHeight;
        Vector3 direction = target - _shootPoint.position;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        // EnemyTemplate frena al ave al atacar: la giramos hacia el jugador.
        Vector3 flat = new Vector3(direction.x, 0f, direction.z);

        if (flat.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(flat);
        }

        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }

        Projectile projectile = _pool.Get();
        projectile.transform.position = _shootPoint.position;
        projectile.Launch(direction, _projectileSpeed, ReleaseProjectile);
    }

    private void ReleaseProjectile(Projectile projectile)
    {
        // Si el ave ya murió, el proyectil en vuelo no tiene pool al que volver.
        if (this == null)
        {
            Destroy(projectile.gameObject);
            return;
        }

        _pool.Release(projectile);
    }

    private void OnDestroy()
    {
        _pool?.Clear();
    }
}
