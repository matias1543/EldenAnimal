/*

 Author: Marcos Vizio
 Email: marcosfvizio@gmail.com

 Creation Date: 2026-10-26 13:57:26
 Last Modification Date: 2026-10-28 03:23:57

 Another File Header is a Visual Studio Code extension to automatically or by command insert a header to your files.

*/

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _moveAction;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _gravity = -20f; //Es la aceleracion hacia abajo en m/s².


    private const float GroundedVerticalVelocity = -2f; //Empuja con esta velocidad hacia el piso
    private CharacterController _characterController;
    private Transform _cameraTransform;
    private float _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (Camera.main == null)
        {
            Debug.LogError("PlayerMovement: No tiene MainCamera");
            return;
        }
        _cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        if (_moveAction == null)
        {
            Debug.LogError("PlayerMovement: falta asignar Move Action en el Inspector");
            return;
        }
        _moveAction.action.Enable();
    }

    private void OnDisable()
    {
        if (_moveAction == null) return;
        _moveAction.action.Disable();
    }

    private void Update()
    {
        if (_moveAction == null) return;
        
        Vector2 input;
        input = _moveAction.action.ReadValue<Vector2>();

        Vector3 direction = GetCameraRelativeDirection(input);
        //Para dibujar la direccion del movimiento y ver si funciona
        Debug.DrawRay(transform.position, direction, Color.blue);

        ApplyGravity();

        Vector3 motion = direction * _moveSpeed * Time.deltaTime;

        motion.y = _verticalVelocity * Time.deltaTime;

        _characterController.Move(motion);

    }

    private void ApplyGravity()
    {
        if (_characterController.isGrounded) _verticalVelocity = GroundedVerticalVelocity;

        _verticalVelocity += _gravity * Time.deltaTime; 
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (_cameraTransform == null)
        {
            return new Vector3(input.x, 0f, input.y);
        }

        Vector3 forward = _cameraTransform.forward;
        Vector3 right = _cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return forward * input.y + right * input.x;
    }
}
