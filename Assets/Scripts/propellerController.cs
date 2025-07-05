using UnityEngine;

public class propellerController : MonoBehaviour
{
    public float propellerSpeed = 720f;
    void Update()
    {
        transform.Rotate(Vector3.up, propellerSpeed * Time.deltaTime, Space.Self);
    }
}
