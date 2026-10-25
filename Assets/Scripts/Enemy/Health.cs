using System;
using UnityEngine;
/// Implementa IDamageable para permitir que otros objetos
/// puedan aplicarle daño sin conocer su implementación interna.
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float _maxHealth = 100f;

    private float _currentHealth;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;

    // Devuelve true cuando la vida llega a cero.
    // Se utiliza para saber si el personaje está muerto.
    public bool IsDead => _currentHealth <= 0f;

    // Evento que se ejecuta cada vez que cambia la vida.
    // Envía dos valores: vida actual y vida máxima.
    // Por ejemplo, la UI puede escucharlo para actualizar una barra.
    public event Action<float, float> OnHealthChanged;

    // Evento que se ejecuta cuando el personaje muere.
    // Permite que cada objeto decida qué hacer al morir.
    public event Action OnDeath;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        // Si ya está muerto, no puede recibir más daño.
        if (IsDead)
            return;
        // Evitamos recibir daño cero o negativo.
        if (damage <= 0f)
            return;
        // Restamos el daño recibido a la vida actual.
        _currentHealth -= damage;

        // Limitamos la vida entre 0 y el máximo permitido.
        // Evita que la vida quede en valores negativos.
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);

        // Notificamos a los scripts suscritos que la vida cambió.
        // Por ejemplo, una barra de vida puede actualizarse.
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        // Si la vida llegó a cero, notificamos la muerte.
        if (_currentHealth <= 0f)
        {
            OnDeath?.Invoke();
        }
        Debug.Log($"{gameObject.name} - Vida restante: {_currentHealth}");
    }
    /// Recupera una cantidad de vida.
    ///
    /// Recibe:
    /// amount: cantidad de vida que se quiere recuperar.
    ///
    /// La curación nunca puede superar la vida máxima.
    public void Heal(float amount)
    {
        // No permitimos curar personajes muertos.
        if (IsDead)
            return;

        // Evitamos curaciones de cero o valores negativos.
        if (amount <= 0f)
            return;

        // Sumamos la curación a la vida actual.
        _currentHealth += amount;

        // Limitamos la vida para que no supere el máximo.
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);

        // Avisamos que la vida cambió para actualizar
        // cualquier sistema interesado, como la UI.
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
}