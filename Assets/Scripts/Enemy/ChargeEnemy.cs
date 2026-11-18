
using UnityEngine;

/// <summary>
/// Monstruo embestidor.
///
/// Hereda de EnemyTemplate la detección, persecución y muerte.
/// Su diferencia: cuando el jugador está a media distancia,
/// embiste (acelera de golpe un instante) para cerrar distancia.
/// </summary>
public class ChargeEnemy : EnemyTemplate
{
    private static readonly int AttackId = Animator.StringToHash("Attack");
    private static readonly int ChargeId = Animator.StringToHash("Charge");

    [Header("Attack")]
    [SerializeField] private float _attackDamage = 12f;

    [Header("Charge")]
    [SerializeField] private float _chargeMinDistance = 3f;
    [SerializeField] private float _chargeMaxDistance = 9f;
    [SerializeField] private float _chargeSpeedMultiplier = 3f;
    [SerializeField] private float _chargeDuration = 0.8f;
    [SerializeField] private float _chargeCooldown = 4f;

    private IDamageable _playerDamageable;
    private float _baseSpeed;
    private float _nextChargeTime;
    private float _chargeEndTime;
    private bool _isCharging;
    private SimpleAudio _audio;
    protected override void Awake()
    {
        base.Awake();

        _audio = GetComponent<SimpleAudio>();
    }
    protected override void Start()
    {
        base.Start();

        if (_player != null)
        {
            _playerDamageable = _player.GetComponent<IDamageable>();
        }

        _baseSpeed = _agent.speed;
    }

    protected override void Update()
    {
        base.Update();

        if (_isCharging && Time.time >= _chargeEndTime)
        {
            _isCharging = false;
            _agent.speed = _baseSpeed;
        }
    }

    protected override void UpdateChaseState()
    {
        base.UpdateChaseState();
        TryCharge();
    }

    private void TryCharge()
    {
        if (_isCharging || Time.time < _nextChargeTime || _player == null)
            return;

        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance < _chargeMinDistance || distance > _chargeMaxDistance)
            return;

        _isCharging = true;
        _agent.speed = _baseSpeed * _chargeSpeedMultiplier;
        _chargeEndTime = Time.time + _chargeDuration;
        _nextChargeTime = Time.time + _chargeCooldown;

        if (_animator != null)
        {
            _animator.SetTrigger(ChargeId);
        }
    }

    protected override void Attack()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(AttackId);
        }
        if (_audio != null)
        {
            _audio.PlayAttack();
        }

        _playerDamageable?.TakeDamage(_attackDamage);
    }
}
