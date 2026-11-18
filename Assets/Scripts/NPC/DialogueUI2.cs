
using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Controla la interfaz visual de diálogos.
///
/// Es compartida por todos los NPCs.
/// No maneja las entradas del jugador ni su distancia.
/// </summary>
public class DialogueUI2 : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject _interactionPrompt;
    [SerializeField] private GameObject _dialoguePanel;
    [SerializeField] private TMP_Text _npcNameText;
    [SerializeField] private TMP_Text _dialogueText;

    private void Awake()
    {
        // Evitamos que existan dos gestores de diálogo.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        _interactionPrompt.SetActive(false);
        _dialoguePanel.SetActive(false);
    }

    /// <summary>
    /// Muestra el mensaje para interactuar.
    /// </summary>
    public void ShowPrompt()
    {
        _interactionPrompt.SetActive(true);
    }

    /// <summary>
    /// Oculta el mensaje de interacción.
    /// </summary>
    public void HidePrompt()
    {
        _interactionPrompt.SetActive(false);
    }

    /// <summary>
    /// Muestra el diálogo con el nombre del NPC
    /// y el texto correspondiente.
    /// </summary>
    public void ShowDialogue(string npcName, string message)
    {
        _npcNameText.text = npcName;
        _dialogueText.text = message;

        HidePrompt();
        _dialoguePanel.SetActive(true);
    }

    /// <summary>
    /// Oculta el panel de diálogo.
    /// </summary>
    public void HideDialogue()
    {
        _dialoguePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    internal void ShowDialogue(object nPCName, object dialogueText)
    {
        throw new NotImplementedException();
    }

    internal void ShowDialogue(string nPCName, object dialogueText)
    {
        throw new NotImplementedException();
    }
}
