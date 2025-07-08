using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class UnitController : MonoBehaviour
{
    public FactionData owner { get; private set; }
    private ConstructController target;
    private ConstructController spawnConstruct;
    private NavMeshAgent navMeshAgent;
    private bool isJumping = false;

    public float moveSpeed = 15f;

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
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(true);
            navMeshAgent.enabled = false;
            moveSpeed *= 2f;
            StartCoroutine(HelicopterFlightRoutine());

            foreach (MeshRenderer renderer in transform.GetChild(1).GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material.color = owner.factionColor;
            }
        }
        else
        {
            navMeshAgent.enabled = true;
            navMeshAgent.SetDestination(target.transform.position);

            foreach (MeshRenderer renderer in transform.GetChild(0).GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material.color = owner.factionColor;
            }
        }
    }
    void Update()
    {
        if (target == null || isHelicopter) return;

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

        if (!isJumping && !navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.2f)
        {
            ArriveAtTarget();
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

        GetComponent<MeshRenderer>().material.color = owner.factionColor;
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