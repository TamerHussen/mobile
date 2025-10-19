using UnityEngine;

public class GyroPlayerController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody playerRigidbody;
    public Transform playerTransform;

    [Header("Settings")]
    public bool enableGyro = true;
    public float yawSmooth = 6f;
    public float cameraSmooth = 8f;
    public float maxUpPitch = 60f;
    public float maxDownPitch = -60f;
    public float leanAmount = 0.3f;

    private Quaternion cameraInitialLocalRot;
    private Quaternion playerInitialRot;
    private Quaternion gyroStartInverse = Quaternion.identity;

    void Start()
    {
        if (playerTransform == null || playerRigidbody == null)
        {
            Debug.LogError("Assign playerTransform and playerRigidbody!");
            enabled = false;
            return;
        }

        cameraInitialLocalRot = transform.localRotation;
        playerInitialRot = playerTransform.rotation;

        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            CalibrateGyro();
        }

        playerRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (!enableGyro || !SystemInfo.supportsGyroscope) return;

        Quaternion deviceRot = Input.gyro.attitude;
        deviceRot = new Quaternion(deviceRot.x, deviceRot.y, -deviceRot.z, -deviceRot.w);
        deviceRot = gyroStartInverse * deviceRot;

        Vector3 euler = deviceRot.eulerAngles;

        // Handle pitch (up/down) and roll (lean)
        float pitch = Mathf.Clamp(NormalizeAngle(euler.x), maxDownPitch, maxUpPitch);
        float roll = NormalizeAngle(euler.z);
        Quaternion pitchQuat = Quaternion.Euler(pitch, 0f, 0f);
        Quaternion leanQuat = Quaternion.Euler(0f, 0f, roll * leanAmount);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, cameraInitialLocalRot * pitchQuat * leanQuat, Time.deltaTime * cameraSmooth);
    }

    public void CalibrateGyro()
    {
        if (!SystemInfo.supportsGyroscope) return;
        Quaternion raw = Input.gyro.attitude;
        raw = new Quaternion(raw.x, raw.y, -raw.z, -raw.w);
        gyroStartInverse = Quaternion.Inverse(raw);
        playerInitialRot = playerTransform.rotation;
    }

    private float NormalizeAngle(float angle)
    {
        angle = (angle + 180f) % 360f;
        if (angle < 0) angle += 360f;
        return angle - 180f;
    }
}
