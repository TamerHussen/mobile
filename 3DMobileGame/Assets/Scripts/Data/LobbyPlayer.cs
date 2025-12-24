using UnityEngine;

[System.Serializable]
public class LobbyPlayer
{
    public string PlayerID;
    public string PlayerName;
    public string Cosmetic;
    public bool IsLocal;

    public LobbyPlayer(string id, string name, string cosmetic, bool isLocal)
    {
        PlayerID = id;
        PlayerName = name;
        Cosmetic = cosmetic;
        IsLocal = isLocal;
    }
}