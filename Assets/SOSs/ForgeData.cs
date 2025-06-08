using UnityEngine;

[CreateAssetMenu(fileName = "New ForgeData", menuName = "Minimalists/Constructs/Forge Data")]
public class ForgeData : ConstructData
{
    [Header("Forge Specific")]
    [Tooltip("e.g., 1.2 means a 20% damage buff")]
    public float damageMultiplier = 1.2f;
}