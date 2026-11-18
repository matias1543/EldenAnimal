
using UnityEngine;

/// <summary>
/// Enemigo tipo cangrejo.
///
/// Hereda de EnemyTemplate los comportamientos de:
/// - Detección del jugador.
/// - Persecución mediante NavMeshAgent.
/// - Control de estados.
/// - Cooldown de ataque.
/// - Muerte mediante Health.
///
/// Implementa un ataque cuerpo a cuerpo
/// utilizando la interfaz IDamageable.
/// </summary>
public class CrabEnemy : EnemyTemplate
{
    [Header("Crab Attack")]

    [SerializeField] private float _damage = 10f;
    private SimpleAudio _audio;

    protected override void Awake()
    {
        base.Awake();

        _audio = GetComponent<SimpleAudio>();
    }
    protected override void Attack()
    {

        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }
        if (_audio != null)
        {
            _audio.PlayAttack();
        }

        if (_player == null)
            return;

        IDamageable damageable = _player.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(_damage);
        }
    }
}
