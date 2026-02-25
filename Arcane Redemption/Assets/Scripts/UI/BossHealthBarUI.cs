using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject bossHealthBarPanel;
    [SerializeField] private RectTransform healthBarFill;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Animation")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float maxHealthBarWidth = 750f;

    [Header("Color")]
    [SerializeField] private Color healthBarColor = Color.red;

    private TreeBoss currentBoss;
    private float targetWidth;
    private Image healthBarImage;

    private void Awake()
    {
        if (bossHealthBarPanel != null)
        {
            bossHealthBarPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[BossHealthBarUI] Boss Health Bar Panel not assigned!");
        }

        if (healthBarFill != null)
        {
            healthBarImage = healthBarFill.GetComponent<Image>();
            if (healthBarImage != null)
            {
                healthBarImage.color = healthBarColor;
            }
        }
    }

    public void ShowBossHealthBar(TreeBoss boss, string bossName)
    {
        if (bossHealthBarPanel == null) return;

        currentBoss = boss;

        if (bossNameText != null)
        {
            bossNameText.text = bossName;
        }

        bossHealthBarPanel.SetActive(true);

        UpdateHealthBar();
    }

    public void HideBossHealthBar()
    {
        if (bossHealthBarPanel == null) return;

        bossHealthBarPanel.SetActive(false);
        currentBoss = null;
    }

    private void Update()
    {
        if (currentBoss != null && bossHealthBarPanel.activeSelf)
        {
            UpdateHealthBar();
        }
    }

    private void UpdateHealthBar()
    {
        if (currentBoss == null || healthBarFill == null) return;

        float healthPercent = currentBoss.HealthPercent;
        targetWidth = maxHealthBarWidth * healthPercent;

        Vector2 currentSize = healthBarFill.sizeDelta;
        currentSize.x = Mathf.Lerp(currentSize.x, targetWidth, Time.deltaTime * smoothSpeed);
        healthBarFill.sizeDelta = currentSize;

        if (healthText != null)
        {
            healthText.text = $"{currentBoss.CurrentHealth:F0} / {currentBoss.MaxHealth:F0}";
        }
    }
}