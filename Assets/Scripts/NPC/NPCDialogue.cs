
using UnityEngine;

/// <summary>
/// Almacena la información de diálogo de un NPC.
/// Cada personaje puede tener un nombre y mensaje diferente.
/// </summary>
public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Settings")]

    [SerializeField] private string _npcName = "Guardia";

    [TextArea(2, 5)]
    [SerializeField]
    private string _dialogueText = "Viajero, reuní las gemas para avanzar.";

    // Permite consultar los datos sin modificarlos.
    public string NPCName => _npcName;
    public string DialogueText => _dialogueText;
}
