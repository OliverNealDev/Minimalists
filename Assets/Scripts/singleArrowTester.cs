using UnityEngine;

public class singleArrowTester : MonoBehaviour
{
    public Transform node1;
    public Transform node2;
    public ProceduralArrow arrowInstance;

    void Update()
    {
        // If all objects are assigned, draw the arrow between them.
        if (node1 != null && node2 != null && arrowInstance != null)
        {
            arrowInstance.SetPoints(node1.position, node2.position);
        }
    }
}