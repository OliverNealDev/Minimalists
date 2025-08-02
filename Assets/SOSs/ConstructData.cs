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
    public int conversionCost = 30;
    public float conversionTime = 3f;
    public int maxUnitCapacity = 20;
    public float unitsPerSecond = 0.5f;
}