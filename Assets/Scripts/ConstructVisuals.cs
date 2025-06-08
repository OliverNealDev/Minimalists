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
    
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color attackHoverColor = Color.red;
    [SerializeField] private Color hoverColor = Color.white;

    public void SetMeshRenderer(MeshRenderer renderer)
    {
        meshRenderer = renderer;
    }
    
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
    
    public void UpdateSelectionColor(Color color)
    {
        selectionIndicator.GetComponent<Renderer>().material.color = color;
    }
    
    public void UpdateUnitCapacity(float current, float max)
    {
        unitCapacitySlider.value = current / max;
        unitCapacityFillImage.gameObject.SetActive(max > 0); // Hide if max is 0
    }
    
    public void HideVisuals()
    {
        unitCountText.enabled = false;
        selectionIndicator.SetActive(false);
        unitCapacitySlider.gameObject.SetActive(false);
    }
    
    public void ShowVisuals()
    {
        unitCountText.enabled = true;
        unitCapacitySlider.gameObject.SetActive(true);
    }
}