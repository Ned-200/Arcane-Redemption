using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireRing : MonoBehaviour
{
    public ParticleSystem ring;
    public List<ParticleCollisionEvent> collisionEvents;
    [SerializeField] int damage = 10;
    [SerializeField] int destroyDelay = 10;

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
            target.TakeDamage(damage);
            Debug.Log($"Fire Ring Hit {target.name} for {damage} damage!");
        }
    }
}