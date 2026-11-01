
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimapa con cámara cenital.
///
/// - Modo chico: sigue al jugador (esquina de la pantalla).
/// - Modo mapa completo (tecla M): muestra toda la isla en el centro.
///
/// Los íconos (jugador, enemigos, cofres, portal) son objetos en la layer
/// Minimap: solo los ve esta cámara, no la cámara principal.
/// </summary>
[RequireComponent(typeof(Camera))]
public class Minimap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _target;

    // Cámara principal: copiamos su giro para que "arriba" en el mapa sea "adelante" en pantalla.
    [SerializeField] private Transform _viewReference;
    [SerializeField] private RectTransform _mapFrame;

    [Header("Camera")]
    [SerializeField] private float _height = 80f;
    [SerializeField] private float _followSize = 22f;
    [SerializeField] private float _fullMapSize = 58f;
    [SerializeField] private Vector3 _mapCenter = Vector3.zero;

    [Header("Layout")]
    [SerializeField] private Vector2 _smallSize = new Vector2(320f, 320f);
    [SerializeField] private Vector2 _smallMargin = new Vector2(40f, 40f);
    [SerializeField] private Vector2 _fullSize = new Vector2(900f, 900f);

    private Camera _camera;
    private bool _fullMap;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _camera.orthographic = true;
        ApplyLayout();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            _fullMap = !_fullMap;
            ApplyLayout();
        }
    }

    private void LateUpdate()
    {
        Vector3 center = _fullMap || _target == null ? _mapCenter : _target.position;
        transform.position = center + Vector3.up * _height;

        float yaw = _viewReference != null ? _viewReference.eulerAngles.y : 0f;
        transform.rotation = Quaternion.Euler(90f, yaw, 0f);
    }

    private void ApplyLayout()
    {
        _camera.orthographicSize = _fullMap ? _fullMapSize : _followSize;

        if (_mapFrame == null)
            return;

        if (_fullMap)
        {
            _mapFrame.anchorMin = _mapFrame.anchorMax = _mapFrame.pivot = new Vector2(0.5f, 0.5f);
            _mapFrame.anchoredPosition = Vector2.zero;
            _mapFrame.sizeDelta = _fullSize;
        }
        else
        {
            _mapFrame.anchorMin = _mapFrame.anchorMax = _mapFrame.pivot = new Vector2(1f, 0f);
            _mapFrame.anchoredPosition = new Vector2(-_smallMargin.x, _smallMargin.y);
            _mapFrame.sizeDelta = _smallSize;
        }
    }
}
