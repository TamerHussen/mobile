using UnityEngine;

public class RotatePreview : MonoBehaviour
{
    public float speed = 40f;

    void Update()
    {
        transform.Rotate(Vector3.up * speed * Time.deltaTime);
    }
}
