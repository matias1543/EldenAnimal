
using UnityEngine;

/// <summary>
/// Controlador básico del jugador para realizar pruebas.
///
/// Permite:
/// - Moverse con WASD.
/// - Recibir daño mediante Health.
/// - Detectar cuando el jugador muere.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;

    private CharacterController _controller;
    private Health _health;

    /// <summary>
    /// Obtiene los componentes necesarios.
    /// </summary>
    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _health = GetComponent<Health>();
    }

    /// <summary>
    /// Nos suscribimos al evento de muerte de Health.
    /// </summary>
    private void OnEnable()
    {
        _health.OnDeath += HandleDeath;
    }

    /// <summary>
    /// Cancelamos la suscripción al desactivar el componente.
    /// </summary>
    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDeath -= HandleDeath;
        }
    }

    /// <summary>
    /// Se ejecuta una vez por frame.
    /// </summary>
    private void Update()
    {
        // No permitimos mover al jugador si está muerto.
        if (_health.IsDead)
            return;

        Move();
    }

    /// <summary>
    /// Mueve al jugador utilizando WASD.
    /// </summary>
    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical);

        // Evitamos moverse más rápido en diagonal.
        direction = Vector3.ClampMagnitude(direction, 1f);

        // Movemos al jugador.
        _controller.Move(direction * _moveSpeed * Time.deltaTime);

        // Rotamos al jugador en la dirección del movimiento.
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// Se ejecuta cuando Health notifica la muerte.
    /// </summary>
    private void HandleDeath()
    {
        Debug.Log("El jugador murió.");
    }
}
