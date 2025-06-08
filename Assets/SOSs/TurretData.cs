using UnityEngine;

[CreateAssetMenu(fileName = "New TurretData", menuName = "Minimalists/Constructs/Turret Data")]
public class TurretData : ConstructData
{
    [Header("Turret Specific")]
    public float fireRate = 1f;
    public float range = 2f;
}