using UnityEngine;

public class PlayerCosmetic : MonoBehaviour
{
    public Renderer BodyRenderer;
    public Material[] CosmeticMaterial;

    public void Apply(string CosmeticName)
    {
        foreach (var mat in CosmeticMaterial)
        {
            if (mat.name == CosmeticName)
            {
                BodyRenderer.material = mat;
                return;
            }
        }
    }
}
