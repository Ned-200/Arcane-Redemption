using System.Collections;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    [Header("Despawn")]
    [SerializeField] private float despawnDelay = 3f;

    [Header("VFX / Drops")]
    [SerializeField] private GameObject breakableEffectPrefab;
    [SerializeField] private GameObject healthPotionPrefab;
    [SerializeField] private GameObject manaPotionPrefab;

    [Header("Pieces (children)")]
    [Tooltip("If empty, will use all direct children as pieces.")]
    [SerializeField] private Transform[] pieces;

    [Tooltip("Auto-add Rigidbody/Collider to pieces if missing.")]
    [SerializeField] private bool autoSetupPieces = true;

    [Tooltip("Disable piece colliders while intact (prevents 'default explosion' from collider overlap).")]
    [SerializeField] private bool disablePieceCollidersWhileIntact = true;

    [Header("Break force (optional)")]
    [Tooltip("Impulse strength. Set to 0 if you only want gravity fall.")]
    [SerializeField] private float explosionForce = 3.5f;

    [Tooltip("How wide the push spreads.")]
    [SerializeField] private float explosionRadius = 1.2f;

    [Tooltip("Extra upward lift.")]
    [SerializeField] private float upwardModifier = 0.6f;

    [Tooltip("Limits physics depenetration 'pop' if pieces start overlapping.")]
    [SerializeField] private float maxDepenetrationVelocity = 0.5f;

    private int potionChance;
    private bool broken = false;
    private Collider[] parentColliders;

    private void Awake()
    {
        parentColliders = GetComponents<Collider>();

        // If not assigned, take all direct children as pieces
        if (pieces == null || pieces.Length == 0)
        {
            pieces = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                pieces[i] = transform.GetChild(i);
        }

        // Setup pieces so they don't fall apart before breaking
        foreach (var p in pieces)
        {
            if (p == null) continue;

            Rigidbody rb = p.GetComponent<Rigidbody>();
            Collider col = p.GetComponent<Collider>();

            if (autoSetupPieces)
            {
                if (col == null)
                    col = p.gameObject.AddComponent<BoxCollider>(); // simplest default collider

                if (rb == null)
                    rb = p.gameObject.AddComponent<Rigidbody>();
            }

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.maxDepenetrationVelocity = maxDepenetrationVelocity;
            }

            if (disablePieceCollidersWhileIntact && col != null)
            {
                col.enabled = false;
            }
        }
    }

    public void Break()
    {
        if (broken) return;
        broken = true;

        // Spawn break VFX
        if (breakableEffectPrefab != null)
            Instantiate(breakableEffectPrefab, transform.position, breakableEffectPrefab.transform.rotation);

        // Stop the intact barrel from blocking
        foreach (var c in parentColliders)
            c.enabled = false;

        // Turn pieces into physics objects + detach + schedule despawn
        foreach (var p in pieces)
        {
            if (p == null) continue;

            Rigidbody rb = p.GetComponent<Rigidbody>();
            Collider col = p.GetComponent<Collider>();

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.maxDepenetrationVelocity = maxDepenetrationVelocity;
            }

            // Detach so parent can be destroyed without deleting pieces immediately
            p.SetParent(null, true);

            // Despawn each piece
            Destroy(p.gameObject, despawnDelay);
        }

        // Enable colliders + apply explosion NEXT physics tick to avoid overlap "default explosion"
        StartCoroutine(EnableCollidersAndOptionalExplosionNextFixed());

        // Drops
        potionChance = Random.Range(0, 15); // 0, 1 and 2 = drop, (3/15) aka (1/5) potion drop chance
        if (potionChance > 3)
            Invoke(nameof(SpawnPotion), 0.05f);

        // Destroy the parent (colliders already disabled)
        Destroy(gameObject, despawnDelay);
    }

    private IEnumerator EnableCollidersAndOptionalExplosionNextFixed()
    {
        yield return new WaitForFixedUpdate();

        foreach (var p in pieces)
        {
            if (p == null) continue;

            Collider col = p.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // Optional explosion impulse. If all values are 0, it will do nothing.
            if (explosionForce > 0f && explosionRadius > 0f)
            {
                Rigidbody rb = p.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardModifier, ForceMode.Impulse);
                }
            }
        }
    }

    private void SpawnPotion()
    {
        Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y - 0.6f, transform.position.z);

        if (potionChance == 0 || potionChance == 1 && healthPotionPrefab != null) // 1 or 2 (of 0-2) is health
            Instantiate(healthPotionPrefab, spawnPos, healthPotionPrefab.transform.rotation);
        else if (potionChance == 2 && manaPotionPrefab != null)
            Instantiate(manaPotionPrefab, spawnPos, manaPotionPrefab.transform.rotation); // 0 (of 0-2) is mana
    }
}