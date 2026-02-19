using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Disintegrate : MonoBehaviour
{
    [Header("Shader property name")]
    [SerializeField] private string weightProp = "_Weight";

    [Header("Timing")]
    [SerializeField] private float delay = 1.2f;
    [SerializeField] private float duration = 1.2f;

    [Header("After")]
    [SerializeField] private bool destroyAfter = true;
    [SerializeField] private float destroyDelay = 0.2f;

    Renderer rend;
    MaterialPropertyBlock mpb;
    bool running;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    // Call this from your enemy death logic
    public void TriggerDisintegration()
    {
        if (running) return;
        running = true;
        Invoke(nameof(StartDisintigrating), delay);
    }

    private void StartDisintigrating()
    {
        StartCoroutine(DisintegrateRoutine());
    }

    System.Collections.IEnumerator DisintegrateRoutine()
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float w = Mathf.Clamp01(t / duration);

            rend.GetPropertyBlock(mpb);
            mpb.SetFloat(weightProp, w);
            rend.SetPropertyBlock(mpb);

            yield return null;
        }

        // Ensure it ends at 1
        rend.GetPropertyBlock(mpb);
        mpb.SetFloat(weightProp, 1f);
        rend.SetPropertyBlock(mpb);

        if (destroyAfter)
            Destroy(gameObject, destroyDelay);
    }
}
