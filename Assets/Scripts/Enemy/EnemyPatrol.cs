
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Componente encargado del patrullaje de enemigos.
///
/// Recorre puntos configurados en el Inspector,
/// espera en cada punto y continúa con el siguiente.
///
/// No controla detección, ataques ni vida.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]

    [SerializeField] private Transform[] _patrolPoints;
    [SerializeField] private float _waitTime = 2f;
    [SerializeField] private float _arrivalDistance = 0.5f;

    private NavMeshAgent _agent;

    private int _currentPointIndex;
    private float _waitUntil;
    private bool _waiting;

    // Indica si existen puntos configurados.
    public bool HasPoints =>
        _patrolPoints != null && _patrolPoints.Length > 0;

    // Indica si el enemigo está desplazándose por la ruta.
    public bool IsMoving { get; private set; }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// Ejecuta la lógica de patrullaje.
    /// EnemyTemplate llama a este método durante Patrol.
    /// </summary>
    public void UpdatePatrol()
    {
        if (!HasPoints || !_agent.isOnNavMesh)
        {
            StopPatrol();
            return;
        }

        // Si está esperando, comprobamos si terminó el tiempo.
        if (_waiting)
        {
            _agent.isStopped = true;
            IsMoving = false;

            if (Time.time < _waitUntil)
                return;

            _waiting = false;

            // Pasamos al siguiente punto de forma circular.
            _currentPointIndex =
                (_currentPointIndex + 1) % _patrolPoints.Length;
        }

        Transform point = _patrolPoints[_currentPointIndex];

        // Si falta un punto, lo saltamos.
        if (point == null)
        {
            _currentPointIndex =
                (_currentPointIndex + 1) % _patrolPoints.Length;

            StopPatrol();
            return;
        }

        // Durante la patrulla queremos llegar al destino.
        _agent.stoppingDistance = 0f;
        _agent.isStopped = false;

        if (!_agent.SetDestination(point.position))
        {
            StopPatrol();
            return;
        }

        // Esperamos a que se calcule la ruta.
        if (_agent.pathPending)
        {
            IsMoving = false;
            return;
        }

        // No consideramos rutas incompletas como llegadas.
        if (_agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            StopPatrol();
            return;
        }

        // Comprobamos si llegó al punto.
        if (_agent.remainingDistance <= _arrivalDistance)
        {
            _agent.isStopped = true;

            _waiting = true;
            _waitUntil = Time.time + _waitTime;

            IsMoving = false;
            return;
        }

        IsMoving = true;
    }

    /// <summary>
    /// Detiene el patrullaje cuando se cambia
    /// a persecución, ataque o muerte.
    /// </summary>
    public void StopPatrol()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }

        IsMoving = false;
        _waiting = false;
    }
}
