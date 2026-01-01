using UnityEngine;

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
        initialLocalRotation = transform.localRotation;

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

        InitializeGyro();
    }

    void InitializeGyro()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            gyroAvailable = true;

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

        Quaternion deviceRotation = GetGyroRotation();

        Vector3 euler = deviceRotation.eulerAngles;

        float pitch = NormalizeAngle(euler.x);
        float roll = NormalizeAngle(euler.z);

        pitch = Mathf.Clamp(pitch, maxDownAngle, maxUpAngle);

        currentPitch = Mathf.Lerp(currentPitch, pitch, Time.deltaTime * pitchSensitivity);

        Quaternion pitchRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        Quaternion rollRotation = Quaternion.Euler(0f, 0f, roll * rollLeanAmount);

        Quaternion targetRotation = initialLocalRotation * pitchRotation * rollRotation;

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
        Quaternion gyroAttitude = Input.gyro.attitude;

        Quaternion convertedRotation = new Quaternion(
            gyroAttitude.x,
            gyroAttitude.y,
            -gyroAttitude.z,
            -gyroAttitude.w
        );

        return gyroCalibration * convertedRotation;
    }

    public void CalibrateGyro()
    {
        if (!gyroAvailable) return;

        Quaternion currentGyro = Input.gyro.attitude;
        currentGyro = new Quaternion(
            currentGyro.x,
            currentGyro.y,
            -currentGyro.z,
            -currentGyro.w
        );

        gyroCalibration = Quaternion.Inverse(currentGyro);

        currentPitch = 0f;

        if (showDebugLogs)
            Debug.Log("Gyro calibrated to current device orientation");
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && gyroAvailable)
        {
            Input.gyro.enabled = true;
            if (showDebugLogs)
                Debug.Log("Gyro re-enabled after app focus");
        }
    }

    void OnApplicationPause(bool isPaused)
    {
        if (!isPaused && gyroAvailable)
        {
            Input.gyro.enabled = true;
        }
    }
}