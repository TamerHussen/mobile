using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Samples.Friends;
using UnityEngine;

public class UsernameChanger : MonoBehaviour
{
    public TMP_InputField input;
    public TextMeshProUGUI feedbackText;
    public GameObject rootPanel;

    public async void ChangeName()
    {
        feedbackText.text = "";

        if (string.IsNullOrWhiteSpace(input.text))
        {
            feedbackText.text = "Name cannot be empty";
            return;
        }

        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(input.text);

            var relationships = FindFirstObjectByType<RelationshipsManager>();
            relationships?.RefreshLocalPlayerName();

            feedbackText.text = "Name changed!";
            Debug.Log("Username changed to " + input.text);

            Close();
        }
        catch (AuthenticationException e)
        {
            // if name already taken or invalid
            feedbackText.text = "Name already taken";
            Debug.LogWarning(e);
        }
    }

    public void Open()
    {
        input.text = "";
        feedbackText.text = "";
        rootPanel.SetActive(true);
    }

    public void Close()
    {
        rootPanel.SetActive(false);
    }
}
