using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla el volumen general del juego con un Slider (0 a 1).
/// AudioListener.volume afecta a todos los sonidos y se mantiene al cambiar de escena.
/// El valor se guarda en PlayerPrefs para que se recuerde al volver a abrir el juego.
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    private const string VolumeKey = "MasterVolume";

    [SerializeField] private TMP_Text _valueLabel;

    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();

        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        AudioListener.volume = savedVolume;
        _slider.SetValueWithoutNotify(savedVolume);
        UpdateLabel(savedVolume);
    }

    private void OnEnable()
    {
        _slider.onValueChanged.AddListener(HandleValueChanged);
    }

    private void OnDisable()
    {
        _slider.onValueChanged.RemoveListener(HandleValueChanged);
    }

    private void HandleValueChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        UpdateLabel(value);
    }

    private void UpdateLabel(float value)
    {
        if (_valueLabel != null)
            _valueLabel.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
