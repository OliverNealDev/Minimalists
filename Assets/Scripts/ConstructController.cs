using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ConstructController : MonoBehaviour
{
    [Header("Construct References")]
    public HouseData House1Data;
    public HouseData House2Data;
    public HouseData House3Data;
    public HouseData House4Data;
    public MortarData Mortar1Data;
    public MortarData Mortar2Data;
    public MortarData Mortar3Data;
    public MortarData Mortar4Data;
    public TurretData Turret1Data;
    public TurretData Turret2Data;
    public TurretData Turret3Data;
    public TurretData Turret4Data;
    public HelipadData HelipadData;

    [Header("Unit Spawning")]
    [SerializeField] private GameObject unitPrefab;
    
    public FactionData Owner { get; private set; }
    public int UnitCount { get; private set; }
    
    public ConstructData currentConstructData;
    private bool isUpgrading = false;
    private bool isConverting = false;
    private ConstructData convertingConstructData;
    public ConstructVisuals visuals;
    private float unitGenerationBuffer = 0f;
    private float shootCooldownBuffer = 0f;
    private List<ConstructController> targetConstructs = new List<ConstructController>();
    
    public GameObject mortarProjectile;

    public GameObject turretTarget;
    
    private bool isMouseOver = false;
    public bool isHoverGlowActive = false;
    
    public ProceduralArrow arrowInstance;
    public MortarProceduralArrow mortarProceduralArrow;
    
    public enum initialOwnerTypes
    {
        Unclaimed,
        Player,
        AI1,
        AI2
    }
    public initialOwnerTypes initialOwner;
    public enum initialConstructTypes
    {
        House,
        Turret,
        Mortar,
        Helipad
    }
    public initialConstructTypes initialConstructType;
    public enum initialLevelTypes
    {
        Level1,
        Level2,
        Level3,
        Level4
    }
    public initialLevelTypes initialLevel;
    public enum initialUnitCapacityTypes
    {
        Automatic,
        Manual
    }
    public initialUnitCapacityTypes initialUnitCapacityType;
    public int manualInitialUnitCapacity = 0;
    
    public bool isIdleScene = false;

    void Awake()
    {
        visuals = GetComponent<ConstructVisuals>();

        arrowInstance = FindFirstObjectByType<ProceduralArrow>();
    }
    void Start()
    {
        switch (initialOwner)
        {
            case initialOwnerTypes.Unclaimed:
                SetInitialOwner(GameManager.Instance.unclaimedFaction);
                break;
            case initialOwnerTypes.Player:
                SetInitialOwner(GameManager.Instance.playerFaction);
                break;
            case initialOwnerTypes.AI1:
                SetInitialOwner(GameManager.Instance.aiFaction);
                break;
            case initialOwnerTypes.AI2:
                SetInitialOwner(GameManager.Instance.ai2Faction);
                break;
        }
        
        switch (initialConstructType)
        {
            case initialConstructTypes.House:
                switch (initialLevel)
                {
                    case initialLevelTypes.Level1:
                        currentConstructData = House1Data;
                        if (initialUnitCapacityType == initialUnitCapacityTypes.Automatic) UnitCount = House1Data.maxUnitCapacity;
                        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
                        break;
                    case initialLevelTypes.Level2:
                        currentConstructData = House2Data;
                        if (initialUnitCapacityType == initialUnitCapacityTypes.Automatic) UnitCount = House2Data.maxUnitCapacity;
                        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
                        break;
                    case initialLevelTypes.Level3:
                        currentConstructData = House3Data;
                        if (initialUnitCapacityType == initialUnitCapacityTypes.Automatic) UnitCount = House3Data.maxUnitCapacity;
                        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
                        break;
                    case initialLevelTypes.Level4:
                        currentConstructData = House4Data;
                        break;
                }
                break;
            case initialConstructTypes.Turret:
                switch (initialLevel)
                {
                    case initialLevelTypes.Level1:
                        currentConstructData = Turret1Data;
                        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
                        break;
                    case initialLevelTypes.Level2:
                        currentConstructData = Turret2Data;
                        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
                        break;
                    case initialLevelTypes.Level3:
                        currentConstructData = Turret3Data;
                        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
                        break;
                    case initialLevelTypes.Level4:
                        currentConstructData = Turret4Data;
                        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
                        break;
                }
                break;
            case initialConstructTypes.Mortar:
                switch (initialLevel)
                {
                    case initialLevelTypes.Level1:
                        currentConstructData = Mortar1Data;
                        break;
                    case initialLevelTypes.Level2:
                        currentConstructData = Mortar2Data;
                        break;
                    case initialLevelTypes.Level3:
                        currentConstructData = Mortar3Data;
                        break;
                    case initialLevelTypes.Level4:
                        currentConstructData = Mortar4Data;
                        break;
                }
                break;
            case initialConstructTypes.Helipad:
                currentConstructData = HelipadData;
                break;
        }

        if (initialUnitCapacityType == initialUnitCapacityTypes.Automatic)
        {
            switch (initialLevel)
            {
                case initialLevelTypes.Level1:
                    UnitCount = House1Data.maxUnitCapacity;
                    break;
                case initialLevelTypes.Level2:
                    UnitCount = House2Data.maxUnitCapacity;
                    break;
                case initialLevelTypes.Level3:
                    UnitCount = House3Data.maxUnitCapacity;
                    break;
                case initialLevelTypes.Level4:
                    UnitCount = House4Data.maxUnitCapacity;
                    break;
                default:
                    UnitCount = 0;
                    Debug.LogWarning("No valid level set for initialUnitCapacityType, defaulting to 0.");
                    break;
            }
        }
        else
        {
            UnitCount = manualInitialUnitCapacity;
        }
        
        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
        visuals.UpdateUnitCapacity(UnitCount, currentConstructData);
        checkUpgradeIndicator();
        //InvokeRepeating("checkUpgradeIndicator", 0.05f, 0.05f);
    }
    void Update()
    {
        if (Owner == null) return;

        if (Owner.isPlayerControlled && Input.GetKeyDown(KeyCode.P))
        {
            UnitCount += 10;
        }
        
        if (currentConstructData is HouseData houseData)
        {
            if (UnitCount < houseData.maxUnitCapacity)
            {
                if (!GameManager.Instance.IsPopulationCapFull(Owner)) unitGenerationBuffer += houseData.unitsPerSecond * Time.deltaTime;
                if (unitGenerationBuffer >= 1f)
                {
                    int wholeUnits = Mathf.FloorToInt(unitGenerationBuffer);
                    UnitCount += wholeUnits;
                    visuals.UpdateUnitCapacity(UnitCount, currentConstructData);
                    checkUpgradeIndicator();
                    unitGenerationBuffer -= wholeUnits;
                }
            }
        }
        else if (currentConstructData is TurretData turretData)
        {
            shootCooldownBuffer -= Time.deltaTime;
            if (shootCooldownBuffer <= 0f && visuals.isLockedOn)
            {
                Collider[] colliders = Physics.OverlapSphere(transform.position, turretData.range);
                UnitController nearestEnemy = null;
                float minDistance = float.MaxValue;

                foreach (Collider col in colliders)
                {
                    UnitController unit = col.GetComponent<UnitController>();
                    if (unit != null && unit.owner != this.Owner && !unit.isHelicopter)
                    {
                        float distance = Vector3.Distance(transform.position, col.transform.position);
                        if (distance < minDistance && distance > 1)
                        {
                            minDistance = distance;
                            nearestEnemy = unit;
                        }
                    }
                }

                if (nearestEnemy != null)
                {
                    GameObject newTurretProjectile = Instantiate(
                        turretData.projectilePrefab, 
                        visuals.bulletSpawnPoint.transform.position, 
                        Quaternion.identity);
                    turretProjectileController projectileController = newTurretProjectile.GetComponent<turretProjectileController>();
                    projectileController.owner = Owner;
                    projectileController.target = nearestEnemy;
                    
                    shootCooldownBuffer = 1f / turretData.fireRate;
                }
            }
        }
        else if (currentConstructData is MortarData mortarData)
        {
            // Placeholder for Mortar logic (e.g., applying buffs in an area)
        }

        visuals.UpdateUnitCount(Mathf.FloorToInt(UnitCount));
    }

    void FixedUpdate() // VERY TAXING and can definitely be optimised
    {
        if (currentConstructData is TurretData turretData)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, turretData.range);
            UnitController nearestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (Collider col in colliders)
            {
                UnitController unit = col.GetComponent<UnitController>();
                if (unit != null && unit.owner != this.Owner && !unit.isHelicopter)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < minDistance && distance > 1)
                    {
                        minDistance = distance;
                        nearestEnemy = unit;
                    }
                }
            }

            if (nearestEnemy != null)
            {
                turretTarget = nearestEnemy.gameObject;
            }
        }

        if (turretTarget != null)
        {
            float distance = Vector3.Distance(transform.position, turretTarget.transform.position);
            if (distance < 1)
            {
                turretTarget = null;
            }
        }
    }
    
    public void SetInitialOwner(FactionData newOwner)
    {
        Owner = newOwner;
        visuals.UpdateColor(newOwner.factionColor);
        UpdateVisualsForOwner();
        checkUpgradeIndicator();
    }
    
    public void SendUnits(ConstructController target, float percentage)
    {
        bool isValidTarget = true;
        foreach (ConstructController construct in targetConstructs)
        {
            if (construct == target)
            {
                isValidTarget = false;
                break;
            }
        }
        
        if (!isValidTarget) return;
        
        int unitsToSend = Mathf.FloorToInt(UnitCount * percentage);
        if (unitsToSend == 0 && UnitCount > 0)
        {
            unitsToSend = 1;
        }
        if (unitsToSend > 0)
        {
            targetConstructs.Add(target);
            StartCoroutine(SpawnUnitsRoutine(unitsToSend, target));
        }
    }
    
    public void SendExactUnits(ConstructController target, int unitsToSend)
    {
        // A node cannot send units to itself.
        if (target == this || unitsToSend <= 0)
        {
            return;
        }
        
        // Prevents sending multiple streams of units to the same target simultaneously.
        bool isAlreadySending = false;
        foreach (ConstructController construct in targetConstructs)
        {
            if (construct == target)
            {
                isAlreadySending = true;
                break;
            }
        }
        
        if (isAlreadySending) return;
        
        if (UnitCount > 0)
        {
            targetConstructs.Add(target);
            StartCoroutine(SpawnUnitsRoutine(unitsToSend, target));
        }
    }
    
    private IEnumerator SpawnUnitsRoutine(int count, ConstructController target)
    {
        float spawnDelay = 1f / 4f;

        for (int i = 0; i < count; i++)
        {
            if (UnitCount <= 0)
            {
                targetConstructs.Remove(target);
                yield break;
            }
            
            UnitCount--;
            visuals.UpdateUnitCapacity(UnitCount, currentConstructData);
            checkUpgradeIndicator();
            Vector3 spawnPosition = new Vector3(transform.position.x, 0.68125f, transform.position.z);
            GameObject unit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
            
            UnitController unitController = unit.GetComponent<UnitController>();
            if (unitController != null)
            {
                unitController.Initialize(Owner, this, target, currentConstructData is HelipadData);
            }
            
            yield return new WaitForSeconds(spawnDelay);
        }
        
        targetConstructs.Remove(target);
    }
    public void ReceiveUnit(FactionData unitOwner)
    {
        if (unitOwner == Owner)
        {
            UnitCount++;
        }
        else
        {
            UnitCount--;
            if (UnitCount < 0)
            {
                Owner = unitOwner;
                UnitCount = 1;
                
                StopAllCoroutines();
                CancelInvoke();
                if (isConverting)
                {
                    isConverting = false;
                    convertingConstructData = null;
                }

                if (isUpgrading)
                {
                    isUpgrading = false;
                }
                targetConstructs.Clear();
                
                ConstructData newStateData;
                
                if (currentConstructData.downgradedVersion != null && !isUpgrading)
                {
                    newStateData = currentConstructData.downgradedVersion;
                }
                else
                {
                    //if (isUpgrading) CancelInvoke("upgradeConstruct");
                    newStateData = currentConstructData; 
                }
                
                currentConstructData = newStateData;
                visuals.ConstructChange(newStateData, true, Owner.factionColor);
                
                //if (isMouseOver) OnMouseEnter();
                InputManager.Instance.UnselectNode(this);
                visuals.UpdateHighlightVisibility(false);
                if (isHoverGlowActive)
                {
                    visuals.HoverGlow(false);
                    isHoverGlowActive = false;
                }
                UpdateVisualsForOwner();
            }
        }
    
        visuals.UpdateUnitCapacity(UnitCount, currentConstructData);
        checkUpgradeIndicator();
    }
    
    public void AttemptUpgrade()
    {
        if (currentConstructData.upgradedVersion == null)
        {
            return;
        }
        
        if (isUpgrading || isConverting) return;

        if (UnitCount >= currentConstructData.upgradeCost)
        {
            UnitCount -= currentConstructData.upgradeCost;
            
            visuals.UpdateUnitCapacity(UnitCount, currentConstructData);
            ConstructData newConstructData = currentConstructData.upgradedVersion;
            if (!isIdleScene)
            {
                visuals.UpgradeScale(currentConstructData.upgradeTime, newConstructData);
                Invoke("UpgradeConstruct", currentConstructData.upgradeTime + 0.4f);
                isUpgrading = true;
            }
            else
            {
                visuals.ConstructChange(newConstructData, true, Owner.factionColor);
                Invoke("UpgradeConstruct", 0.4f);
            }
            checkUpgradeIndicator();
            
            //Debug.Log($"{name} has started the upgrade to {currentConstructData.constructName}!");
        }
        else
        {
            //Debug.Log("Not enough units to upgrade.");
        }
    }
    private void UpgradeConstruct()
    {
        currentConstructData = currentConstructData.upgradedVersion;
        isUpgrading = false;
        
        visuals.UpdateUnitCapacity(UnitCount, currentConstructData);
        checkUpgradeIndicator();
    }

    public void AttemptConvertConstruct(ConstructData constructData)
    {
        if (isUpgrading || isConverting) return;
        
        if (UnitCount >= constructData.conversionCost)
        {
            UnitCount -= constructData.conversionCost;
            visuals.UpdateUnitCapacity(UnitCount, currentConstructData);
            
            visuals.UpgradeScale(constructData.conversionTime, constructData);
            convertingConstructData = constructData;
            Invoke("ConvertConstruct", constructData.conversionTime + 0.4f);
            isConverting = true;
            //UpdateVisualsForOwner();
            checkUpgradeIndicator();
        }
    }

    private void ConvertConstruct()
    {
        currentConstructData = convertingConstructData;
        isConverting = false;
        convertingConstructData = null;
        
        visuals.UpdateUnitCapacity(UnitCount, currentConstructData);
        checkUpgradeIndicator();
    }
    
    private void UpdateVisualsForOwner()
    {
        if (Owner.factionName == "Player" || Owner.factionName == "Unclaimed")
        {
            visuals.ShowVisuals();
            //if (isMouseOver) visuals.UpdateHighlightColor(Color.grey);
            if (isMouseOver && !isHoverGlowActive)
            {
                visuals.HoverGlow(true);
                isHoverGlowActive = true;
            }
        }
        else
        {
            visuals.HideVisuals();
            if (isMouseOver && InputManager.Instance.SelectedNodes.Count > 0) visuals.UpdateHighlightColor(Color.red);
        }
    }
    
    private void checkUpgradeIndicator()
    {
        if (currentConstructData == null) return;
        
        if (currentConstructData.upgradedVersion != null && 
            !visuals.isUpgradeIndicatorVisible && 
            UnitCount >= currentConstructData.upgradeCost && 
            Owner.factionName == "Player" && 
            !isUpgrading &&
            !isConverting)
        {
            visuals.setUpgradeIndicatorVisibility(true);
            Debug.Log("Upgrade indicator is now visible for " + currentConstructData.constructName);
        }
        else if ((currentConstructData.upgradedVersion != null && 
                 visuals.isUpgradeIndicatorVisible && 
                 UnitCount < currentConstructData.upgradeCost && 
                 Owner.factionName == "Player") || 
                 isUpgrading ||
                 isConverting)
        {
            visuals.setUpgradeIndicatorVisibility(false);
            Debug.Log("Upgrade indicator is now hidden for " + currentConstructData.constructName);
        }
    }
    
    private void OnMouseEnter()
    {
        isMouseOver = true;
        
        if (Owner.factionName == "Player")
        {
            if (!InputManager.Instance.SelectedNodes.Contains(this))
            {
                if (!isHoverGlowActive)
                {
                    //visuals.UpdateHighlightColor(Color.grey);
                    visuals.HoverGlow(true);
                    isHoverGlowActive = true;
                }
            }
        }
        else if (InputManager.Instance.SelectedNodes.Count > 0)
        {
            visuals.UpdateHighlightColor(Color.red);
        }
    }

    public void CheckHighlight()
    {
        if (isMouseOver)
        {
            if (Owner.factionName == "Player")
            {
                if (!InputManager.Instance.SelectedNodes.Contains(this))
                {
                    if (!isHoverGlowActive)
                    {
                        //visuals.UpdateHighlightColor(Color.grey);
                        visuals.HoverGlow(true);
                        isHoverGlowActive = true;
                    }
                }
            }
            else if (InputManager.Instance.SelectedNodes.Count > 0)
            {
                visuals.UpdateHighlightColor(Color.red);
            }
            else
            {
                visuals.UpdateHighlightVisibility(false);
            }
        }
    }

    public void DisableHoverGlow()
    {
        if (isHoverGlowActive)
        {
            visuals.HoverGlow(false);
            isHoverGlowActive = false;
        }
    }
    
    private void OnMouseExit()
    {
        isMouseOver = false;
        
        if (!InputManager.Instance.SelectedNodes.Contains(this))
        {
            visuals.UpdateHighlightVisibility(false);
        }

        if (isHoverGlowActive)
        {
            visuals.HoverGlow(false);
            isHoverGlowActive = false;
        }
    }
}
