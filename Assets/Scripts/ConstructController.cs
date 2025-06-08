using System;
using UnityEngine;
using System.Collections;

public class ConstructController : MonoBehaviour
{
    [Header("Setup")]
    public ConstructData initialUnclaimedData;
    public ConstructData initialClaimedData;

    [Header("Unit Spawning")]
    [SerializeField] private GameObject unitPrefab;
    
    public FactionData Owner { get; private set; }
    public float UnitCount { get; private set; }
    
    private ConstructData currentConstructData;
    public ConstructVisuals visuals;
    private float unitGenerationBuffer = 0f;
    
    private bool isMouseOver = false;

    void Awake()
    {
        visuals = GetComponent<ConstructVisuals>();
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
        if (Owner.factionName == "Unclaimed")
        {
            UnitCount = 5;
            currentConstructData = initialUnclaimedData;
        }
        else
        {
            UnitCount = 10;
            currentConstructData = initialClaimedData;
        }
        
        visuals.UpdateColor(newOwner.factionColor);
        UpdateVisualsForOwner();
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
            Vector3 spawnPosition = new Vector3(transform.position.x, 0.18125f, transform.position.z);
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
                bool wasUnclaimed = Owner.factionName == "Unclaimed";
                Owner = unitOwner;
                UnitCount = 1;

                if (wasUnclaimed)
                {
                    currentConstructData = initialClaimedData;
                }

                if (isMouseOver) OnMouseEnter();
                visuals.UpdateColor(Owner.factionColor);
                UpdateVisualsForOwner();
            }
        }
    }

    public void AttemptUpgrade()
    {
        if (currentConstructData.upgradedVersion == null)
        {
            Debug.Log("This construct is at its maximum level.");
            return;
        }

        if (UnitCount >= currentConstructData.upgradeCost)
        {
            UnitCount -= currentConstructData.upgradeCost;
            currentConstructData = currentConstructData.upgradedVersion;
            
            // Here you could trigger a visual effect for the upgrade
            Debug.Log($"{name} has been upgraded to {currentConstructData.name}!");
        }
        else
        {
            Debug.Log("Not enough units to upgrade.");
        }
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
