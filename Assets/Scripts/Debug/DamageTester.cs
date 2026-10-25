
using UnityEngine;

/// <summary>
/// Herramienta de prueba: aplica daño automáticamente cada segundo.
///
/// No contiene lógica de vida propia: solo llama a TakeDamage()
/// a través de IDamageable, así que funciona con cualquier objeto
/// que pueda recibir daño (por ejemplo, Health).
/// </summary>
public class DamageTester : MonoBehaviour
{
    [SerializeField] private float _damagePerSecond = 10f;

    private IDamageable _damageable;

    private void Awake()
    {
        // Buscamos una sola vez el componente que puede recibir daño.
        _damageable = GetComponent<IDamageable>();
    }

    private void Start()
    {
        // Llama a ApplyDamage cada 1 segundo, empezando a 1 segundo de iniciar.
        InvokeRepeating(nameof(ApplyDamage), 1f, 1f);
    }

    private void ApplyDamage()
    {
        _damageable?.TakeDamage(_damagePerSecond);
    }
}
