using UnityEngine;


[CreateAssetMenu(fileName = "New Cosmetic", menuName = "Game Data/Cosmetic Item")]
public class CosmeticData : ScriptableObject
{
    [Header("Cosmetic Info")]
    public string cosmeticName = "DefaultCosmetic"; 
    public string displayName = "Default";
    public Sprite icon;

    [Header("Unlock Requirements")]
    public bool isDefault = true;
    public int coinCost = 0;

    [Header("Optional Requirements")]
    public int requiredLevel = 0; 
    public string description = "Default cosmetic";

    [Header("Visual")]
    public Color backgroundColor = Color.white;
}
