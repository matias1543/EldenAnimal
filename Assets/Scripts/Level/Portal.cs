
using System;
using UnityEngine;

/// <summary>
/// Portal de salida del nivel.
///
/// Permanece apagado hasta que GemCollector avisa que se juntaron
/// todas las gemas. Al activarse muestra un haz de luz visible desde
/// lejos (la pista para encontrarlo) y, al entrar el jugador, avisa la victoria.
/// </summary>
public class Portal : MonoBehaviour
{
    [SerializeField] private GemCollector _gemCollector;

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
            _gemCollector.OnAllGemsCollected += Activate;
        }
    }

    private void OnDisable()
    {
        if (_gemCollector != null)
        {
            _gemCollector.OnAllGemsCollected -= Activate;
        }
    }

    private void Activate()
    {
        if (_isActive)
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
