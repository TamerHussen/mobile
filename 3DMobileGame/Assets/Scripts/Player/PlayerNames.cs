using TMPro;
using UnityEngine;

public class PlayerNames : MonoBehaviour
{
    public TextMeshPro NameText;
    public Vector3 offset = new Vector3(0, 2, 0);

    Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        // face camera
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
    }

    public void SetName(string PlayerName)
    {
        NameText.text = PlayerName;
    }
}
