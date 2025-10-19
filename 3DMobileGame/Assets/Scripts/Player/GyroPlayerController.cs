using UnityEngine;

public class GyroPlayerController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody playerRigidbody; // your player Rigidbody
    public Transform playerTransform; // assign the player object here (your PlayerController object)

    [Header("Settings")]
    public bool enableGyro = true;
    public float yawSmooth = 6f;
    public float cameraSmooth = 8f;
    public float lookSensitivity = 1.0f;
    public float maxUpPitch = 60f;
    public float maxDownPitch = -60f;
    public float leanAmount = 0.5f;

    private Quaternion cameraInitialLocalRot;
    private Quaternion playerInitialRot;

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
            Input.gyro.enabled = true;

        // Freeze X/Z rotation to prevent tipping
        playerRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (!enableGyro || !SystemInfo.supportsGyroscope) return;

        Quaternion deviceRotation = GyroToUnity(Input.gyro.attitude);
        Vector3 euler = deviceRotation.eulerAngles * lookSensitivity;

        float pitch = Mathf.Clamp(NormalizeAngle(euler.x), maxDownPitch, maxUpPitch);
        float yaw = NormalizeAngle(euler.y);
        float roll = NormalizeAngle(euler.z);

        // Apply yaw to player
        Quaternion targetPlayerRot = Quaternion.Euler(0f, yaw + playerInitialRot.eulerAngles.y, 0f);
        playerRigidbody.MoveRotation(Quaternion.Slerp(playerRigidbody.rotation, targetPlayerRot, Time.deltaTime * yawSmooth));

        // Apply pitch + lean to camera
        Quaternion pitchQuat = Quaternion.Euler(pitch, 0f, 0f);
        Quaternion leanQuat = Quaternion.Euler(0f, 0f, roll * leanAmount);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, cameraInitialLocalRot * pitchQuat * leanQuat, Time.deltaTime * cameraSmooth);
    }

    private Quaternion GyroToUnity(Quaternion q) => new Quaternion(q.x, q.y, -q.z, -q.w);

    private float NormalizeAngle(float angle)
    {
        angle = (angle + 180f) % 360f;
        if (angle < 0) angle += 360f;
        return angle - 180f;
    }
}
