using UnityEngine;

public class UpdateTower : MonoBehaviour
{
    private PlayerData playerData;

    private void Start() {
        // Fetch Player Data (This must be done at start, not awake.)
        playerData = GameObject.FindWithTag("PlayerData").GetComponent<PlayerData>();
        if (playerData == null)
        {
            Debug.LogError("UpdateTower: Cannot find PlayerData!");
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        GameObject other = collision.gameObject;

        // Get player from collision
        ProjectileBase projectile = other.GetComponent<ProjectileBase>();
        if (projectile != null) {   
            if (projectile.IsPlayerProjectile())
            {
                if (this.gameObject.tag == "FlameWall" && projectile.element == "Water") {
                    playerData.fireWallDoused = true;
                    Debug.Log("UpdateTower: Tower Fire Wall Doused!");

                } else if (this.gameObject.tag == "PlantBridge" && projectile.element == "Plant") {
                    playerData.plantBridgeGrown = true;
                    Debug.Log("UpdateTower: Tower Plant Bridge Grown!");

                } else if (this.gameObject.tag == "PlantWall" && projectile.element == "Fire") {
                    playerData.vineWallBurned = true;
                    Debug.Log("UpdateTower: Tower Vine Wall Burned!");
                }
            }
        }
    }
}
