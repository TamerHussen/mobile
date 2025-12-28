using UnityEngine;

public class UILobbyManager : MonoBehaviour
{
    public GameObject levelsPanel;
    public GameObject cosmeticsPanel;
    public GameObject achievementsPanel;
    public GameObject friendsPanel;
    public GameObject settingsPanel;

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
        if (!levelsPanel.activeSelf)
        {
            HideAll();
            levelsPanel.SetActive(true);

        }

        else
        {
            HideAll();
        }
    }

    public void ShowCosmetics()
    {
        if (!cosmeticsPanel.activeSelf)
        {
            HideAll();
            cosmeticsPanel.SetActive(true);

        }

        else
        {
            HideAll();
        }
    }

    public void ShowAchievements()
    {
        if (!achievementsPanel.activeSelf)
        {
            HideAll();
            achievementsPanel.SetActive(true);

        }

        else
        {
            HideAll();
        }
    }

    public void ShowFriends()
    {

        if (!friendsPanel.activeSelf)
        {
            HideAll();
            friendsPanel.SetActive(true);

        }

        else
        {
            HideAll();
        }
    }

    public void ShowSettings()
    {
        if (!settingsPanel.activeSelf)
        {
            HideAll();
            settingsPanel.SetActive(true);

        }

        else
        {
            HideAll();
        }
    }
}
