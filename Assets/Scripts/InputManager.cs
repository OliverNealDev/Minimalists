using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

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
                    }
                }
                CheckNodeHighlights();
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            UpgradeSelectedNodes();
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
                foreach (ConstructController c in SelectedNodes)
                {
                    if (c == clickedNode) continue;
                    c.SendUnits(clickedNode, 0.5f);
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
    
    private void ConvertSelectedNodes(string constructType)
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
