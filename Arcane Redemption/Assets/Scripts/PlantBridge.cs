using UnityEngine;

public class PlantBridge : MonoBehaviour
{
    
    [SerializeField] private GameObject plantBridge;
    private Renderer bridgeRenderer;
    private Collider bridgeCollider;
    [SerializeField] private Material leavesMaterial;
    private int leavesMaterialIndex = 0;
    void Start()
    {
        bridgeRenderer = plantBridge.GetComponent<Renderer>();
        bridgeCollider = plantBridge.GetComponent<Collider>();
        if (bridgeRenderer != null)
        {
            bridgeRenderer.enabled = false;
        } else
        {
            Debug.LogError("PlantBridge: Could not fetch bridgeRenderer component!");
        }
        if (bridgeCollider != null)
        {
            bridgeCollider.enabled = false;
        } else
        {
            Debug.LogError("PlantBridge: Could not fetch bridgeCollider component!");
        }
    }
    void Update()
    {
        
    }

    public void GrowBridge()
    {
        
        if (bridgeRenderer != null)
        {
            bridgeRenderer.enabled = true;
        } else
        {
            Debug.LogError("PlantBridge: Could not fetch bridgeRenderer component!");
        }
        if (bridgeCollider != null)
        {
            bridgeCollider.enabled = true;
        } else
        {
            Debug.LogError("PlantBridge: Could not fetch bridgeCollider component!");
        }

        // Change Tree Appearance
        
        Renderer rend = transform.Find("InnerOrb").GetComponent<Renderer>();
        rend.enabled = false;
        rend = transform.Find("OuterOrb").GetComponent<Renderer>();
        rend.enabled = false;

        rend = transform.Find("Tree1").GetComponent<Renderer>();
        var mats = rend.materials;
        mats[leavesMaterialIndex] = leavesMaterial;
        rend.materials = mats;

        rend = transform.Find("Tree2").GetComponent<Renderer>();
        mats = rend.materials;
        mats[leavesMaterialIndex] = leavesMaterial;
        rend.materials = mats;

        rend = transform.Find("Tree2").GetComponent<Renderer>();
        mats = rend.materials;
        mats[leavesMaterialIndex] = leavesMaterial;
        rend.materials = mats;

    }
}
