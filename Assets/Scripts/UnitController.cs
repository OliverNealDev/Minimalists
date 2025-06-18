using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class UnitController : MonoBehaviour
{
    public FactionData owner { get; private set; }
    private ConstructController target;
    private ConstructController spawnConstruct;
    private NavMeshAgent navMeshAgent;

    public float moveSpeed = 15f;

    // --- New Helicopter Variables ---
    public bool isHelicopter = false;
    [Header("Helicopter Flight Settings")]
    public float cruiseHeight = 15f;           // The altitude for the main flight portion.
    public float transitionDistance = 20f;     // The horizontal distance covered during the smooth takeoff.
    public float transitionDuration = 2.5f;    // Time in seconds for the smooth takeoff and landing curves.
    public float landingApproachDistance = 25f;// The distance from the target to begin the smooth landing.
    // ------------------------------

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        GameManager.Instance.registerUnit(this);

        if (isHelicopter)
        {
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(true);
            navMeshAgent.enabled = false;
            moveSpeed *= 1.5f; // Increase speed for helicopters
            StartCoroutine(HelicopterFlightRoutine());
        }
        else
        {
            navMeshAgent.enabled = true;
            navMeshAgent.SetDestination(target.transform.position);
        }
    }

    // Update is now only for ground units
    void Update()
    {
        if (target == null || isHelicopter) return;

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.2f)
        {
            ArriveAtTarget();
        }
    }

    public void Initialize(FactionData setOwner, ConstructController setSpawnConstruct, ConstructController setTarget, bool Helicopter)
    {
        owner = setOwner;
        spawnConstruct = setSpawnConstruct;
        target = setTarget;
        isHelicopter = Helicopter;

        GetComponent<MeshRenderer>().material.color = owner.factionColor;
        navMeshAgent.enabled = false;
    }

    public void OnShot()
    {
        GameManager.Instance.unregisterUnit(this);
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Controls the helicopter through a 3-phase flight path with smooth, curved transitions.
    /// </summary>
    private IEnumerator HelicopterFlightRoutine()
    {
        // --- PRE-FLIGHT SETUP ---
        if (target == null) { Destroy(gameObject); yield break; }
        Vector3 startPos = transform.position;

        // --- PHASE 1: SMOOTH TAKEOFF TRANSITION ---
        // "smoothly goes up and forward"
        // This creates a curve that starts moving vertically and ends moving horizontally.
        Vector3 horizontalDirection = target.transform.position - startPos;
        horizontalDirection.y = 0;
        horizontalDirection.Normalize();

        Vector3 p0_takeoff = startPos;
        Vector3 p2_takeoff = startPos + (horizontalDirection * transitionDistance);
        p2_takeoff.y = startPos.y + cruiseHeight;
        Vector3 p1_takeoff = new Vector3(p0_takeoff.x, p2_takeoff.y, p0_takeoff.z);

        float timer = 0f;
        while (timer < transitionDuration)
        {
            float t = timer / transitionDuration;
            Vector3 newPos = CalculateQuadraticBezierPoint(t, p0_takeoff, p1_takeoff, p2_takeoff);
            
            if ((newPos - transform.position).sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(newPos - transform.position);
            
            transform.position = newPos;
            timer += Time.deltaTime;
            yield return null;
        }

        // --- PHASE 2: CRUISE ---
        // "cruises"
        // Flies in a straight line at cruise altitude.
        while (target != null && Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(target.transform.position.x, 0, target.transform.position.z)) > landingApproachDistance)
        {
            Vector3 cruiseTargetPos = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, cruiseTargetPos, moveSpeed * Time.deltaTime);
            transform.LookAt(cruiseTargetPos);
            yield return null;
        }
        
        if (target == null) { Destroy(gameObject); yield break; }

        // --- PHASE 3: SMOOTH LANDING TRANSITION ---
        // "smoothly goes down and slows down forward movement"
        // This curve starts moving horizontally and ends moving mostly vertically, slowing down as it approaches the target.
        Vector3 p0_landing = transform.position;
        timer = 0f;

        while(timer < transitionDuration)
        {
            if (target == null) { Destroy(gameObject); yield break; }
            
            // Continuously update the end point and control point to follow a moving target
            Vector3 p2_landing = target.transform.position;
            Vector3 p1_landing = new Vector3(p2_landing.x, p0_landing.y, p2_landing.z);

            float t = timer / transitionDuration;
            Vector3 newPos = CalculateQuadraticBezierPoint(t, p0_landing, p1_landing, p2_landing);

            if ((newPos - transform.position).sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(newPos - transform.position);

            transform.position = newPos;
            timer += Time.deltaTime;
            yield return null;
        }

        // --- Final Arrival ---
        ArriveAtTarget();
    }

    /// <summary>
    /// Calculates a point on a quadratic Bezier curve.
    /// </summary>
    /// <param name="t">Time (0 to 1)</param>
    /// <param name="p0">Start point</param>
    /// <param name="p1">Control point</param>
    /// <param name="p2">End point</param>
    /// <returns></returns>
    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0; // (1-t)^2 * P0
        p += 2 * u * t * p1; // 2 * (1-t) * t * P1
        p += tt * p2;        // t^2 * P2
        return p;
    }
    
    /// <summary>
    /// A centralized method for when a unit reaches its destination.
    /// </summary>
    private void ArriveAtTarget()
    {
        if (target != null)
        {
            target.ReceiveUnit(owner);
        }
        GameManager.Instance.unregisterUnit(this);
        Destroy(gameObject);
    }
}