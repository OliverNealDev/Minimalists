// In Scripts/Core/GameManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    private UIManager uiManager;

    public enum GameState { Playing, Paused, Won, Lost }
    public GameState currentState;

    public List<ConstructController> allConstructs;
    public List<UnitController> allUnits;
    public FactionData unclaimedFaction;
    public FactionData playerFaction;
    public FactionData aiFaction;
    public FactionData ai2Faction;
    public FactionData ai3Faction;
    public FactionData ai4Faction;
    
    [SerializeField] private GameObject constructPrefab; // Prefab for constructs
    [SerializeField] private int constructs = 10; // Example limit for constructs
    private Vector3 constructSpawnPosition;
    private bool isValidSpawnPosition = false;
    
    public bool isIdleScene = false; // Set this to true for idle scenes

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();
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
        if (isIdleScene) return;
        
        if (currentState == GameState.Playing)
        {
            CheckWinCondition();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Playing)
            {
                uiManager.ShowPausePanel(true);
                currentState = GameState.Paused;
                Time.timeScale = 0f; // Pause the game
            }
            else if (currentState == GameState.Paused)
            {
                uiManager.ShowPausePanel(false);
                uiManager.ResumeGame();
            }
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
            }
            if (construct.Owner == faction)
            {
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
        var playersLeft = new List<FactionData>();
        foreach (ConstructController construct in allConstructs)
        {
            if (construct.Owner != null && !playersLeft.Contains(construct.Owner) && construct.Owner != unclaimedFaction)
            {
                playersLeft.Add(construct.Owner);
            }
        }
        if (playersLeft.Count == 1)
        {
            if (playersLeft[0].isPlayerControlled)
            {
                uiManager.ShowWinPanel();
                currentState = GameState.Won;
            }
            else
            {
                uiManager.ShowLosePanel();
                currentState = GameState.Lost;
            }
        }
        else if (!playersLeft.Contains(playerFaction))
        {
            uiManager.ShowLosePanel();
            currentState = GameState.Lost;
        }
    }
    
    
}