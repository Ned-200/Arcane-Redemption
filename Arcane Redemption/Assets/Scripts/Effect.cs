using UnityEngine;

public class DamageEffect : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 1.0f;

    void Start()
    {
        Destroy(gameObject, destroyDelay);
    }

}
