using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using System.Text;
using UnityEngine;

public class SaveToCloudManager : MonoBehaviour
{
    public void SaveToCloud()
    {
        // Verify user is authenticated before attempting cloud save
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Debug.LogWarning("Cannot save to cloud: User not authenticated.");
            return;
        }

        var savedGameClient = PlayGamesPlatform.Instance.SavedGame;

        savedGameClient.OpenWithAutomaticConflictResolution(
            "savefile_01", // Unique filename
            DataSource.ReadCacheOrNetwork,
            ConflictResolutionStrategy.UseLongestPlaytime,
            OnSavedGameOpened);
    }

    private void OnSavedGameOpened(SavedGameRequestStatus status, ISavedGameMetadata game)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            // Serialize local data
            string json = JsonUtility.ToJson(SaveManager.Instance.data);
            byte[] data = Encoding.UTF8.GetBytes(json);

            // Update metadata (optional description)
            SavedGameMetadataUpdate update = new SavedGameMetadataUpdate.Builder()
                .WithUpdatedDescription("Saved at " + System.DateTime.Now.ToString())
                .Build();

            PlayGamesPlatform.Instance.SavedGame.CommitUpdate(game, update, data, (commitStatus, _) =>
            {
                Debug.Log("Cloud Save Status: " + commitStatus);
            });
        }
        else
        {
            Debug.LogError("Failed to open cloud save: " + status);
        }
    }
}
