using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Spear : MonoBehaviour
{
    [Header("Spear")]
    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _damage = 20f;
    [SerializeField] private float _lifeTime = 3f;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _rigidbody.linearVelocity = transform.forward * _speed;
        Destroy(gameObject, _lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        if (other.CompareTag("Player")) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            Destroy(gameObject);
            return;
        }

        damageable.TakeDamage(_damage);

        Destroy(gameObject);
    }

}
