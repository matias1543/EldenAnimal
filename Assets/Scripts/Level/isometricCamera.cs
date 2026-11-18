using UnityEngine;

/// <summary>
/// Cámara isométrica inspirada en TUNIC.
///
/// Sigue suavemente al jugador manteniendo
/// una distancia y rotación fija.
/// </summary>
[RequireComponent(typeof(Camera))]
public class IsometricCamera : MonoBehaviour
{
    [Header("Target")]

    // Personaje que la cámara debe seguir.
    [SerializeField] private Transform _target;


    [Header("Camera Settings")]

    // Distancia entre la cámara y el jugador.
    [SerializeField] private float _distance = 12f;

    // Inclinación vertical de la cámara.
    [SerializeField] private float _angle = 45f;

    // Rotación horizontal para generar la vista isométrica.
    [SerializeField] private float _rotation = 45f;

    // Altura del punto hacia el que mira la cámara.
    [SerializeField] private float _targetHeight = 1f;


    [Header("Movement")]

    // Tiempo aproximado que tarda la cámara en seguir al jugador.
    // Valores menores producen un seguimiento más rápido.
    [SerializeField] private float _smoothTime = 0.2f;

    // Velocidad interna utilizada por SmoothDamp.
    private Vector3 _velocity;


    /// <summary>
    /// Configura la posición y rotación inicial de la cámara.
    /// </summary>
    private void Start()
    {
        if (_target == null)
        {
            Debug.LogError("Camera target is not assigned.");
            return;
        }

        // Colocamos la cámara directamente en su posición inicial.
        transform.position = CalculatePosition();

        // Establecemos la rotación fija de la cámara.
        transform.rotation = Quaternion.Euler(
            _angle,
            _rotation,
            0f
        );
    }


    /// <summary>
    /// LateUpdate se ejecuta después de Update.
    /// Es ideal para cámaras porque permite seguir al jugador
    /// después de que este haya actualizado su movimiento.
    /// </summary>
    private void LateUpdate()
    {
        if (_target == null)
            return;

        // Calculamos la posición deseada.
        Vector3 desiredPosition = CalculatePosition();

        // Movemos suavemente la cámara hacia esa posición.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _velocity,
            _smoothTime
        );

        // Mantenemos la orientación fija.
        transform.rotation = Quaternion.Euler(
            _angle,
            _rotation,
            0f
        );
    }


    /// <summary>
    /// Calcula la posición que debe ocupar la cámara
    /// según la posición del jugador, el ángulo y la distancia.
    /// </summary>
    private Vector3 CalculatePosition()
    {
        // Punto al que apunta la cámara.
        Vector3 targetPosition =
            _target.position + Vector3.up * _targetHeight;

        // Calculamos la orientación de la cámara.
        Quaternion rotation = Quaternion.Euler(
            _angle,
            _rotation,
            0f
        );

        // Calculamos el desplazamiento respecto al jugador.
        Vector3 offset = rotation * Vector3.back * _distance;

        // Devolvemos la posición deseada.
        return targetPosition + offset;
    }
}
