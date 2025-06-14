using System;
using UnityEngine;
using System.Collections;

public class ConstructController : MonoBehaviour
{
    [Header("Construct References")]
    public HouseData House1Data;
    public HouseData House2Data;
    public HouseData House3Data;
    public HouseData House4Data;
    public ConstructData Forge1Data;
    public ConstructData Forge2Data;
    public ConstructData Forge3Data;
    public ConstructData Forge4Data;
    public ConstructData Turret1Data;
    public ConstructData Turret2Data;
    public ConstructData Turret3Data;
    public ConstructData Turret4Data;

    [Header("Unit Spawning")]
    [SerializeField] private GameObject unitPrefab;
    
    public FactionData Owner { get; private set; }
    public float UnitCount { get; private set; }
    
    public ConstructData currentConstructData;
    private bool isUpgrading = false;
    public ConstructVisuals visuals;
    private float unitGenerationBuffer = 0f;
    
    private bool isMouseOver = false;
    
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
        Forge
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

    void Awake()
    {
        visuals = GetComponent<ConstructVisuals>();
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
                        UnitCount = House1Data.maxUnitCapacity;
                        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
                        break;
                    case initialLevelTypes.Level2:
                        currentConstructData = House2Data;
                        UnitCount = House2Data.maxUnitCapacity;
                        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
                        break;
                    case initialLevelTypes.Level3:
                        currentConstructData = House3Data;
                        UnitCount = House3Data.maxUnitCapacity;
                        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
                        break;
                    case initialLevelTypes.Level4:
                        currentConstructData = House4Data;
                        UnitCount = House4Data.maxUnitCapacity;
                        visuals.ConstructChange(currentConstructData, false, Owner.factionColor);
                        break;
                }
                break;
            case initialConstructTypes.Turret:
                switch (initialLevel)
                {
                    case initialLevelTypes.Level1:
                        currentConstructData = Turret1Data;
                        break;
                    case initialLevelTypes.Level2:
                        currentConstructData = Turret2Data;
                        break;
                    case initialLevelTypes.Level3:
                        currentConstructData = Turret3Data;
                        break;
                    case initialLevelTypes.Level4:
                        currentConstructData = Turret4Data;
                        break;
                }
                break;
            case initialConstructTypes.Forge:
                switch (initialLevel)
                {
                    case initialLevelTypes.Level1:
                        currentConstructData = Forge1Data;
                        break;
                    case initialLevelTypes.Level2:
                        currentConstructData = Forge2Data;
                        break;
                    case initialLevelTypes.Level3:
                        currentConstructData = Forge3Data;
                        break;
                    case initialLevelTypes.Level4:
                        currentConstructData = Forge4Data;
                        break;
                }
                break;
        }

        checkUpgradeIndicator();
        //InvokeRepeating("checkUpgradeIndicator", 0.05f, 0.05f);
    }

    void Update()
    {
        if (Owner == null) return;

        if (currentConstructData is HouseData houseData)
        {
            if (UnitCount < houseData.maxUnitCapacity)
            {
                unitGenerationBuffer += houseData.unitsPerSecond * Time.deltaTime;
                if (unitGenerationBuffer >= 1f)
                {
                    int wholeUnits = Mathf.FloorToInt(unitGenerationBuffer);
                    UnitCount += wholeUnits;
                    checkUpgradeIndicator();
                    unitGenerationBuffer -= wholeUnits;
                }
            }
            visuals.UpdateUnitCapacity(UnitCount, houseData.maxUnitCapacity);
        }
        else if (currentConstructData is TurretData turretData)
        {
            // Placeholder for Turret logic (e.g., finding and shooting targets)
        }
        else if (currentConstructData is ForgeData forgeData)
        {
            // Placeholder for Forge logic (e.g., applying buffs in an area)
        }

        visuals.UpdateUnitCount(Mathf.FloorToInt(UnitCount));
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
        int unitsToSend = Mathf.FloorToInt(UnitCount * percentage);
        if (unitsToSend > 0 && currentConstructData is HouseData)
        {
            StartCoroutine(SpawnUnitsRoutine(unitsToSend, target));
        }
    }

    private IEnumerator SpawnUnitsRoutine(int count, ConstructController target)
    {
        float spawnDelay = 1f / 4f;

        for (int i = 0; i < count; i++)
        {
            if (UnitCount <= 0) yield break;
            
            UnitCount--;
            checkUpgradeIndicator();
            Vector3 spawnPosition = new Vector3(transform.position.x, 0.68125f, transform.position.z);
            GameObject unit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
            
            UnitController unitController = unit.GetComponent<UnitController>();
            if (unitController != null)
            {
                unitController.Initialize(Owner, this, target);
            }
            
            yield return new WaitForSeconds(spawnDelay);
        }
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

                // --- CORRECTED LOGIC ---

                ConstructData newStateData;

                // Check if a downgrade is possible
                if (currentConstructData.downgradedVersion != null)
                {
                    // It can be downgraded. The new state is the downgraded version.
                    newStateData = currentConstructData.downgradedVersion;
                }
                else
                {
                    // It's already the lowest level. The state doesn't change, it just gets recaptured.
                    newStateData = currentConstructData; 
                }

                // Now, update the actual data on this controller
                currentConstructData = newStateData;

                CancelInvoke("upgradeConstruct");
                
                // And call the animation with the (guaranteed non-null) data.
                visuals.ConstructChange(newStateData, true, Owner.factionColor);

                // --- END OF CORRECTION ---

                if (isMouseOver) OnMouseEnter();
                //visuals.UpdateColor(Owner.factionColor);
                UpdateVisualsForOwner();
            }
        }
    
        checkUpgradeIndicator();
    }

    public void AttemptUpgrade()
    {
        if (currentConstructData.upgradedVersion == null)
        {
            Debug.Log("This construct is at its maximum level.");
            return;
        }
        
        if (isUpgrading) return;

        if (UnitCount >= currentConstructData.upgradeCost)
        {
            UnitCount -= currentConstructData.upgradeCost;
            
            visuals.UpgradeScale(currentConstructData.upgradeTime, currentConstructData.constructName);
            Invoke("UpgradeConstruct", currentConstructData.upgradeTime);
            isUpgrading = true;
            checkUpgradeIndicator();
            
            Debug.Log($"{name} has started the upgrade to {currentConstructData.constructName}!");
        }
        else
        {
            Debug.Log("Not enough units to upgrade.");
        }
    }

    private void UpgradeConstruct()
    {
        currentConstructData = currentConstructData.upgradedVersion;
        isUpgrading = false;
    }
    
    private void UpdateVisualsForOwner()
    {
        if (Owner.factionName == "Player" || Owner.factionName == "Unclaimed")
        {
            visuals.ShowVisuals();
        }
        else
        {
            visuals.HideVisuals();
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            visuals.UpdateSelectionColor(Color.white);
            visuals.UpdateSelection(true);
        }
        else if (isMouseOver)
        {
            visuals.UpdateSelectionColor(Color.grey);
            visuals.UpdateSelection(true);
        }
        else
        {
            visuals.UpdateSelection(false);
        }
    }
    
    private void checkUpgradeIndicator()
    {
        if (currentConstructData == null) return;
        
        if (currentConstructData.upgradedVersion != null && 
            !visuals.isUpgradeIndicatorVisible && 
            UnitCount >= currentConstructData.upgradeCost && 
            Owner.factionName == "Player" && 
            !isUpgrading)
        {
            visuals.setUpgradeIndicatorVisibility(true);
            Debug.Log("Upgrade indicator is now visible for " + currentConstructData.constructName);
        }
        else if ((currentConstructData.upgradedVersion != null && 
                 visuals.isUpgradeIndicatorVisible && 
                 UnitCount < currentConstructData.upgradeCost && 
                 Owner.factionName == "Player") || 
                 isUpgrading)
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
            if (InputManager.Instance.IsSelecting)
            {
                if (InputManager.Instance.startNode != this)
                {
                    visuals.UpdateSelectionColor(Color.green);
                    visuals.UpdateSelection(true);
                }
            }
            else
            {
                visuals.UpdateSelectionColor(Color.grey);
                visuals.UpdateSelection(true);
            }
        }
        else if (InputManager.Instance.IsSelecting)
        {
            visuals.UpdateSelectionColor(Color.red);
            visuals.UpdateSelection(true);
        }
    }

    private void OnMouseExit()
    {
        isMouseOver = false;
        
        if (!InputManager.Instance.IsSelecting || (InputManager.Instance.IsSelecting && InputManager.Instance.startNode != this))
        {
            visuals.UpdateSelection(false);
        }
    }
}
