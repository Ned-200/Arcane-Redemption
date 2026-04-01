using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerCharacter : BaseCharacter
{
    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float cameraDistance = 5f;
    [SerializeField] private float cameraHeight = 1.8f;
    [SerializeField] private float shoulderOffset = 0.5f;
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Camera Smoothing")]
    [SerializeField] private float rotationSmoothTime = 0.12f;
    [SerializeField] private float cameraSmoothSpeed = 10f;

    [Header("Camera Collision")]
    [SerializeField] private float cameraRadius = 0.3f;
    [SerializeField] private LayerMask collisionLayers;

    [Header("Respawn System")]
    [SerializeField] private Transform defaultRespawnPoint;
    public Transform respawnPoint;
    [SerializeField] private float respawnCooldown = 3.0f;
    private CharacterController characterController;    

    [Header("Sounds")]
    [SerializeField] private AudioClip[] damagedSounds;
    [SerializeField] private AudioClip deathSound;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private Transform cameraTransform;
    private float currentYaw;
    private float currentPitch;
    private float yawVelocity;
    private float pitchVelocity;
    private float currentDistance;

   // private LowHealthPostProcess lowHealthEffects;
    private PlayerController playerController;
    private InventorySystem inventorySystem;

    [Header("UI Bars")]
    private GameObject HUD;
    private GameObject HealthBar;
    private GameObject ManaBar;
    private GameObject StaminaBar;
    private GameObject HealthPotionCounter;
    private GameObject ManaPotionCounter;
    
    private GameObject DeathScreen;

    private bool canRespawn;
    private bool deathHandled;

    protected override void Awake()
    {
        base.Awake();

        playerController = GetComponent<PlayerController>();
       // lowHealthEffects = FindObjectOfType<LowHealthPostProcess>();

        if (respawnPoint == null)
        {
            respawnPoint = defaultRespawnPoint != null ? defaultRespawnPoint : transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (GameObject.FindWithTag("MainCamera"))
        {
            cameraTransform = GameObject.FindWithTag("MainCamera").transform;
            if (showDebugInfo)
            {
                Debug.Log("PlayerCharacter: Camera found and connected successfully!");
            }
        }
        else
        {
            Debug.LogError("PlayerCharacter: Main Camera not found! Make sure your camera is tagged MainCamera.");
        }

        HUD = GameObject.Find("HUD");
        HealthPotionCounter = GameObject.Find("HealthPotionCounter");
        ManaPotionCounter = GameObject.Find("ManaPotionCounter");
        DeathScreen = GameObject.Find("DeathScreen");

        if (HUD != null)
        {
            HealthBar = HUD.transform.Find("HealthBar").gameObject;
            ManaBar = HUD.transform.Find("ManaBar").gameObject;
            StaminaBar = HUD.transform.Find("StaminaBar").gameObject;
        } else
        {
            Debug.LogError("PlayerCharacter: HUD UI not found.");
        }

        if (HealthBar == null || ManaBar == null || StaminaBar == null)
        {
            Debug.LogError("PlayerCharacter: Health, mana, or stamina bar UI not found.");
        }

        if (HealthPotionCounter == null || ManaPotionCounter == null)
        {
            Debug.LogError("PlayerCharacter: Potion counter UI not found.");
        }

        if (DeathScreen == null)
        {
            Debug.LogError("PlayerCharacter: DeathScreen UI not found.");
        }
        else
        {
            DeathScreen.SetActive(false);
        }

        if (playerController == null)
        {
            Debug.LogError("PlayerCharacter: PlayerController not found on player.");
        }

        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("PlayerCharacter: CharacterController not found on player.");
        }

        if (lowHealthEffects == null)
        {
            Debug.LogWarning("PlayerCharacter: LowHealthPostProcess not found in scene.");
        }
        else
        {
            lowHealthEffects.SetHealthPercent(HealthPercent);
        }

        currentYaw = transform.eulerAngles.y;
        currentPitch = 0f;
        currentDistance = cameraDistance;

        Debug.Log($"[PlayerCharacter] Initialized - Health: {CurrentHealth}/{MaxHealth}");
    }

    private void Start()
    {
        GameObject playerData = GameObject.FindWithTag("PlayerData");

        if (playerData == null)
        {
            Debug.LogError("PlayerCharacter: PlayerData was not found! Check PlayerData object tag.");
        }
        else
        {
            inventorySystem = playerData.GetComponent<InventorySystem>();
        }

        if (inventorySystem == null)
        {
            Debug.LogError("PlayerCharacter: InventorySystem component missing from PlayerData.");
        }
    }

    protected override void Update()
    {
        base.Update();

        if (IsAlive)
        {
            HandleCameraInput();
        }
        else
        {
            HandleDeathScreen();
        }
    }

    private void LateUpdate()
    {
        UpdateCameraPosition();
    }

    private void HandleDeathScreen()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canRespawn)
        {
            PerformRespawn();
        }
    }
    
    private IEnumerator EnableRespawnAfterDelay()
    {
        canRespawn = false;

        yield return new WaitForSeconds(respawnCooldown);

        if (DeathScreen != null)
        {
            Debug.Log("DeathScreen found, showing it now.");
            DeathScreen.SetActive(true);
        }
        else
        {
            Debug.LogError("DeathScreen is NULL in OnDeath.");
        }

        canRespawn = true;
        Debug.Log("Player can respawn now. Press space!");
    }

    private void PerformRespawn()
    {
        canRespawn = false;
        deathHandled = false;

        if (DeathScreen != null)
            DeathScreen.SetActive(false);

        characterController.enabled = false;
        characterController.transform.position = respawnPoint.position;
        characterController.transform.rotation = respawnPoint.rotation;
        characterController.enabled = true;

        currentHealth = maxHealth;
        currentMana = maxMana;
        currentStamina = maxStamina;

        if (playerController != null)
        {
            playerController.canMove = true;
            playerController.enabled = false;
            playerController.enabled = true;
            
            playerController.playerAnim.SetBool("Died", false);
        }

        if (lowHealthEffects != null)
        {
            lowHealthEffects.SetHealthPercent(HealthPercent);
        }

        Debug.Log($"[PlayerCharacter] Respawned at {respawnPoint.name}");
    }

    private void HandleCameraInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        currentYaw += mouseX;

        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }

    private void UpdateCameraPosition()
    {
        if (cameraTransform == null) return;

        Vector3 pivotPosition = transform.position + Vector3.up * cameraHeight + transform.right * shoulderOffset;
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        Vector3 desiredPosition = pivotPosition - rotation * Vector3.forward * cameraDistance;

        Vector3 direction = desiredPosition - pivotPosition;
        float desiredDistance = direction.magnitude;

        RaycastHit hit;
        if (Physics.SphereCast(pivotPosition, cameraRadius, direction.normalized, out hit, desiredDistance, collisionLayers))
        {
            currentDistance = Mathf.Lerp(currentDistance, hit.distance * 0.9f, Time.deltaTime * cameraSmoothSpeed);
        }
        else
        {
            currentDistance = Mathf.Lerp(currentDistance, cameraDistance, Time.deltaTime * cameraSmoothSpeed);
        }

        Vector3 finalPosition = pivotPosition - rotation * Vector3.forward * currentDistance;
        cameraTransform.position = finalPosition;
        cameraTransform.rotation = rotation;
    }

    protected override void OnDamageTaken(float damage)
    {
        base.OnDamageTaken(damage);

        Debug.Log($"[PlayerCharacter] TOOK DAMAGE: {damage} | Health: {CurrentHealth}/{MaxHealth} ({HealthPercent * 100f:F1}%)");

        if (lowHealthEffects != null)
        {
            lowHealthEffects.SetHealthPercent(HealthPercent);
        }

        // Play impact effects
        if (damagedSounds.Length > 0)
        {
            AudioSource.PlayClipAtPoint(damagedSounds[Random.Range(0, damagedSounds.Length)], transform.position);
        }

    }

    protected override void OnDeath()
    {
        base.OnDeath();

        if (deathHandled) return;
        deathHandled = true;

        Debug.Log($"Player '{gameObject.name}' health reached zero!");

        if (playerController != null)
        {
            playerController.canMove = false;
            playerController.playerAnim.SetBool("Died", true);
        }
        else
        {
            Debug.LogError("playerController is NULL in OnDeath.");
        }

        // Play death sound
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        if (lowHealthEffects != null)
        {
            lowHealthEffects.SetHealthPercent(0);
        }

        Debug.Log("Starting respawn cooldown...");
        StartCoroutine(EnableRespawnAfterDelay());
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && IsAlive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        Vector3 pivotPosition = transform.position + Vector3.up * cameraHeight + transform.right * shoulderOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivotPosition, 0.2f);

        if (Application.isPlaying && cameraTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pivotPosition, cameraTransform.position);
        }
    }

    private void OnGUI()
    {
        if (HealthBar != null && ManaBar != null && StaminaBar != null)
        {
            GameObject Health = HealthBar.transform.Find("Health").gameObject;
            GameObject Mana = ManaBar.transform.Find("Mana").gameObject;
            GameObject Stamina = StaminaBar.transform.Find("Stamina").gameObject;

            RectTransform HealthRect = Health.GetComponent<RectTransform>();
            RectTransform ManaRect = Mana.GetComponent<RectTransform>();
            RectTransform StaminaRect = Stamina.GetComponent<RectTransform>();

            float newWidth = (CurrentHealth / MaxHealth) * 800f;
            HealthRect.sizeDelta = new Vector2(newWidth, 30);

            newWidth = (CurrentMana / MaxMana) * 600f;
            ManaRect.sizeDelta = new Vector2(newWidth, 30);

            newWidth = (CurrentStamina / MaxStamina) * 1000f;
            StaminaRect.sizeDelta = new Vector2(newWidth, 30);
        }

        if (inventorySystem != null)
        {
            if (HealthPotionCounter != null)
                HealthPotionCounter.GetComponent<TextMeshProUGUI>().text = $"{inventorySystem.HealthPotionCount}";

            if (ManaPotionCounter != null)
                ManaPotionCounter.GetComponent<TextMeshProUGUI>().text = $"{inventorySystem.ManaPotionCount}";
        }

        if (playerController != null)
        {
            if (!playerController.canMove && HUD.activeSelf)
            {
                HUD.SetActive(false);
            } else if (playerController.canMove && !HUD.activeSelf)
            {
                HUD.SetActive(true);
            }
        }
    }
}