
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

    // Referencia cacheada al componente que recibe daño del jugador.
    // La buscamos una sola vez en Start para no llamar
    // a GetComponent en cada ataque.
    private IDamageable _playerDamageable;

    protected override void Start()
    {
        // Ejecutamos el Start de EnemyTemplate,
        // que busca al jugador por su tag.
        base.Start();

        if (_player != null)
        {
            _playerDamageable = _player.GetComponent<IDamageable>();
        }
    }

    protected override void Attack()
    {
        // Reproducimos la animación de ataque.
        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }

        // Si el jugador puede recibir daño, lo atacamos.
        _playerDamageable?.TakeDamage(_attackDamage);
    }
}
