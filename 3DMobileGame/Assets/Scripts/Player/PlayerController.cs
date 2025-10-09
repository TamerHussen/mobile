using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private FixedJoystick _joystick;
    [SerializeField] private Animator _animator;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 7f;
    [SerializeField] private float _jumpAccelThreshold = 1.5f; // Adjust to control jump sensitivity

    private bool _isGrounded = true;

    private void Awake()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Update()
    {
        CheckJumpByAccelerometer();
    }

    private void Move()
    {
        // Joystick input
        Vector3 direction = new Vector3(_joystick.Horizontal, 0f, _joystick.Vertical);

        if (direction.magnitude > 1f)
            direction.Normalize();

        // Player moves in its own facing direction (gyro will handle turning)
        Vector3 move = transform.TransformDirection(direction) * _moveSpeed;
        _rigidbody.linearVelocity = new Vector3(move.x, _rigidbody.linearVelocity.y, move.z);

        // Update animation speed
        if (_animator != null)
        {
            _animator.SetFloat("Speed", direction.magnitude);
        }
    }

    private void CheckJumpByAccelerometer()
    {
        Vector3 accel = Input.acceleration;

        // Simple jump detection: upward acceleration spike
        if (_isGrounded && accel.sqrMagnitude > _jumpAccelThreshold * _jumpAccelThreshold)
        {
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            _isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Basic ground check
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.7f)
        {
            _isGrounded = true;
        }
    }
}
