
using UnityEngine;

/// <summary>
/// Datos de un tipo de poción (ScriptableObject).
///
/// Para crear una poción nueva (por ejemplo, una más grande)
/// se crea otro asset desde el menú Create, sin tocar código.
/// </summary>
[CreateAssetMenu(fileName = "Potion", menuName = "Elden Animal/Potion")]
public class PotionData : ScriptableObject
{
    [SerializeField] private string _displayName = "Poción";
    [SerializeField] private float _healAmount = 25f;
    [SerializeField] private Color _color = Color.red;

    public string DisplayName => _displayName;
    public float HealAmount => _healAmount;
    public Color Color => _color;
}
