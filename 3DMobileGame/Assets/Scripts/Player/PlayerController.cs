using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private FixedJoystick _joystick; // Move joystick
    [SerializeField] private FixedJoystick _lookJoystick; // Look joystick
    [SerializeField] private Animator _animator;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 7f;
    [SerializeField] private float _jumpAccelThreshold = 1.5f; // Adjust to control jump sensitivity

    //Walls
    [SerializeField] Transform NorthWall;
    [SerializeField] Transform SouthWall;
    [SerializeField] Transform EastWall;
    [SerializeField] Transform WestWall;

    bool teleported;

    private float currentYaw = 0f; // track player yaw

    private bool _isGrounded = true;

    // Dodge fields
    private Vector3 _dodgeVelocity = Vector3.zero;
    private float _dodgeTimeRemaining = 0f;

    private void Awake()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        RotatePlayer();
        Move();

        if (_dodgeTimeRemaining > 0f)
        {
            _dodgeTimeRemaining -= Time.fixedDeltaTime;
            if (_dodgeTimeRemaining <= 0f)
            {
                _dodgeTimeRemaining = 0f;
                _dodgeVelocity = Vector3.zero;
            }
        }
    }

    private void Update()
    {
        CheckJumpByAccelerometer();
    }

    private void Move()
    {
        Vector3 input = new Vector3(_joystick.Horizontal, 0f, _joystick.Vertical);
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 horizontalVelocity;
        if (_dodgeTimeRemaining > 0f)
        {
            horizontalVelocity = new Vector3(_dodgeVelocity.x, 0f, _dodgeVelocity.z);
        }
        else
        {
            // Move relative to world, not player rotation
            Vector3 moveDir = (Vector3.forward * input.z + Vector3.right * input.x).normalized;
            moveDir = Quaternion.Euler(0f, currentYaw, 0f) * moveDir; // apply facing direction to movement
            Vector3 baseMove = moveDir * _moveSpeed;
            horizontalVelocity = new Vector3(baseMove.x, 0f, baseMove.z);
        }

        Vector3 targetVel = horizontalVelocity;
        targetVel.y = _rigidbody.linearVelocity.y;

        Vector3 newPos = _rigidbody.position + targetVel * Time.fixedDeltaTime;
        _rigidbody.MovePosition(newPos);

        if (_animator != null)
            _animator.SetFloat("Speed", input.magnitude);
    }

    private void RotatePlayer()
    {
        if (_lookJoystick == null) return;

        float turnSpeed = 100f;
        float lookInput = _lookJoystick.Horizontal;

        currentYaw += lookInput * turnSpeed * Time.deltaTime;

        Quaternion targetRotation = Quaternion.Euler(0f, currentYaw, 0f);
        _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, targetRotation, Time.fixedDeltaTime * 8f));
    }

    public void StartDodge(Vector3 worldDirection, float strength, float duration)
    {
        Vector3 dir = worldDirection;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

        _dodgeVelocity = dir.normalized * strength;
        _dodgeTimeRemaining = Mathf.Max(0.05f, duration);
    }

    private void CheckJumpByAccelerometer()
    {
        Vector3 accel = Input.acceleration;

        if (_isGrounded && accel.sqrMagnitude > _jumpAccelThreshold * _jumpAccelThreshold)
        {
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            _isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.7f)
        {
            Debug.Log($"Player collided with {collision.collider.name} - contact normal: {collision.contacts[0].normal}");

            _isGrounded = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            if (!teleported && other.gameObject.CompareTag("NorthWall"))
            {
                teleported = true;
                transform.position = SouthWall.transform.position;
            }
            if (!teleported && other.gameObject.CompareTag("SouthWall"))
            {
                teleported = true;
                transform.position = NorthWall.transform.position;
            }
            if (!teleported && other.gameObject.CompareTag("EastWall"))
            {
                teleported = true;
                transform.position = WestWall.transform.position;
            }
            if (!teleported && other.gameObject.CompareTag("WestWall"))
            {
                teleported = true;
                transform.position = EastWall.transform.position;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            Invoke("setTeleportCooldownOFF", 1f);
        }
    }

    void setTeleportCooldownOFF()
    {
        teleported = false;
    }

    // allow enemy to call vibration
    public void Vibrate()
    {
    #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
    #endif
    }
}
