using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[ExecuteInEditMode] // Allows the script to run in the editor
public class NavMeshPathfinder : MonoBehaviour
{
    // Assign these in the inspector to define the path start and end
    public Transform startNode;
    public Transform endNode;
    public float searchRadius = 1.0f;

    // The calculated path will be stored here, now as Vector3 to keep height info
    private List<Vector3> _path;

    private void Update()
    {
        // Continuously calculate the path if the nodes are assigned
        if (startNode != null && endNode != null)
        {
            _path = FindPath(startNode.position, endNode.position, searchRadius);
        }
    }

    /// <summary>
    /// Draws gizmos in the scene view for visualization
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw spheres at the node positions
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

        // Draw the calculated path
        if (_path != null && _path.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _path.Count - 1; i++)
            {
                // Draw the line directly between the Vector3 points in the path
                Gizmos.DrawLine(_path[i], _path[i + 1]);
            }
        }
    }

    public static List<Vector3> FindPath(Vector3 startPosition, Vector3 endPosition, float searchRadius = 1.0f)
    {
        NavMeshPath path = new NavMeshPath();

        // Find the closest point on the NavMesh to the start and end positions.
        // startHit and endHit will contain the correct Y-value of the NavMesh surface.
        NavMesh.SamplePosition(startPosition, out NavMeshHit startHit, searchRadius, NavMesh.AllAreas);
        NavMesh.SamplePosition(endPosition, out NavMeshHit endHit, searchRadius, NavMesh.AllAreas);

        if (!startHit.hit || !endHit.hit)
        {
            // No need to log an error every frame, the gizmos will show the lack of a path
            return new List<Vector3>();
        }

        // Calculate the path using the valid NavMesh hit points
        if (NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                List<Vector3> corners = new List<Vector3>(path.corners);

                // Create the precise start and end points. We use the X and Z from the original requested position,
                // but the Y from the NavMesh hit point. This snaps the path's start and end vertically to the NavMesh.
                Vector3 finalStartPosition = new Vector3(startPosition.x, startHit.position.y, startPosition.z);
                Vector3 finalEndPosition = new Vector3(endPosition.x, endHit.position.y, endPosition.z);

                if (corners.Count > 0)
                {
                    corners[0] = finalStartPosition;
                }
                else
                {
                    corners.Add(finalStartPosition);
                }

                if (corners.Count > 1)
                {
                    corners[corners.Count - 1] = finalEndPosition;
                }
                else
                {
                     corners.Add(finalEndPosition);
                }

                return corners;
            }
        }

        return new List<Vector3>();
    }
}
