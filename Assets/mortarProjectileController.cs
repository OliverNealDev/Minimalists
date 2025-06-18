using UnityEngine;

public class mortarProjectileController : MonoBehaviour
{
    public float mortarProjectileSpeed = 10;
    public float mortarProjectileWaitTime = 2;
    
    public ConstructController targetNode;
    public MortarData mortarData;
    
    private enum ProjectileState
    {
        Rising,
        Waiting,
        Falling
    }
    private ProjectileState currentState = ProjectileState.Rising;
    
    private float timeSinceSpawned;
    private float timeWaiting;
    
    void Start()
    {
        
    }
    
    void Update()
    {
        timeSinceSpawned += Time.deltaTime;

        if (currentState == ProjectileState.Rising)
        {
            transform.position += new Vector3(0, mortarProjectileSpeed * Time.deltaTime, 0);
            if (timeSinceSpawned > 2)
            {
                currentState = ProjectileState.Waiting;
            }
        }
        else if (currentState == ProjectileState.Waiting)
        {
            timeWaiting += Time.deltaTime;
            if (timeWaiting > mortarProjectileWaitTime)
            {
                currentState = ProjectileState.Falling;
                timeWaiting = 0;
                transform.position = new Vector3(targetNode.transform.position.x, targetNode.transform.position.y + 20, targetNode.transform.position.z);
            }
        }
        else if (currentState == ProjectileState.Falling)
        {
            transform.position -= new Vector3(0, mortarProjectileSpeed * Time.deltaTime, 0);
            if (transform.position.y <= 0.68125f)
            {
                targetNode.ReceiveMortarProjectile(mortarData);
                Destroy(gameObject);
            }
        }
    }
}
