using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private FixedJoystick _joystick;
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

    private void Awake()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        RotatePlayer();
        Move();

    }

    private void Update()
    {
        CheckJumpByAccelerometer();
    }

    private void Move()
    {
        // Get input each frame (Update-driven)
        Vector3 input = new Vector3(_joystick.Horizontal, 0f, _joystick.Vertical);
        if (input.sqrMagnitude > 1f)
            input.Normalize();



        // Movement direction relative to player rotation
        Vector3 moveDir = (transform.forward * input.z + transform.right * input.x).normalized;
        Vector3 targetVel = moveDir * _moveSpeed;
        targetVel.y = _rigidbody.linearVelocity.y;

        // Use MovePosition instead of editing velocity directly
        Vector3 newPos = _rigidbody.position + targetVel * Time.fixedDeltaTime;
        _rigidbody.MovePosition(newPos);

        if (_animator != null)
            _animator.SetFloat("Speed", input.magnitude);
    }



    private void RotatePlayer()
    {
        float turnSpeed = 150f; // degrees per second, tune as needed
        float joystickInput = _joystick.Horizontal;

        // accumulate yaw based on joystick input
        currentYaw += joystickInput * turnSpeed * Time.deltaTime;

        Quaternion targetRotation = Quaternion.Euler(0f, currentYaw, 0f);
        _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, targetRotation, Time.deltaTime * 8f));
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



}
