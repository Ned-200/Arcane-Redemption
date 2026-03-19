using UnityEngine;

public class PlantBridge : MonoBehaviour
{
    
    [SerializeField] private GameObject plantBridge;
    [SerializeField] private bool startHidden = true;
    private DisintegrateUP bridgeDisintegrate;
    private Collider bridgeCollider;
    [SerializeField] private Material leavesMaterial;
    private int leavesMaterialIndex = 0;
    public bool activated;


    
    void Start()
    {
        bridgeDisintegrate = plantBridge.GetComponent<DisintegrateUP>();
        bridgeCollider = plantBridge.GetComponent<Collider>();
        if (bridgeDisintegrate != null)
        {
            if (startHidden) {
                bridgeDisintegrate.TriggerDisintegration(true);
            }
        } else
        {
            Debug.LogError("PlantBridge: Could not fetch bridgeDisintegrate component!");
        }
        if (bridgeCollider != null)
        {
            if (startHidden) {
                bridgeCollider.enabled = false;
            } else
            {
                bridgeCollider.enabled = true;
            }
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
        if (activated) return;
        activated = true;
        
        if (bridgeDisintegrate != null)
        {
            bridgeDisintegrate.TriggerDisintegration(false);
        } else
        {
            Debug.LogError("PlantBridge: Could not fetch bridgeDisintegrate component!");
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
