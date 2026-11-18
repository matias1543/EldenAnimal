
using UnityEngine;

/// <summary>
/// Gestiona la interacción del jugador con los NPCs.
/// Permite hablar únicamente con el NPC más cercano
/// que se encuentre dentro de su rango.
/// </summary>
[RequireComponent(typeof(DialogueUI2))]
public class DialogueInteractionManager : MonoBehaviour
{
    private Transform _player;
    private DialogueUI2 _ui;
    private NPCInteraction[] _npcs;

    private NPCInteraction _activeNPC;
    private bool _isTalking;

    private void Start()
    {
        _ui = GetComponent<DialogueUI2>();

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            _player = player.transform;

        // Buscamos los NPCs presentes en la escena.
        _npcs = FindObjectsByType<NPCInteraction>(
            FindObjectsSortMode.None
        );
    }

    private void Update()
    {
        if (_player == null)
            return;

        NPCInteraction nearestNPC = FindNearestNPC();

        // Si estamos hablando, mantenemos el diálogo
        // mientras el jugador siga cerca del NPC.
        if (_isTalking)
        {
            if (_activeNPC == null ||
                !_activeNPC.CanInteract(_player))
            {
                CloseDialogue();
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                CloseDialogue();
            }

            return;
        }

        // Mostramos el mensaje solo si hay
        // un NPC dentro del rango.
        if (nearestNPC == null)
        {
            _ui.HidePrompt();
            return;
        }

        _ui.ShowPrompt();

        if (Input.GetKeyDown(KeyCode.E))
        {
            _activeNPC = nearestNPC;
            _isTalking = true;

            _ui.ShowDialogue(
                _activeNPC.Dialogue.NPCName,
                _activeNPC.Dialogue.DialogueText
            );
        }
    }

    /// <summary>
    /// Encuentra el NPC interactuable más cercano.
    /// </summary>
    private NPCInteraction FindNearestNPC()
    {
        NPCInteraction nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (NPCInteraction npc in _npcs)
        {
            if (npc == null ||
                !npc.isActiveAndEnabled ||
                !npc.CanInteract(_player))
                continue;

            float distance = Vector3.Distance(
                _player.position,
                npc.transform.position
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = npc;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Cierra el diálogo y permite volver a interactuar.
    /// </summary>
    private void CloseDialogue()
    {
        _isTalking = false;
        _activeNPC = null;

        _ui.HideDialogue();
        _ui.HidePrompt();
    }
}
