
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida del jefe, arriba de la pantalla.
///
/// Aparece cuando el jefe ve al jugador y desaparece al morir.
/// Solo escucha eventos del jefe y de su Health.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private BossSlime _boss;
    [SerializeField] private GameObject _root;
    [SerializeField] private RectTransform _fill;
    [SerializeField] private Text _nameText;

    private Health _health;

    private void Awake()
    {
        _root.SetActive(false);
    }

    // En Start: el Awake del jefe (que obtiene su Health) ya se ejecutó.
    private void Start()
    {
        if (_boss == null)
            return;

        _health = _boss.Health;
        _boss.OnEngaged += Show;
        _health.OnHealthChanged += UpdateFill;
        _health.OnDeath += Hide;
    }

    private void OnDestroy()
    {
        if (_boss != null)
        {
            _boss.OnEngaged -= Show;
        }

        if (_health != null)
        {
            _health.OnHealthChanged -= UpdateFill;
            _health.OnDeath -= Hide;
        }
    }

    private void Show()
    {
        _nameText.text = _boss.BossName;
        UpdateFill(_health.CurrentHealth, _health.MaxHealth);
        _root.SetActive(true);
    }

    private void Hide()
    {
        _root.SetActive(false);
    }

    private void UpdateFill(float current, float max)
    {
        _fill.anchorMax = new Vector2(max > 0f ? current / max : 0f, 1f);
    }
}
