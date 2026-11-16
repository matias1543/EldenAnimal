
using UnityEngine;

/// <summary>
/// Puerta mágica que se abre sola al juntar las gemas necesarias.
///
/// Tiene un hueco por gema que se enciende a medida que el jugador
/// las consigue, aunque esté lejos: así sabe cuánto le falta.
/// </summary>
public class GemDoor : GemLock
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [Header("Sockets")]
    [SerializeField] private Renderer[] _sockets = new Renderer[0];
    [SerializeField] private Color _emptyColor = new Color(0.25f, 0.25f, 0.3f);
    [SerializeField] private Color _filledColor = new Color(1f, 0.85f, 0.2f);

    private MaterialPropertyBlock _propertyBlock;

    protected override void Awake()
    {
        base.Awake();
        _propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (_gemCollector != null)
        {
            _gemCollector.OnGemsChanged += HandleGemsChanged;
        }
    }

    private void OnDisable()
    {
        if (_gemCollector != null)
        {
            _gemCollector.OnGemsChanged -= HandleGemsChanged;
        }
    }

    private void Start()
    {
        UpdateSockets(CurrentGems);
    }

    private void HandleGemsChanged(int current, int required)
    {
        UpdateSockets(current);

        if (HasEnoughGems)
        {
            Open();
        }
    }

    private void UpdateSockets(int gems)
    {
        for (int i = 0; i < _sockets.Length; i++)
        {
            _propertyBlock.SetColor(BaseColorId, i < gems ? _filledColor : _emptyColor);
            _sockets[i].SetPropertyBlock(_propertyBlock);
        }
    }
}
