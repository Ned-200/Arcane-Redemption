using UnityEngine;

public class SpinningPlatforms : MonoBehaviour
{
    [SerializeField] private int rotateSpeed = -8;
    void Update()
    {
        transform.Rotate (new Vector3 (0, rotateSpeed, 0) * Time.deltaTime);
    }
}
