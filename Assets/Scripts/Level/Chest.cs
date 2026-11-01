
using System.Collections;
using UnityEngine;

/// <summary>
/// Cofre que se abre una sola vez y le entrega una gema al jugador.
///
/// Implementa IInteractable, así PlayerInteractor puede abrirlo
/// sin conocer esta clase.
/// </summary>
public class Chest : MonoBehaviour, IInteractable
{
    [Header("Visual")]
    [SerializeField] private Transform _lid;
    [SerializeField] private GameObject _gemVisual;
    [SerializeField] private float _openAngle = -110f;
    [SerializeField] private float _openDuration = 0.5f;

    // Ícono del minimapa: se oculta al abrir el cofre.
    [SerializeField] private GameObject _minimapMarker;

    [Header("Gem")]
    [SerializeField] private float _gemRiseHeight = 1.2f;
    [SerializeField] private float _gemShowDuration = 1.2f;

    private bool _isOpen;

    public string Prompt => "E: Abrir cofre";
    public bool CanInteract => !_isOpen;

    private void Awake()
    {
        if (_gemVisual != null)
        {
            _gemVisual.SetActive(false);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (_isOpen)
            return;

        _isOpen = true;

        if (_minimapMarker != null)
        {
            _minimapMarker.SetActive(false);
        }

        // La gema la cuenta GemCollector; el cofre solo se la entrega.
        if (interactor.TryGetComponent(out GemCollector collector))
        {
            collector.AddGem();
        }

        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        Quaternion closed = _lid != null ? _lid.localRotation : Quaternion.identity;
        Quaternion open = closed * Quaternion.Euler(_openAngle, 0f, 0f);

        for (float t = 0f; t < _openDuration; t += Time.deltaTime)
        {
            if (_lid != null)
            {
                _lid.localRotation = Quaternion.Slerp(closed, open, t / _openDuration);
            }

            yield return null;
        }

        if (_lid != null)
        {
            _lid.localRotation = open;
        }

        if (_gemVisual == null)
            yield break;

        // La gema sube girando y desaparece.
        _gemVisual.SetActive(true);
        Vector3 start = _gemVisual.transform.localPosition;
        Vector3 end = start + Vector3.up * _gemRiseHeight;

        for (float t = 0f; t < _gemShowDuration; t += Time.deltaTime)
        {
            _gemVisual.transform.localPosition = Vector3.Lerp(start, end, t / _gemShowDuration);
            _gemVisual.transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);
            yield return null;
        }

        _gemVisual.SetActive(false);
    }
}
