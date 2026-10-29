
using UnityEngine;

/// <summary>
/// Enemigo tipo pez globo.
///
/// Hereda de EnemyTemplate:
/// - Detección del jugador.
/// - Persecución mediante NavMeshAgent.
/// - Control de estados.
/// - Cooldown de ataque.
/// - Muerte mediante Health.
///
/// Es un enemigo más lento que el cangrejo
/// y realiza menos daño con sus ataques.
/// </summary>
public class SpikeFishEnemy : EnemyTemplate
{
    [Header("Spike Fish Attack")]

    // Cantidad de daño que realiza el pez globo.
    [SerializeField] private float _damage = 1f;

    /// <summary>
    /// Ejecuta el ataque cuerpo a cuerpo del pez globo.
    ///
    /// EnemyTemplate llama automáticamente a este método
    /// cuando el jugador está dentro del rango de ataque
    /// y finalizó el cooldown.
    /// </summary>
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

        // Buscamos el componente que pueda recibir daño.
        IDamageable damageable =
            _player.GetComponentInParent<IDamageable>();

        // Si encontramos un objeto que puede recibir daño,
        // aplicamos el daño correspondiente.
        if (damageable != null)
        {
            damageable.TakeDamage(_damage);
        }
    }
}
