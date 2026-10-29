/*

 Author: Marcos Vizio
 Email: marcosfvizio@gmail.com

 Creation Date: 2026-10-26 13:57:26
 Last Modification Date: 2026-10-29 03:14:46

 Another File Header is a Visual Studio Code extension to automatically or by command insert a header to your files.

*/

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Stamina))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _sprintAction;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _gravity = -20f; //Es la aceleracion hacia abajo en m/s².

    [Header("Sprint")]
    [SerializeField] private float _sprintMultiplier = 1.8f;
    [SerializeField] private float _sprintStaminaPerSecond = 25f;
    
    private const float GroundedVerticalVelocity = -2f; //Empuja con esta velocidad hacia el piso
    private CharacterController _characterController;
    private Stamina _stamina;
    private Transform _cameraTransform;
    private float _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _stamina = GetComponent<Stamina>();
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
        if (_sprintAction == null)
        {
            Debug.LogError("PlayerMovement: falta asignar Sprint Action en el Inspector");
            return;
        }
        _sprintAction.action.Enable();
    }

    private void OnDisable()
    {
        if (_moveAction == null) return;
        _moveAction.action.Disable();
        if (_sprintAction == null) return;
        _sprintAction.action.Disable();
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

        float speed = GetCurrentSpeed(direction);

        Vector3 motion = direction * speed * Time.deltaTime;

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

    private float GetCurrentSpeed(Vector3 direction)
    {
        if (_sprintAction == null) return _moveSpeed;

        bool wantsToSprint = _sprintAction.action.IsPressed() && direction.sqrMagnitude > 0f;

        if (!wantsToSprint) return _moveSpeed;

        float cost = _sprintStaminaPerSecond * Time.deltaTime;

        if(!_stamina.TryConsume(cost)) return _moveSpeed;

        return _moveSpeed * _sprintMultiplier;
    }
}
