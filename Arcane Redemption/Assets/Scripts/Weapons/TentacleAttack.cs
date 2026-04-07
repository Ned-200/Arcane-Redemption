using UnityEngine;

public class TentacleAttack : MonoBehaviour
{
    private float tentacleAngle;
    [SerializeField] private float tentacleSlamDamage = 20.0f;
    [SerializeField] private float startAngle  = 0.0f;
    [SerializeField] private float endAngle = 360.0f;
    [SerializeField] private AudioClip[] swingSounds;
    [SerializeField] private AudioClip[] slamSounds;
    [SerializeField] private Disintegrate disintegrate;

    void Start()
    {
        tentacleAngle = startAngle;
        if (swingSounds.Length > 0) 
        {
            AudioSource.PlayClipAtPoint(swingSounds[Random.Range(0, swingSounds.Length)], transform.position);
        }
    }

    void Update()
    {
        if (tentacleAngle < endAngle) {
            tentacleAngle += 1;
            transform.Rotate(1, 0, 0, Space.Self);
        } else if (tentacleAngle == endAngle)
        {
            disintegrate.TriggerDisintegration();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // if (currentPhase != SquidBossPhase.VulnerablePhase) return;
        // if (!isPerformingTentacleSlam) return;

        PlayerCharacter player = other.GetComponent<PlayerCharacter>();
        if (player != null)
        {
            player.TakeDamage(tentacleSlamDamage);
            Debug.Log($"[{gameObject.name}] 💥 Tentacle slam hit {player.name} for {tentacleSlamDamage} damage!");

            if (slamSounds.Length > 0)
            {
                AudioSource.PlayClipAtPoint(slamSounds[Random.Range(0, slamSounds.Length)], transform.position);
            }
        }
    }
}
