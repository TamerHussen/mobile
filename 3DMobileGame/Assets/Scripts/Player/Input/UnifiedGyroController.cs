using UnityEngine;

// attach this to Main Camera ONLY
// remove all other gyro scripts
public class UnifiedGyroController : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Camera Pitch Settings")]
    [Tooltip("How fast camera tilts up/down")]
    public float pitchSensitivity = 3f;

    [Tooltip("Max angle camera can look up")]
    public float maxUpAngle = 60f;

    [Tooltip("Max angle camera can look down")]
    public float maxDownAngle = -60f;

    [Header("Camera Roll/Lean Settings")]
    [Tooltip("How much camera leans when device tilts")]
    public float rollLeanAmount = 0.3f;

    [Tooltip("How smooth the camera movement is")]
    public float smoothSpeed = 8f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool gyroAvailable = false;
    private Quaternion initialLocalRotation;
    private Quaternion gyroCalibration = Quaternion.identity;
    private float currentPitch = 0f;

    void Start()
    {
        // save initial camera rotation
        initialLocalRotation = transform.localRotation;

        // find player if not assigned
        if (player == null)
        {
            player = transform.parent;
            if (player == null)
            {
                Debug.LogError("Camera must be child of Player or have player assigned!");
                enabled = false;
                return;
            }
        }

        // init gyro
        InitializeGyro();
    }

    void InitializeGyro()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            gyroAvailable = true;

            // wait a moment then calibrate
            Invoke(nameof(CalibrateGyro), 0.5f);

            if (showDebugLogs)
                Debug.Log("Gyroscope enabled and calibrating...");
        }
        else
        {
            gyroAvailable = false;
            if (showDebugLogs)
                Debug.LogWarning("Gyroscope not supported on this device");
        }
    }

    void Update()
    {
        if (!gyroAvailable) return;

        // get gyro data converted to unity space
        Quaternion deviceRotation = GetGyroRotation();

        // extract pitch (up/down) and roll (tilt left/right)
        Vector3 euler = deviceRotation.eulerAngles;

        // convert to -180 to 180 range
        float pitch = NormalizeAngle(euler.x);
        float roll = NormalizeAngle(euler.z);

        // clamp pitch for comfort
        pitch = Mathf.Clamp(pitch, maxDownAngle, maxUpAngle);

        // smooth the pitch value
        currentPitch = Mathf.Lerp(currentPitch, pitch, Time.deltaTime * pitchSensitivity);

        // build target rotation: pitch for look up/down, roll for lean
        Quaternion pitchRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        Quaternion rollRotation = Quaternion.Euler(0f, 0f, roll * rollLeanAmount);

        // combine rotations
        Quaternion targetRotation = initialLocalRotation * pitchRotation * rollRotation;

        // smoothly apply to camera
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * smoothSpeed
        );

        if (showDebugLogs && Time.frameCount % 30 == 0)
        {
            Debug.Log($"Pitch: {currentPitch:F1}° | Roll: {roll:F1}°");
        }
    }

    Quaternion GetGyroRotation()
    {
        // get raw gyro attitude
        Quaternion gyroAttitude = Input.gyro.attitude;

        // convert from right-handed (gyro) to left-handed (unity)
        Quaternion convertedRotation = new Quaternion(
            gyroAttitude.x,
            gyroAttitude.y,
            -gyroAttitude.z,
            -gyroAttitude.w
        );

        // apply calibration offset
        return gyroCalibration * convertedRotation;
    }

    public void CalibrateGyro()
    {
        if (!gyroAvailable) return;

        // get current gyro reading
        Quaternion currentGyro = Input.gyro.attitude;
        currentGyro = new Quaternion(
            currentGyro.x,
            currentGyro.y,
            -currentGyro.z,
            -currentGyro.w
        );

        // store inverse as calibration offset
        gyroCalibration = Quaternion.Inverse(currentGyro);

        // reset pitch
        currentPitch = 0f;

        if (showDebugLogs)
            Debug.Log("Gyro calibrated to current device orientation");
    }

    float NormalizeAngle(float angle)
    {
        // convert 0-360 to -180 to 180
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // re-enable gyro when app regains focus
        if (hasFocus && gyroAvailable)
        {
            Input.gyro.enabled = true;
            if (showDebugLogs)
                Debug.Log("Gyro re-enabled after app focus");
        }
    }

    void OnApplicationPause(bool isPaused)
    {
        // handle app pause/resume
        if (!isPaused && gyroAvailable)
        {
            Input.gyro.enabled = true;
        }
    }
}