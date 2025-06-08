using UnityEngine;

public class faceCamera : MonoBehaviour
{
    private Camera _camera;

    void Start()
    {
        _camera = Camera.main;
    }
    
    void Update()
    {
        transform.LookAt(-_camera.transform.position, Vector3.up);
    }
}
