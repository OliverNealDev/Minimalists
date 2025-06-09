using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ConstructVisuals : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private TextMeshProUGUI unitCountText;
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private Slider unitCapacitySlider;
    [SerializeField] private Image unitCapacityFillImage;
    
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color attackHoverColor = Color.red;
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private GameObject nodeConstruct;

    [SerializeField] private GameObject house1Model;
    [SerializeField] private GameObject house2Model;
    [SerializeField] private GameObject house3Model;

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
        unitCapacityFillImage.gameObject.SetActive(max > 0);
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
    
    public void UpgradeScale(float time, float scale)
    {
        StartCoroutine(AnimateScale(time, scale));
    }

    private IEnumerator AnimateScale(float duration, float scaleIncrease)
    {
        Vector3 initialScale = nodeConstruct.transform.localScale;
        Vector3 finalScale = initialScale + new Vector3(scaleIncrease, scaleIncrease, scaleIncrease);
        float elapsedTime = 0f;
        float pulseFrequency = 1.0f;

        while (elapsedTime < duration)
        {
            float sineWave = Mathf.Sin(elapsedTime * pulseFrequency * 2.0f * Mathf.PI);
            float pulseProgress = (sineWave + 1.0f) / 2.0f;
            nodeConstruct.transform.localScale = Vector3.Lerp(initialScale, finalScale, pulseProgress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        float reformDuration = 0.25f;
        float shrinkTime = reformDuration / 2.0f;
        float growTime = reformDuration / 2.0f;

        Vector3 scaleBeforeShrink = nodeConstruct.transform.localScale;
        elapsedTime = 0f;
        while (elapsedTime < shrinkTime)
        {
            nodeConstruct.transform.localScale = Vector3.Lerp(scaleBeforeShrink, Vector3.zero, elapsedTime / shrinkTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < growTime)
        {
            nodeConstruct.transform.localScale = Vector3.Lerp(Vector3.zero, finalScale, elapsedTime / growTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        nodeConstruct.transform.localScale = finalScale;
    }
}