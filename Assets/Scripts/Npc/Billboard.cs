
using UnityEngine;

/// <summary>
/// Hace que el objeto mire siempre hacia la cámara principal
/// (por ejemplo, el "!" sobre la cabeza del NPC).
/// </summary>
public class Billboard : MonoBehaviour
{
    private Transform _camera;

    private void Start()
    {
        if (Camera.main != null)
        {
            _camera = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (_camera != null)
        {
            transform.rotation = _camera.rotation;
        }
    }
}
