using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class CustomPathfinder : MonoBehaviour
{
    public Transform startNode;
    public Transform endNode;
    public float agentRadius = 0.5f;

    private List<Vector3> _path;

    private void Update()
    {
        if (startNode != null && endNode != null)
        {
            // Corrected Line: Accessing 'AllConstructs' as a property (no parentheses).
            var allObstacles = GameManager.Instance.allConstructs
                .Select(c => c.transform)
                .Where(t => t != startNode && t != endNode)
                .ToList();

            _path = AStarPathfinder.FindPath(startNode.position, endNode.position, allObstacles, agentRadius);
        }
    }

    private void OnDrawGizmos()
    {
        if (startNode != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(startNode.position, 0.2f);
        }

        if (endNode != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(endNode.position, 0.2f);
        }

        if (_path != null && _path.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _path.Count - 1; i++)
            {
                Gizmos.DrawLine(_path[i], _path[i + 1]);
            }
        }
    }
}