using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireRing : MonoBehaviour
{
    public ParticleSystem ring;
    public List<ParticleCollisionEvent> collisionEvents;
    [SerializeField] private int damage = 10;
    [SerializeField] private int destroyDelay = 10;
    [SerializeField] private AudioClip[] damageSounds;
    [SerializeField] private GameObject burningPrefab;
    private Animator playerAnim;

    void Start()
    {

        ring = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
        Destroy(this.gameObject, destroyDelay);
    }

    void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = ring.GetCollisionEvents(other, collisionEvents);

        // Try to damage the target
        PlayerCharacter target = other.GetComponent<PlayerCharacter>();
        if (target != null)
        {
            // Run player animations
            Animator playerAnim = other.GetComponent<Animator>();
            if (playerAnim != null) {
                playerAnim.Play("HurtFire", 1); 
                playerAnim.Play("HurtFire", 2); 
            }

            // Player burning sound
            if (damageSounds != null)
            {
                AudioSource.PlayClipAtPoint(damageSounds[Random.Range(0, damageSounds.Length)], other.transform.position);
            }

            // Spawn burning effect
            if (burningPrefab != null)
            {
                GameObject burning = Instantiate(burningPrefab, other.transform.position, burningPrefab.transform.rotation);
                if (other != null) {
                    burning.transform.SetParent(other.transform);
                }
            } else
            {
                Debug.LogError("Fire Ring: burningPrefab not assigned!");
            }

            target.TakeDamage(damage);
            Debug.Log($"Fire Ring Hit {target.name} for {damage} damage!");
        }
    }
}