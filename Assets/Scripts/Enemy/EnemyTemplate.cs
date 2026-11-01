
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Death
}

/// <summary>
/// Clase base para los enemigos de Elden Animal.
///
/// Controla:
/// - Estados del enemigo.
/// - Detección y persecución del jugador.
/// - Rango y cooldown de ataque.
/// - Reacción a la muerte.
///
/// Delega el patrullaje en EnemyPatrol
/// y la administración de vida en Health.
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyTemplate : MonoBehaviour
{
    protected Transform _player;
    protected NavMeshAgent _agent;
    protected Animator _animator;
    protected Health _health;

    // Componente que administra los recorridos.
    private EnemyPatrol _patrol;

    [Header("Detection")]
    [SerializeField] private float _detectionRange = 10f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackCooldown = 1.2f;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 3.5f;

    private float _nextAttackTime;
    private bool _deathSequenceStarted;

    protected EnemyState _currentState { get; private set; }
        = EnemyState.Idle;

    /// <summary>
    /// Obtiene los componentes que necesita el enemigo.
    /// </summary>
    protected virtual void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _health = GetComponent<Health>();

        // EnemyPatrol es opcional.
        // Si no está, el enemigo permanece en Idle.
        _patrol = GetComponent<EnemyPatrol>();

        _agent.speed = _moveSpeed;
        _agent.stoppingDistance = _attackRange;
    }

    /// <summary>
    /// Busca al jugador e inicializa el estado.
    /// </summary>
    protected virtual void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            _player = playerObject.transform;
        }
        else
        {
            Debug.LogError("Player not found.");
        }

        // Si el enemigo tiene recorridos, comienza patrullando.
        if (CanPatrol())
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    /// <summary>
    /// Escucha la muerte notificada por Health.
    /// </summary>
    protected virtual void OnEnable()
    {
        _health.OnDeath += HandleDeath;
    }

    protected virtual void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDeath -= HandleDeath;
        }
    }

    /// <summary>
    /// Ejecuta la máquina de estados.
    /// </summary>
    protected virtual void Update()
    {
        // La muerte se procesa aunque no exista el jugador.
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
        }
    }

    /// <summary>
    /// Comprueba si tenemos un patrullaje disponible.
    /// </summary>
    private bool CanPatrol()
    {
        return _patrol != null &&
               _patrol.isActiveAndEnabled &&
               _patrol.HasPoints;
    }

    /// <summary>
    /// Centraliza los cambios de estado.
    /// </summary>
    protected void ChangeState(EnemyState newState)
    {
        if (_currentState == newState)
            return;

        // Al abandonar Patrol, detenemos su movimiento.
        if (_currentState == EnemyState.Patrol)
        {
            _patrol?.StopPatrol();
        }

        _currentState = newState;
    }

    /// <summary>
    /// En Idle espera hasta detectar al jugador
    /// o hasta comenzar un recorrido.
    /// </summary>
    private void UpdateIdleState()
    {
        if (_agent.isOnNavMesh)
            _agent.isStopped = true;

        SetMoving(false);

        float distance = Vector3.Distance(
            transform.position,
            _player.position
        );

        if (distance <= _detectionRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (CanPatrol())
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    /// <summary>
    /// Comprueba la detección y delega el recorrido
    /// al componente EnemyPatrol.
    /// </summary>
    private void UpdatePatrolState()
    {
        float distance = Vector3.Distance(
            transform.position,
            _player.position
        );

        // El jugador tiene prioridad sobre el recorrido.
        if (distance <= _detectionRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (!CanPatrol())
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        // Delegamos el comportamiento a EnemyPatrol.
        _patrol.UpdatePatrol();

        // Sincronizamos la animación de movimiento.
        SetMoving(_patrol.IsMoving);
    }

    /// <summary>
    /// Persigue al jugador mientras esté dentro
    /// de su rango de detección.
    /// </summary>
    protected virtual void UpdateChaseState()
    {
        float distance = Vector3.Distance(
            transform.position,
            _player.position
        );

        // Si pierde al jugador, vuelve a patrullar.
        if (distance > _detectionRange)
        {
            if (_agent.isOnNavMesh)
                _agent.isStopped = true;

            SetMoving(false);

            ChangeState(
                CanPatrol()
                    ? EnemyState.Patrol
                    : EnemyState.Idle
            );

            return;
        }

        // Si alcanza el rango de ataque, deja de moverse.
        if (distance <= _attackRange)
        {
            if (_agent.isOnNavMesh)
                _agent.isStopped = true;

            SetMoving(false);

            ChangeState(EnemyState.Attack);
            return;
        }

        if (!_agent.isOnNavMesh)
            return;

        // Restauramos la distancia de detención
        // utilizada durante la persecución.
        _agent.stoppingDistance = _attackRange;

        _agent.isStopped = false;
        _agent.SetDestination(_player.position);

        SetMoving(true);
    }

    /// <summary>
    /// Controla el rango y cooldown del ataque.
    /// Cada enemigo implementa su propio Attack().
    /// </summary>
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

    /// <summary>
    /// Implementación específica del ataque.
    /// </summary>
    protected abstract void Attack();

    /// <summary>
    /// Recibe el evento de muerte de Health.
    /// </summary>
    private void HandleDeath()
    {
        ChangeState(EnemyState.Death);

        // Ejecutamos la muerte inmediatamente.
        UpdateDeathState();
    }

    /// <summary>
    /// Detiene al enemigo y reproduce su muerte.
    /// </summary>
    private void UpdateDeathState()
    {
        if (_deathSequenceStarted)
            return;

        _deathSequenceStarted = true;

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }

        SetMoving(false);

        if (_animator != null)
        {
            _animator.SetTrigger("Death");
        }

        Destroy(gameObject, 2f);
    }

    /// <summary>
    /// Centraliza el control de las animaciones
    /// de movimiento.
    /// </summary>
    private void SetMoving(bool value)
    {
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", value);
        }
    }
}
