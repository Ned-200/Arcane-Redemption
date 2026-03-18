using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DisintegrateUP : MonoBehaviour
{
    [Header("Material Slots (Renderer.materials)")]
    [SerializeField] private int disintegrateIndex = 1;

    [Header("Shader Property Names")]
    [SerializeField] private string weightProp = "_Weight";
    [SerializeField] private string minHeightProp = "_MinHeight";
    [SerializeField] private string maxHeightProp = "_MaxHeight";

    [Header("Texture / Color Property Names")]
    [SerializeField] private string mainTexProp = "_MainTex";
    [SerializeField] private string baseMapProp = "_BaseMap";
    [SerializeField] private string colorProp = "_Color";
    [SerializeField] private string baseColorProp = "_BaseColor";
    [SerializeField] private string bumpMapProp = "_BumpMap";

    [Header("Timing")]
    [SerializeField] private float delay = 1.2f;
    [SerializeField] private float duration = 1.2f;

    [Header("After")]
    [SerializeField] private bool destroyAfter = true;
    [SerializeField] private float destroyDelay = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugHeights = false;
    [SerializeField] private bool debugMaterials = false;

    private Renderer rend;
    private bool running;

    private readonly List<Material> disintegrateMaterials = new();

    private void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void TriggerDisintegrationUP()
    {
        if (running) return;
        running = true;
        StartCoroutine(DisintegrateUPRoutine());
    }

    private IEnumerator DisintegrateUPRoutine()
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        Material[] originalMats = rend.materials;

        if (originalMats == null || originalMats.Length == 0)
        {
            Debug.LogError($"[DisintegrateUP] No materials found on {name}");
            yield break;
        }

        if (disintegrateIndex < 0 || disintegrateIndex >= originalMats.Length || originalMats[disintegrateIndex] == null)
        {
            Debug.LogError($"[DisintegrateUP] Missing disintegrate template material at index {disintegrateIndex} on {name}");
            yield break;
        }

        Material templateDisintegrateMat = originalMats[disintegrateIndex];
        Material[] runtimeMats = new Material[originalMats.Length];
        disintegrateMaterials.Clear();

        GetMeshBounds(out float minY, out float maxY);

        for (int i = 0; i < originalMats.Length; i++)
        {
            Material sourceMat = originalMats[i];
            if (sourceMat == null)
            {
                runtimeMats[i] = null;
                continue;
            }

            // Create a unique disintegrate material for this slot
            Material disMat = new Material(templateDisintegrateMat);
            disMat.name = $"{templateDisintegrateMat.name}_Instance_{i}";

            CopyMaterialAppearance(sourceMat, disMat);
            ApplyHeightBounds(disMat, minY, maxY);

            if (disMat.HasProperty(weightProp))
                disMat.SetFloat(weightProp, 0f);

            runtimeMats[i] = disMat;
            disintegrateMaterials.Add(disMat);

            if (debugMaterials)
            {
                Debug.Log($"[DisintegrateUP] Slot {i}: source '{sourceMat.name}' -> disintegrate '{disMat.name}'");
            }
        }

        rend.materials = runtimeMats;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float w = Mathf.Clamp01(t / duration);

            for (int i = 0; i < disintegrateMaterials.Count; i++)
            {
                Material mat = disintegrateMaterials[i];
                if (mat != null && mat.HasProperty(weightProp))
                    mat.SetFloat(weightProp, w);
            }

            yield return null;
        }

        for (int i = 0; i < disintegrateMaterials.Count; i++)
        {
            Material mat = disintegrateMaterials[i];
            if (mat != null && mat.HasProperty(weightProp))
                mat.SetFloat(weightProp, 1f);
        }

        if (destroyAfter)
            Destroy(gameObject, destroyDelay);
    }

   private void CopyMaterialAppearance(Material sourceMat, Material targetMat)
{
    if (sourceMat == null || targetMat == null)
        return;

    // -------------------------
    // Copy ALBEDO TEXTURE
    // Source may use _MainTex or _BaseMap
    // Target dissolve shader mainly uses _MainTex
    // -------------------------

    Texture sourceAlbedo = null;

    if (sourceMat.HasProperty(mainTexProp))
        sourceAlbedo = sourceMat.GetTexture(mainTexProp);

    if (sourceAlbedo == null && sourceMat.HasProperty(baseMapProp))
        sourceAlbedo = sourceMat.GetTexture(baseMapProp);

    if (sourceAlbedo != null)
    {
        if (targetMat.HasProperty(mainTexProp))
            targetMat.SetTexture(mainTexProp, sourceAlbedo);

        if (targetMat.HasProperty(baseMapProp))
            targetMat.SetTexture(baseMapProp, sourceAlbedo);
    }

    // -------------------------
    // Copy ALBEDO COLOR
    // Source may use _Color or _BaseColor
    // Target dissolve shader mainly uses _Color
    // -------------------------

    Color sourceColor = Color.white;
    bool foundColor = false;

    if (sourceMat.HasProperty(colorProp))
    {
        sourceColor = sourceMat.GetColor(colorProp);
        foundColor = true;
    }
    else if (sourceMat.HasProperty(baseColorProp))
    {
        sourceColor = sourceMat.GetColor(baseColorProp);
        foundColor = true;
    }

    if (foundColor)
    {
        if (targetMat.HasProperty(colorProp))
            targetMat.SetColor(colorProp, sourceColor);

        if (targetMat.HasProperty(baseColorProp))
            targetMat.SetColor(baseColorProp, sourceColor);
    }

    // -------------------------
    // Copy NORMAL MAP
    // -------------------------

    Texture sourceNormal = null;

    if (sourceMat.HasProperty(bumpMapProp))
        sourceNormal = sourceMat.GetTexture(bumpMapProp);

    if (sourceNormal != null && targetMat.HasProperty(bumpMapProp))
        targetMat.SetTexture(bumpMapProp, sourceNormal);

    // -------------------------
    // Copy TEXTURE SCALE / OFFSET
    // Prefer the source property that actually exists
    // Apply to target _MainTex because that is what your shader samples
    // -------------------------

    if (targetMat.HasProperty(mainTexProp))
    {
        if (sourceMat.HasProperty(mainTexProp))
        {
            targetMat.SetTextureScale(mainTexProp, sourceMat.GetTextureScale(mainTexProp));
            targetMat.SetTextureOffset(mainTexProp, sourceMat.GetTextureOffset(mainTexProp));
        }
        else if (sourceMat.HasProperty(baseMapProp))
        {
            targetMat.SetTextureScale(mainTexProp, sourceMat.GetTextureScale(baseMapProp));
            targetMat.SetTextureOffset(mainTexProp, sourceMat.GetTextureOffset(baseMapProp));
        }
    }

    if (targetMat.HasProperty(baseMapProp))
    {
        if (sourceMat.HasProperty(baseMapProp))
        {
            targetMat.SetTextureScale(baseMapProp, sourceMat.GetTextureScale(baseMapProp));
            targetMat.SetTextureOffset(baseMapProp, sourceMat.GetTextureOffset(baseMapProp));
        }
        else if (sourceMat.HasProperty(mainTexProp))
        {
            targetMat.SetTextureScale(baseMapProp, sourceMat.GetTextureScale(mainTexProp));
            targetMat.SetTextureOffset(baseMapProp, sourceMat.GetTextureOffset(mainTexProp));
        }
    }

    // -------------------------
    // Copy bump strength
    // -------------------------

    if (sourceMat.HasProperty("_BumpScale") && targetMat.HasProperty("_BumpScale"))
        targetMat.SetFloat("_BumpScale", sourceMat.GetFloat("_BumpScale"));

    if (sourceMat.HasProperty("_BumpStr") && targetMat.HasProperty("_BumpStr"))
        targetMat.SetFloat("_BumpStr", sourceMat.GetFloat("_BumpStr"));
}
    private void ApplyHeightBounds(Material mat, float minY, float maxY)
    {
        if (mat == null) return;

        if (Mathf.Approximately(minY, maxY))
            maxY = minY + 0.001f;

        if (mat.HasProperty(minHeightProp))
            mat.SetFloat(minHeightProp, minY);

        if (mat.HasProperty(maxHeightProp))
            mat.SetFloat(maxHeightProp, maxY);
    }

    private void GetMeshBounds(out float minY, out float maxY)
    {
        minY = 0f;
        maxY = 1f;
        bool foundBounds = false;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Bounds b = mf.sharedMesh.bounds;
            minY = b.min.y;
            maxY = b.max.y;
            foundBounds = true;
        }

        if (!foundBounds)
        {
            SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                Bounds b = smr.sharedMesh.bounds;
                minY = b.min.y;
                maxY = b.max.y;
                foundBounds = true;
            }
        }

        if (!foundBounds)
        {
            Debug.LogWarning($"[DisintegrateUP] No MeshFilter or SkinnedMeshRenderer mesh found on {name}. Using default height range 0 to 1.");
        }

        if (Mathf.Approximately(minY, maxY))
            maxY = minY + 0.001f;

        if (debugHeights)
        {
            Debug.Log($"[DisintegrateUP] {name} -> MinY: {minY}, MaxY: {maxY}");
        }
    }
}