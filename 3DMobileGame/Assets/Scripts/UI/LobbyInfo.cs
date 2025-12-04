using UnityEngine;
using TMPro;

public class LobbyInfo : MonoBehaviour
{
    public TextMeshProUGUI selectedLevelText;
    public TextMeshProUGUI selectedCosmeticText;
    public TextMeshProUGUI playerCountText;

    public Transform previewSpawnPoint;  // for cosmetics

    private string selectedLevel = "None";
    private string selectedCosmetic = "None";

    void Start()
    {
        selectedLevelText.text = "Level: None";
        selectedCosmeticText.text = "Cosmetic: None";

        // Player count (for now always 1)
        playerCountText.text = "1/3";
    }

    public void SetSelectedLevel(string levelName)
    {
        selectedLevel = levelName;
        selectedLevelText.text = "Level: " + levelName;
    }

    public void SetSelectedCosmetic(string CosmeticName)
    {
        selectedCosmetic = CosmeticName;
        selectedCosmeticText.text = "Cosmetic: " + CosmeticName;

    }

    public string GetSelectedLevel()
    {
        return selectedLevel;
    }
}
