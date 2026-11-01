using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Stamina _stamina;
    [SerializeField] private Image _fillImage;

    private void OnEnable()
    {
        if (_stamina == null)
        {
            Debug.LogError("StaminaBar: falta asignar Stamina en el Inspector");
            return;
        }
        if (_fillImage == null)
        {
            Debug.LogError("StaminaBar: falta asignar Image en el Inspector");
            return;
        }

        _stamina.OnStaminaChanged += HandleStaminaChanged;
    }

    private void OnDisable()
    {
        if (_stamina == null) return;

        _stamina.OnStaminaChanged -= HandleStaminaChanged;
    }

    private void Start()
    {
        if(_stamina == null) return;
        if(_fillImage == null) return;

        HandleStaminaChanged(_stamina.CurrentStamina, _stamina.MaxStamina);
    }

    private void HandleStaminaChanged(float current, float max)
    {
        if(max <= 0f) return;
        _fillImage.fillAmount = current / max;
    }
}
