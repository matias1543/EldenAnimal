
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Escudo del jugador: mientras se mantiene el clic derecho,
/// reduce el daño recibido a cambio de stamina.
///
/// Implementa IDamageModifier: Health le consulta el daño
/// antes de descontarlo, sin conocer esta clase.
/// La pose de defensa la reproduce el Animator con el bool IsBlocking.
/// </summary>
[RequireComponent(typeof(Stamina))]
[RequireComponent(typeof(Animator))]
public class PlayerBlock : MonoBehaviour, IDamageModifier
{
    private static readonly int IsBlockingId = Animator.StringToHash("IsBlocking");

    [Header("Block")]
    [SerializeField, Range(0f, 1f)] private float _damageReduction = 0.8f;
    [SerializeField] private float _staminaPerBlock = 15f;

    private Stamina _stamina;
    private Animator _animator;

    // Evita warnings si el Animator todavía no tiene el parámetro IsBlocking.
    private bool _hasBlockParameter;

    public bool IsBlocking { get; private set; }

    // Se emite cuando el escudo detiene un golpe.
    public event Action OnBlocked;

    private void Awake()
    {
        _stamina = GetComponent<Stamina>();
        _animator = GetComponent<Animator>();

        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.nameHash == IsBlockingId)
            {
                _hasBlockParameter = true;
            }
        }
    }

    private void Update()
    {
        bool blocking = Mouse.current != null && Mouse.current.rightButton.isPressed;

        if (blocking == IsBlocking)
            return;

        IsBlocking = blocking;

        if (_hasBlockParameter)
        {
            _animator.SetBool(IsBlockingId, blocking);
        }
    }

    /// <summary>
    /// Mejora el escudo (por ejemplo, como recompensa de una misión).
    /// </summary>
    public void Upgrade(float damageReduction, float staminaPerBlock)
    {
        _damageReduction = Mathf.Clamp01(damageReduction);
        _staminaPerBlock = Mathf.Max(0f, staminaPerBlock);
    }

    public float ModifyDamage(float damage)
    {
        if (!IsBlocking)
            return damage;

        // Sin stamina suficiente el escudo no aguanta el golpe.
        if (!_stamina.TryConsume(_staminaPerBlock))
            return damage;

        OnBlocked?.Invoke();
        return damage * (1f - _damageReduction);
    }
}
