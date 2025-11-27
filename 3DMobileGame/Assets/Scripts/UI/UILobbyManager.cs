using UnityEngine;

public class UILobbyManager : MonoBehaviour
{
    public GameObject levelsPanel;
    public GameObject cosmeticsPanel;
    public GameObject achievementsPanel;
    public GameObject friendsPanel;
    public GameObject settingsPanel;

    // Close all panels
    void HideAll()
    {
        levelsPanel.SetActive(false);
        cosmeticsPanel.SetActive(false);
        achievementsPanel.SetActive(false);
        friendsPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void ShowLevels()
    {
        HideAll();
        levelsPanel.SetActive(true);
    }

    public void ShowCosmetics()
    {
        HideAll();
        cosmeticsPanel.SetActive(true);
    }

    public void ShowAchievements()
    {
        HideAll();
        achievementsPanel.SetActive(true);
    }

    public void ShowFriends()
    {
        HideAll();
        friendsPanel.SetActive(true);
    }

    public void ShowSettings()
    {
        HideAll();
        settingsPanel.SetActive(true);
    }
}
