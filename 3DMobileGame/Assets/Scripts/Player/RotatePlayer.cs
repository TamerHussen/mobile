using UnityEngine;

public class RotatePlayer : MonoBehaviour
{
    public float rotationSpeed = 40f;

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}
