using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 7f;
    [SerializeField] private float _jumpAccelThreshold = 1.5f;

    // ✅ AUTO-FOUND AT RUNTIME (not serialized)
    private Rigidbody _rigidbody;
    private FixedJoystick _joystick; // Move joystick
    private FixedJoystick _lookJoystick; // Look joystick
    private Animator _animator;

    private Transform NorthWall;
    private Transform SouthWall;
    private Transform EastWall;
    private Transform WestWall;

    private bool teleported;
    private float currentYaw = 0f;
    private bool _isGrounded = true;
    private Vector3 _dodgeVelocity = Vector3.zero;
    private float _dodgeTimeRemaining = 0f;

    private void Awake()
    {
        // Get Rigidbody
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError("Rigidbody not found on player!");
            return;
        }

        _rigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        // Get Animator (optional)
        _animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        // ✅ AUTO-FIND JOYSTICKS BY NAME
        FindJoysticks();

        // ✅ AUTO-FIND PORTAL WALLS BY TAG
        FindPortalWalls();
    }

    void FindJoysticks()
    {
        // Find joysticks in scene by name
        var moveJoystickObj = GameObject.Find("Fixed Joystick");
        var lookJoystickObj = GameObject.Find("Rotation Joystick");

        if (moveJoystickObj != null)
        {
            _joystick = moveJoystickObj.GetComponent<FixedJoystick>();
            if (_joystick != null)
                Debug.Log("✅ Move joystick auto-assigned");
            else
                Debug.LogWarning("⚠️ 'Fixed Joystick' found but no FixedJoystick component!");
        }
        else
        {
            Debug.LogWarning("⚠️ Move joystick not found! Looking for 'Fixed Joystick' GameObject.");
        }

        if (lookJoystickObj != null)
        {
            _lookJoystick = lookJoystickObj.GetComponent<FixedJoystick>();
            if (_lookJoystick != null)
                Debug.Log("✅ Look joystick auto-assigned");
            else
                Debug.LogWarning("⚠️ 'Rotation Joystick' found but no FixedJoystick component!");
        }
        else
        {
            Debug.LogWarning("⚠️ Look joystick not found! Looking for 'Rotation Joystick' GameObject.");
        }
    }

    void FindPortalWalls()
    {
        // Find walls by tag
        var northWallObj = GameObject.FindGameObjectWithTag("NorthWall");
        var southWallObj = GameObject.FindGameObjectWithTag("SouthWall");
        var eastWallObj = GameObject.FindGameObjectWithTag("EastWall");
        var westWallObj = GameObject.FindGameObjectWithTag("WestWall");

        if (northWallObj != null) NorthWall = northWallObj.transform;
        if (southWallObj != null) SouthWall = southWallObj.transform;
        if (eastWallObj != null) EastWall = eastWallObj.transform;
        if (westWallObj != null) WestWall = westWallObj.transform;

        if (NorthWall == null || SouthWall == null || EastWall == null || WestWall == null)
        {
            Debug.LogWarning("⚠️ Portal walls not found! Make sure walls are tagged correctly.");
        }
        else
        {
            Debug.Log("✅ Portal walls auto-assigned");
        }
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
            Vector3 moveDir = (Vector3.forward * input.z + Vector3.right * input.x).normalized;
            moveDir = Quaternion.Euler(0f, currentYaw, 0f) * moveDir;
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
            _isGrounded = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ✅ Check by TAG instead of layer (more reliable)
        if (!teleported)
        {
            if (other.CompareTag("NorthWall") && SouthWall != null)
            {
                teleported = true;
                transform.position = SouthWall.position;
                Debug.Log("Teleported: North → South");
            }
            else if (other.CompareTag("SouthWall") && NorthWall != null)
            {
                teleported = true;
                transform.position = NorthWall.position;
                Debug.Log("Teleported: South → North");
            }
            else if (other.CompareTag("EastWall") && WestWall != null)
            {
                teleported = true;
                transform.position = WestWall.position;
                Debug.Log("Teleported: East → West");
            }
            else if (other.CompareTag("WestWall") && EastWall != null)
            {
                teleported = true;
                transform.position = EastWall.position;
                Debug.Log("Teleported: West → East");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Reset cooldown when leaving any portal wall
        if (other.CompareTag("NorthWall") || other.CompareTag("SouthWall") ||
            other.CompareTag("EastWall") || other.CompareTag("WestWall"))
        {
            Invoke("ResetTeleportCooldown", 1f);
        }
    }

    void ResetTeleportCooldown()
    {
        teleported = false;
    }

    public void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}