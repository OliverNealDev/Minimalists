using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ParticleSystem))]
public class NavMeshArrowStreamController : MonoBehaviour
{
    [Header("Tuning")]
    [Tooltip("Adjust this to correct the arrow mesh's base orientation. If your arrow model points 'up' by default, try an X value of -90.")]
    public Vector3 rotationOffset = new Vector3(-90, 0, 0);

    private ParticleSystem ps;
    private NavMeshPath path;
    private ParticleSystem.Particle[] particles;
    private float totalPathLength;
    private Quaternion offsetQuaternion;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        path = new NavMeshPath();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];

        // Convert the offset to a Quaternion once at the start for efficiency.
        offsetQuaternion = Quaternion.Euler(rotationOffset);
    }

    public void SetPoints(Vector3 startPoint, Vector3 endPoint)
    {
        transform.position = startPoint;

        if (NavMesh.CalculatePath(startPoint, endPoint, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            totalPathLength = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                totalPathLength += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
        }
        else
        {
            path.ClearCorners();
            totalPathLength = 0f;
            Debug.LogWarning($"<color=orange>Path Calculation Failed.</color> Status: {path.status}");
        }
    }

    void LateUpdate()
    {
        if (path.corners.Length < 2)
        {
            return;
        }

        int numParticlesAlive = ps.GetParticles(particles);

        for (int i = 0; i < numParticlesAlive; i++)
        {
            float progress = 1f - (particles[i].remainingLifetime / particles[i].startLifetime);
            float distanceAlongPath = progress * totalPathLength;

            Vector3 newPosition = path.corners[0];
            Vector3 segmentDirection = transform.forward;

            for (int j = 0; j < path.corners.Length - 1; j++)
            {
                float segmentLength = Vector3.Distance(path.corners[j], path.corners[j + 1]);

                if (segmentLength <= 0.001f)
                    continue;

                if (distanceAlongPath <= segmentLength)
                {
                    float progressOnSegment = distanceAlongPath / segmentLength;
                    newPosition = Vector3.Lerp(path.corners[j], path.corners[j + 1], progressOnSegment);
                    segmentDirection = (path.corners[j + 1] - path.corners[j]).normalized;
                    break;
                }
                else
                {
                    distanceAlongPath -= segmentLength;
                }
            }
            
            particles[i].position = newPosition;
            
            if (segmentDirection != Vector3.zero)
            {
                Vector3 groundNormal = Vector3.up;
                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 0.1f, NavMesh.AllAreas))
                {
                    groundNormal = hit.normal;
                }
                
                Quaternion pathRotation = Quaternion.LookRotation(segmentDirection, groundNormal);
                Quaternion finalRotation = pathRotation * offsetQuaternion;

                particles[i].rotation3D = finalRotation.eulerAngles;
            }
        }

        ps.SetParticles(particles, numParticlesAlive);
    }
}