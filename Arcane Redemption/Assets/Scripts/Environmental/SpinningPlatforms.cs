using UnityEngine;

public class SpinningPlatforms : MonoBehaviour
{
    void Update()
    {
        transform.Rotate (new Vector3 (0, -10, 0) * Time.deltaTime);
    }
}
