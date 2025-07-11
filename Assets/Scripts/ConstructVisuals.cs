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

    [SerializeField] private Light constructLight;
    [SerializeField] private Light turretRadiusLight;

    private GameObject turretGroup;
    
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
    
    private ConstructController constructController;
    
    private Color lastKnownColor;
    
    private bool isHoverGlowing = false;

    [Header("Targeting")]
    public float rotationSpeed = 5f; // How fast the turret turns. Adjust in the Inspector.

    [Header("State (Read-Only)")]
    [Tooltip("Is the turret currently aimed at the target?")]
    public bool isLockedOn = false; // Public bool to track lock-on state

    private GameObject lastKnownTarget;
    public GameObject bulletSpawnPoint;

    void Awake()
    {
        SetMeshRenderers();
        upgradeIndicatorImage.gameObject.SetActive(false);
        originalScale = nodeConstruct.transform.localScale;
        constructController = GetComponent<ConstructController>();
    }

    void Update()
    {
        // Check if we have a valid target from the controller
        if (constructController.currentConstructData is TurretData && constructController.turretTarget != null)
        {
            // --- Rotation Logic ---

            // 1. Get the direction vector from the turret to the target
            Vector3 direction = constructController.turretTarget.transform.position - turretGroup.transform.position;

            // Optional: Uncomment the next line to keep the turret from tilting up or down
            // direction.y = 0;

            // 2. Calculate the target rotation
            // We create a quaternion that "looks" in the calculated direction.
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 3. Smoothly rotate towards the target rotation using Slerp
            // Slerp (Spherical Linear Interpolation) is perfect for smooth rotations.
            turretGroup.transform.rotation = Quaternion.Slerp(
                turretGroup.transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );

            // --- Lock-On State Logic ---

            // Calculate the angle between where the turret is currently facing and the direction to the target.
            float angleToTarget = Vector3.Angle(turretGroup.transform.forward, direction);

            // If the angle is very small, we consider the turret "locked on".
            // You can adjust the threshold (e.g., 1.0f) to be more or less strict.
            if (angleToTarget < 1.0f)
            {
                isLockedOn = true;
            }
            else
            {
                isLockedOn = false;
            }
        }
        else
        {
            // If there is no target, it cannot be locked on.
            isLockedOn = false;
        }
    }
    
    public void SetMeshRenderers()
    {
        meshRenderers.Clear();
        bulletSpawnPoint = null;
        Transform parentObject = nodeConstruct.transform.GetChild(0);

        for (int i = 0; i < parentObject.childCount; i++)
        {
            Transform child = parentObject.GetChild(i);

            if (child.CompareTag("meshGroup"))
            {
                Debug.Log($"Found meshGroup: {child.name}");
                turretGroup = child.gameObject;
                for (int f = 0; f < child.childCount; f++)
                {
                    if (child.GetChild(f).GetComponent<MeshRenderer>() != null)
                    {
                        meshRenderers.Add(child.GetChild(f).GetComponent<MeshRenderer>());
                    }
                    else
                    {
                        if (child.GetChild(f).tag == "bulletSpawnPoint")
                        {
                            bulletSpawnPoint = child.GetChild(f).gameObject;
                        }
                    }
                }
            }
            else
            {
                // Check if the component exists before adding it
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    meshRenderers.Add(renderer);
                }
            }
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
    public void UpdateTurretRadiusLight()
    {
        switch (gameObject.GetComponent<ConstructController>().currentConstructData.constructName)
        {
            case "Turret4":
                turretRadiusLight.gameObject.SetActive(true);
                turretRadiusLight.spotAngle = 57.4f;
                turretRadiusLight.innerSpotAngle = 57.4f;
                turretRadiusLight.color = lastKnownColor;
                break;
            case "Turret3":
                turretRadiusLight.gameObject.SetActive(true);
                turretRadiusLight.spotAngle = 54;
                turretRadiusLight.innerSpotAngle = 54;
                turretRadiusLight.color = lastKnownColor;
                break;
            case "Turret2":
                turretRadiusLight.gameObject.SetActive(true);
                turretRadiusLight.spotAngle = 50.5f;
                turretRadiusLight.innerSpotAngle = 50.5f;
                turretRadiusLight.color = lastKnownColor;
                break;
            case "Turret1":
                turretRadiusLight.gameObject.SetActive(true);
                turretRadiusLight.spotAngle = 47;
                turretRadiusLight.innerSpotAngle = 47;
                turretRadiusLight.color = lastKnownColor;
                break;
            default:
                Debug.LogWarning("I'm not a turret lol");
                turretRadiusLight.gameObject.SetActive(false);
                break;
        }
        
        if (gameObject.GetComponent<ConstructController>().Owner == GameManager.Instance.unclaimedFaction)
        {
            turretRadiusLight.color = new Color(0.75f, 0.75f, 0.75f, 1f); // Grey color for unclaimed constructs
        }
    }
    
    public void UpdateColor(Color color)
    {
        lastKnownColor = color;
        foreach (var renderer in meshRenderers)
        {
            renderer.material.color = color;
        }

        if (GetComponent<ConstructController>().isHoverGlowActive)
        {
            HoverGlow(true);
        }
    }
    
    public void quickUpdateColor(Color color)
    {
        constructLight.color = color;
        unitCapacityFillImage.color = color;
    }

    public void UpdateUnitCount(int count)
    {
        unitCountText.text = count.ToString();
    }
    
    public void UpdateHighlightVisibility(bool isSelected)
    {
        selectionIndicator.SetActive(isSelected);
    }
    
    public void UpdateHighlightColor(Color color)
    {
        selectionIndicator.GetComponent<Renderer>().material.color = color;
        
        selectionIndicator.SetActive(true);
    }
    
    public void HoverGlow(bool shouldGlow)
    {
        foreach (MeshRenderer mr in meshRenderers)
        {
            if (shouldGlow)
            {
                mr.material.color *= 1.5f;
                isHoverGlowing = true;
            }
            else if (!shouldGlow)
            {
                mr.material.color /= 1.5f;
                isHoverGlowing = false;
            }
        }
    }
    
    public void UpdateUnitCapacity(float current, ConstructData constructData)
    {
        if (constructData is HouseData)
        {
            int max = ((HouseData)constructData).maxUnitCapacity;
            unitCapacitySlider.value = current / max;
            //unitCapacityFillImage.gameObject.SetActive(max > 0);
            if (current == 0)
            {
                unitCapacityFillImage.gameObject.SetActive(false);
            }
            else
            {
                unitCapacityFillImage.gameObject.SetActive(true);
            }
        }
        else if (current > 0)
        {
            unitCapacitySlider.value = 1;
            unitCapacityFillImage.gameObject.SetActive(true);
        }
        else
        {
            unitCapacitySlider.value = 0;
            unitCapacityFillImage.gameObject.SetActive(false);
        }
    }
    
    public void HideVisuals()
    {
        unitCountText.enabled = false;
        //selectionIndicator.SetActive(false);
        unitCapacitySlider.gameObject.SetActive(false);
    }
    
    public void ShowVisuals()
    {
        unitCountText.enabled = true;
        unitCapacitySlider.gameObject.SetActive(true);
    }
    
    public void UpgradeScale(float time, ConstructData constructData)
    {
        StartCoroutine(AnimateScale(time, constructData));
    }
    
    void calibrateVisuals()
    {
        if (nodeConstruct == null) return;

        nodeConstruct.transform.localScale = originalScale;
        Destroy(nodeConstruct);
        nodeConstruct = Instantiate(GetComponent<ConstructController>().currentConstructData.visualPrefab, transform.position, Quaternion.identity);
        nodeConstruct.transform.parent = transform;
        SetMeshRenderers();
        UpdateColor(lastKnownColor);
        UpdateTurretRadiusLight();
    }
    
    public void ConstructChange(ConstructData constructData, bool isAnimated, Color newConstructColor)
    {
        StopAllCoroutines(); // was in isUpgrading but house didnt visually upgrade once
        calibrateVisuals();
        quickUpdateColor(newConstructColor);
        
        if (isUpgrading)
        {
            nodeConstruct.transform.localScale = originalScale;
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
        UpdateTurretRadiusLight();
    }

    private IEnumerator AnimateScale(float duration, ConstructData constructData)
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
        
        Destroy(nodeConstruct);
        GameObject newNodeConstruct = Instantiate(constructData.visualPrefab, transform.position, Quaternion.identity);
        newNodeConstruct.transform.parent = transform;
        nodeConstruct = newNodeConstruct;
        
        SetMeshRenderers();
        UpdateColor(lastKnownColor);

        elapsedTime = 0f;
        while (elapsedTime < growTime)
        {
            nodeConstruct.transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, elapsedTime / growTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        UpdateTurretRadiusLight();

        isUpgrading = false;
        nodeConstruct.transform.localScale = initialScale;
    }
    
    public IEnumerator AnimateConstructChange(ConstructData constructData, Color newConstructColor)
    {
        var scaleIncrease = 0.25f;
        Vector3 initialScale = originalScale;
        Vector3 finalScale = initialScale + new Vector3(scaleIncrease, scaleIncrease, scaleIncrease);
        float elapsedTime = 0f;
        float pulseFrequency = 1.0f;
        
        float reformDuration = 0.4f; // previously 0.25f
        if (GameManager.Instance.isIdleScene) reformDuration *= 2f;
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
        
        GameObject newNodeConstruct = Instantiate(constructData.visualPrefab, transform.position, Quaternion.identity);
        newNodeConstruct.transform.parent = transform;
        nodeConstruct = newNodeConstruct;
        
        SetMeshRenderers();
        UpdateColor(newConstructColor);
        UpdateTurretRadiusLight();

        elapsedTime = 0f;
        while (elapsedTime < growTime)
        {
            nodeConstruct.transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, elapsedTime / growTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        nodeConstruct.transform.localScale = initialScale;

        UpdateTurretRadiusLight();
    }
}