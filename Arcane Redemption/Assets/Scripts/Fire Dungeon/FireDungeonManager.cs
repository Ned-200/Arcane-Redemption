using UnityEngine;

public class FireDungeonManager : MonoBehaviour
{
    [SerializeField] GameObject[] battleLockedDoors;
    [SerializeField] GameObject[] enemies;

    private void Update() {
        if (enemies[0] == null & enemies[1] == null)
        {
            Destroy(battleLockedDoors[0]);
            Destroy(battleLockedDoors[1]);
        }

        if (enemies[2] == null & enemies[3] == null)
        {
            Destroy(battleLockedDoors[2]);
        }

        if (enemies[4] == null & enemies[5] == null & enemies[6] == null & enemies[7] == null & enemies[8] == null & enemies[9] == null)
        {
            Destroy(battleLockedDoors[3]);
        }
    }
}
