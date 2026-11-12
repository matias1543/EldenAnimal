
using UnityEngine;

/// <summary>
/// Define los datos necesarios para interactuar
/// con un NPC, incluyendo su rango de interacción.
/// </summary>
[RequireComponent(typeof(NPCDialogue))]
public class NPCInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float _interactionRange = 3f;

    public float InteractionRange => _interactionRange;
    public NPCDialogue Dialogue { get; private set; }

    private void Awake()
    {
        Dialogue = GetComponent<NPCDialogue>();
    }

    /// <summary>
    /// Comprueba si el jugador está dentro del rango.
    /// </summary>
    public bool CanInteract(Transform player)
    {
        if (player == null)
            return false;

        float distance = Vector3.Distance(transform.position, player.position);

        return distance <= _interactionRange;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position,_interactionRange);
    }
}
