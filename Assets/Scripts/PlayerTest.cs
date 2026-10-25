
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
public class PlayerTest : MonoBehaviour
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
        // Obtenemos el movimiento del teclado.
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Obtenemos la cámara principal.
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        // Obtenemos las direcciones de la cámara.
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;

        // Eliminamos la inclinación vertical.
        // El personaje solo debe moverse sobre el suelo.
        forward.y = 0f;
        right.y = 0f;

        // Normalizamos las direcciones.
        forward.Normalize();
        right.Normalize();

        // Calculamos el movimiento relativo a la cámara.
        Vector3 direction =
            forward * vertical +
            right * horizontal;

        // Evitamos que el movimiento diagonal sea más rápido.
        direction = Vector3.ClampMagnitude(direction, 1f);

        // Movemos al personaje.
        _controller.Move(
            direction * _moveSpeed * Time.deltaTime
        );

        // Rotamos al personaje hacia donde camina.
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
