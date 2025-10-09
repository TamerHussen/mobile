using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Accelerometer : MonoBehaviour
{
    public bool isFlat = true;
    private Rigidbody _rigid;

    void Start()
    {
        _rigid = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Vector3 tilt = Input.acceleration;

        if (isFlat)
            tilt = Quaternion.Euler(90, 0, 0) * tilt;

        _rigid.AddForce(tilt, ForceMode.Acceleration);
    }
}
