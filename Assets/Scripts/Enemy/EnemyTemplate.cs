using UnityEngine;
using UnityEngine.AI;

/// Idle: está quieto esperando detectar al jugador.
/// Chase: persigue al jugador.
/// Attack: ataca cuando el jugador está suficientemente cerca.
/// Death: ejecuta la secuencia de muerte.
public enum EnemyState
{
    Idle,
    Patrol,
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

    [Header("Patrol")]

    // Activa o desactiva el patrullaje.
    [SerializeField] private bool _enablePatrol = true;

    // Puntos que recorrerá el enemigo en orden.
    [SerializeField] private Transform[] _patrolPoints;

    // Tiempo que espera al llegar a cada punto.
    [SerializeField] private float _waitTime = 2f;

    // Distancia a la que consideramos que llegó al punto.
    [SerializeField] private float _arrivalDistance = 0.5f;

    // Índice del punto que debe visitar.
    private int _currentPatrolIndex;

    // Momento en el que termina la espera.
    private float _waitUntil;

    // Indica si está esperando en un punto.
    private bool _waitingAtPoint;

    // Momento en el que puede atacar nuevamente.
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
            Debug.LogError("Player not found.");
        }
        // Si tiene patrullaje configurado, empieza patrullando.
        if (HasPatrolPoints())
        {
            ChangeState(EnemyState.Patrol);
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
        if (_health != null)
        {
            _health.OnDeath -= HandleDeath;
        }
    }

    protected virtual void Update()
    {
        // La muerte debe procesarse incluso si no existe el jugador.
        if (_currentState == EnemyState.Death)
        {
            UpdateDeathState();
            return;
        }

        if (_player == null)
            return;

        switch (_currentState)
        {
            case EnemyState.Idle:
                UpdateIdleState();
                break;

            case EnemyState.Patrol:
                UpdatePatrolState();
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
        if (_currentState == newState)
            return;

        // Al salir de Patrol, cancelamos cualquier espera.
        if (_currentState == EnemyState.Patrol)
        {
            _waitingAtPoint = false;
        }

        _currentState = newState;
    }
    /// <summary>
    /// Comprueba si el patrullaje está activado
    /// y existen puntos configurados.
    /// </summary>
    private bool HasPatrolPoints()
    {
        return _enablePatrol && _patrolPoints != null && _patrolPoints.Length > 0;
    }

    private void UpdateIdleState()
    {
        _agent.isStopped = true;
        SetMoving(false);

        float distance = Vector3.Distance(transform.position, _player.position);

        // La detección tiene prioridad sobre el patrullaje.
        if (distance <= _detectionRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (HasPatrolPoints())
        {
            ChangeState(EnemyState.Patrol);
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

    /// <summary>
    /// Estado Patrol.
    ///
    /// Recorre los puntos configurados en orden.
    /// Al llegar a cada uno, espera unos segundos.
    /// Si detecta al jugador, interrumpe la patrulla.
    /// </summary>
    private void UpdatePatrolState()
    {
        // Comprobamos si el jugador está cerca.
        float distance = Vector3.Distance(
            transform.position,
            _player.position
        );

        if (distance <= _detectionRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Si no hay puntos, volvemos a Idle.
        if (!HasPatrolPoints())
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        // Si estamos esperando, comprobamos el tiempo.
        if (_waitingAtPoint)
        {
            _agent.isStopped = true;
            SetMoving(false);

            if (Time.time < _waitUntil)
                return;

            // Terminó la espera: avanzamos al siguiente punto.
            _waitingAtPoint = false;

            _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;
        }

        // Obtenemos el punto actual.
        Transform point = _patrolPoints[_currentPatrolIndex];

        if (point == null)
        {
            // Saltamos puntos que no estén asignados.
            _currentPatrolIndex =
                (_currentPatrolIndex + 1) % _patrolPoints.Length;

            _agent.isStopped = true;
            SetMoving(false);
            return;
        }

        // En patrullaje queremos llegar hasta el punto.
        _agent.stoppingDistance = 0f;
        _agent.isStopped = false;

        // Indicamos el destino al NavMeshAgent.
        if (!_agent.SetDestination(point.position))
        {
            _agent.isStopped = true;
            SetMoving(false);
            return;
        }

        // Mientras se calcula la ruta, esperamos.
        if (_agent.pathPending)
        {
            SetMoving(false);
            return;
        }

        // Si el agente no tiene una ruta completa,
        // evitamos tratarlo como una llegada válida.
        if (_agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            _agent.isStopped = true;
            SetMoving(false);
            return;
        }

        // Comprobamos si llegó al destino.
        if (_agent.remainingDistance <= _arrivalDistance)
        {
            // Detenemos al enemigo.
            _agent.isStopped = true;
            SetMoving(false);

            // Comenzamos la espera.
            _waitingAtPoint = true;
            _waitUntil = Time.time + _waitTime;

            return;
        }

        // Activamos la animación de caminar.
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

            _nextAttackTime = Time.time + _attackCooldown;
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