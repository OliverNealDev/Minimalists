// In Scripts/Constructs/ConstructVisuals.cs
using UnityEngine;
using TMPro;

public class ConstructVisuals : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private TextMeshProUGUI unitCountText;
    [SerializeField] private GameObject selectionIndicator; // A simple ring/highlight object

    public void UpdateColor(Color color)
    {
        meshRenderer.material.color = color;
    }

    public void UpdateUnitCount(int count)
    {
        unitCountText.text = count.ToString();
    }
    
    public void UpdateSelection(bool isSelected)
    {
        selectionIndicator.SetActive(isSelected);
    }
}