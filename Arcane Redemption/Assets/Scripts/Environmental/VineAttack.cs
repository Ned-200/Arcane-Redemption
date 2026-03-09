using UnityEngine;

/// <summary>
/// Handles vine attack behavior - spawning from ground, rising animation, and auto-destruction.
/// Designed to be spawned by TreeBoss during melee attacks.
/// </summary>
public class VineAttack : MonoBehaviour
{
    [Header("Rise Animation")]
    [SerializeField] private float riseHeight = 3f;
    [SerializeField] private float riseSpeed = 5f;
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Timing")]
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Visual")]
    [SerializeField] private Material vineMaterial;
    [SerializeField] private Color startColor = Color.green;
    [SerializeField] private Color endColor = new Color(0.2f, 0.6f, 0.2f, 0f);

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float spawnTime;
    private Renderer vineRenderer;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        vineRenderer = GetComponent<Renderer>();
        
        if (vineRenderer != null)
        {
            propertyBlock = new MaterialPropertyBlock();
            
            if (vineMaterial != null)
            {
                vineRenderer.material = vineMaterial;
            }
        }

        startPosition = transform.position;
        targetPosition = startPosition + Vector3.up * riseHeight;
        spawnTime = Time.time;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        float elapsed = Time.time - spawnTime;
        float progress = Mathf.Clamp01(elapsed / (lifetime - fadeOutDuration));

        AnimateRise(progress);
        AnimateFade(elapsed);
    }

    private void AnimateRise(float progress)
    {
        float curvedProgress = riseCurve.Evaluate(progress);
        transform.position = Vector3.Lerp(startPosition, targetPosition, curvedProgress);
    }

    private void AnimateFade(float elapsed)
    {
        if (vineRenderer == null) return;

        float fadeStart = lifetime - fadeOutDuration;
        
        if (elapsed >= fadeStart)
        {
            float fadeProgress = (elapsed - fadeStart) / fadeOutDuration;
            Color currentColor = Color.Lerp(startColor, endColor, fadeProgress);

            if (propertyBlock != null)
            {
                vineRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", currentColor);
                vineRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}