
using System;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// Base de todo lo que bloquea un camino hasta cumplir una condición.
///
/// Se encarga de lo común: qué objetos bloquean el paso (colliders, barreras),
/// el NavMeshLink que se habilita al abrir y el aviso de "camino abierto".
/// Cada cerradura concreta decide CUÁNDO abrirse:
/// - GemLock: al tener cierta cantidad de gemas (Brutus, Puerta de la Cima, Cueva).
/// - QuestLock: al completar una misión (Kiko en el puente sur).
/// </summary>
public abstract class PathLock : MonoBehaviour
{
    [Header("Path Lock")]
    // Objetos que bloquean el paso; se desactivan al abrir.
    [SerializeField] private GameObject[] _blockers = new GameObject[0];

    // Conexión de NavMesh (puente, cueva): los enemigos solo cruzan con el camino abierto.
    [SerializeField] private NavMeshLink _navMeshLink;

    [SerializeField] private string _openedMessage = "¡Se abrió el camino!";

    public string OpenedMessage => _openedMessage;
    public bool IsOpen { get; private set; }

    public event Action<PathLock> OnOpened;

    protected virtual void Awake()
    {
        SetLinkActive(false);
    }

    protected void Open()
    {
        if (IsOpen)
            return;

        IsOpen = true;

        foreach (GameObject blocker in _blockers)
        {
            if (blocker != null)
            {
                blocker.SetActive(false);
            }
        }

        SetLinkActive(true);
        HandleOpened();
        OnOpened?.Invoke(this);
    }

    // Efectos propios de cada cerradura al abrirse (animaciones, sonidos...).
    protected virtual void HandleOpened() { }

    private void SetLinkActive(bool active)
    {
        if (_navMeshLink != null)
        {
            _navMeshLink.activated = active;
        }
    }
}
