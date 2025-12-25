using UnityEngine;
using UnityEngine.UI;

public class CosmeticButton : MonoBehaviour
{
    public string cosmeticName;
    private Button button;
    private Image buttonImage;

    [Header("Visual Feedback")]
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;

    void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
    }

    void Start()
    {
        RebindButton();
    }

    void OnEnable()
    {
        UpdateVisual();
    }

    public void RebindButton()
    {
        if (button == null)
            button = GetComponent<Button>();

        // Clear existing listeners to avoid duplicates
        button.onClick.RemoveAllListeners();

        // Re-add the listener
        button.onClick.AddListener(OnClick);

        Debug.Log($"Rebound button for cosmetic: {cosmeticName}");

        // Update visual state
        UpdateVisual();
    }

    void OnClick()
    {
        Debug.Log($"Cosmetic button clicked: {cosmeticName}");

        if (LobbyInfo.Instance != null)
        {
            _ = LobbyInfo.Instance.SetSelectedCosmetic(cosmeticName);
        }
        else
        {
            Debug.LogError("LobbyInfo.Instance is null!");
        }

        // Update all buttons
        UpdateAllButtons();
    }

    void UpdateVisual()
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (LobbyInfo.Instance == null) return;

        bool isSelected = LobbyInfo.Instance.GetSelectedCosmetic() == cosmeticName;
        buttonImage.color = isSelected ? selectedColor : normalColor;
    }

    void UpdateAllButtons()
    {
        var allButtons = FindObjectsByType<CosmeticButton>(FindObjectsSortMode.None);
        foreach (var btn in allButtons)
        {
            btn.UpdateVisual();
        }
    }
}