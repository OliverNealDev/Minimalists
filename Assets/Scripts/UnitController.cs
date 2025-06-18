using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class UnitController : MonoBehaviour
{
    public FactionData owner {get; private set;}
    private ConstructController target;
    private ConstructController spawnConstruct;
    private NavMeshAgent navMeshAgent;
    private bool isUsingNavMesh = false;

    public float moveSpeed = 1f;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }
    
    void Start()
    {
        GameManager.Instance.registerUnit(this);
        
        isUsingNavMesh = true;
        navMeshAgent.enabled = true;
        navMeshAgent.SetDestination(target.transform.position);
        
    }

    void Update()
    {
        if (target == null) return;
        
        /*if (!isUsingNavMesh)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, moveSpeed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, 0.68125f, transform.position.z);
        }*/

        if (Vector3.Distance(transform.position, target.transform.position) < 0.2f)
        {
            // The method ReceiveUnit(owner) needs to be added to your ConstructController script.
            GameManager.Instance.unregisterUnit(this);
            target.ReceiveUnit(owner); 
            Destroy(gameObject);
        }
    }
    
    public void Initialize(FactionData setOwner, ConstructController setSpawnConstruct, ConstructController setTarget)
    {
        owner = setOwner;
        spawnConstruct = setSpawnConstruct;
        target = setTarget;
        
        GetComponent<MeshRenderer>().material.color = owner.factionColor;
        navMeshAgent.enabled = false;

        //StartCoroutine(ProximityCheckRoutine());
    }

    public void OnShot()
    {
        GameManager.Instance.unregisterUnit(this);
        Destroy(gameObject);
    }

    private IEnumerator ProximityCheckRoutine()
    {
        while (true)
        {
            bool closeToTarget = Vector3.Distance(transform.position, target.transform.position) < 1f;
            bool closeToSpawn = Vector3.Distance(transform.position, spawnConstruct.transform.position) < 1f;
            
            if (!closeToTarget && !isUsingNavMesh)
            {
                isUsingNavMesh = true;
                navMeshAgent.enabled = true;
                navMeshAgent.SetDestination(target.transform.position);
            }
            else if (closeToTarget && isUsingNavMesh)
            {
                isUsingNavMesh = false;
                navMeshAgent.enabled = false;
            }
            
            yield return new WaitForSeconds(1f / 20f);
        }
    }
}