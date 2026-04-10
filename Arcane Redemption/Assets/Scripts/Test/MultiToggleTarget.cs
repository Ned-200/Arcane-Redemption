using System.Collections.Generic;
using UnityEngine;

public class MultiToggleTarget : MonoBehaviour
{
    [System.Serializable]
    private class RequestData
    {
        public SwordHitToggle sourceFlower;
        public bool wantsOpen;
        public int order;

        public RequestData(SwordHitToggle sourceFlower, bool wantsOpen, int order)
        {
            this.sourceFlower = sourceFlower;
            this.wantsOpen = wantsOpen;
            this.order = order;
        }
    }

    [Header("Startup State")]
    [SerializeField] private bool defaultOpen = false;

    [Header("Debug")]
    [SerializeField] private bool isOpen = false;

    private Dictionary<int, RequestData> requests = new Dictionary<int, RequestData>();
    private static int globalOrderCounter = 0;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        isOpen = defaultOpen;
        ApplyState();
    }

    public void SetRequest(SwordHitToggle source, bool wantsOpen)
    {
        if (source == null) return;

        int id = source.GetInstanceID();
        globalOrderCounter++;

        requests[id] = new RequestData(source, wantsOpen, globalOrderCounter);

        RecalculateState();
    }

    private void RecalculateState()
    {
        if (requests.Count == 0)
        {
            isOpen = defaultOpen;
            ApplyState();
            return;
        }

        RequestData newestRequest = null;

        foreach (var kvp in requests)
        {
            if (newestRequest == null || kvp.Value.order > newestRequest.order)
            {
                newestRequest = kvp.Value;
            }
        }

        isOpen = newestRequest.wantsOpen;
        ApplyState();

        NotifyFlowersIfContradicted();
    }

    private void NotifyFlowersIfContradicted()
    {
        foreach (var kvp in requests)
        {
            RequestData request = kvp.Value;

            if (request.sourceFlower == null)
                continue;

            // If a flower wants this target open, but the target ended up closed,
            // force that flower closed too.
            if (request.wantsOpen && !isOpen)
            {
                request.sourceFlower.ForceClosed();
            }
        }
    }

    

    private void ApplyState()
    {
        DisintegrateUP dis = GetComponent<DisintegrateUP>();

        if (dis == null)
            dis = GetComponentInChildren<DisintegrateUP>(true);

        if (dis != null)
        {
            dis.TriggerDisintegration(!isOpen);
        }
        else
        {
            gameObject.SetActive(isOpen);
        }
    }
}