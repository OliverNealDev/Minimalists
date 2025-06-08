using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class NodeUIController : MonoBehaviour
{
    public Transform targetNode;
    public float screenOffsetY = 30f;

    private Camera mainCamera;
    private Vector3 worldOffsetFromPivot;

    void Start()
    {
        mainCamera = Camera.main;

        if (targetNode == null)
        {
            Debug.LogError("NodeUIController requires a Target Node to be assigned.", this);
            this.enabled = false;
            return;
        }

        CalculateWorldOffset();
    }

    void LateUpdate()
    {
        if (targetNode == null) return;

        Vector3 worldPosition = targetNode.TransformPoint(worldOffsetFromPivot);
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        screenPosition.y += screenOffsetY;
        
        transform.position = screenPosition;
    }

    private void CalculateWorldOffset()
    {
        MeshFilter meshFilter = targetNode.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            worldOffsetFromPivot = meshFilter.mesh.bounds.center + new Vector3(0, meshFilter.mesh.bounds.extents.y, 0);
        }
        else
        {
            Collider col = targetNode.GetComponent<Collider>();
            if (col != null)
            {
                worldOffsetFromPivot = col.bounds.center - targetNode.position + new Vector3(0, col.bounds.extents.y, 0);
            }
            else
            {
                worldOffsetFromPivot = Vector3.up;
            }
        }
    }
}