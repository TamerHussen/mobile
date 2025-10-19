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
        Vector3 input = new Vector3(_joystick.Horizontal, 0f, _joystick.Vertical);
        if (input.magnitude > 1f) input.Normalize();

        Vector3 forward = transform.forward; // use the player object itself
        forward.y = 0f;
        Vector3 right = transform.right;
        right.y = 0f;

        Vector3 moveDir = (forward * input.z + right * input.x).normalized;
        Vector3 move = moveDir * _moveSpeed;

        _rigidbody.linearVelocity = new Vector3(move.x, _rigidbody.linearVelocity.y, move.z);

        if (_animator != null)
            _animator.SetFloat("Speed", input.magnitude);
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
