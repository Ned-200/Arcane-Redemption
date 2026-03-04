using UnityEngine;

public class DebugManager : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugKeys = true;
    [SerializeField] private KeyCode godModeToggleKey = KeyCode.P;

    [Header("God Mode Features")]
    [SerializeField] private bool unlimitedStamina = true;
    [SerializeField] private bool unlimitedMana = true;
    [SerializeField] private bool noCooldowns = true;
    [SerializeField] private bool invulnerable = true;

    private PlayerCharacter player;
    private bool isGodModeActive = false;

    public bool IsGodModeActive => isGodModeActive;
    public bool UnlimitedStamina => isGodModeActive && unlimitedStamina;
    public bool UnlimitedMana => isGodModeActive && unlimitedMana;
    public bool NoCooldowns => isGodModeActive && noCooldowns;
    public bool Invulnerable => isGodModeActive && invulnerable;

    public static DebugManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerCharacter>();
    }

    private void Update()
    {
        if (!enableDebugKeys) return;

        if (Input.GetKeyDown(godModeToggleKey))
        {
            ToggleGodMode();
        }
    }

    private void ToggleGodMode()
    {
        isGodModeActive = !isGodModeActive;

        if (isGodModeActive)
        {
            Debug.LogWarning("?? GOD MODE ACTIVATED! [P] - Unlimited Stamina/Mana, No Cooldowns, Invulnerable");
            
            if (player != null)
            {
                player.RestoreStamina(player.MaxStamina);
                player.RestoreMana(player.MaxMana);
            }
        }
        else
        {
            Debug.Log("? God Mode Deactivated [P]");
        }
    }

    private void OnGUI()
    {
        if (!isGodModeActive) return;

        GUI.color = Color.yellow;
        GUI.Label(new Rect(10, Screen.height - 30, 300, 30), "? GOD MODE ACTIVE [P]");
    }
}