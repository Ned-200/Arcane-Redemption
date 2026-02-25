using UnityEngine;

public class DamageEffect : MonoBehaviour
{
    private float destroyDelay = 1.0f;

    void Start()
    {
        Destroy(gameObject, destroyDelay);
    }

}
