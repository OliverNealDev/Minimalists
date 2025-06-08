using UnityEngine;

// This is the base class. It can't be created on its own.
public abstract class ConstructData : ScriptableObject
{
    [Header("Common Data")]
    public string constructName;
    public GameObject visualPrefab;

    [Header("Common Upgrade Settings")]
    public int upgradeCost = 50;
    public ConstructData upgradedVersion;
}