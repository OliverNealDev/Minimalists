using System;
using UnityEngine;

public class turretProjectileController : MonoBehaviour
{
    public FactionData owner;
    public UnitController target;
    public float speed = 5f;

    void Update()
    {
        if (target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.gameObject.transform.position,
                speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target.transform.position) < 0.1f)
            {
                target.OnShot();
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
