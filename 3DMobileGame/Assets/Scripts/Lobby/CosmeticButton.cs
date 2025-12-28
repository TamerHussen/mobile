using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CosmeticButton : MonoBehaviour
{
    [Header("Cosmetic Data")]
    public CosmeticData cosmeticData;

    [Header("UI Components")]
    public Image iconImage;
    public Image backgroundImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button button;

    [Header("Status Icons")]
    public GameObject lockedIcon;
    public GameObject equippedIcon;

    [Header("Visual Feedback")]
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.white;
    public Color equippedColor = Color.green;

    private bool isUnlocked = false;
    private bool isEquipped = false;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }

    void Start()
    {
        if (cosmeticData == null)
        {
            Debug.LogError($"CosmeticData not assigned on {gameObject.name}!");
            return;
        }

        SetupUI();
        UpdateVisual();
        RebindButton();
    }

    void OnEnable()
    {
        UpdateVisual();
    }

    void SetupUI()
    {
        if (iconImage != null && cosmeticData.icon != null)
        {
            iconImage.sprite = cosmeticData.icon;
        }

        if (nameText != null)
        {
            nameText.text = cosmeticData.displayName;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = cosmeticData.backgroundColor;
        }
    }

    public void RebindButton()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        UpdateVisual();
    }

    void OnClick()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager not found!");
            return;
        }

        isUnlocked = cosmeticData.isDefault || SaveManager.Instance.data.HasCosmetic(cosmeticData.cosmeticName);

        if (!isUnlocked)
        {
            TryPurchase();
        }
        else
        {
            EquipCosmetic();
        }
    }

    void TryPurchase()
    {
        if (CoinsManager.Instance == null)
        {
            Debug.LogError("CoinsManager not found!");
            return;
        }

        if (cosmeticData.requiredLevel > SaveManager.Instance.data.level)
        {
            Debug.Log($"❌ Need level {cosmeticData.requiredLevel} to unlock {cosmeticData.displayName}");
            return;
        }

        if (CoinsManager.Instance.GetCoins() < cosmeticData.coinCost)
        {
            Debug.Log($"❌ Not enough coins! Need {cosmeticData.coinCost}, have {CoinsManager.Instance.GetCoins()}");
            return;
        }

        CoinsManager.Instance.SpendCoins(cosmeticData.coinCost);
        SaveManager.Instance.data.UnlockCosmetic(cosmeticData.cosmeticName);
        SaveManager.Instance.Save();

        Debug.Log($"✅ Purchased {cosmeticData.displayName} for {cosmeticData.coinCost} coins!");

        // Auto-equip after purchase
        EquipCosmetic();
    }

    async void EquipCosmetic()
    {
        if (LobbyInfo.Instance != null)
        {
            await LobbyInfo.Instance.SetSelectedCosmetic(cosmeticData.cosmeticName);
            Debug.Log($"✅ Equipped {cosmeticData.displayName}");
        }

        UpdateAllButtons();
    }

    void UpdateVisual()
    {
        if (SaveManager.Instance == null || cosmeticData == null)
            return;

        isUnlocked = cosmeticData.isDefault || SaveManager.Instance.data.HasCosmetic(cosmeticData.cosmeticName);

        isEquipped = LobbyInfo.Instance != null &&
                     LobbyInfo.Instance.GetSelectedCosmetic() == cosmeticData.cosmeticName;

        if (priceText != null)
        {
            if (cosmeticData.isDefault)
            {
                priceText.text = "FREE";
            }
            else if (isUnlocked)
            {
                priceText.text = "OWNED";
            }
            else
            {
                priceText.text = $"{cosmeticData.coinCost} Coins";
            }
        }

        if (backgroundImage != null)
        {
            if (isEquipped)
            {
                backgroundImage.color = equippedColor;
            }
            else if (isUnlocked)
            {
                backgroundImage.color = unlockedColor;
            }
            else
            {
                backgroundImage.color = lockedColor;
            }
        }

        if (lockedIcon != null)
        {
            lockedIcon.SetActive(!isUnlocked);
        }

        if (equippedIcon != null)
        {
            equippedIcon.SetActive(isEquipped);
        }

        if (button != null)
        {
            if (!isUnlocked && CoinsManager.Instance != null)
            {
                button.interactable = CoinsManager.Instance.GetCoins() >= cosmeticData.coinCost;
            }
            else
            {
                button.interactable = true;
            }
        }
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