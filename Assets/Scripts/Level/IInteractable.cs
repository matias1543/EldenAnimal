
using UnityEngine;

/// <summary>
/// Contrato para cualquier elemento del escenario con el que
/// el jugador puede interactuar (cofres, puertas, palancas...).
///
/// PlayerInteractor no conoce las clases concretas: solo usa esta interfaz.
/// </summary>
public interface IInteractable
{
    // Texto que se muestra en pantalla, por ejemplo "E: Abrir cofre".
    string Prompt { get; }

    // Indica si todavía se puede interactuar (un cofre abierto ya no).
    bool CanInteract { get; }

    void Interact(GameObject interactor);
}
