
using UnityEngine;

/// <summary>
/// Proyectil disparado por el enemigo pulpo.
///
/// Se encarga de:
/// - Moverse en una dirección determinada.
/// - Aplicar daño mediante IDamageable.
/// - Ignorar colisiones con enemigos.
/// - Destruirse al impactar o al terminar su tiempo de vida.
///
/// Utiliza el sistema compartido Health + IDamageable.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class OctopusProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]

    // Cantidad de daño que aplica el proyectil.
    [SerializeField] private float _damage = 10f;

    // Tiempo máximo de existencia del proyectil.
    [SerializeField] private float _lifeTime = 5f;

    // Rigidbody encargado del movimiento físico.
    private Rigidbody _rb;

    // Identificador de la layer Enemy.
    private int _enemyLayer;

    /// <summary>
    /// Obtiene las referencias necesarias al inicializar.
    /// </summary>
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    /// <summary>
    /// Programa la destrucción automática del proyectil.
    /// Evita que permanezca en la escena indefinidamente.
    /// </summary>
    private void Start()
    {
        Destroy(gameObject, _lifeTime);
    }

    /// <summary>
    /// Lanza el proyectil en una dirección determinada.
    ///
    /// direction: dirección hacia la que debe viajar.
    /// speed: velocidad del proyectil.
    /// </summary>
    public void Launch(Vector3 direction, float speed)
    {
        // Evitamos lanzar con una dirección nula.
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        // Normalizamos para mantener una velocidad constante.
        direction.Normalize();

        // Orientamos el proyectil hacia su dirección.
        transform.rotation = Quaternion.LookRotation(direction);

        // Aplicamos velocidad mediante Rigidbody.
        _rb.linearVelocity = direction * speed;
    }

    /// <summary>
    /// Detecta las colisiones del proyectil.
    ///
    /// Si impacta contra un enemigo, ignora el daño.
    /// Si impacta contra un objeto con IDamageable,
    /// aplica daño utilizando el sistema compartido.
    ///
    /// Después de un impacto válido para destrucción,
    /// elimina el proyectil.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        GameObject target = collision.gameObject;

        // Si impactamos contra otro enemigo,
        // no aplicamos daño ni destruimos el proyectil.
        if (target.layer == _enemyLayer)
            return;

        // Buscamos IDamageable en el objeto impactado
        // o en cualquiera de sus padres.
        IDamageable damageable =
            target.GetComponentInParent<IDamageable>();

        // Si el objeto puede recibir daño, lo aplicamos.
        if (damageable != null)
        {
            damageable.TakeDamage(_damage);
        }

        // Destruimos el proyectil después del impacto.
        Destroy(gameObject);
    }
}
