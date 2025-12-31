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

    private Rigidbody rb;
    private PlayerController controller;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        rb = GetComponent<Rigidbody>();
        controller = GetComponent<PlayerController>();
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
        Debug.Log("========== REVIVE AD SUCCESS - RESPAWNING PLAYER ==========");

        hasUsedReviveAd = true;
        isDead = false;
        currentLives = 1;

        UpdateLivesUI();

        StartCoroutine(RespawnPlayer());
    }

    IEnumerator RespawnPlayer()
    {
        yield return null;

        if (respawnPoints == null || respawnPoints.Length == 0)
        {
            Debug.LogWarning("No respawn points assigned!");
            yield break;
        }

        Transform spawn = respawnPoints[Random.Range(0, respawnPoints.Length)];

        Debug.Log($"Respawning at: {spawn.position}");

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (controller != null)
            controller.enabled = false;

        if (rb != null)
        {
            rb.position = spawn.position;
            rb.rotation = spawn.rotation;
        }
        else
        {
            transform.position = spawn.position;
            transform.rotation = spawn.rotation;
        }

        yield return new WaitForFixedUpdate();

        if (controller != null)
            controller.enabled = true;

        Debug.Log($"Player respawned at {spawn.position}");

        StartCoroutine(InvincibilityCoroutine());
    }

    IEnumerator RespawnAfterHit()
    {
        yield return new WaitForSeconds(0.5f);

        if (respawnPoints != null && respawnPoints.Length > 0)
        {
            Transform spawn = respawnPoints[Random.Range(0, respawnPoints.Length)];

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = spawn.position;
                rb.rotation = spawn.rotation; 
            }
            else
            {
                transform.position = spawn.position;
                transform.rotation = spawn.rotation;
            }

            Debug.Log($"Respawned after hit at {spawn.position}");
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
        Debug.Log("Player is now invincible for 3 seconds");

        yield return new WaitForSeconds(respawnInvincibilityTime);

        isInvincible = false;
        Debug.Log("Player invincibility ended");
    }
}