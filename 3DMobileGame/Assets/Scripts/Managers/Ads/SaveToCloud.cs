using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using System.Text;
using UnityEngine;

public class SaveToCloudManager : MonoBehaviour
{
    public static SaveToCloudManager Instance;

    [Header("Settings")]
    public bool autoSyncOnSave = true;
    public float autoSyncInterval = 300f; // Every 5 minutes

    private float syncTimer = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!autoSyncOnSave) return;

        syncTimer += Time.deltaTime;
        if (syncTimer >= autoSyncInterval)
        {
            syncTimer = 0f;
            SaveToCloud();
        }
    }

    public void SaveToCloud()
    {
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Debug.LogWarning("Cannot save to cloud: User not authenticated.");
            return;
        }

        if (SaveManager.Instance == null || SaveManager.Instance.data == null)
        {
            Debug.LogError("SaveManager data is null!");
            return;
        }

        var savedGameClient = PlayGamesPlatform.Instance.SavedGame;
        savedGameClient.OpenWithAutomaticConflictResolution(
            "savefile_01",
            DataSource.ReadCacheOrNetwork,
            ConflictResolutionStrategy.UseLongestPlaytime,
            OnSavedGameOpened);
    }

    private void OnSavedGameOpened(SavedGameRequestStatus status, ISavedGameMetadata game)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            string json = JsonUtility.ToJson(SaveManager.Instance.data);
            byte[] data = Encoding.UTF8.GetBytes(json);

            SavedGameMetadataUpdate update = new SavedGameMetadataUpdate.Builder()
                .WithUpdatedDescription("Saved at " + System.DateTime.Now.ToString())
                .Build();

            PlayGamesPlatform.Instance.SavedGame.CommitUpdate(game, update, data, (commitStatus, _) =>
            {
                if (commitStatus == SavedGameRequestStatus.Success)
                    Debug.Log("✅ Cloud save successful!");
                else
                    Debug.LogError("❌ Cloud save failed: " + commitStatus);
            });
        }
        else
        {
            Debug.LogError("Failed to open cloud save: " + status);
        }
    }

    // Load from cloud
    public void LoadFromCloud()
    {
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Debug.LogWarning("Cannot load from cloud: User not authenticated.");
            return;
        }

        var savedGameClient = PlayGamesPlatform.Instance.SavedGame;
        savedGameClient.OpenWithAutomaticConflictResolution(
            "savefile_01",
            DataSource.ReadCacheOrNetwork,
            ConflictResolutionStrategy.UseLongestPlaytime,
            OnCloudLoadOpened);
    }

    private void OnCloudLoadOpened(SavedGameRequestStatus status, ISavedGameMetadata game)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(game, (readStatus, data) =>
            {
                if (readStatus == SavedGameRequestStatus.Success)
                {
                    string json = Encoding.UTF8.GetString(data);
                    SaveManager.Instance.data = JsonUtility.FromJson<PlayerData>(json);
                    Debug.Log("✅ Cloud load successful!");
                }
                else
                {
                    Debug.LogError("❌ Failed to read cloud data: " + readStatus);
                }
            });
        }
        else
        {
            Debug.LogError("Failed to open cloud save for loading: " + status);
        }
    }
}