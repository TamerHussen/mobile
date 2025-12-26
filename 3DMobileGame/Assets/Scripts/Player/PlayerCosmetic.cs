using UnityEngine;
using System.Collections.Generic;

public class PlayerCosmetic : MonoBehaviour
{
    [Header("Cosmetic Models")]
    public List<GameObject> CosmeticModels;

    [Header("Debug")]
    public bool logDebugInfo = true;

    void Start()
    {
        if (logDebugInfo)
        {
            Debug.Log($"PlayerCosmetic initialized with {CosmeticModels.Count} models");
            foreach (var model in CosmeticModels)
            {
                if (model != null)
                    Debug.Log($"  - Model: {model.name}");
            }
        }
    }

    public void Apply(string CosmeticName)
    {
        if (string.IsNullOrEmpty(CosmeticName))
        {
            Debug.LogWarning("CosmeticName is null or empty!");
            return;
        }

        if (CosmeticModels == null || CosmeticModels.Count == 0)
        {
            Debug.LogError("CosmeticModels list is null or empty!");
            return;
        }

        bool found = false;

        if (logDebugInfo)
            Debug.Log($"Applying cosmetic: '{CosmeticName}' to {gameObject.name}");

        foreach (var model in CosmeticModels)
        {
            if (model == null)
            {
                Debug.LogWarning("Null model in CosmeticModels list!");
                continue;
            }

            bool isMatch = model.name.Equals(CosmeticName, System.StringComparison.OrdinalIgnoreCase);

            model.SetActive(isMatch);

            if (logDebugInfo)
                Debug.Log($"  Model '{model.name}': {(isMatch ? "ENABLED" : "Disabled")}");

            if (isMatch) found = true;
        }

        if (!found)
        {
            Debug.LogWarning($"Cosmetic '{CosmeticName}' not found! Available models:");
            foreach (var model in CosmeticModels)
            {
                if (model != null)
                    Debug.Log($"  - {model.name}");
            }

            if (CosmeticModels.Count > 0 && CosmeticModels[0] != null)
            {
                Debug.Log($"Enabling fallback model: {CosmeticModels[0].name}");
                CosmeticModels[0].SetActive(true);
            }
        }
    }
}