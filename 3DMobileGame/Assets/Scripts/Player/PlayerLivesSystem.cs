using UnityEngine;
using System.Collections;

public class PlayerLivesSystem : MonoBehaviour
{
    public static PlayerLivesSystem Instance;

    [Header("Lives Settings")]
    public int maxLives = 3;
    public int currentLives;

    private bool hasUsedReviveAd;
    private bool isInvincible;
    private bool isDead;

    [Header("Respawn Settings")]
    public Transform[] respawnPoints;
    public float respawnInvincibilityTime = 3f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        currentLives = maxLives;
        UpdateLivesUI();
    }

    public void LoseLife()
    {
        if (isInvincible || isDead) return;

        currentLives--;
        UpdateLivesUI();

        if (currentLives <= 0)
            HandleDeath();
        else
            StartCoroutine(InvincibilityCoroutine());
    }

    void HandleDeath()
    {
        isDead = true;

        bool canRevive =
            !hasUsedReviveAd &&
            GoogleAdsManager.Instance != null &&
            GoogleAdsManager.Instance.IsRewardedAdReady();

        if (canRevive && LevelUIManager.Instance != null)
            LevelUIManager.Instance.ShowRevive();
        else
            ForceGameOver();
    }

    public void OnReviveAdSuccess()
    {
        hasUsedReviveAd = true;
        isDead = false;
        currentLives = 1;

        UpdateLivesUI();

        if (respawnPoints != null && respawnPoints.Length > 0)
        {
            Transform spawn = respawnPoints[Random.Range(0, respawnPoints.Length)];
            transform.position = spawn.position;
            transform.rotation = spawn.rotation;
        }

        StartCoroutine(InvincibilityCoroutine());
    }

    public void ForceGameOver()
    {
        isDead = true;
        Time.timeScale = 0f;

        int score = GetComponent<PlayerScore>()?.Score ?? 0;
        int coins = Mathf.FloorToInt(score * 0.05f);

        if (LevelUIManager.Instance != null)
            LevelUIManager.Instance.ShowGameOver(score, coins);
        else
        {
            Debug.LogError("LevelUIManager missing! Returning to Lobby");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    private void UpdateLivesUI()
    {
        if (LevelUIManager.Instance != null)
            LevelUIManager.Instance.UpdateLivesDisplay(currentLives, maxLives);
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(respawnInvincibilityTime);
        isInvincible = false;
    }
}
