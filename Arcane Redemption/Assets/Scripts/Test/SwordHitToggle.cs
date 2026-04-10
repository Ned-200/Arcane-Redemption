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

    [Header("Flower State")]
    [SerializeField] private bool toggledState = false;

    private float lastHitTime = -999f;
    private bool isForceUpdating = false;

    public bool IsOpen => toggledState;

    private void Start()
    {
        if (flowerAnim != null)
            flowerAnim.SetBool("isOpen", toggledState);
    }

    public void Toggle()
    {
        if (Time.time < lastHitTime + hitCooldown)
            return;

        lastHitTime = Time.time;
        toggledState = !toggledState;

        if (flowerAnim != null)
            flowerAnim.SetBool("isOpen", toggledState);

        ApplyRequests();

        if (flowerSounds != null && flowerSounds.Length > 0)
        {
            AudioSource.PlayClipAtPoint(
                flowerSounds[Random.Range(0, flowerSounds.Length)],
                transform.position
            );
        }
    }

    public void ForceClosed()
    {
        if (isForceUpdating)
            return;

        if (!toggledState)
            return;

        isForceUpdating = true;

        toggledState = false;

        if (flowerAnim != null)
            flowerAnim.SetBool("isOpen", false);

        ApplyRequests();

        isForceUpdating = false;
    }

    private void ApplyRequests()
    {
        foreach (GameObject obj in objectsToEnable)
        {
            if (obj == null) continue;
            ApplyToObjectAndChildren(obj, toggledState);
        }

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj == null) continue;
            ApplyToObjectAndChildren(obj, !toggledState);
        }
    }

    private void ApplyToObjectAndChildren(GameObject obj, bool shouldBeOpen)
    {
        bool handled = false;

        MultiToggleTarget[] targets = obj.GetComponentsInChildren<MultiToggleTarget>(true);

        if (targets != null && targets.Length > 0)
        {
            foreach (MultiToggleTarget target in targets)
            {
                if (target == null) continue;
                target.SetRequest(this, shouldBeOpen);
            }

            handled = true;
        }
        else
        {
            MultiToggleTarget rootTarget = obj.GetComponent<MultiToggleTarget>();
            if (rootTarget != null)
            {
                rootTarget.SetRequest(this, shouldBeOpen);
                handled = true;
            }
        }

        if (!handled)
        {
            SetObjectStateDirectAll(obj, shouldBeOpen);
        }

        PlayVineSound(obj);
    }

    private void SetObjectStateDirectAll(GameObject obj, bool turnOn)
    {
        bool handled = false;

        DisintegrateUP[] disTargets = obj.GetComponentsInChildren<DisintegrateUP>(true);

        if (disTargets != null && disTargets.Length > 0)
        {
            foreach (DisintegrateUP dis in disTargets)
            {
                if (dis == null) continue;
                dis.TriggerDisintegration(!turnOn);
            }

            handled = true;
        }
        else
        {
            DisintegrateUP rootDis = obj.GetComponent<DisintegrateUP>();
            if (rootDis != null)
            {
                rootDis.TriggerDisintegration(!turnOn);
                handled = true;
            }
        }

        if (!handled)
        {
            obj.SetActive(turnOn);
        }
    }

    private void PlayVineSound(GameObject obj)
    {
        Transform vineWall = FindChildWithTagRecursive(obj.transform, "Tough Plant Wall");

        if (vineSounds != null && vineSounds.Length > 0 && vineWall != null && lastHitTime != -999f)
        {
            AudioSource.PlayClipAtPoint(
                vineSounds[Random.Range(0, vineSounds.Length)],
                vineWall.position
            );
        }
    }

//     public void ForceClosed()
// {
//     // if (isForceUpdating)
//     //     return;

//     // if (!toggledState)
//     //     return;

//     // isForceUpdating = true;

//     // toggledState = false;

//     // if (flowerAnim != null)
//     //     flowerAnim.SetBool("isOpen", false);

//     // ApplyRequests();

//     // isForceUpdating = false;
// }

    private Transform FindChildWithTagRecursive(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
                return child;

            Transform found = FindChildWithTagRecursive(child, tag);
            if (found != null)
                return found;
        }

        return null;
    }
}