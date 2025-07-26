using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Slider = UnityEngine.UI.Slider;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public Button houseButton;
    public Button towerButton;
    public Button helipadButton;
    public Button upgradeButton;
    public Button evenButton;

    public HouseData house1Data;
    public TurretData turret1Data;
    public HelipadData helipad1Data;
    
    public LayerMask constructLayer;
    public float doubleClickThreshold = 0.25f;
    
    //public ConstructController MortarAwaitingTarget { get; private set; }

    private Camera mainCamera;
    //public ConstructController startNode;
    private ConstructController lastClickedNode;
    private float timeSinceLastClick;
    
    public Slider UnitPercentBar;
    
    private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip sendSound;
    
    
    //public bool IsSelecting => startNode != null;
    
    public List<ConstructController> SelectedNodes { get; private set; } = new List<ConstructController>();
    
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
    }
    
    void Update()
    {
        timeSinceLastClick += Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
        }
        
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                UnselectAllNodes();
                foreach (ConstructController node in GameManager.Instance.allConstructs)
                {
                    if (node.Owner == GameManager.Instance.playerFaction)
                    {
                        SelectedNodes.Add(node);
                        node.visuals.UpdateHighlightColor(Color.white);
                        node.DisableHoverGlow();
                    }
                }
                CheckNodeHighlights();
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            UpgradeSelectedNodes();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            EvenOutUnitCounts();
        }

        if (Input.mouseScrollDelta.y != 0)
        {
            if (Input.mouseScrollDelta.y > 0)
            {
                if (UnitPercentBar.value >= 4f) return; // Prevent going above 4 (100%)
                UnitPercentBar.value += 1f;
            }
            else if (Input.mouseScrollDelta.y < 0)
            {
                if (UnitPercentBar.value <= 1f) return; // Prevent going below 1 (25%)
                UnitPercentBar.value -= 1f;
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ConvertSelectedNodes("House");
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            ConvertSelectedNodes("Turret");
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ConvertSelectedNodes("Helipad");
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnselectAllNodes();
        }
    }

    private void HandleLeftClick()
    {
        if (IsPointerOverUIWithTag("UIBlocker"))
        {
            return; // Exit the method if a blocking UI element was clicked
        }
        
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, constructLayer))
        {
            ConstructController clickedNode = hit.collider.GetComponent<ConstructController>();
            if (clickedNode == null || clickedNode.Owner != GameManager.Instance.playerFaction)
            {
                if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                {
                    UnselectAllNodes();
                }
                return;
            }
            
            if (timeSinceLastClick < doubleClickThreshold && lastClickedNode == clickedNode && (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)))
            {
                clickedNode.AttemptUpgrade();
            }
            else if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                timeSinceLastClick = 0;
                lastClickedNode = clickedNode;
            }
            
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                UnselectAllNodes();
            }
            
            if (SelectedNodes.Contains(clickedNode) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                UnselectNode(clickedNode);
                return;
            }
            
            if (!SelectedNodes.Contains(clickedNode))
            {
                SelectedNodes.Add(clickedNode);
                clickedNode.visuals.UpdateHighlightColor(Color.white);
                clickedNode.DisableHoverGlow();
                
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(clickSound);
            }
        }
        else
        {
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                UnselectAllNodes();
            }
        }
    }

    private void HandleRightClick()
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, constructLayer))
        {
            ConstructController clickedNode = hit.collider.GetComponent<ConstructController>();
            if (clickedNode == null) // shouldn't happen, but just in case
            {
                return;
            }

            if (SelectedNodes.Count > 0)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(sendSound);
                
                foreach (ConstructController c in SelectedNodes)
                {
                    if (c == clickedNode) continue;
                    c.SendUnits(clickedNode, UnitPercentBar.value / 4);
                }
            }
        }
    }
    
    private void UnselectAllNodes()
    {
        foreach (var node in SelectedNodes)
        {
            node.visuals.UpdateHighlightVisibility(false);
        }
        SelectedNodes.Clear();
        
        if (SelectedNodes.Count == 0)
        {
            CheckNodeHighlights();
        }
    }
    
    public void UnselectNode(ConstructController node)
    {
        if (SelectedNodes.Contains(node)) // double check to avoid errors
        {
            node.visuals.UpdateHighlightVisibility(false);
            SelectedNodes.Remove(node);
            
            if (SelectedNodes.Count == 0)
            {
                CheckNodeHighlights();
            }
        }
    }

    public void UpgradeSelectedNodes()
    {
        foreach (ConstructController c in SelectedNodes)
        {
            c.AttemptUpgrade();
        }
    }

    private void CheckNodeHighlights()
    {
        foreach (ConstructController node in GameManager.Instance.allConstructs)
        {
            node.CheckHighlight();
        }
    }
    
    public void ConvertSelectedNodes(string constructType)
    {
        foreach (ConstructController c in SelectedNodes)
        {
            if (c.currentConstructData is not HouseData && constructType == "House")
            {
                c.AttemptConvertConstruct(house1Data);
            }
            else if (c.currentConstructData is not TurretData && constructType == "Turret")
            {
                c.AttemptConvertConstruct(turret1Data);
            }
            else if (c.currentConstructData is not HelipadData && constructType == "Helipad")
            {
                c.AttemptConvertConstruct(helipad1Data);
            }
        }
    }
    
    public void EvenOutUnitCounts()
    {
        // Ensure there are at least two nodes selected to balance between.
        if (SelectedNodes.Count < 2)
        {
            return;
        }

        // --- 1. Calculate Total and Target Distribution ---
        int totalUnits = SelectedNodes.Sum(node => node.UnitCount);
        int nodeCount = SelectedNodes.Count;
        int baseUnitCount = totalUnits / nodeCount;
        int remainder = totalUnits % nodeCount;

        // --- 2. Identify Givers and Receivers ---
        var givers = new List<ConstructController>();
        var receivers = new List<ConstructController>();
        var promisedUnits = new Dictionary<ConstructController, int>();

        // Create a sorted list to distribute the remainder deterministically.
        List<ConstructController> sortedNodes = SelectedNodes.OrderBy(n => n.GetInstanceID()).ToList();

        foreach (var node in sortedNodes)
        {
            int targetCount = baseUnitCount;
            if (remainder > 0)
            {
                targetCount++;
                remainder--;
            }

            int difference = node.UnitCount - targetCount;

            if (difference > 0)
            {
                givers.Add(node);
                promisedUnits.Add(node, difference); // This node has a surplus of units.
            }
            else if (difference < 0)
            {
                receivers.Add(node);
                promisedUnits.Add(node, difference); // This node has a deficit of units.
            }
        }

        // --- 3. Execute the Transfers ---
        foreach (var receiver in receivers)
        {
            int unitsNeeded = -promisedUnits[receiver]; // Get the positive deficit value.

            foreach (var giver in givers)
            {
                if (unitsNeeded <= 0) break; // Stop if the receiver's needs are met.

                int unitsAvailable = promisedUnits[giver];
                if (unitsAvailable > 0)
                {
                    int unitsToSend = Mathf.Min(unitsNeeded, unitsAvailable);

                    if (unitsToSend > 0)
                    {
                        giver.SendExactUnits(receiver, unitsToSend);

                        // Update the dictionaries to reflect the sent units.
                        promisedUnits[giver] -= unitsToSend;
                        unitsNeeded -= unitsToSend;
                    }
                }
            }
        }
    }

    public void onUnitPercentBarValueManuallyChanged()
    {
        if (UnitPercentBar.value < 1f)
        {
            UnitPercentBar.value = 1f; // Minimum value is 1 (25%)
        }
    }

    private bool IsPointerOverUIWithTag(string tag)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
    
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
    
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.CompareTag(tag))
            {
                return true;
            }
        }
    
        return false;
    }
    
    /*private void HandleSelectionStart(ConstructController clickedNode)
    {
        timeSinceLastClick = 0;
        lastClickedNode = clickedNode;
        startNode = clickedNode;
        startNode.SetSelected(true);
    }*/

    /*private void HandleSelectionEnd()
    {
        if (startNode == null) return;

        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, 100f, constructLayer))
        {
            ConstructController endNode = hit.collider.GetComponent<ConstructController>();
            if (endNode == null || endNode != lastClickedNode)
            {
                lastClickedNode = null;
            }
            if (endNode != null && endNode != startNode)
            {
                endNode.SetSelected(false);
                startNode.SendUnits(endNode, 0.5f);
            }
        }
        else
        {
            lastClickedNode = null;
        }
        
        startNode.SetSelected(false);
        ResetClickState();
    }*/

    /*private void ResetClickState()
    {
        startNode = null;
        timeSinceLastClick = 0;
    }*/
}
