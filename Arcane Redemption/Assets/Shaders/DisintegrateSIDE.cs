using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DisintegrateSIDE : MonoBehaviour
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

    [Header("After Dissolve Out")]
    [SerializeField] private bool disableObjectAfterDissolve = true;
    [SerializeField] private float disableDelay = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugHeights = false;
    [SerializeField] private bool debugMaterials = false;

    private Renderer rend;
    private bool running;
    private Coroutine activeRoutine;

    private Material[] originalMats;
    private Material[] runtimeMats;
    private readonly List<Material> disintegrateMaterials = new();

    private bool materialsPrepared = false;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        originalMats = rend.materials;
    }

    /// <summary>
    /// dissolveOut = true  -> visible to dissolved
    /// dissolveOut = false -> dissolved to visible
    /// </summary>
    public void TriggerDisintegration(bool dissolveOut)
    {
        if (!gameObject.activeSelf && !dissolveOut)
            gameObject.SetActive(true);

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(DisintegrateRoutine(dissolveOut));
    }

    // Optional compatibility with old calls
    public void TriggerDisintegration()
    {
        TriggerDisintegration(true);
    }

    public void UseOriginalMaterials()
    {
        if (rend == null)
            rend = GetComponent<Renderer>();

        if (originalMats != null && originalMats.Length > 0)
        {
            rend.materials = originalMats;
        }
    }

    public void SnapToState(bool visible)
    {
        if (!materialsPrepared)
        {
            PrepareRuntimeMaterials();
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        gameObject.SetActive(true);

        if (visible)
        {
            UseOriginalMaterials();
        }
        else
        {
            if (runtimeMats != null && runtimeMats.Length > 0)
            {
                rend.materials = runtimeMats;
                SetWeight(1f);
            }

            if (disableObjectAfterDissolve)
                gameObject.SetActive(false);
        }
    }

    private IEnumerator DisintegrateRoutine(bool dissolveOut)
    {
        running = true;

        if (!materialsPrepared)
        {
            PrepareRuntimeMaterials();
        }

        if (runtimeMats == null || runtimeMats.Length == 0)
        {
            Debug.LogError($"[DisintegrateSIDE] No runtime materials available on {name}");
            running = false;
            activeRoutine = null;
            yield break;
        }

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        rend.materials = runtimeMats;

        // 0 = visible, 1 = hidden
        float startWeight = dissolveOut ? 0f : 1f;
        float endWeight = dissolveOut ? 1f : 0f;

        SetWeight(startWeight);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float w = Mathf.Lerp(startWeight, endWeight, t / duration);
            SetWeight(w);
            yield return null;
        }

        SetWeight(endWeight);

        if (dissolveOut)
        {
            if (disableObjectAfterDissolve)
            {
                if (disableDelay > 0f)
                    yield return new WaitForSeconds(disableDelay);

                gameObject.SetActive(false);
            }
        }
        else
        {
            UseOriginalMaterials();
        }

        running = false;
        activeRoutine = null;
    }

    private void PrepareRuntimeMaterials()
    {
        if (originalMats == null || originalMats.Length == 0)
            originalMats = rend.materials;

        if (originalMats == null || originalMats.Length == 0)
        {
            Debug.LogError($"[DisintegrateSIDE] No materials found on {name}");
            return;
        }

        if (disintegrateIndex < 0 || disintegrateIndex >= originalMats.Length || originalMats[disintegrateIndex] == null)
        {
            Debug.LogError($"[DisintegrateSIDE] Missing disintegrate template material at index {disintegrateIndex} on {name}");
            return;
        }

        Material templateDisintegrateMat = originalMats[disintegrateIndex];
        runtimeMats = new Material[originalMats.Length];
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
                Debug.Log($"[DisintegrateSIDE] Slot {i}: source '{sourceMat.name}' -> runtime '{disMat.name}'");
            }
        }

        materialsPrepared = true;
    }

    private void SetWeight(float value)
    {
        for (int i = 0; i < disintegrateMaterials.Count; i++)
        {
            Material mat = disintegrateMaterials[i];
            if (mat != null && mat.HasProperty(weightProp))
            {
                mat.SetFloat(weightProp, value);
            }
        }
    }

    private void CopyMaterialAppearance(Material sourceMat, Material targetMat)
    {
        if (sourceMat == null || targetMat == null)
            return;

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

        Texture sourceNormal = null;

        if (sourceMat.HasProperty(bumpMapProp))
            sourceNormal = sourceMat.GetTexture(bumpMapProp);

        if (sourceNormal != null && targetMat.HasProperty(bumpMapProp))
            targetMat.SetTexture(bumpMapProp, sourceNormal);

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
            Debug.LogWarning($"[DisintegrateSIDE] No MeshFilter or SkinnedMeshRenderer mesh found on {name}. Using default height range 0 to 1.");
        }

        if (Mathf.Approximately(minY, maxY))
            maxY = minY + 0.001f;

        if (debugHeights)
        {
            Debug.Log($"[DisintegrateSIDE] {name} -> MinY: {minY}, MaxY: {maxY}");
        }
    }
}