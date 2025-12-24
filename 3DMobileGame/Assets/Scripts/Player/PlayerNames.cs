using TMPro;
using Unity.Services.Lobbies.Models;
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
        transform.forward = cam.forward;
    }

    public void SetName(string PlayerName)
    {
        NameText.text = PlayerName;
    }

    public void UpdatePlayerUI(Player player)
    {
        if (player.Data != null && player.Data.TryGetValue("Name", out var nameData))
        {
            SetName(nameData.Value);
        }
        else
        {
            SetName("Unknown Player");
        }
    }
}
