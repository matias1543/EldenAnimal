
using System;
using UnityEngine;

/// <summary>
/// Proyectil reutilizable (object pooling).
///
/// En lugar de destruirse, avisa a quien lo disparó mediante un callback
/// para volver al pool. Aplica daño con IDamageable e ignora a los enemigos.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float _damage = 8f;
    [SerializeField] private float _lifeTime = 4f;

    private Rigidbody _rb;
    private Vector3 _velocity;
    private float _timeLeft;
    private int _enemyLayer;

    // Evita devolver el proyectil al pool dos veces.
    private bool _released = true;
    private Action<Projectile> _release;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;

        _enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    /// <summary>
    /// Dispara el proyectil.
    /// release: método que lo devuelve al pool al impactar o al terminar su vida.
    /// </summary>
    public void Launch(Vector3 direction, float speed, Action<Projectile> release)
    {
        direction.Normalize();

        _velocity = direction * speed;
        _timeLeft = _lifeTime;
        _release = release;
        _released = false;

        _rb.position = transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Update()
    {
        if (_released)
            return;

        _timeLeft -= Time.deltaTime;

        if (_timeLeft <= 0f)
        {
            Release();
        }
    }

    private void FixedUpdate()
    {
        if (_released)
            return;

        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_released)
            return;

        // Ignoramos zonas de interacción (cofres, portal) y a los propios enemigos.
        if (other.isTrigger || other.gameObject.layer == _enemyLayer)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(_damage);

        Release();
    }

    private void Release()
    {
        if (_released)
            return;

        _released = true;

        if (_release != null)
        {
            _release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
