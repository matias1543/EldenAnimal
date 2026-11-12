
using System.Collections;
using UnityEngine;

/// <summary>
/// Feedback visual al recibir daño: el modelo se tiñe de rojo
/// durante un instante.
///
/// No contiene lógica de vida: solo escucha el evento
/// OnHealthChanged de Health (patrón Observer).
/// Sirve para cualquier enemigo, sin modificar EnemyTemplate.
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyHitFeedback : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.15f;

    // ID cacheado de la propiedad de color del shader URP Lit.
    // Evita buscar la propiedad por nombre (string) en cada golpe.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Health _health;
    private Renderer[] _renderers;

    // Permite cambiar el color sin crear copias del material.
    private MaterialPropertyBlock _propertyBlock;
    private WaitForSeconds _flashWait;
    private Coroutine _flashRoutine;

    // Vida anterior, para distinguir daño de curación.
    private float _lastHealth;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _renderers = GetComponentsInChildren<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
        _flashWait = new WaitForSeconds(_flashDuration);
    }

    private void Start()
    {
        _lastHealth = _health.CurrentHealth;
    }

    private void OnEnable()
    {
        _health.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        _health.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        bool tookDamage = current < _lastHealth;
        _lastHealth = current;

        // OnHealthChanged también se emite al curar: solo reaccionamos al daño.
        if (!tookDamage)
            return;

        // Si llega otro golpe durante el flash, lo reiniciamos.
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
        }

        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        _propertyBlock.SetColor(BaseColorId, _flashColor);

        foreach (Renderer meshRenderer in _renderers)
        {
            meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        yield return _flashWait;

        // Quitamos el bloque para volver al color original del material.
        foreach (Renderer meshRenderer in _renderers)
        {
            meshRenderer.SetPropertyBlock(null);
        }

        _flashRoutine = null;
    }
}
