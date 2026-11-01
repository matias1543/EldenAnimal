
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permite al jugador interactuar con elementos cercanos.
///
/// Detecta un IInteractable cuando entra en su zona (trigger)
/// y lo usa al presionar la acción Interact (tecla E).
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _interactAction;

    private IInteractable _current;

    // Avisa qué elemento está al alcance (o null) para mostrar el texto en la UI.
    public event Action<IInteractable> OnFocusChanged;

    private void OnEnable()
    {
        if (_interactAction == null)
        {
            Debug.LogError($"{GetType().Name}: falta asignar Interact Action en el Inspector");
            return;
        }

        _interactAction.action.Enable();
    }

    private void Update()
    {
        if (_current == null || _interactAction == null)
            return;

        if (!_interactAction.action.WasPressedThisFrame())
            return;

        if (!_current.CanInteract)
            return;

        _current.Interact(gameObject);

        // El estado pudo cambiar (por ejemplo, el cofre ya está abierto).
        OnFocusChanged?.Invoke(_current.CanInteract ? _current : null);
    }

    // El CharacterController del jugador recibe los mensajes de los triggers.
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable) && interactable.CanInteract)
        {
            _current = interactable;
            OnFocusChanged?.Invoke(_current);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable) && interactable == _current)
        {
            _current = null;
            OnFocusChanged?.Invoke(null);
        }
    }
}
