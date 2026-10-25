using UnityEngine;
using UnityEngine.AI;

/// Idle: está quieto esperando detectar al jugador.
/// Chase: persigue al jugador.
/// Attack: ataca cuando el jugador está suficientemente cerca.
/// Death: ejecuta la secuencia de muerte.
public enum EnemyState
{
    Idle,
    Chase,
    Attack,
    Death
}
/// Clase base abstracta para los enemigos del juego.

/// Contiene el comportamiento común:
/// - Detección del jugador.
/// - Persecución mediante NavMeshAgent.
/// - Control de estados.
/// - Control del tiempo entre ataques.
/// - Reacción a la muerte.
/// - Comunicación con el Animator.

/// Los enemigos concretos deben heredar de esta clase
/// e implementar su propio método Attack().
///
/// La vida se administra mediante el componente Health.
/// </summary>

// Obliga a que el GameObject tenga estos componentes.
// Unity los agrega automáticamente al incorporar este script
// si todavía no existen.
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyTemplate : MonoBehaviour
{
    // Referencia al Transform del jugador.
    // Se utiliza para conocer su posición.
    protected Transform _player;
    protected NavMeshAgent _agent;
    protected Animator _animator;
    protected Health _health;

    [Header("Detection")]

    // Velocidad de desplazamiento del enemigo.
    [SerializeField] private float _detectionRange = 10f;

    // Distancia necesaria para comenzar a atacar.
    [SerializeField] private float _attackRange = 2f;

    // Tiempo mínimo en segundos entre ataques consecutivos.
    [SerializeField] private float _attackCooldown = 1.2f;


    [Header("Movement")]

    // Velocidad de desplazamiento del enemigo.
    [SerializeField] private float _moveSpeed = 3.5f;


    private float _nextAttackTime;

    // Evita ejecutar la secuencia de muerte más de una vez.
    private bool _deathSequenceStarted;

    // Estado actual del enemigo.
    // Solo puede modificarse desde esta clase mediante ChangeState().
    // Las clases hijas pueden consultarlo.
    protected EnemyState _currentState { get; private set; } = EnemyState.Idle;

    protected virtual void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _health = GetComponent<Health>();

        _agent.speed = _moveSpeed;
        _agent.stoppingDistance = _attackRange;
    }

    /// Busca al jugador mediante el tag "Player"
    /// y almacena su Transform.
    protected virtual void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            _player = playerObject.transform;
        }
        else
        {
            Debug.LogError("Player not found. Check the Player tag.");
        }
    }

    /// Se ejecuta cuando el componente se habilita.
    /// Suscribe al enemigo al evento de muerte de Health.
    /// De esta forma, Health puede notificar la muerte
    /// sin conocer EnemyTemplate.
    protected virtual void OnEnable()
    {
        _health.OnDeath += HandleDeath;
    }

    /// Se ejecuta cuando el componente se deshabilita
    /// o cuando el GameObject es destruido.
    /// Cancela la suscripción al evento para evitar
    /// mantener referencias innecesarias.
    protected virtual void OnDisable()
    {
        _health.OnDeath -= HandleDeath;
    }


    protected virtual void Update()
    {
        if (_player == null)
            return;

        switch (_currentState)
        {
            case EnemyState.Idle:
                UpdateIdleState();
                break;

            case EnemyState.Chase:
                UpdateChaseState();
                break;

            case EnemyState.Attack:
                UpdateAttackState();
                break;

            case EnemyState.Death:
                UpdateDeathState();
                break;
        }
    }

    /// Cambia el estado actual del enemigo.
    /// Recibe:
    /// newState: nuevo estado al que queremos pasar.
    /// Centralizar los cambios permite agregar posteriormente
    /// acciones al entrar o salir de cada estado.
    protected void ChangeState(EnemyState newState)
    {
        // Evitamos cambiar al mismo estado innecesariamente.
        if (_currentState == newState)
            return;

        // Actualizamos el estado actual.
        _currentState = newState;
    }

    private void UpdateIdleState()
    {
        SetMoving(false);

        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= _detectionRange)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    protected virtual void UpdateChaseState()
    {
        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance > _detectionRange)
        {
            ChangeState(EnemyState.Idle);

            _agent.isStopped = true;

            SetMoving(false);

            return;
        }

        if (distance <= _attackRange)
        {
            ChangeState(EnemyState.Attack);

            _agent.isStopped = true;

            SetMoving(false);

            return;
        }

        _agent.isStopped = false;
        _agent.SetDestination(_player.position);

        SetMoving(true);
    }

    protected virtual void UpdateAttackState()
    {
        float distance = Vector3.Distance(
            transform.position,
            _player.position
        );

        if (distance > _attackRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (Time.time >= _nextAttackTime)
        {
            Attack();

            _nextAttackTime =
                Time.time + _attackCooldown;
        }
    }

    /// Método abstracto que deben implementar
    /// obligatoriamente los enemigos concretos.
    /// Por ejemplo:
    /// - MeleeEnemy: ataque cuerpo a cuerpo.
    /// - RangedEnemy: lanzamiento de proyectiles.
    /// No contiene implementación porque cada tipo
    /// de enemigo puede atacar de una manera diferente.
    protected abstract void Attack();

    /// Se ejecuta cuando Health notifica que la vida
    /// del enemigo llegó a cero.
    /// Cambia el estado del enemigo a Death.
    private void HandleDeath()
    {
        ChangeState(EnemyState.Death);
    }

    /// Ejecuta la secuencia de muerte del enemigo.
    /// Detiene el movimiento, activa la animación de muerte
    /// y destruye el GameObject después de dos segundos.
    /// La secuencia solamente puede comenzar una vez.
    private void UpdateDeathState()
    {
        // Evitamos repetir la secuencia cada frame.
        if (_deathSequenceStarted)
            return;

        _deathSequenceStarted = true;

        // Detenemos el movimiento del enemigo.
        if (_agent != null)
        {
            _agent.isStopped = true;
        }

        SetMoving(false);

        if (_animator != null)
        {
            _animator.SetTrigger("Death");
        }

        // Destruimos el enemigo después de dos segundos.
        // Esto permite reproducir la animación de muerte.
        Destroy(gameObject, 2f);
    }

    /// Método auxiliar para controlar la animación
    /// de movimiento del enemigo.
    /// Recibe:
    /// value: true si se está moviendo, false si está quieto.
    /// Centraliza el acceso al Animator para evitar
    /// repetir código en los diferentes estados.
    private void SetMoving(bool value)
    {
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", value);
        }
    }
}