using UnityEngine;

/// <summary>
/// Manually pushes cape bones away from boot bones in LateUpdate,
/// after all animation layers have been applied.
/// Attach this to the Player root (or any active GameObject on the player).
/// </summary>
public class CapeCollisionPusher : MonoBehaviour
{
    [System.Serializable]
    public class BootPusher
    {
        public Transform bone;       // Drag in your left/right boot bone
        [Range(0.05f, 0.5f)]
        public float radius = 0.15f; // How close before pushing starts
    }

    [Header("Boot Bones (Pushers)")]
    [SerializeField] private BootPusher[] bootPushers;

    [Header("Cape Bones (Pushed)")]
    [Tooltip("Drag in every cape bone that can clip — lower cape bones are usually enough.")]
    [SerializeField] private Transform[] capeBones;

    [Header("Tuning")]
    [Tooltip("Multiplier on the push force. 1 = exactly to the surface, >1 = extra separation.")]
    [SerializeField] private float pushStrength = 1.2f;

    [Tooltip("How fast the cape bones drift back to their animated position when not being pushed.")]
    [SerializeField] private float restoreSpeed = 8f;

    // Stores the local positions set by the animator each frame (before we modify them)
    private Vector3[] _animatedLocalPositions;

    private void Awake()
    {
        _animatedLocalPositions = new Vector3[capeBones.Length];
    }

    private void LateUpdate()
    {
        if (capeBones == null || bootPushers == null) return;

        for (int i = 0; i < capeBones.Length; i++)
        {
            Transform cape = capeBones[i];
            if (cape == null) continue;

            // Snapshot what the animator wants this bone to be
            _animatedLocalPositions[i] = cape.localPosition;

            // Apply a push for every boot that overlaps this bone
            foreach (BootPusher boot in bootPushers)
            {
                if (boot.bone == null) continue;

                Vector3 offset = cape.position - boot.bone.position;
                float dist = offset.magnitude;

                if (dist < boot.radius && dist > 0.0001f)
                {
                    float overlap = (boot.radius - dist) * pushStrength;
                    // Move the cape bone outward along the separation vector
                    cape.position += offset.normalized * overlap;
                }
            }
        }
    }
}