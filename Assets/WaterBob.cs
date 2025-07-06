using UnityEngine;

public class WaterBob : MonoBehaviour
{
    [Header("Bobbing Settings")]
    [Tooltip("The height of the bobbing motion. This is the distance it will move up and down from its starting point.")]
    public float amplitude = 0.1f;

    [Tooltip("The speed of the bobbing motion.")]
    public float speed = 0.5f;

    // The initial position of the GameObject, stored when the game starts.
    private Vector3 startPosition;

    void Start()
    {
        // Record the starting position of the GameObject.
        // All bobbing calculations will be relative to this point.
        startPosition = transform.position;
    }

    void Update()
    {
        // Calculate the vertical displacement using a sine wave.
        // Time.time ensures the motion is continuous and smooth.
        // 'speed' controls how fast the wave oscillates.
        // 'amplitude' controls the height of the wave.
        float displacement = Mathf.Sin(Time.time * speed) * amplitude;

        // Create the new position by adding the displacement to the starting Y position.
        // We use the original X and Z to prevent any unwanted sideways movement.
        Vector3 newPosition = new Vector3(
            transform.position.x,
            startPosition.y + displacement,
            transform.position.z
        );

        // Apply the new position to the GameObject's transform.
        transform.position = newPosition;
    }
}