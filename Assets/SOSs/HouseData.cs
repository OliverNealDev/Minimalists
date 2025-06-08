using UnityEngine;

[CreateAssetMenu(fileName = "New HouseData", menuName = "Minimalists/Constructs/House Data")]
public class HouseData : ConstructData
{
    [Header("House Specific")]
    public int maxUnitCapacity = 20;
    public float unitsPerSecond = 0.5f;
}