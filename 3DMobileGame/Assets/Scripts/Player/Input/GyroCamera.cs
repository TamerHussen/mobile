using UnityEngine;

public class GyroCamera : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float leanAmount = 15f;
    private Quaternion initialRotation;

    void Start()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            initialRotation = transform.localRotation;
        }
        else
        {
            Debug.LogWarning("Gyro not supported on this device.");
        }
    }

    void Update()
    {
        if (!SystemInfo.supportsGyroscope) return;

        Quaternion deviceRotation = GyroToUnity(Input.gyro.attitude);
        Vector3 euler = deviceRotation.eulerAngles;

        float yaw = euler.y;
        Quaternion targetRotation = Quaternion.Euler(0, yaw, 0);
        player.rotation = Quaternion.Slerp(player.rotation, targetRotation, Time.deltaTime * rotationSpeed);

        float roll = Mathf.DeltaAngle(0, euler.z);
        Quaternion leanRot = Quaternion.Euler(0, 0, -roll * leanAmount / 45f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, initialRotation * leanRot, Time.deltaTime * 5f);
    }

    private Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }
}
