using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    // Call this from each level button in your UI
    public void LoadLevel(string levelName)
    {
        Time.timeScale = 1f; // Ensure game is running
        SceneManager.LoadScene(levelName);
    }
}
