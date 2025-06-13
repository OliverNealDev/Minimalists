using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine.Experimental.Audio;

public class ConstructVisuals : MonoBehaviour
{
    [SerializeField] private List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    
    [SerializeField] private TextMeshProUGUI unitCountText;
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private Slider unitCapacitySlider;
    [SerializeField] private Image unitCapacityFillImage;

    [SerializeField] private Image upgradeIndicatorImage;
    public bool isUpgradeIndicatorVisible = false;
    private bool isUpgrading = false;
    
    private Vector3 originalScale;

    
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color attackHoverColor = Color.red;
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private GameObject nodeConstruct;

    [SerializeField] private GameObject house1Model;
    [SerializeField] private GameObject house2Model;
    [SerializeField] private GameObject house3Model;
    [SerializeField] private GameObject house4Model;
    
    private Color lastKnownColor;

    void Awake()
    {
        SetMeshRenderers();
        upgradeIndicatorImage.gameObject.SetActive(false);
        originalScale = nodeConstruct.transform.localScale;
    }
    
    public void SetMeshRenderers()
    {
        meshRenderers.Clear();
        for (int i = 0; i < nodeConstruct.transform.GetChild(0).childCount; i++)
        {
            meshRenderers.Add(nodeConstruct.transform.GetChild(0).GetChild(i).GetComponent<MeshRenderer>());
        }
    }
    
    public void setUpgradeIndicatorVisibility(bool isVisible)
    {
        if (isVisible)
        {
            isUpgradeIndicatorVisible = true;
        }
        else if (!isVisible)
        {
            isUpgradeIndicatorVisible = false;
        }
        upgradeIndicatorImage.gameObject.SetActive(isVisible);
    }
    
    public void UpdateColor(Color color)
    {
        lastKnownColor = color;
        foreach (var renderer in meshRenderers)
        {
            renderer.material.color = color;
        }
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
    
    public void UpgradeScale(float time, string constructName)
    {
        StartCoroutine(AnimateScale(time, constructName));
    }
    
    public void ConstructChange(ConstructData constructData, bool isAnimated, Color newConstructColor)
    {
        if (isUpgrading)
        {
            nodeConstruct.transform.localScale = originalScale;
            StopAllCoroutines();
            if (constructData.upgradedVersion != null)
            {
                //Debug.Log("downgrading");
                constructData = constructData.upgradedVersion;
            }

            isUpgrading = false;
        }
        
        if (isAnimated)
        {
            StartCoroutine(AnimateConstructChange(constructData, newConstructColor));
        }
        else
        {
            ChangeConstruct(constructData, newConstructColor);
        }
    }

    void ChangeConstruct(ConstructData constructData, Color newConstructColor)
    {
        Destroy(nodeConstruct);
        nodeConstruct = Instantiate(constructData.visualPrefab, transform.position, Quaternion.identity);
        nodeConstruct.transform.parent = transform;
        SetMeshRenderers();
        UpdateColor(newConstructColor);
    }

    private IEnumerator AnimateScale(float duration, string constructName)
    {
        isUpgrading = true;
        var scaleIncrease = 0.25f;
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
        
        float reformDuration = 0.4f; // previously 0.25f
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

        switch (constructName)
        {
            case "House1":
                Destroy(nodeConstruct);
                nodeConstruct = Instantiate(house2Model, transform.position, Quaternion.identity);
                nodeConstruct.transform.parent = transform;
                break;
            case "House2":
                Destroy(nodeConstruct);
                nodeConstruct = Instantiate(house3Model, transform.position, Quaternion.identity);
                nodeConstruct.transform.parent = transform;
                break;
            case "House3":
                Destroy(nodeConstruct);
                nodeConstruct = Instantiate(house4Model, transform.position, Quaternion.identity);
                nodeConstruct.transform.parent = transform;
                break;
            default:
                Debug.LogError($"{constructName} is not a valid construct name!");
                break;
        }
        
        SetMeshRenderers();
        UpdateColor(lastKnownColor);

        elapsedTime = 0f;
        while (elapsedTime < growTime)
        {
            nodeConstruct.transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, elapsedTime / growTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isUpgrading = false;
        nodeConstruct.transform.localScale = initialScale;
    }
    
    public IEnumerator AnimateConstructChange(ConstructData constructData, Color newConstructColor)
    {
        var scaleIncrease = 0.25f;
        Vector3 initialScale = nodeConstruct.transform.localScale;
        Vector3 finalScale = initialScale + new Vector3(scaleIncrease, scaleIncrease, scaleIncrease);
        float elapsedTime = 0f;
        float pulseFrequency = 1.0f;
        
        float reformDuration = 0.4f; // previously 0.25f
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

        Destroy(nodeConstruct);
        
        Debug.Log(constructData.constructName);
        
        switch (constructData.constructName)
        {
            case "House1":
                //Destroy(nodeConstruct);
                nodeConstruct = Instantiate(house1Model, transform.position, Quaternion.identity);
                nodeConstruct.transform.parent = transform;
                break;
            case "House2":
                //Destroy(nodeConstruct);
                nodeConstruct = Instantiate(house2Model, transform.position, Quaternion.identity);
                nodeConstruct.transform.parent = transform;
                break;
            case "House3":
                //Destroy(nodeConstruct);
                nodeConstruct = Instantiate(house3Model, transform.position, Quaternion.identity);
                nodeConstruct.transform.parent = transform;
                break;
            default:
                Debug.LogError($"{constructData.constructName} is not a valid construct name!");
                break;
        }
        
        //nodeConstruct = Instantiate(constructData.visualPrefab, transform.position, Quaternion.identity);
        nodeConstruct.transform.parent = transform;
        SetMeshRenderers();
        UpdateColor(newConstructColor);

        elapsedTime = 0f;
        while (elapsedTime < growTime)
        {
            nodeConstruct.transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, elapsedTime / growTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        nodeConstruct.transform.localScale = initialScale;
    }
}