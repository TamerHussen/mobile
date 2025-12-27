using TMPro;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    public string PlayerID {  get; set; }
    public PlayerNames nameTag;
    public PlayerCosmetic cosmetic;

    public void Bind(LobbyPlayer data)
    {
        PlayerID = data.PlayerID;
        if (nameTag != null) nameTag.SetName(data.PlayerName);
        if (cosmetic  != null) cosmetic.Apply(data.Cosmetic);
    }
}
