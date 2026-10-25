
using UnityEngine;

/// <summary>
/// Enemigo tipo tortuga.
/// Hereda los estados, movimiento, detección y muerte
/// de EnemyTemplate.
/// Solo implementa su ataque cuerpo a cuerpo.
/// </summary>
public class TurtleEnemy : EnemyTemplate
{
    [Header("Attack")]
    [SerializeField] private float _attackDamage = 10f;

    protected override void Attack()
    {
        // Reproducimos la animación de ataque.
        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }

        // Verificamos que exista el jugador.
        if (_player == null)
            return;

        // Buscamos un componente que pueda recibir daño.
        IDamageable target = _player.GetComponent<IDamageable>();

        // Si el jugador puede recibir daño, lo atacamos.
        if (target != null)
        {
            target.TakeDamage(_attackDamage);
        }
    }
}
