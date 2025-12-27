using UnityEngine;

/// <summary>
/// Manages ad frequency to prevent spamming players with too many ads.
/// Optional helper class - attach to your GameManager or similar.
/// </summary>
public class AdFrequencyManager : MonoBehaviour
{
    public static AdFrequencyManager Instance;

    [Header("Interstitial Ad Settings")]
    [Tooltip("Minimum seconds between interstitial ads")]
    public float minTimeBetweenInterstitials = 180f; // 3 minutes

    [Tooltip("Show interstitial every X deaths")]
    public int deathsPerInterstitial = 3;

    [Tooltip("Show interstitial every X levels completed")]
    public int levelsPerInterstitial = 2;

    [Header("Tracking")]
    private float lastInterstitialTime = 0f;
    private int deathCount = 0;
    private int levelCompletionCount = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this when player dies
    /// </summary>
    public void OnPlayerDeath()
    {
        deathCount++;
        Debug.Log($"Player deaths: {deathCount}");

        if (ShouldShowInterstitialAfterDeath())
        {
            ShowInterstitialAd();
        }
    }

    /// <summary>
    /// Call this when player completes a level
    /// </summary>
    public void OnLevelComplete()
    {
        levelCompletionCount++;
        Debug.Log($"Levels completed: {levelCompletionCount}");

        if (ShouldShowInterstitialAfterLevel())
        {
            ShowInterstitialAd();
        }
    }

    /// <summary>
    /// Check if enough time has passed since last interstitial
    /// </summary>
    bool EnoughTimePassed()
    {
        return (Time.time - lastInterstitialTime) >= minTimeBetweenInterstitials;
    }

    /// <summary>
    /// Should we show an interstitial after this death?
    /// </summary>
    bool ShouldShowInterstitialAfterDeath()
    {
        if (!EnoughTimePassed())
        {
            Debug.Log("Not enough time passed since last ad");
            return false;
        }

        if (deathCount % deathsPerInterstitial == 0)
        {
            Debug.Log("Death threshold reached - showing ad");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Should we show an interstitial after this level?
    /// </summary>
    bool ShouldShowInterstitialAfterLevel()
    {
        if (!EnoughTimePassed())
        {
            Debug.Log("Not enough time passed since last ad");
            return false;
        }

        if (levelCompletionCount % levelsPerInterstitial == 0)
        {
            Debug.Log("Level threshold reached - showing ad");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Show interstitial ad with timing tracking
    /// </summary>
    void ShowInterstitialAd()
    {
        if (GoogleAdsManager.Instance == null)
        {
            Debug.LogError("GoogleAdsManager not found!");
            return;
        }

        if (GoogleAdsManager.Instance.IsInterstitialAdReady())
        {
            GoogleAdsManager.Instance.ShowInterstitialAd();
            lastInterstitialTime = Time.time;
            Debug.Log($"✅ Showed interstitial ad. Next possible in {minTimeBetweenInterstitials}s");
        }
        else
        {
            Debug.Log("Interstitial ad not ready yet");
        }
    }

    /// <summary>
    /// Force show an interstitial (ignores time limit)
    /// Use sparingly - only for guaranteed natural breaks
    /// </summary>
    public void ForceShowInterstitial()
    {
        if (GoogleAdsManager.Instance != null)
        {
            GoogleAdsManager.Instance.ShowInterstitialAd();
            lastInterstitialTime = Time.time;
        }
    }

    /// <summary>
    /// Reset all counters (call on app launch or after ads are disabled)
    /// </summary>
    public void ResetCounters()
    {
        deathCount = 0;
        levelCompletionCount = 0;
        lastInterstitialTime = 0f;
        Debug.Log("Ad frequency counters reset");
    }

    /// <summary>
    /// Get time remaining until next interstitial is allowed
    /// </summary>
    public float TimeUntilNextInterstitial()
    {
        float elapsed = Time.time - lastInterstitialTime;
        float remaining = Mathf.Max(0, minTimeBetweenInterstitials - elapsed);
        return remaining;
    }

    /// <summary>
    /// Check if we can show an interstitial right now (time-wise)
    /// </summary>
    public bool CanShowInterstitialNow()
    {
        return EnoughTimePassed();
    }
}