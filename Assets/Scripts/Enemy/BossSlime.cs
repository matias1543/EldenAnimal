
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Jefe final: el Rey Slime.
///
/// Hereda de EnemyTemplate la detección, persecución y muerte, y agrega:
/// - Provocación (Taunt) al ver al jugador por primera vez.
/// - Golpe en área que lo deja mareado unos segundos (la ventana para pegarle).
/// - Fase 2 al bajar de cierta vida: más rápido y llama slimes que lo ayudan.
/// Los valores salen de BossStats (ScriptableObject).
/// </summary>
public class BossSlime : EnemyTemplate
{
    private static readonly int AttackId = Animator.StringToHash("Attack");
    private static readonly int DizzyId = Animator.StringToHash("Dizzy");
    private static readonly int TauntId = Animator.StringToHash("Taunt");

    [Header("Boss")]
    [SerializeField] private string _bossName = "Rey Slime";
    [SerializeField] private BossStats _stats;
    [SerializeField] private GameObject _minionPrefab;

    private IDamageable _playerDamageable;
    private WaitForSeconds _slamWait;
    private float _dizzyUntil;
    private bool _engaged;
    private bool _enraged;

    public string BossName => _bossName;
    public Health Health => _health;

    // Primera vez que ve al jugador (la UI muestra la barra de vida).
    public event Action OnEngaged;

    // Entró en fase 2.
    public event Action OnEnraged;

    private bool IsDizzy => Time.time < _dizzyUntil;

    protected override void Start()
    {
        base.Start();

        if (_player != null)
        {
            _playerDamageable = _player.GetComponent<IDamageable>();
        }

        _slamWait = new WaitForSeconds(_stats != null ? _stats.SlamDelay : 0.6f);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _health.OnHealthChanged += HandleHealthChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _health.OnHealthChanged -= HandleHealthChanged;
    }

    protected override void Update()
    {
        base.Update();

        if (!_engaged && _currentState == EnemyState.Chase)
        {
            _engaged = true;
            SetTrigger(TauntId);
            OnEngaged?.Invoke();
        }
    }

    // Mientras está mareado no persigue ni ataca.
    protected override void UpdateChaseState()
    {
        if (IsDizzy)
        {
            _agent.isStopped = true;
            return;
        }

        base.UpdateChaseState();
    }

    protected override void UpdateAttackState()
    {
        if (IsDizzy)
            return;

        base.UpdateAttackState();
    }

    protected override void Attack()
    {
        if (_stats == null)
            return;

        SetTrigger(AttackId);
        StartCoroutine(SlamRoutine());
    }

    private IEnumerator SlamRoutine()
    {
        // Esperamos al momento del impacto de la animación.
        yield return _slamWait;

        if (_health.IsDead)
            yield break;

        // Golpe en área: daña si el jugador sigue cerca.
        if (_player != null && Vector3.Distance(transform.position, _player.position) <= _stats.SlamRadius)
        {
            _playerDamageable?.TakeDamage(_stats.SlamDamage);
        }

        _dizzyUntil = Time.time + _stats.DizzyDuration;
        SetTrigger(DizzyId);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (_enraged || _stats == null || current <= 0f || current / max > _stats.EnrageThreshold)
            return;

        _enraged = true;
        _agent.speed *= _stats.EnragedSpeedMultiplier;
        SetTrigger(TauntId);
        SummonMinions();
        OnEnraged?.Invoke();
    }

    private void SummonMinions()
    {
        if (_minionPrefab == null)
            return;

        for (int i = 0; i < _stats.MinionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / _stats.MinionCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _stats.MinionSpawnRadius;

            if (NavMesh.SamplePosition(transform.position + offset, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                Instantiate(_minionPrefab, hit.position, transform.rotation);
            }
        }
    }

    private void SetTrigger(int id)
    {
        if (_animator != null)
        {
            _animator.SetTrigger(id);
        }
    }
}
