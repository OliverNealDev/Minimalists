using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

[RequireComponent(typeof(NavMeshAgent))]
public class UnitController : MonoBehaviour
{
    public FactionData owner { get; private set; }
    private ConstructController target;
    private ConstructController spawnConstruct;
    private NavMeshAgent navMeshAgent;
    private bool isJumping = false;

    public float moveSpeed = 15f;
    public Vector3 GroundedScale = new Vector3(4.309667f, 4.309667f, 4.309667f);
    public float GroundedYAxisOffset = -0.2757f;
    public float SmallGroundedYAxisOffset = -0.824f;
    public Vector3 AirScale = new Vector3(3.88f, 3.88f, 3.88f);

    private Vector3 StartingScale;
    private bool isScalingToStarting = false;
    private bool hasFinishedInitialScale = false; // Flag to prevent premature scaling

    [Header("Ground Unit Jump Settings")]
    public float jumpHeight = 5f;
    public float jumpDuration = 0.8f;
    public float jumpCheckDistance = 4f;
    public float jumpClearance = 3f;

    public bool isHelicopter = false;
    [Header("Helicopter Flight Settings")]
    public float cruiseHeight = 15f;
    public float transitionDistance = 20f;
    public float transitionDuration = 2.5f;
    public float landingApproachDistance = 25f;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }
    void Start()
    {
        GameManager.Instance.registerUnit(this);
        if (isHelicopter)
        {
            // Set initial scale for the helicopter model
            transform.GetChild(1).localScale = AirScale / 10f;
            StartingScale = AirScale / 10f;
        }
        else
        {
            // Set initial scale and Y-position for the ground unit model
            Transform groundModel = transform.GetChild(0);
            groundModel.localScale = GroundedScale / 10f;
            StartingScale = GroundedScale / 10f;
            Vector3 startPos = groundModel.localPosition;
            startPos.y = SmallGroundedYAxisOffset;
            groundModel.localPosition = startPos;
        }

        // Start the initial scaling animation
        StartCoroutine(ScaleToNormal());
        
        if (isHelicopter)
        {
            // Configure for helicopter
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(true);
            navMeshAgent.enabled = false;
            moveSpeed *= 2f;
            StartCoroutine(HelicopterFlightRoutine());

            // Apply faction color to the helicopter model
            foreach (MeshRenderer renderer in transform.GetChild(1).GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material.color = owner.factionColor;
            }
        }
        else
        {
            // Configure for ground unit
            navMeshAgent.enabled = true;
            if (target != null)
            {
                navMeshAgent.SetDestination(target.transform.position);
            }

            // Apply faction color to the ground unit model
            foreach (MeshRenderer renderer in transform.GetChild(0).GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material.color = owner.factionColor;
            }
        }
    }
    void Update()
    {
        // Early exit if there's no target or if it's a helicopter (which has its own movement logic)
        if (target == null || isHelicopter) return;

        // Logic for ground unit jumping over obstacles
        if (!isJumping && navMeshAgent.hasPath && navMeshAgent.velocity.sqrMagnitude > 0.1f)
        {
            foreach (var construct in GameManager.Instance.allConstructs)
            {
                if (construct == spawnConstruct || construct == target) continue;

                float distanceToConstruct = Vector3.Distance(transform.position, construct.transform.position);
                if (distanceToConstruct < jumpCheckDistance)
                {
                    Vector3 directionToConstruct = (construct.transform.position - transform.position).normalized;
                    directionToConstruct.y = 0;
                    Vector3 moveDirection = navMeshAgent.velocity.normalized;
                    moveDirection.y = 0;

                    if (Vector3.Dot(moveDirection, directionToConstruct) > 0.7f)
                    {
                        StartCoroutine(JumpOverObstacle(construct.transform));
                        break;
                    }
                }
            }
        }

        // Check if the unit has arrived at its destination
        if (!isJumping && !navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.2f)
        {
            ArriveAtTarget();
            return;
        }
        
        // Start scaling down when approaching the target, ONLY if the initial scale-up is finished.
        if (hasFinishedInitialScale && navMeshAgent.remainingDistance < 0.75f && !isScalingToStarting)
        {
            isScalingToStarting = true;
            StartCoroutine(ScaleToStarting());
        }
    }

    private IEnumerator ScaleToNormal()
    {
        float duration = 0.75f;
        float timer = 0f;

        Transform childTransform = isHelicopter ? transform.GetChild(1) : transform.GetChild(0);
        Vector3 startScale = childTransform.localScale;
        Vector3 endScale = isHelicopter ? AirScale : GroundedScale;

        Vector3 startPos = childTransform.localPosition;
        // The end Y position is the standard grounded offset for ground units, or its current Y for helicopters
        float endYPos = isHelicopter ? startPos.y : GroundedYAxisOffset;
        Vector3 endPos = new Vector3(startPos.x, endYPos, startPos.z);


        while (timer < duration)
        {
            float t = timer / duration;
            
            // Lerp scale and position simultaneously
            childTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            if (!isHelicopter)
            {
                childTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are set precisely
        childTransform.localScale = endScale;
        if (!isHelicopter)
        {
            childTransform.localPosition = endPos;
        }
        
        // Signal that the initial scaling is complete
        hasFinishedInitialScale = true;
        
        transform.GetChild(0).GetComponent<WaterBob>().enabled = true;
    }
    
    private IEnumerator ScaleToStarting()
    {
        float duration = 0.75f;
        float timer = 0f;

        Transform childTransform = isHelicopter ? transform.GetChild(1) : transform.GetChild(0);
        Vector3 startScale = childTransform.localScale;
        // Note: StartingScale was set in the Start() method
        Vector3 endScale = StartingScale;

        Vector3 startPos = childTransform.localPosition;
        // The end Y position is the small offset for ground units, or its current Y for helicopters
        float endYPos = isHelicopter ? startPos.y : SmallGroundedYAxisOffset;
        Vector3 endPos = new Vector3(startPos.x, endYPos, startPos.z);

        while (timer < duration)
        {
            float t = timer / duration;
            
            // Lerp scale and position simultaneously
            childTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            if (!isHelicopter)
            {
                childTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are set precisely
        childTransform.localScale = endScale;
        if (!isHelicopter)
        {
            childTransform.localPosition = endPos;
        }
    }
    
    private IEnumerator JumpOverObstacle(Transform obstacle)
    {
        isJumping = true;
        navMeshAgent.isStopped = true;

        Vector3 startPos = transform.position;
        Vector3 direction = (obstacle.position - startPos).normalized;
        direction.y = 0;
        Vector3 endPos = obstacle.position + direction * jumpClearance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(endPos, out hit, 5.0f, NavMesh.AllAreas))
        {
            endPos = hit.position;
        }

        Vector3 controlPoint = startPos + (endPos - startPos) / 2 + Vector3.up * jumpHeight;

        float timer = 0f;
        while (timer < jumpDuration)
        {
            float t = timer / jumpDuration;
            transform.position = CalculateQuadraticBezierPoint(t, startPos, controlPoint, endPos);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        navMeshAgent.isStopped = false;
        if(target != null) navMeshAgent.SetDestination(target.transform.position);
        isJumping = false;
    }

    public void Initialize(FactionData setOwner, ConstructController setSpawnConstruct, ConstructController setTarget, bool Helicopter)
    {
        owner = setOwner;
        spawnConstruct = setSpawnConstruct;
        target = setTarget;
        isHelicopter = Helicopter;

        // This line might be redundant if you are coloring the child models directly,
        // but it depends on your prefab structure.
        // GetComponent<MeshRenderer>().material.color = owner.factionColor;
        navMeshAgent.enabled = false;
    }
    public void OnShot()
    {
        GameManager.Instance.unregisterUnit(this);
        Destroy(gameObject);
    }
    private void ArriveAtTarget()
    {
        if (target != null)
        {
            target.ReceiveUnit(owner);
        }
        GameManager.Instance.unregisterUnit(this);
        Destroy(gameObject);
    }

    private IEnumerator HelicopterFlightRoutine()
    {
        if (target == null) { Destroy(gameObject); yield break; }
        Vector3 startPos = transform.position;

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

            Vector3 cruiseTargetPos = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z);
            transform.LookAt(cruiseTargetPos);

            transform.position = newPos;
            timer += Time.deltaTime;
            yield return null;
        }

        while (target != null && Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(target.transform.position.x, 0, target.transform.position.z)) > landingApproachDistance)
        {
            Vector3 cruiseTargetPos = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, cruiseTargetPos, moveSpeed * Time.deltaTime);
            transform.LookAt(cruiseTargetPos);
            yield return null;
        }

        if (target == null) { Destroy(gameObject); yield break; }

        Vector3 p0_landing = transform.position;
        timer = 0f;
        
        while(timer < transitionDuration)
        {
            if (target == null) { Destroy(gameObject); yield break; }

            Vector3 p2_landing = target.transform.position;
            Vector3 p1_landing = new Vector3(p2_landing.x, p0_landing.y, p2_landing.z);

            float t = timer / transitionDuration;
            Vector3 newPos = CalculateQuadraticBezierPoint(t, p0_landing, p1_landing, p2_landing);

            if (t > 0.5f && !isScalingToStarting)
            {
                isScalingToStarting = true;
                StartCoroutine(ScaleToStarting());
            }

            if ((newPos - transform.position).sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(newPos - transform.position);

            transform.position = newPos;
            timer += Time.deltaTime;
            yield return null;
        }

        ArriveAtTarget();
    }
    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;
        return p;
    }
}
