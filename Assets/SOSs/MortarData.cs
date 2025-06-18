using UnityEngine;

[CreateAssetMenu(fileName = "New MortarData", menuName = "Minimalists/Constructs/Mortar Data")]
public class MortarData : ConstructData
{
    [Header("Mortar Specific")]
    public float KillPercentage = 0.5f;
    public float DowngradeChance = 0.5f;
}