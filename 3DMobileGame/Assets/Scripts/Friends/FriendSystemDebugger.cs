using Unity.Services.Authentication;
using UnityEngine;
using TMPro;

public class FriendSystemDebugger : MonoBehaviour
{
    [Header("Debug UI (Optional)")]
    public TextMeshProUGUI debugOutput;

    public void DebugFriendSystem()
    {
        string output = "=== FRIEND SYSTEM DEBUG ===\n\n";

        if (SaveManager.Instance == null)
        {
            output += "❌ SaveManager is NULL!\n";
        }
        else if (SaveManager.Instance.data == null)
        {
            output += "❌ SaveManager.data is NULL!\n";
        }
        else
        {
            output += "✅ SaveManager exists\n";
            output += $"Display Name: '{SaveManager.Instance.data.playerName}'\n";
            output += $"Unique Name: '{SaveManager.Instance.data.uniquePlayerName}'\n\n";
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            output += "❌ Not signed in to Authentication!\n";
        }
        else
        {
            output += "✅ Authentication signed in\n";
            output += $"Player ID: {AuthenticationService.Instance.PlayerId}\n";
            output += $"Auth Name: '{AuthenticationService.Instance.PlayerName}'\n\n";
        }

        output += "--- VALIDATION ---\n";

        if (SaveManager.Instance?.data != null && AuthenticationService.Instance.IsSignedIn)
        {
            string displayName = SaveManager.Instance.data.playerName;
            string uniqueName = SaveManager.Instance.data.uniquePlayerName;
            string authName = AuthenticationService.Instance.PlayerName;

            if (string.IsNullOrEmpty(uniqueName))
            {
                output += "❌ Unique name is EMPTY!\n";
            }
            else if (!uniqueName.Contains("#"))
            {
                output += $"❌ Unique name missing #: '{uniqueName}'\n";
            }
            else
            {
                output += $"✅ Unique name has #: '{uniqueName}'\n";
            }

            if (!string.IsNullOrEmpty(uniqueName) && uniqueName.Contains("#"))
            {
                string expectedDisplay = uniqueName.Split('#')[0];
                if (displayName == expectedDisplay)
                {
                    output += $"✅ Display matches unique prefix\n";
                }
                else
                {
                    output += $"⚠️ Display '{displayName}' != Unique prefix '{expectedDisplay}'\n";
                }
            }

            if (authName == uniqueName)
            {
                output += $"✅ Auth name matches unique name\n";
            }
            else
            {
                output += $"❌ Auth '{authName}' != Unique '{uniqueName}'\n";
            }

            output += "\n--- FRIEND REQUEST INFO ---\n";
            output += "To add you as a friend, they need to type:\n";
            output += $">>> {uniqueName} <<<\n";
            output += "\n";
            output += "To add someone else, you need their unique name with #\n";
        }

        Debug.Log(output);

        if (debugOutput != null)
        {
            debugOutput.text = output;
        }
    }

    public void DebugFriendRequest(string inputName)
    {
        Debug.Log($"=== FRIEND REQUEST DEBUG ===");
        Debug.Log($"Input name: '{inputName}'");
        Debug.Log($"My Auth Name: '{AuthenticationService.Instance.PlayerName}'");
        Debug.Log($"My Unique Name: '{SaveManager.Instance.data.uniquePlayerName}'");
        Debug.Log($"Has # in input: {inputName.Contains("#")}");

        if (inputName.Contains("#"))
        {
            var parts = inputName.Split('#');
            Debug.Log($"Display part: '{parts[0]}'");
            Debug.Log($"Suffix part: '{parts[1]}'");
        }
    }
}