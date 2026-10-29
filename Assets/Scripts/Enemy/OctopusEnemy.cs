
using UnityEngine;

/// <summary>
/// Enemigo tipo pulpo.
///
/// Hereda de EnemyTemplate:
/// - Detección del jugador.
/// - Persecución mediante NavMeshAgent.
/// - Control de estados.
/// - Cooldown de ataque.
/// - Muerte mediante Health.
///
/// A diferencia de los enemigos cuerpo a cuerpo,
/// el pulpo ataca a distancia lanzando proyectiles.
/// </summary>
public class OctopusEnemy : EnemyTemplate
{
    [Header("Octopus Attack")]

    // Prefab del proyectil que dispara el pulpo.
    [SerializeField] private GameObject _projectilePrefab;

    // Punto desde donde aparece el proyectil.
    [SerializeField] private Transform _shootPoint;

    // Velocidad a la que viaja el proyectil.
    [SerializeField] private float _projectileSpeed = 8f;

    /// <summary>
    /// Ejecuta el ataque a distancia del pulpo.
    ///
    /// EnemyTemplate llama automáticamente a este método
    /// cuando el jugador está dentro del rango de ataque
    /// y terminó el cooldown.
    /// </summary>
    protected override void Attack()
    {
        // Verificamos que tengamos las referencias necesarias.
        if (_player == null ||
            _projectilePrefab == null ||
            _shootPoint == null)
        {
            return;
        }

        // Reproducimos la animación de ataque.
        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }

        // Calculamos la dirección desde el punto de disparo
        // hasta la posición del jugador.
        Vector3 direction =
            (_player.position - _shootPoint.position).normalized;

        // Evitamos crear un proyectil si la dirección es nula.
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        // Instanciamos el proyectil en el punto de disparo
        // y lo orientamos hacia el jugador.
        GameObject projectile = Instantiate(
            _projectilePrefab,
            _shootPoint.position,
            Quaternion.LookRotation(direction)
        );

        // Obtenemos el script del proyectil creado.
        OctopusProjectile projectileScript =
            projectile.GetComponent<OctopusProjectile>();

        // Si existe, lo lanzamos en la dirección calculada.
        if (projectileScript != null)
        {
            projectileScript.Launch(
                direction,
                _projectileSpeed
            );
        }
        else
        {
            Debug.LogWarning(
                "The projectile prefab is missing OctopusProjectile."
            );

            Destroy(projectile);
        }
    }
}
