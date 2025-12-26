using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class WatchAdButton : MonoBehaviour
{
    [Header("Components")]
    public Button button;
    public TextMeshProUGUI buttonText;
    public TextMeshProUGUI rewardText;

    [Header("Settings")]
    public float checkInterval = 1f;

    private float checkTimer = 0f;

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(OnWatchAdClicked);

        UpdateButtonState();
    }

    void Update()
    {
        // Periodically check if ad is ready
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            UpdateButtonState();
        }
    }

    void OnWatchAdClicked()
    {
        if (GoogleAdsManager.Instance != null)
        {
            if (GoogleAdsManager.Instance)
            {
            }
            else
            {
                Debug.Log("Ad not ready, loading...");

                if (buttonText != null)
                    buttonText.text = "Loading Ad...";
            }
        }
        else
        {
            Debug.LogError("GoogleAdsManager not found!");
        }
    }

    void UpdateButtonState()
    {
        if (GoogleAdsManager.Instance == null)
        {
            button.interactable = false;
            if (buttonText != null)
                buttonText.text = "Ads Not Available";
            return;
        }

        bool isReady = GoogleAdsManager.Instance;
        button.interactable = isReady;

        if (buttonText != null)
        {
            buttonText.text = isReady ? "Watch Ad" : "Loading...";
        }

        if (rewardText != null && isReady)
        {
            rewardText.text = $"+Coins";
        }
    }
}