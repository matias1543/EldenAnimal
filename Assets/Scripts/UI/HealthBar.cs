using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health _health;
    [SerializeField] private Image _fillImage;

    private void OnEnable()
    {
        if (_health == null)
        {
            Debug.LogError("HealthBar: falta asignar Health en el Inspector");
            return;
        }
        if (_fillImage == null)
        {
            Debug.LogError("HealthBar: falta asignar Image en el Inspector");
            return;
        }

        _health.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (_health == null) return;

        _health.OnHealthChanged -= HandleHealthChanged;
    }

    private void Start()
    {
        if (_health == null) return;
        if (_fillImage == null) return;
        HandleHealthChanged(_health.CurrentHealth, _health.MaxHealth);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (max <= 0f) return;
        _fillImage.fillAmount = current / max;
    }
}
