using UnityEngine;

public class PlantBridge : MonoBehaviour
{
    [SerializeField] private GameObject plantBridge;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private Material leavesMaterial;
    [SerializeField] private int leavesMaterialIndex = 0;

    private DisintegrateUP[] bridgeDisintegrates;
    private Collider[] bridgeColliders;

    public bool activated;

    void Start()
    {
        bridgeDisintegrates = plantBridge.GetComponentsInChildren<DisintegrateUP>();
        bridgeColliders = plantBridge.GetComponentsInChildren<Collider>();

        if (bridgeDisintegrates.Length > 0)
        {
            if (startHidden)
            {
                foreach (DisintegrateUP dis in bridgeDisintegrates)
                {
                    dis.TriggerDisintegration(true);
                }
            }
        }
        else
        {
            Debug.LogError("PlantBridge: Could not fetch any DisintegrateUP components!");
        }

        if (bridgeColliders.Length > 0)
        {
            foreach (Collider col in bridgeColliders)
            {
                col.enabled = !startHidden;
            }
        }
        else
        {
            Debug.LogError("PlantBridge: Could not fetch any Collider components!");
        }
    }

    public void GrowBridge()
    {
        if (activated) return;
        activated = true;

        if (bridgeDisintegrates != null && bridgeDisintegrates.Length > 0)
        {
            foreach (DisintegrateUP dis in bridgeDisintegrates)
            {
                dis.TriggerDisintegration(false);
            }
        }
        else
        {
            Debug.LogError("PlantBridge: Could not fetch any DisintegrateUP components!");
        }

        if (bridgeColliders != null && bridgeColliders.Length > 0)
        {
            foreach (Collider col in bridgeColliders)
            {
                col.enabled = true;
            }
        }
        else
        {
            Debug.LogError("PlantBridge: Could not fetch any Collider components!");
        }

        // Change Tree Appearance
        Renderer rend = transform.Find("InnerOrb")?.GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        rend = transform.Find("OuterOrb")?.GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        rend = transform.Find("Tree1")?.GetComponent<Renderer>();
        if (rend != null)
        {
            var mats = rend.materials;
            if (leavesMaterialIndex >= 0 && leavesMaterialIndex < mats.Length)
            {
                mats[leavesMaterialIndex] = leavesMaterial;
                rend.materials = mats;
            }
        }

        rend = transform.Find("Tree2")?.GetComponent<Renderer>();
        if (rend != null)
        {
            var mats = rend.materials;
            if (leavesMaterialIndex >= 0 && leavesMaterialIndex < mats.Length)
            {
                mats[leavesMaterialIndex] = leavesMaterial;
                rend.materials = mats;
            }
        }
    }
}