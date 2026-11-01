using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class PlayerAnimation : MonoBehaviour
{
    private const string SpeedParameter = "Speed";

    [Header("Animation")]
    [SerializeField] private float _speedDampTime = 0.1f;

    private Animator _animator;
    private CharacterController _characterController;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _characterController = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        Vector3 velocity = _characterController.velocity;

        velocity.y = 0f;

        float speed = velocity.magnitude;

        _animator.SetFloat(SpeedParameter, speed, _speedDampTime, Time.deltaTime);
    }
}
