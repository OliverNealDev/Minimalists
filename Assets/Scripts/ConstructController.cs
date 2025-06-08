using System.Collections;
using UnityEngine;

public class ConstructController : MonoBehaviour
{
    public ConstructData constructData;
    public FactionData Owner { get; private set; }
    public float UnitCount { get; private set; }

    private ConstructVisuals visuals;
    private float unitGenerationBuffer = 0f;

    [SerializeField] private GameObject unitPrefab;

    void Awake()
    {
        visuals = GetComponent<ConstructVisuals>();
    }

    void Update()
    {
        if (Owner != null)
        {
            if (UnitCount < constructData.maxUnitCapacity)
            {
                unitGenerationBuffer += constructData.unitsPerSecond * Time.deltaTime;
                if (unitGenerationBuffer >= 1f)
                {
                    int wholeUnits = Mathf.FloorToInt(unitGenerationBuffer);
                    UnitCount += wholeUnits;
                    unitGenerationBuffer -= wholeUnits;
                }
            }
        }
        visuals.UpdateUnitCount(Mathf.FloorToInt(UnitCount));
        visuals.UpdateUnitCapacity(UnitCount, constructData.maxUnitCapacity);
    }
    
    public void SetInitialOwner(FactionData newOwner)
    {
        Owner = newOwner;
        UnitCount = 10;
        visuals.UpdateUnitCapacity(UnitCount, constructData.maxUnitCapacity);
        visuals.UpdateColor(newOwner.factionColor);
    }
    
    public void SendUnits(ConstructController target, float percentage)
    {
        int unitsToSend = Mathf.FloorToInt(UnitCount * percentage);
        if (unitsToSend > 0)
        {
            Debug.Log($"Sending {unitsToSend} units from {name} to {target.name}");
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
                Debug.LogWarning("No units left to send.");
                yield break;
            }
            
            Vector3 spawnPosition = new Vector3(transform.position.x, 0.18125f, transform.position.z);
            GameObject unit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
            UnitCount--;
            visuals.UpdateUnitCapacity(UnitCount, constructData.maxUnitCapacity);
            
            UnitController unitController = unit.GetComponent<UnitController>();
            if (unitController != null)
            {
                unitController.Initialize(Owner, this.GetComponent<ConstructController>(), target);
            }
            
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    public void ReceiveUnit(FactionData unitOwner)
    {
        if (unitOwner == Owner)
        {
            UnitCount++;
            visuals.UpdateUnitCapacity(UnitCount, constructData.maxUnitCapacity);
        }
        else
        {
            UnitCount--;
            if (UnitCount < 0)
            {
                Owner = unitOwner;
                UnitCount += 2;
                visuals.UpdateUnitCapacity(UnitCount, constructData.maxUnitCapacity);
                visuals.UpdateColor(Owner.factionColor);
            }
        }
    }
    
    public void SetSelected(bool isSelected)
    {
        visuals.UpdateSelection(isSelected);
    }
}