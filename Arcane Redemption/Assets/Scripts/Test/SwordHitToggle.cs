using UnityEngine;

public class SwordHitToggle : MonoBehaviour
{
    [Header("Toggle Groups")]
    [SerializeField] private GameObject[] objectsToEnable;
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("Hit Protection")]
    [SerializeField] private float hitCooldown = 0.2f;
    
    [Header("Animations")]
    [SerializeField] private Animator flowerAnim;

    private bool toggledState = false;
    private float lastHitTime = -999f;


    private void Start()
    {
        flowerAnim.SetBool("isOpen", toggledState);

        foreach (GameObject obj in objectsToEnable)
        {
            if (obj == null) continue;
            SetObjectState(obj, toggledState);
        }

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj == null) continue;
            SetObjectState(obj, !toggledState);
        }
    }

    public void Toggle()
    {
        if (Time.time < lastHitTime + hitCooldown)
            return;

        lastHitTime = Time.time;
        toggledState = !toggledState;

        flowerAnim.SetBool("isOpen", toggledState);

        foreach (GameObject obj in objectsToEnable)
        {
            if (obj == null) continue;
            SetObjectState(obj, toggledState);
        }

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj == null) continue;
            SetObjectState(obj, !toggledState);
        }
    }

    private void SetObjectState(GameObject obj, bool turnOn)
    {
        DisintegrateUP dis = obj.GetComponent<DisintegrateUP>();

        if (dis == null)
            dis = obj.GetComponentInChildren<DisintegrateUP>(true);

        if (dis != null)
        {
            Debug.Log($"[SwordHitToggle] Found DisintegrateUP on {dis.gameObject.name} for target {obj.name}");
            dis.TriggerDisintegration(!turnOn);
        }
        else
        {
            Debug.LogWarning($"[SwordHitToggle] No DisintegrateUP found on {obj.name}, using SetActive({turnOn})");
            obj.SetActive(turnOn);
        }
    }
}