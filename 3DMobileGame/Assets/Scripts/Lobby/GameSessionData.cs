using UnityEngine;
using System.Collections.Generic;

public class GameSessionData : MonoBehaviour
{
    public static GameSessionData Instance;

    public string LevelName;
    public List<LobbyPlayer> players;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
