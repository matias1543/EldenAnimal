
using System;
using UnityEngine;

/// <summary>
/// Portal de salida del nivel.
///
/// Permanece apagado hasta que se juntaron todas las gemas (GemCollector)
/// y, si tiene un guardián asignado (el jefe), hasta que el guardián muere.
/// Al activarse muestra un haz de luz visible desde lejos (la pista para
/// encontrarlo) y, al entrar el jugador, avisa la victoria.
/// </summary>
public class Portal : MonoBehaviour
{
    [SerializeField] private GemCollector _gemCollector;

    // Opcional: enemigo que hay que derrotar para que el portal se encienda.
    [SerializeField] private Health _guardian;

    // Haz de luz y efectos que solo se ven con el portal activo.
    [SerializeField] private GameObject _activeVisual;

    private bool _isActive;
    private bool _playerEntered;

    public event Action OnActivated;
    public event Action OnPlayerEntered;

    private void Awake()
    {
        if (_activeVisual != null)
        {
            _activeVisual.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (_gemCollector != null)
        {
            _gemCollector.OnAllGemsCollected += TryActivate;
        }

        if (_guardian != null)
        {
            _guardian.OnDeath += TryActivate;
        }
    }

    private void OnDisable()
    {
        if (_gemCollector != null)
        {
            _gemCollector.OnAllGemsCollected -= TryActivate;
        }

        if (_guardian != null)
        {
            _guardian.OnDeath -= TryActivate;
        }
    }

    // Se llama al juntar todas las gemas y al morir el guardián:
    // el portal se enciende cuando se cumplen las dos condiciones.
    private void TryActivate()
    {
        if (_isActive)
            return;

        if (_gemCollector != null && !_gemCollector.HasAllGems)
            return;

        if (_guardian != null && !_guardian.IsDead)
            return;

        _isActive = true;

        if (_activeVisual != null)
        {
            _activeVisual.SetActive(true);
        }

        OnActivated?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive || _playerEntered)
            return;

        if (!other.CompareTag("Player"))
            return;

        _playerEntered = true;
        OnPlayerEntered?.Invoke();
    }
}
