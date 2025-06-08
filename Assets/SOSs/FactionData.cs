using UnityEngine;

[CreateAssetMenu(fileName = "FactionData", menuName = "Scriptable Objects/FactionData")]
public class FactionData : ScriptableObject
{
    public string factionName;
    public Color factionColor = Color.white;
    public bool isPlayerControlled = true;
}
