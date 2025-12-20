using UnityEngine;
using TMPro;
using Unity.Services.Authentication;
public class UsernameChanger : MonoBehaviour
{
    public TMP_InputField input;

    public async void ChangeBame()
    {
        if (string.IsNullOrEmpty(input.text)) return;
        
        await AuthenticationService.Instance.UpdatePlayerNameAsync(input.text);
        Debug.Log("Username changed to " + input.text);
    }
}
