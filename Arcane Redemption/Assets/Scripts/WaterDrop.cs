using UnityEngine;

public class WaterDrop : MonoBehaviour
{
    [SerializeField] private AudioClip[] dropSounds;

    void OnParticleCollision(GameObject other)
    {
        AudioSource.PlayClipAtPoint(dropSounds[Random.Range(0,dropSounds.Length)], other.transform.position);
    }
}
