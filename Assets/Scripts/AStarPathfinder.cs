using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class AStarPathfinder
{
    private class PathNode
    {
        public Vector3 Position;
        public float GCost;
        public float HCost;
        public float FCost => GCost + HCost;
        public PathNode Parent;

        public PathNode(Vector3 position)
        {
            Position = position;
        }
    }

    public static List<Vector3> FindPath(Vector3 startPosition, Vector3 endPosition, IEnumerable<Transform> obstacles, float agentRadius)
    {
        var allObstacles = obstacles.ToList();
        
        PathNode startNode = new PathNode(startPosition);
        PathNode endNode = new PathNode(endPosition);

        List<PathNode> openSet = new List<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            PathNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (HasClearLineOfSight(currentNode.Position, endNode.Position, allObstacles, agentRadius))
            {
                endNode.Parent = currentNode;
                return RetracePath(startNode, endNode);
            }

            foreach (var neighborNode in GetNeighbors(currentNode, allObstacles, agentRadius))
            {
                if (closedSet.Any(n => n.Position == neighborNode.Position))
                {
                    continue;
                }

                float newMovementCostToNeighbor = currentNode.GCost + Vector3.Distance(currentNode.Position, neighborNode.Position);
                
                var existingOpenNode = openSet.FirstOrDefault(n => n.Position == neighborNode.Position);
                if (existingOpenNode == null || newMovementCostToNeighbor < existingOpenNode.GCost)
                {
                    neighborNode.GCost = newMovementCostToNeighbor;
                    neighborNode.HCost = Vector3.Distance(neighborNode.Position, endNode.Position);
                    neighborNode.Parent = currentNode;

                    if (existingOpenNode == null)
                    {
                        openSet.Add(neighborNode);
                    }
                }
            }
        }

        return new List<Vector3>();
    }

    private static List<PathNode> GetNeighbors(PathNode node, List<Transform> obstacles, float agentRadius)
    {
        List<PathNode> neighbors = new List<PathNode>();

        foreach (var obstacle in obstacles)
        {
            Bounds bounds = GetObjectBounds(obstacle);

            Vector3[] corners = new Vector3[8];
            corners[0] = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            corners[1] = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z);
            corners[2] = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z);
            corners[3] = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
            corners[4] = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z);
            corners[5] = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z);
            corners[6] = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z);
            corners[7] = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
            
            foreach (var corner in corners)
            {
                Vector3 directionFromCenter = (corner - bounds.center).normalized;
                Vector3 waypoint = corner + directionFromCenter * agentRadius;

                if (HasClearLineOfSight(node.Position, waypoint, obstacles, agentRadius))
                {
                    neighbors.Add(new PathNode(waypoint));
                }
            }
        }
        return neighbors;
    }

    private static bool HasClearLineOfSight(Vector3 start, Vector3 end, List<Transform> obstacles, float agentRadius)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        foreach (var obs in obstacles)
        {
            if (obs.position == start || obs.position == end) continue;

            if (Physics.SphereCast(start, agentRadius, direction, out RaycastHit hit, distance))
            {
                if (obstacles.Contains(hit.transform))
                {
                    return false;
                }
            }
        }
        return true;
    }
    
    private static Bounds GetObjectBounds(Transform obj)
    {
        if (obj.TryGetComponent<Collider>(out var col))
        {
            return col.bounds;
        }
        if (obj.TryGetComponent<MeshFilter>(out var mf))
        {
            return mf.mesh.bounds;
        }
        return new Bounds(obj.position, Vector3.one * 0.1f);
    }

    private static List<Vector3> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<Vector3> path = new List<Vector3>();
        PathNode currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }
        path.Add(startNode.Position);
        path.Reverse();
        return path;
    }
}