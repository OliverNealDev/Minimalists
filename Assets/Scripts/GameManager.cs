// In Scripts/Core/GameManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Paused, Won, Lost }
    public GameState currentState;

    public List<ConstructController> allConstructs;
    public List<UnitController> allUnits;
    public FactionData unclaimedFaction;
    public FactionData playerFaction;
    public FactionData aiFaction;
    public FactionData ai2Faction;
    
    [SerializeField] private GameObject constructPrefab; // Prefab for constructs
    [SerializeField] private int constructs = 10; // Example limit for constructs
    private Vector3 constructSpawnPosition;
    private bool isValidSpawnPosition = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    
    void Start()
    {
        //spawnConstructs();
        allConstructs = FindObjectsByType<ConstructController>(FindObjectsSortMode.None).ToList();
        
        /*ConstructController playerStartNode = allConstructs[0];
        playerStartNode.SetInitialOwner(playerFaction);
        
        ConstructController aiStartNode = allConstructs[1];
        aiStartNode.SetInitialOwner(aiFaction);
        
        ConstructController ai2StartNode = allConstructs[2];
        ai2StartNode.SetInitialOwner(ai2Faction);
        
        foreach (ConstructController construct in allConstructs)
        {
            if (construct.Owner == null)
            {
                construct.SetInitialOwner(unclaimedFaction);
            }
        }*/
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            CheckWinCondition();
        }
    }
    
    public void registerUnit(UnitController unit)
    {
        allUnits.Add(unit);
    }
    
    public void unregisterUnit(UnitController unit)
    {
        allUnits.Remove(unit);
    }

    public bool IsPopulationCapFull(FactionData faction)
    {
        int countedPopulationCapacity = 0;
        int countedPopulation = 0;
        
        foreach (ConstructController construct in allConstructs)
        {
            if (construct.Owner == faction && construct.currentConstructData is HouseData)
            {
                countedPopulationCapacity += ((HouseData)construct.currentConstructData).maxUnitCapacity;
                countedPopulation += construct.UnitCount;
            }
        }

        foreach (UnitController unit in allUnits)
        {
            if (unit.owner == faction)
            {
                countedPopulation += 1;
            }
        }
        
        if (countedPopulation >= countedPopulationCapacity)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void spawnConstructs()
    {
        for (int i = 0; i < constructs; i++)
        {
            isValidSpawnPosition = false;
            for (int attempts = 0; attempts < 50; attempts++)
            {
                if (isValidSpawnPosition) break;
                constructSpawnPosition = new Vector3(Random.Range(-4.5f, 4.5f), 0.25f, Random.Range(-4.5f, 4.5f));
                foreach (ConstructController construct in allConstructs)
                {
                    if (Vector3.Distance(construct.transform.position, constructSpawnPosition) < 2f)
                    {
                        if (attempts == 49) // If we tried 50 times and found no valid position
                        {
                            Debug.LogWarning("Failed to find a valid spawn position for construct after 50 attempts.");
                        }
                    }
                    else
                    {
                        isValidSpawnPosition = true;
                    }
                }
            }

            isValidSpawnPosition = false;
            
            GameObject constructObj = Instantiate(constructPrefab, constructSpawnPosition, Quaternion.identity);
            ConstructController constructController = constructObj.GetComponent<ConstructController>();
            allConstructs.Add(constructController);
        }
    }

    void CheckWinCondition()
    {
        // Logic to check if all nodes belong to the player or the AI
        // If so, change currentState to Won or Lost
    }
}