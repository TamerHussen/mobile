using UnityEngine;
using TMPro;

public class LobbyInfo : MonoBehaviour
{
    public TextMeshProUGUI selectedLevelText;
    public TextMeshProUGUI selectedCosmeticText;
    public TextMeshProUGUI playerCountText;

    public Transform previewSpawnPoint;  // for cosmetics

    private string selectedLevel = "None";
    private int selectedCosmetic = -1;

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

    public void SetSelectedCosmetic(int index)
    {
        selectedCosmetic = index;
        selectedCosmeticText.text = "Cosmetic: " + index.ToString();
    }

    public string GetSelectedLevel()
    {
        return selectedLevel;
    }
}
