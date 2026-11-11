using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerAim))]
public class PlayerLives : MonoBehaviour
{
    [SerializeField] private string _animatorTrigger = "Die";
    private InputActionMap _playerActionMap;
    private Animator _animator;
    private Health _health;
    private PlayerAim _playerAim;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _animator = GetComponent<Animator>();
        _playerAim = GetComponent<PlayerAim>();

        _playerActionMap = InputSystem.actions.FindActionMap("Player");

        if (_playerActionMap == null)
        {
            Debug.LogError("No se encontro el PlayerActionMap");
            return;
        }
    }

    private void OnEnable()
    {
        _health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        _health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (_playerActionMap != null) _playerActionMap.Disable();
        _playerAim.enabled = false;
        _animator.SetTrigger(_animatorTrigger);
        Debug.Log("Player murio.");
    }



}
