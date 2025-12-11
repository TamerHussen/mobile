using UnityEngine;
using System.IO;

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
}
