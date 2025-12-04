using System.Linq;
using UnityEngine;

public class CosmeticSelector : MonoBehaviour
{
    public LobbyInfo lobbyInfo;
    public GameObject[] cosmeticModels; // prefabs
    public string CosmeticName;
    private GameObject currentModel;

    public void SelectCosmetic(string CosmeticName)
    {
        // Remove old model
        if (currentModel != null)
            Destroy(currentModel);

        int index = CosmeticName.IndexOf(CosmeticName);

        if (index >= 0 && index < cosmeticModels.Count())
        {

            // Spawn new one
            currentModel = Instantiate(cosmeticModels[index], lobbyInfo.previewSpawnPoint.position, Quaternion.identity);

            // Save choice
            lobbyInfo.SetSelectedCosmetic(CosmeticName);

        }



    }
}
