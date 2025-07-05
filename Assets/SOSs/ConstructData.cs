using UnityEngine;

// This is the base class. It can't be created on its own.
public abstract class ConstructData : ScriptableObject
{
    [Header("Common Data")]
    public string constructName;
    public GameObject visualPrefab;

    [Header("Common Upgrade Settings")]
    public int upgradeCost = 50;
    public float upgradeTime = 5f;
    public ConstructData upgradedVersion;
    public ConstructData downgradedVersion;
    public int houseConversionCost = 30;
    public int towerConversionCost = 30;
    public int helipadConversionCost = 20;
}