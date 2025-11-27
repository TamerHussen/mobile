using UnityEngine;

public class CosmeticSelector : MonoBehaviour
{
    public LobbyInfo lobbyInfo;
    public GameObject[] cosmeticModels; // prefabs

    private GameObject currentModel;

    public void SelectCosmetic(int index)
    {
        // Remove old model
        if (currentModel != null)
            Destroy(currentModel);

        // Spawn new one
        currentModel = Instantiate(cosmeticModels[index], lobbyInfo.previewSpawnPoint.position, Quaternion.identity);

        // Save choice
        lobbyInfo.SetSelectedCosmetic(index);
    }
}
