using UnityEngine;

[CreateAssetMenu(fileName = "ConstructData", menuName = "Scriptable Objects/ConstructData")]
public class ConstructData : ScriptableObject
{
    public string constructName;
    public int maxUnitCapacity = 50;
    public float unitsPerSecond = 1.0f;
    public GameObject visualPrefab;
}
