using System;
using UnityEngine;

public class Stamina : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _regenPerSecond = 20f;
    [SerializeField] private float _regenDelay = 1f;

    private float _currentStamina;
    private float _nextRegenTime;

    public float CurrentStamina => _currentStamina; 
    public float MaxStamina => _maxStamina;

    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        _currentStamina = _maxStamina;
    }

    public bool TryConsume(float amount)
    {
        if (_currentStamina < amount) return false;
        _currentStamina -= amount;
        _nextRegenTime = Time.time + _regenDelay;
        OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
        return true;
    }

    private void Update()
    {
        Regenerate();
    }

    private void Regenerate()
    {
        if (_currentStamina >= _maxStamina) return;
        if (Time.time < _nextRegenTime) return;
        _currentStamina += _regenPerSecond * Time.deltaTime;
        _currentStamina = Mathf.Min(_currentStamina, _maxStamina);
        OnStaminaChanged?.Invoke(_currentStamina, _maxStamina); 
    }

    [ContextMenu("Gastar 30")]
    private void DebugConsume()
    {
        bool consumed = TryConsume(30f);
        Debug.Log($"Gastar 30: {consumed} - quedan {_currentStamina}");
    }
}
