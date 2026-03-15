using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ObstructedTeleportDoor : TeleportDoor
{
    [SerializeField] GameObject obstruction;
    protected override void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !teleporting)
        {
            // Show Loading Screen
            LoadingUI.SetActive(true);

            Invoke(nameof(Teleport), 1.5f);
        }
    }

    protected override void OnTriggerEnter(Collider collision)
    {
        if (obstruction == null)
        {
            base.OnTriggerEnter(collision);
        }
    }

    protected override void OnTriggerExit(Collider collision)
    {
        if (obstruction == null)
        {
            base.OnTriggerExit(collision);
        }
    }
}
