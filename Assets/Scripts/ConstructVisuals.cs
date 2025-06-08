// In Scripts/Constructs/ConstructVisuals.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ConstructVisuals : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private TextMeshProUGUI unitCountText;
    [SerializeField] private GameObject selectionIndicator; // A simple ring/highlight object
    [SerializeField] private Slider unitCapacitySlider;
    [SerializeField] private Image unitCapacityFillImage;

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
    
    public void UpdateUnitCapacity(float current, float max)
    {
        unitCapacitySlider.value = current / max;
        unitCapacityFillImage.gameObject.SetActive(max > 0); // Hide if max is 0
    }
}