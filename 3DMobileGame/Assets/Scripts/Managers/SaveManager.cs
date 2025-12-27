using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    private string savePath;
    public PlayerData data = new PlayerData();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Application.persistentDataPath + "/save.json";
            Load();
            Debug.Log($"SaveManager initialized. Player: {data.playerName}, Cosmetic: {data.selectedCosmetic}, Coins: {data.coins}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Save()
    {
        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
        Debug.Log("Game Saved!");
    }

    public void Load()
    {
        if (File.Exists(savePath))
        {
            data = JsonUtility.FromJson<PlayerData>(File.ReadAllText(savePath));
            Debug.Log("Game Loaded!");
        }
        else
        {
            Debug.Log("No save file found. Starting new game.");
        }
    }

    void OnApplicationQuit()
    {
        Save();
    }
}

[System.Serializable]
public class PlayerData
{
    public int coins = 0;
    public int level = 1;
    public string playerName = "Player";
    public string uniquePlayerName = "";
    public string selectedCosmetic = "Default";
    public string lastSelectedLevel = "None";
    public List<string> unlockedCosmetics = new List<string> { "Default" };

    public PlayerData()
    {
        // Default constructor for deserialization
    }

    public PlayerData(string name, string cosmetic = "Default")
    {
        playerName = name;
        selectedCosmetic = cosmetic;
    }

    public bool HasCosmetic(string cosmeticName)
    {
        return unlockedCosmetics.Contains(cosmeticName);
    }

    public void UnlockCosmetic(string cosmeticName)
    {
        if (!unlockedCosmetics.Contains(cosmeticName))
        {
            unlockedCosmetics.Add(cosmeticName);
        }
    }
}