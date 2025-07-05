using UnityEngine;

public class ArrowTester : MonoBehaviour
{
    public Transform startNode;
    public Transform endNode;
    public NavMeshArrowStreamController arrowStreamPrefab; // The prefab we created

    private NavMeshArrowStreamController arrowStreamInstance;

    void Start()
    {
        // Create an instance of the arrow stream from the prefab
        arrowStreamInstance = Instantiate(arrowStreamPrefab);
    }

    void Update()
    {
        if (startNode != null && endNode != null && arrowStreamInstance != null)
        {
            // Every frame, tell the stream to update its path
            arrowStreamInstance.SetPoints(startNode.position, endNode.position);
        }
    }
}