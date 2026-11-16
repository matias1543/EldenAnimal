
using UnityEngine;

/// <summary>
/// Poción en el escenario: al tocarla, el jugador la guarda en su PotionBag.
///
/// El color y la curación salen del PotionData asignado,
/// así el mismo prefab sirve para cualquier tipo de poción.
/// </summary>
public class PotionPickup : MonoBehaviour
{
    [SerializeField] private PotionData _data;
    [SerializeField] private Transform _visual;
    [SerializeField] private Renderer _liquidRenderer;

    [Header("Animation")]
    [SerializeField] private float _spinSpeed = 90f;
    [SerializeField] private float _bobHeight = 0.15f;
    [SerializeField] private float _bobSpeed = 2f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Vector3 _visualStart;

    /// <summary>
    /// Define qué poción es. Lo usa EnemyDrop al soltarla, antes de que corra Start.
    /// </summary>
    public void Init(PotionData data)
    {
        _data = data;
    }

    private void Start()
    {
        if (_visual != null)
        {
            _visualStart = _visual.localPosition;
        }

        if (_liquidRenderer != null && _data != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, _data.Color);
            _liquidRenderer.SetPropertyBlock(block);
        }
    }

    private void Update()
    {
        if (_visual == null)
            return;

        _visual.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.World);
        _visual.localPosition = _visualStart + Vector3.up * (Mathf.Sin(Time.time * _bobSpeed) * _bobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out PotionBag bag))
            return;

        // Si la bolsa está llena, la poción queda en el piso.
        if (bag.TryAdd(_data))
        {
            Destroy(gameObject);
        }
    }
}
