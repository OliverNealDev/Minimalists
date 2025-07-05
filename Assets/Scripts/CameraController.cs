using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float dragSpeed = 2f;
    public float moveSpeed = 15f; // Speed for WASD movement
    public LayerMask groundLayer;
    [Tooltip("How much faster the drag/WASD is when fully zoomed out.")]
    public float zoomMoveMultiplier = 4.0f;

    [Header("Rotation Settings")]
    public float rotationAngle = 45f;
    public float rotationLerpSpeed = 10f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float zoomLerpSpeed = 10f;
    public float minOrthographicSize = 2f;
    public float maxOrthographicSize = 20f;

    private Camera mainCamera;
    private float targetRotationY = 0f;
    private float currentRotationY = 0f;
    private float targetOrthographicSize = 0f;
    private Vector3 lastMousePosition;
    
    [Header("ToggleEnable")]
    public bool enableWASDMovement = true;
    public bool enableDragMovement = true;
    public bool enableRotation = true;
    public bool enableZoom = true;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        if (!mainCamera.orthographic)
        {
            Debug.LogWarning("CameraController is designed for an Orthographic camera but the attached camera is set to Perspective.", this);
        }
    }

    void Start()
    {
        currentRotationY = transform.eulerAngles.y;
        targetRotationY = currentRotationY;
        targetOrthographicSize = mainCamera.orthographicSize;
    }

    void Update()
    {
        HandleInput();
    }

    void LateUpdate()
    {
        if (enableDragMovement) HandleDragMovement();
        if (enableWASDMovement) HandleWASDMovement();
        if (enableZoom) UpdateZoom();
        if (enableRotation) UpdateRotation();
    }
    
    private void HandleInput()
    {
        if (enableRotation)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                targetRotationY += rotationAngle;
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                targetRotationY -= rotationAngle;
            }
        }

        if (enableZoom)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f)
            {
                targetOrthographicSize -= scrollInput * zoomSpeed;
                targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minOrthographicSize, maxOrthographicSize);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }
    }
    
    private void HandleWASDMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (horizontalInput == 0f && verticalInput == 0f)
        {
            return;
        }

        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 moveDirection = (right * horizontalInput + forward * verticalInput).normalized;

        float zoomRatio = (mainCamera.orthographicSize - minOrthographicSize) / (maxOrthographicSize - minOrthographicSize);
        float dynamicSpeed = Mathf.Lerp(moveSpeed, moveSpeed * zoomMoveMultiplier, zoomRatio);
        
        transform.position += moveDirection * dynamicSpeed * Time.deltaTime;
    }

    private void HandleDragMovement()
    {
        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

            float zoomRatio = (mainCamera.orthographicSize - minOrthographicSize) / (maxOrthographicSize - minOrthographicSize);
            float dynamicSpeed = Mathf.Lerp(1f, zoomMoveMultiplier, zoomRatio);

            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            Vector3 move = (right * -delta.x + forward * -delta.y) * dragSpeed * dynamicSpeed * Time.deltaTime;

            transform.position += move;

            lastMousePosition = Input.mousePosition;
        }
    }

    private void UpdateRotation()
    {
        if (Mathf.Abs(currentRotationY - targetRotationY) < 0.01f) return;

        currentRotationY = Mathf.LerpAngle(currentRotationY, targetRotationY, Time.deltaTime * rotationLerpSpeed);

        Vector3 pivotPoint = GetPivotPoint();
        
        Quaternion newRotation = Quaternion.Euler(transform.eulerAngles.x, currentRotationY, 0);
        float distance = Vector3.Distance(transform.position, pivotPoint);
        Vector3 newPosition = pivotPoint - (newRotation * Vector3.forward * distance);

        transform.rotation = newRotation;
        transform.position = newPosition;
    }
    
    private void UpdateZoom()
    {
        if (Mathf.Abs(mainCamera.orthographicSize - targetOrthographicSize) < 0.01f) return;

        Vector3 mouseWorldPosBeforeZoom = GetWorldPoint(Input.mousePosition);
        
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetOrthographicSize, Time.deltaTime * zoomLerpSpeed);

        Vector3 mouseWorldPosAfterZoom = GetWorldPoint(Input.mousePosition);
        
        Vector3 positionOffset = mouseWorldPosBeforeZoom - mouseWorldPosAfterZoom;
        transform.position += positionOffset;
    }
    
    private Vector3 GetPivotPoint()
    {
        return GetWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f));
    }

    private Vector3 GetWorldPoint(Vector3 screenPoint)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit, 300f, groundLayer))
        {
            return hit.point;
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        return transform.position + transform.forward * 10f;
    }
}
