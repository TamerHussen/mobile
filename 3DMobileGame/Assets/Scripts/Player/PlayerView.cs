using TMPro;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    public PlayerNames nameTag;
    public PlayerCosmetic cosmetic;

    public void Bind(LobbyPlayer data)
    {
        if (nameTag != null) nameTag.SetName(data.PlayerName);
        if (cosmetic  != null) cosmetic.Apply(data.Cosmetic);
    }
}
