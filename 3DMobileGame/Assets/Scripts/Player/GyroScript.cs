using UnityEngine;

public class GyroScript : MonoBehaviour
{
    private Quaternion correctionQuaternion;

    void Start()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            Debug.Log("Gyro Enabled");
            UpdateCorrection();
        }
        else
        {
            Debug.LogWarning("Gyroscope not supported on this device.");
        }
    }

    void UpdateCorrection()
    {
        switch (Screen.orientation)
        {
            case ScreenOrientation.LandscapeLeft:
                correctionQuaternion = Quaternion.Euler(0f, 0f, 0f);
                break;
            case ScreenOrientation.LandscapeRight:
                correctionQuaternion = Quaternion.Euler(0f, 0f, 180f);
                break;
            case ScreenOrientation.Portrait:
                correctionQuaternion = Quaternion.Euler(90f, 0f, 0f);
                break;
            case ScreenOrientation.PortraitUpsideDown:
                correctionQuaternion = Quaternion.Euler(-90f, 0f, 0f);
                break;
            default:
                correctionQuaternion = Quaternion.identity;
                break;
        }
    }

    void Update()
    {
        if (!SystemInfo.supportsGyroscope) return;

        if (Input.deviceOrientation != DeviceOrientation.Unknown)
        {
            UpdateCorrection();
        }

        transform.localRotation = correctionQuaternion * GyroToUnity(Input.gyro.attitude);
    }

    private Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }
}
