using UnityEngine;
using System.Collections.Generic;

public class PlayerCosmetic : MonoBehaviour
{
    public List<GameObject> CosmeticModels;

    public void Apply(string CosmeticName)
    {
        bool found = false;

        foreach (var model in CosmeticModels)
        {
            if (model == null) continue;

            // Enable the model if the name matches, disable otherwise
            bool isMatch = model.name == CosmeticName;
            model.SetActive(isMatch);

            if (isMatch) found = true;
        }

        if (!found)
        {
            Debug.LogWarning($"Cosmetic '{CosmeticName}' not found in CosmeticModels list!");
        }
    }
}
