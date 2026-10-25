
using UnityEngine;

/// <summary>
/// Sistema temporal de ataque del jugador.
/// Permite atacar enemigos cercanos presionando Espacio.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]

    // Cantidad de daño que realiza el jugador.
    [SerializeField] private float _attackDamage = 25f;

    // Distancia máxima del ataque.
    [SerializeField] private float _attackRange = 2f;

    // Tiempo mínimo entre ataques.
    [SerializeField] private float _attackCooldown = 0.5f;

    // Radio del área de impacto.
    [SerializeField] private float _attackRadius = 1f;

    // Momento en el que se permite atacar nuevamente.
    private float _nextAttackTime;

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    /// <summary>
    /// Comprueba si el jugador presiona Espacio
    /// y si puede realizar otro ataque.
    /// </summary>
    private void Update()
    {
        // No permitimos atacar si el jugador está muerto.
        if (_health != null && _health.IsDead)
            return;

        // Verificamos si se presionó Espacio y terminó el cooldown.
        if (Input.GetKeyDown(KeyCode.Space) &&
            Time.time >= _nextAttackTime)
        {
            Attack();

            _nextAttackTime = Time.time + _attackCooldown;
        }
    }

    /// <summary>
    /// Busca enemigos dentro del área de ataque
    /// y les aplica daño mediante IDamageable.
    /// </summary>
    private void Attack()
    {
        // Calculamos el centro del ataque delante del jugador.
        Vector3 attackPosition =
            transform.position + transform.forward * _attackRange;

        // Buscamos colliders dentro del radio del ataque.
        Collider[] hits = Physics.OverlapSphere(
            attackPosition,
            _attackRadius
        );

        foreach (Collider hit in hits)
        {
            // Evitamos dañarnos a nosotros mismos.
            if (hit.transform.root == transform.root)
                continue;

            // Buscamos IDamageable en el objeto golpeado
            // o en alguno de sus padres.
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(_attackDamage);

                Debug.Log(
                    $"Golpeamos a {hit.name} por {_attackDamage} de daño."
                );
            }
        }
    }

    /// <summary>
    /// Dibuja visualmente el área del ataque
    /// cuando seleccionamos al jugador en Unity.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector3 attackPosition =
            transform.position + transform.forward * _attackRange;

        Gizmos.DrawWireSphere(
            attackPosition,
            _attackRadius
        );
    }
}
