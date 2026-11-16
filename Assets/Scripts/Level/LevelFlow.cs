
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla el final del nivel: victoria, derrota y reinicio.
///
/// Escucha la muerte del jugador (Health.OnDeath) y la entrada
/// al portal (Portal.OnPlayerEntered). No muestra UI: avisa con eventos.
/// </summary>
public class LevelFlow : MonoBehaviour
{
    [SerializeField] private Health _playerHealth;
    [SerializeField] private Portal _portal;

    // Tiempo antes de pausar el juego, para que se vea la muerte o la entrada al portal.
    [SerializeField] private float _endDelay = 1f;

    // Mejoras del jugador ganadas en misiones: se reinician al empezar el nivel.
    [SerializeField] private PlayerPowerData _playerPower;

    private bool _finished;

    public bool IsFinished => _finished;

    public event Action OnVictory;
    public event Action OnDefeat;

    private void Awake()
    {
        if (_playerPower != null)
        {
            _playerPower.ResetRuntime();
        }
    }

    private void OnEnable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDeath += HandlePlayerDeath;
        }

        if (_portal != null)
        {
            _portal.OnPlayerEntered += HandlePortalEntered;
        }
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDeath -= HandlePlayerDeath;
        }

        if (_portal != null)
        {
            _portal.OnPlayerEntered -= HandlePortalEntered;
        }
    }

    private void Update()
    {
        // Atajo para reiniciar desde la pantalla final.
        if (_finished && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Restart();
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void HandlePlayerDeath() => Finish(false);

    private void HandlePortalEntered() => Finish(true);

    private void Finish(bool victory)
    {
        if (_finished)
            return;

        _finished = true;
        StartCoroutine(FinishRoutine(victory));
    }

    private IEnumerator FinishRoutine(bool victory)
    {
        yield return new WaitForSecondsRealtime(_endDelay);

        Time.timeScale = 0f;

        if (victory)
        {
            OnVictory?.Invoke();
        }
        else
        {
            OnDefeat?.Invoke();
        }
    }
}
