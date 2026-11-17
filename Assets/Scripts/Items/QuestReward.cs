
using UnityEngine;

/// <summary>
/// Recompensa de una misión (ScriptableObject abstracto).
///
/// Cada tipo de recompensa implementa Apply a su manera
/// (más daño, pociones...). Quest no necesita saber cuál es.
/// </summary>
public abstract class QuestReward : ScriptableObject
{
    [SerializeField] private string _description = "Recompensa";

    // Texto para mostrar al completar la misión.
    public string Description => _description;

    public abstract void Apply(GameObject player);
}
