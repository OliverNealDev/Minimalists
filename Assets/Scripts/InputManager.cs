// In Scripts/Input/InputManager.cs
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private Camera mainCamera;
    private ConstructController startNode;
    public LayerMask constructLayer;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleSelectionStart();
        }

        if (Input.GetMouseButtonUp(0))
        {
            HandleSelectionEnd();
        }
    }

    private void HandleSelectionStart()
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, constructLayer))
        {
            ConstructController controller = hit.collider.GetComponent<ConstructController>();
            // Only select if it's a player-owned node
            if (controller != null && controller.Owner == GameManager.Instance.playerFaction)
            {
                startNode = controller;
                startNode.SetSelected(true); // Visual feedback
            }
        }
    }
    
    private void HandleSelectionEnd()
    {
        if (startNode == null) return;

        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, 100f, constructLayer))
        {
            ConstructController endNode = hit.collider.GetComponent<ConstructController>();
            if (endNode != null && endNode != startNode)
            {
                // Send 50% on a simple click, could add more logic for percentages
                startNode.SendUnits(endNode, 0.5f);
            }
        }
        
        startNode.SetSelected(false); // Deselect visual
        startNode = null;
    }
}