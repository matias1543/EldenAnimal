
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

    // Daño que realiza el cangrejo por ataque.
    [SerializeField] private float _damage = 10f;

    /// <summary>
    /// Ejecuta el ataque cuerpo a cuerpo del cangrejo.
    ///
    /// Es llamado automáticamente por EnemyTemplate
    /// cuando el jugador está dentro del rango de ataque
    /// y terminó el cooldown.
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

        // Buscamos un componente que implemente IDamageable.
        // Puede encontrarse en el jugador o en sus padres.
        IDamageable damageable =
            _player.GetComponentInParent<IDamageable>();

        // Si el jugador puede recibir daño, lo aplicamos.
        if (damageable != null)
        {
            damageable.TakeDamage(_damage);
        }
    }
}
