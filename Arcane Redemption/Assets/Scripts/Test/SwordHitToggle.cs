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

    [Header("Sound")]
    [SerializeField] private AudioClip[] flowerSounds;
    [SerializeField] private AudioClip[] vineSounds;

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


        // Play sounds
        if (flowerSounds.Length > 0)
        {
            AudioSource.PlayClipAtPoint(flowerSounds[Random.Range(0, flowerSounds.Length)], transform.position);
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

        // Play sounds
        Transform vineWall = FindChildWithTag(obj.transform, "Tough Plant Wall");
        if (vineSounds.Length > 0 && vineWall != null)
        {
            AudioSource.PlayClipAtPoint(vineSounds[Random.Range(0, vineSounds.Length)], vineWall.position);
        }
    }

    Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
            {
                return child;
            }
        }
        return null;
    }
}