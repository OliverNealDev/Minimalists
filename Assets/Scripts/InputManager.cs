using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    
    public LayerMask constructLayer;
    public float doubleClickThreshold = 0.25f;

    private Camera mainCamera;
    public ConstructController startNode;
    private ConstructController lastClickedNode;
    private float timeSinceLastClick;
    
    public bool IsSelecting => startNode != null;
    
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
            HandleClick();
        }

        if (Input.GetMouseButtonUp(0))
        {
            HandleSelectionEnd();
        }
    }

    private void HandleClick()
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, constructLayer))
        {
            ConstructController clickedNode = hit.collider.GetComponent<ConstructController>();
            if (clickedNode == null || clickedNode.Owner != GameManager.Instance.playerFaction)
            {
                return;
            }

            if (timeSinceLastClick < doubleClickThreshold && lastClickedNode == clickedNode)
            {
                clickedNode.AttemptUpgrade();
                Debug.Log("Double-click detected on node: " + clickedNode.name);
                ResetClickState();
            }
            else
            {
                Debug.Log(timeSinceLastClick);
                HandleSelectionStart(clickedNode);
            }
        }
    }

    private void HandleSelectionStart(ConstructController clickedNode)
    {
        timeSinceLastClick = 0;
        lastClickedNode = clickedNode;
        startNode = clickedNode;
        startNode.SetSelected(true);
    }

    private void HandleSelectionEnd()
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
    }

    private void ResetClickState()
    {
        startNode = null;
        timeSinceLastClick = 0;
    }
}
