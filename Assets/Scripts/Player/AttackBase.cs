using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Stamina))]
public abstract class AttackBase : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _attackAction;

    [Header("Attack")]
    [SerializeField] private float _cooldown = 0.8f;
    [SerializeField] private float _staminaCost = 10f;
    [SerializeField] private string _animatorTrigger = "Attack";

    private Animator _animator;
    private Stamina _stamina;
    private float _nextAttackTime;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _stamina = GetComponent<Stamina>();
    }

    // No hay OnDisable con Disable(): la acción es compartida en todo el proyecto, y deshabilitarla
    // al apagar un ataque (por ejemplo, al cambiar de arma) también le saca el click al otro.
    // Un ataque apagado igual deja de atacar, porque Unity no ejecuta su Update.
    private void OnEnable()
    {
        if (_attackAction == null)
        {
            Debug.LogError($"{GetType().Name}: falta asignar Attack Action en el Inspector");
            return;
        }
        _attackAction.action.Enable();
    }

    private void Update()
    {
        if (_attackAction == null) return;

        if (!_attackAction.action.WasPressedThisFrame()) return;

        TryAttack();
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;

        if (!_stamina.TryConsume(_staminaCost)) return;

        _nextAttackTime = Time.time + _cooldown;

        _animator.SetTrigger(_animatorTrigger);

        PerformAttack();
    }

    protected abstract void PerformAttack();
}
