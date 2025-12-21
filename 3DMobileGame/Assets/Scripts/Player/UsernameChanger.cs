using System.Text.RegularExpressions;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Samples.Friends;
using UnityEngine;

public class UsernameChanger : MonoBehaviour
{
    public TMP_InputField input;
    public TextMeshProUGUI feedbackText;
    public GameObject rootPanel;

    const int MinLength = 3;
    const int MaxLength = 16;

    // Letters, numbers, underscore
    static readonly Regex ValidNameRegex =
        new Regex(@"^[A-Za-z0-9_]+$");

    public async void ChangeName()
    {
        feedbackText.text = "";

        string name = input.text.Trim();

        // Empty
        if (string.IsNullOrEmpty(name))
        {
            feedbackText.text = "Name cannot be empty";
            return;
        }

        // Length
        if (name.Length < MinLength)
        {
            feedbackText.text = $"Name must be at least {MinLength} characters";
            return;
        }

        if (name.Length > MaxLength)
        {
            feedbackText.text = $"Name must be at most {MaxLength} characters";
            return;
        }

        if (!ValidNameRegex.IsMatch(name))
        {
            feedbackText.text = "Only letters, numbers, and _ are allowed";
            return;
        }

        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(name);

            var relationships = FindFirstObjectByType<RelationshipsManager>();
            relationships?.RefreshLocalPlayerName();

            LobbyInfo.Instance?.UpdateHostName(name);

            feedbackText.text = "Name changed!";
            Debug.Log("Username changed to " + name);

            Close();
        }
        catch (AuthenticationException e)
        {
            feedbackText.text = "Name unavailable or invalid";
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
