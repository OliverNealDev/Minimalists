using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralArrow : MonoBehaviour
{
    [Header("Arrow Shape")]
    [Tooltip("The width of the arrow's body.")]
    public float bodyWidth = 0.5f;

    [Tooltip("The width of the arrowhead.")]
    public float headWidth = 1.0f;

    [Tooltip("The length of the arrowhead.")]
    public float headLength = 0.8f;

    [Header("Arching")]
    [Tooltip("The height of the arch from the baseline.")]
    public float archHeight = 2.0f;

    [Tooltip("The number of segments to use for the curve. More segments create a smoother arch.")]
    [Range(3, 50)]
    public int resolution = 15;

    [Header("Position Offsets")]
    [Tooltip("An offset applied to the start point of the arrow.")]
    public Vector3 startOffset = Vector3.zero;

    [Tooltip("An offset applied to the end point of the arrow.")]
    public Vector3 endOffset = Vector3.zero;
    
    private Vector3 parentWorldOffset;

    private MeshFilter meshFilter;
    private Mesh mesh;
    private bool isInitialized = false;

    void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.name = "Procedural Arrow Mesh";
        meshFilter.mesh = mesh;
        isInitialized = true;
    }

    public void SetPoints(Vector3 startPoint, Vector3 endPoint)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        parentWorldOffset = transform.parent.position;
        parentWorldOffset.y -= 5;
        parentWorldOffset.z += 5;
        
        Vector3 actualStartPoint = startPoint + startOffset - parentWorldOffset;
        Vector3 actualEndPoint = endPoint + endOffset - parentWorldOffset;

        if (Vector3.Distance(actualStartPoint, actualEndPoint) < 0.1f)
        {
            mesh.Clear();
            return;
        }

        Vector3 controlPoint = (actualStartPoint + actualEndPoint) / 2f + Vector3.up * archHeight;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float totalCurveLength = 0;
        Vector3 previousPoint = actualStartPoint;

        for (int i = 1; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            Vector3 pointOnCurve = GetPointOnBezierCurve(actualStartPoint, controlPoint, actualEndPoint, t);
            totalCurveLength += Vector3.Distance(previousPoint, pointOnCurve);
            previousPoint = pointOnCurve;
        }

        if (totalCurveLength < headLength)
        {
            mesh.Clear();
            return;
        }

        float distanceAlongCurve = 0;
        previousPoint = actualStartPoint;

        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            Vector3 pointOnCurve = GetPointOnBezierCurve(actualStartPoint, controlPoint, actualEndPoint, t);
            
            if (i > 0)
            {
                distanceAlongCurve += Vector3.Distance(previousPoint, pointOnCurve);
            }
            previousPoint = pointOnCurve;

            if (totalCurveLength - distanceAlongCurve <= headLength)
            {
                break;
            }

            Vector3 direction = GetTangentOnBezierCurve(actualStartPoint, controlPoint, actualEndPoint, t).normalized;
            Vector3 side = Vector3.Cross(direction, Vector3.up).normalized;

            vertices.Add(pointOnCurve - side * bodyWidth / 2f);
            vertices.Add(pointOnCurve + side * bodyWidth / 2f);

            float vCoord = distanceAlongCurve / totalCurveLength;
            uvs.Add(new Vector2(0, vCoord));
            uvs.Add(new Vector2(1, vCoord));

            if (i > 0)
            {
                int baseIndex = vertices.Count - 4;
                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }
        }

        CreatePreciseArrowHead(vertices, triangles, uvs, actualStartPoint, controlPoint, actualEndPoint, totalCurveLength);

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
    
    // ---- THIS METHOD IS NOW UPDATED ----
    private void CreatePreciseArrowHead(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 p0, Vector3 p1, Vector3 p2, float totalLength)
    {
        // 1. Calculate the precise orientation and position of the head
        Vector3 headTip = p2;
        Vector3 endDirection = GetTangentOnBezierCurve(p0, p1, p2, 1f).normalized;
        Vector3 headBasePosition = headTip - endDirection * headLength;
        Vector3 side = Vector3.Cross(endDirection, Vector3.up).normalized;

        // 2. Define all the vertices for the head region
        Vector3 bodyJoinLeft = headBasePosition - side * bodyWidth / 2f;
        Vector3 bodyJoinRight = headBasePosition + side * bodyWidth / 2f;
        Vector3 headBaseLeft = headBasePosition - side * headWidth / 2f;
        Vector3 headBaseRight = headBasePosition + side * headWidth / 2f;
        
        // 3. Add these vertices to the list
        int lastBodyIndex = vertices.Count - 1;
        if (lastBodyIndex < 1) return; // Not enough body to attach a head to

        int bodyJoinIndex = vertices.Count;
        vertices.Add(bodyJoinLeft);   // 0
        vertices.Add(bodyJoinRight);  // 1

        int headBaseIndex = vertices.Count;
        vertices.Add(headBaseLeft);   // 2
        vertices.Add(headBaseRight);  // 3
        vertices.Add(headTip);        // 4

        // 4. Add UVs for the new vertices
        float bodyLengthUv = (totalLength - headLength) / totalLength;
        uvs.Add(new Vector2(0.5f - (bodyWidth / headWidth / 2f), bodyLengthUv)); // bodyJoinLeft
        uvs.Add(new Vector2(0.5f + (bodyWidth / headWidth / 2f), bodyLengthUv)); // bodyJoinRight
        uvs.Add(new Vector2(0, bodyLengthUv)); // headBaseLeft
        uvs.Add(new Vector2(1, bodyLengthUv)); // headBaseRight
        uvs.Add(new Vector2(0.5f, 1)); // headTip

        // 5. Connect the last body segment to the perfectly aligned "join" vertices
        triangles.Add(lastBodyIndex - 1);           // Previous body left
        triangles.Add(bodyJoinIndex);               // bodyJoinLeft
        triangles.Add(lastBodyIndex);               // Previous body right
        
        triangles.Add(lastBodyIndex);               // Previous body right
        triangles.Add(bodyJoinIndex);               // bodyJoinLeft
        triangles.Add(bodyJoinIndex + 1);           // bodyJoinRight

        // 6. Create the "shoulder" of the arrowhead (the flat back)
        triangles.Add(bodyJoinIndex);               // bodyJoinLeft
        triangles.Add(headBaseIndex);               // headBaseLeft
        triangles.Add(headBaseIndex + 1);           // headBaseRight

        triangles.Add(bodyJoinIndex);               // bodyJoinLeft
        triangles.Add(headBaseIndex + 1);           // headBaseRight
        triangles.Add(bodyJoinIndex + 1);           // bodyJoinRight
        
        // 7. Create the main arrowhead triangle
        triangles.Add(headBaseIndex);               // headBaseLeft
        triangles.Add(headBaseIndex + 2);           // headTip
        triangles.Add(headBaseIndex + 1);           // headBaseRight
    }

    public void Hide()
    {
        if (mesh != null)
        {
            mesh.Clear();
        }
    }

    private Vector3 GetPointOnBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * p0 + 2f * oneMinusT * t * p1 + t * t * p2;
    }

    private Vector3 GetTangentOnBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
    }
}