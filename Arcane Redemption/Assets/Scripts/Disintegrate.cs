using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Disintegrate : MonoBehaviour
{
    [Header("Material Slots (Renderer.materials)")]
    [SerializeField] private int normalIndex = 0;
    [SerializeField] private int disintegrateIndex = 1;

    [Header("Shader property name")]
    [SerializeField] private string weightProp = "_Weight";

    [Header("Timing")]
    [SerializeField] private float delay = 1.2f;
    [SerializeField] private float duration = 1.2f;

    [Header("After")]
    [SerializeField] private bool destroyAfter = true;
    [SerializeField] private float destroyDelay = 0.5f;

    private Renderer rend;
    private Material disMatInstance;
    private bool running;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    // Call this from your enemy death logic
    public void TriggerDisintegration()
    {
        if (running) return;
        running = true;
        StartCoroutine(DisintegrateRoutine());
    }

    private IEnumerator DisintegrateRoutine()
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        var mats = rend.materials;

        if (mats == null || mats.Length <= disintegrateIndex || mats[disintegrateIndex] == null)
        {
            Debug.LogError($"[Disintegrate] Missing disintegrate material at index {disintegrateIndex} on {name}");
            yield break;
        }

        // Make a per-enemy instance so changing _Weight doesn't affect all enemies using the same material asset
        disMatInstance = new Material(mats[disintegrateIndex]);

        // (Optional) copy texture/color from the normal material so it matches your enemy look
        if (mats.Length > normalIndex && mats[normalIndex] != null)
        {
            if (mats[normalIndex].HasProperty("_MainTex") && disMatInstance.HasProperty("_MainTex"))
                disMatInstance.SetTexture("_MainTex", mats[normalIndex].GetTexture("_MainTex"));

            if (mats[normalIndex].HasProperty("_Color") && disMatInstance.HasProperty("_Color"))
                disMatInstance.SetColor("_Color", mats[normalIndex].GetColor("_Color"));
        }

        // Start invisible (your shader should already be invisible at 0, but set it anyway)
        if (disMatInstance.HasProperty(weightProp))
            disMatInstance.SetFloat(weightProp, 0f);

        // ✅ "Delete" material 0 by replacing the renderer materials array with ONLY the disintegrate material
        rend.materials = new[] { disMatInstance };

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float w = Mathf.Clamp01(t / duration);

            if (disMatInstance.HasProperty(weightProp))
                disMatInstance.SetFloat(weightProp, w);

            yield return null;
        }

        if (disMatInstance.HasProperty(weightProp))
            disMatInstance.SetFloat(weightProp, 1f);

        if (destroyAfter)
            Destroy(gameObject, destroyDelay);
    }
}