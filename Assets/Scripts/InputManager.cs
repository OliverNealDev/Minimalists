using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    
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

        if (Input.GetKeyDown(KeyCode.U))
        {
            UpgradeSelectedNodes();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnselectAllNodes();
        }
    }

    private void HandleLeftClick()
    {
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

    private void UpgradeSelectedNodes()
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
