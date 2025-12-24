using UnityEngine;
using UnityEngine.UI;

public class LevelSelector : MonoBehaviour
{
    [Header("Level Settings")]
    public string levelName = "Level1";

    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => SelectLevel(levelName));
        }
    }

    public void SelectLevel(string level)
    {
        if (LobbyInfo.Instance != null)
        {
            LobbyInfo.Instance.SetSelectedLevel(level);
            Debug.Log($"Selected level: {level}");
        }
    }
}