using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    [Header("Aim")]
    //Esta en grados por segundo
    [SerializeField] private float _rotationSpeed = 1080f;
    private Camera _camera;

    private void Start()
    {
        if (Camera.main == null)
        {
            Debug.LogError("Player Aim: No hay Camera.main");
            return;
        }
        _camera = Camera.main;
    }

    private void Update()
    {
        if (_camera == null) return; 
        if (Mouse.current == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = _camera.ScreenPointToRay(mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (!groundPlane.Raycast(ray, out float distance)) return;

        Vector3 aimPoint = ray.GetPoint(distance);

        Debug.DrawLine(transform.position, aimPoint, Color.red);

        Vector3 direction = aimPoint - transform.position;

        FaceDirection(direction);        
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }
}
