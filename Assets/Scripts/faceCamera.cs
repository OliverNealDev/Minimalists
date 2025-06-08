using UnityEngine;

public class faceCamera : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    // Use LateUpdate to ensure the camera has finished its movement for the frame.
    void LateUpdate()
    {
        // This makes the object's orientation perfectly match the camera's.
        // It is the most robust way to make UI or sprites face the camera.
        transform.rotation = mainCamera.transform.rotation;
    }
}