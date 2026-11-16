
using System;
using UnityEngine;

/// <summary>
/// Objeto de misión que el jugador recoge al tocarlo (por ejemplo, la Perla del Oeste).
///
/// No sabe nada de misiones: solo avisa con un evento que fue recogido.
/// </summary>
public class QuestItem : MonoBehaviour
{
    [SerializeField] private string _displayName = "Objeto";
    [SerializeField] private Transform _visual;
    [SerializeField] private float _spinSpeed = 60f;

    public string DisplayName => _displayName;

    public event Action<QuestItem> OnCollected;

    private void Update()
    {
        if (_visual != null)
        {
            _visual.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        OnCollected?.Invoke(this);
        gameObject.SetActive(false);
    }
}
