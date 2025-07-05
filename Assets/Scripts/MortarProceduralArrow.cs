using UnityEngine;

[AddComponentMenu("Procedural/Mortar Procedural Arrow")]
public class MortarProceduralArrow : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;

    [Header("Arrow Components")]
    public ProceduralArrow upArrow;
    public ProceduralArrow downArrow;

    [Header("Path Configuration")]
    public float verticalHeight = 8.0f;
    public float downwardArch = 4.0f;

    private Material upArrowMaterial;
    private Material downArrowMaterial;

    void Awake()
    {
        if (!ValidateAndSetup())
        {
            enabled = false;
        }
    }

    void OnDestroy()
    {
        if (upArrowMaterial != null) Destroy(upArrowMaterial);
        if (downArrowMaterial != null) Destroy(downArrowMaterial);
    }

    void Update()
    {
        if (target != null)
        {
            DrawArrows();
        }
        else
        {
            HideArrows();
        }
    }

    private void DrawArrows()
    {
        Vector3 startPoint = transform.position;
        Vector3 endPoint = target.position;
        Vector3 apexPoint = new Vector3(startPoint.x, startPoint.y + verticalHeight, startPoint.z);

        upArrow.archHeight = 0;
        downArrow.archHeight = downwardArch;

        upArrow.SetPoints(startPoint, apexPoint);
        downArrow.SetPoints(apexPoint, endPoint);
    }

    public void HideArrows()
    {
        if (upArrow != null)
        {
            upArrow.Hide();
        }
        if (downArrow != null)
        {
            downArrow.Hide();
        }
    }

    private bool ValidateAndSetup()
    {
        if (upArrow == null || downArrow == null)
        {
            Debug.LogError("Both 'upArrow' and 'downArrow' must be assigned.", this);
            return false;
        }

        MeshRenderer upRenderer = upArrow.GetComponent<MeshRenderer>();
        MeshRenderer downRenderer = downArrow.GetComponent<MeshRenderer>();

        if (upRenderer == null || downRenderer == null)
        {
            Debug.LogError("The assigned arrow objects must have a MeshRenderer component.", this);
            return false;
        }

        upArrowMaterial = new Material(upRenderer.sharedMaterial);
        downArrowMaterial = new Material(downRenderer.sharedMaterial);

        upArrowMaterial.color = Color.green;
        downArrowMaterial.color = Color.red;

        upRenderer.material = upArrowMaterial;
        downRenderer.material = downArrowMaterial;

        return true;
    }
}
