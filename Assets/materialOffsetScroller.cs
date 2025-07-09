using UnityEngine;

public class materialOffsetScroller : MonoBehaviour
{
    private Material material;
    [SerializeField] private float scrollXSpeed = 0.16f; // Speed of the horizontal scroll
    [SerializeField] private float scrollYSpeed = 0.09f; // Speed of the vertical scroll, set to 0 for horizontal scrolling only
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        material = GetComponent<Renderer>().material;
        if (material == null)
        {
            Debug.LogError("No material found on the GameObject. Please assign a material with a texture to scroll.", this);
            this.enabled = false;
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (material != null)
        {
            material.mainTextureOffset += new Vector2(scrollXSpeed * Time.deltaTime, scrollYSpeed * Time.deltaTime);
        }
        else
        {
            Debug.LogWarning("Material is not assigned or has been destroyed.", this);
        }
    }
}
