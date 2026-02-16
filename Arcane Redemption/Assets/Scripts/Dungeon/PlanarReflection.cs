using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class PlanarReflection : MonoBehaviour
{
    [Header("Links")]
    [Tooltip("Main camera to mirror. If null, will use Camera.main.")]
    public Camera mainCamera;

    [Tooltip("Assign your water material (the one using _PlanarReflectionTex).")]
    public Material waterMaterial;

    [Header("RenderTexture")]
    public int textureSize = 1024;
    public RenderTextureFormat format = RenderTextureFormat.ARGBHalf;
    [Range(0.01f, 1f)]
    public float renderScale = 1f;

    [Header("Reflection")]
    [Tooltip("Layers the reflection camera will render.")]
    public LayerMask reflectionMask = ~0;

    [Tooltip("Clip plane offset to reduce artifacts (acne).")]
    public float clipPlaneOffset = 0.07f;

    [Tooltip("Optional: lower than main cam for performance.")]
    [Range(0.1f, 1f)]
    public float reflectionResolutionScale = 0.75f;

    [Header("Shader Property")]
    public string reflectionTextureProperty = "_PlanarReflectionTex";

    Camera _reflectionCam;
    RenderTexture _rt;

    static bool _isRendering;

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        EnsureResources();
        UpdateMaterial();
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
        Cleanup();
    }

    void OnValidate()
    {
        EnsureResources();
        UpdateMaterial();
    }

    void EnsureResources()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (_reflectionCam == null)
        {
            var go = new GameObject("PlanarReflectionCamera (URP)");
            go.hideFlags = HideFlags.HideAndDontSave;
            _reflectionCam = go.AddComponent<Camera>();
            _reflectionCam.enabled = false;

            // URP camera data (important for SRP settings)
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderShadows = false;
            data.requiresDepthTexture = false;
            data.requiresColorTexture = false;
        }

        int w = Mathf.Max(32, Mathf.RoundToInt(textureSize * reflectionResolutionScale));
        int h = w;

        if (_rt == null || _rt.width != w || _rt.height != h)
        {
            if (_rt != null) _rt.Release();
            _rt = new RenderTexture(w, h, 16, format)
            {
                name = "RT_PlanarReflection",
                useMipMap = false,
                autoGenerateMips = false
            };
            _rt.Create();
        }
    }

    void UpdateMaterial()
    {
        if (waterMaterial != null && _rt != null)
            waterMaterial.SetTexture(reflectionTextureProperty, _rt);
    }

    void Cleanup()
    {
        if (_reflectionCam != null)
        {
            if (Application.isPlaying) Destroy(_reflectionCam.gameObject);
            else DestroyImmediate(_reflectionCam.gameObject);
            _reflectionCam = null;
        }

        if (_rt != null)
        {
            _rt.Release();
            if (Application.isPlaying) Destroy(_rt);
            else DestroyImmediate(_rt);
            _rt = null;
        }
    }

    void BeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (_isRendering) return;
        if (!isActiveAndEnabled) return;

        // Only render when the main camera renders (avoid rendering for reflection cam itself, SceneView, etc.)
        var mc = mainCamera != null ? mainCamera : Camera.main;
        if (mc == null) return;
        if (cam != mc) return;

        if (waterMaterial == null) return;

        EnsureResources();
        UpdateMaterial();

        RenderReflection(context, mc);
    }

    void RenderReflection(ScriptableRenderContext context, Camera src)
    {
        _isRendering = true;

        // Copy camera settings
        _reflectionCam.CopyFrom(src);
        _reflectionCam.cullingMask = reflectionMask;
        _reflectionCam.targetTexture = _rt;
        _reflectionCam.depthTextureMode = DepthTextureMode.None;

        // Reflect camera around the water plane (this object's up defines the plane normal)
        var planePos = transform.position;
        var planeNormal = transform.up;

        // Compute reflection matrix
        float d = -Vector3.Dot(planeNormal, planePos) - clipPlaneOffset;
        Vector4 plane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, d);

        Matrix4x4 reflectionMat = Matrix4x4.zero;
        CalculateReflectionMatrix(ref reflectionMat, plane);

        Vector3 oldPos = src.transform.position;
        Vector3 newPos = reflectionMat.MultiplyPoint(oldPos);

        _reflectionCam.worldToCameraMatrix = src.worldToCameraMatrix * reflectionMat;
        _reflectionCam.transform.position = newPos;
        _reflectionCam.transform.rotation = src.transform.rotation;

        // Oblique near-plane clipping so we only render what’s “above” the water plane
        Vector4 clipPlaneCameraSpace = CameraSpacePlane(_reflectionCam, planePos, planeNormal, 1.0f);
        _reflectionCam.projectionMatrix = src.CalculateObliqueMatrix(clipPlaneCameraSpace);

        // Render via URP
        UniversalRenderPipeline.RenderSingleCamera(context, _reflectionCam);

        _isRendering = false;
    }

    static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * 0.01f;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cPos = m.MultiplyPoint(offsetPos);
        Vector3 cNormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));
    }

    static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = 1F - 2F * plane[0] * plane[0];
        reflectionMat.m01 = -2F * plane[0] * plane[1];
        reflectionMat.m02 = -2F * plane[0] * plane[2];
        reflectionMat.m03 = -2F * plane[3] * plane[0];

        reflectionMat.m10 = -2F * plane[1] * plane[0];
        reflectionMat.m11 = 1F - 2F * plane[1] * plane[1];
        reflectionMat.m12 = -2F * plane[1] * plane[2];
        reflectionMat.m13 = -2F * plane[3] * plane[1];

        reflectionMat.m20 = -2F * plane[2] * plane[0];
        reflectionMat.m21 = -2F * plane[2] * plane[1];
        reflectionMat.m22 = 1F - 2F * plane[2] * plane[2];
        reflectionMat.m23 = -2F * plane[3] * plane[2];

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
    }
}
