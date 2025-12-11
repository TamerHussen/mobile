using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using System.Text;
using UnityEngine;

public class SaveToCloudManager : MonoBehaviour
{
    public void SaveToCloud()
    {
        var savedGameClient = GooglePlayGames.PlayGamesPlatform.Instance.SavedGame;
        savedGameClient.OpenWithAutomaticConflictResolution("savefile",
            DataSource.ReadCacheOrNetwork,
            ConflictResolutionStrategy.UseLongestPlaytime,
            (status, game) =>
            {
                if (status == SavedGameRequestStatus.Success)
                {
                    string json = JsonUtility.ToJson(SaveManager.Instance.data);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    SavedGameMetadataUpdate update = new SavedGameMetadataUpdate.Builder().Build();
                    savedGameClient.CommitUpdate(game, update, data, (commitStatus, _) =>
                    {
                        Debug.Log("Saved to Cloud: " + (commitStatus == SavedGameRequestStatus.Success));
                    });
                }
                else
                {
                    Debug.LogWarning("Failed to open saved game for cloud save.");
                }
            });
    }
}
