using UnityEngine;
using TMPro;

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

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private Transform cameraTransform;
    private float currentYaw;
    private float currentPitch;
    private float yawVelocity;
    private float pitchVelocity;
    private float currentDistance;
    
    private InventorySystem inventorySystem;

    [Header("UI Bars")]
    private GameObject HealthBar;
    private GameObject ManaBar;
    private GameObject StaminaBar;
    private GameObject HealthPotionCounter;
    private GameObject ManaPotionCounter;

    protected override void Awake()
    {
        base.Awake();
        
        if (respawnPoint == null)
        {
            respawnPoint = defaultRespawnPoint != null ? defaultRespawnPoint : transform;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cameraTransform = Camera.main.transform;
        
        if (cameraTransform == null)
        {
            Debug.LogError("PlayerCharacter: Main Camera not found! Make sure you have a camera tagged as 'MainCamera' in the scene.");
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log("PlayerCharacter: Camera found and connected successfully!");
            }
        }

        HealthBar = GameObject.Find("HealthBar");
        ManaBar = GameObject.Find("ManaBar");
        StaminaBar = GameObject.Find("StaminaBar");
        HealthPotionCounter = GameObject.Find("HealthPotionCounter");
        ManaPotionCounter = GameObject.Find("ManaPotionCounter");
        if (HealthBar == null || ManaBar == null || StaminaBar == null)
        {
            Debug.LogError("PlayerCharacter: Player health, stamina, or mana bar UI not found!! Check Canvas Gameobject.");
        }
        if (HealthPotionCounter == null || ManaPotionCounter == null)
        {
            Debug.LogError("PlayerCharacter: Player potions UI not found!! Check Canvas Gameobject.");
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
            Debug.LogError("PlayerCharacter: PlayerData was not found! Check PlayerData object Tag!");
        }
        else
        {
            inventorySystem = playerData.GetComponent<InventorySystem>();
        }

        if (inventorySystem == null)
        {
            Debug.LogError("PlayerCharacter: PlayerData does not have an InventorySystem component!");
        }
    }

    protected override void Update()
    {
        base.Update();
        HandleCameraInput();
    }

    private void LateUpdate()
    {
        UpdateCameraPosition();
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
        
        Debug.Log($"[PlayerCharacter] TOOK DAMAGE: {damage} | Health: {CurrentHealth}/{MaxHealth} ({HealthPercent * 100:F1}%)");
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        
        Debug.LogError($"Player '{gameObject.name}' health reached zero!");
        
        if (respawnPoint != null)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        if (respawnPoint == null)
        {
            Debug.LogWarning("[PlayerCharacter] No respawn point set - respawning at current location");
            respawnPoint = transform;
        }

        transform.position = respawnPoint.position;
        transform.rotation = respawnPoint.rotation;

        Heal(MaxHealth);
        RestoreStamina(MaxStamina);
        RestoreMana(MaxMana);

        Debug.Log($"[PlayerCharacter] Respawned at {respawnPoint.name}");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
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

            float newWidth = (CurrentHealth / MaxHealth) * 800;
            HealthRect.sizeDelta = new Vector2(newWidth, 30);

            newWidth = (CurrentMana / MaxMana) * 600;
            ManaRect.sizeDelta = new Vector2(newWidth, 30);

            newWidth = (CurrentStamina / MaxStamina) * 1000;
            StaminaRect.sizeDelta = new Vector2(newWidth, 30);
        }
        
        if (inventorySystem != null)
        {
            HealthPotionCounter.GetComponent<TextMeshProUGUI>().text = $"{inventorySystem.HealthPotionCount}";
            ManaPotionCounter.GetComponent<TextMeshProUGUI>().text = $"{inventorySystem.ManaPotionCount}";
        }
    }
}
