
using UnityEngine;

/// <summary>
/// Enemigo tipo slime.
/// Hereda los estados, movimiento, detección y muerte
/// de EnemyTemplate.
/// Solo implementa su ataque cuerpo a cuerpo.
/// </summary>
public class SlimeEnemy : EnemyTemplate
{
    [Header("Attack")]
    [SerializeField] private float _attackDamage = 5f;
    private SimpleAudio _audio;
    private IDamageable _playerDamageable;
    protected override void Awake()
    {
        base.Awake();

        _audio = GetComponent<SimpleAudio>();
    }
    protected override void Start()
    {
        base.Start();

        if (_player != null)
        {
            _playerDamageable = _player.GetComponent<IDamageable>();
        }
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

        _playerDamageable?.TakeDamage(_attackDamage);
    }
}
