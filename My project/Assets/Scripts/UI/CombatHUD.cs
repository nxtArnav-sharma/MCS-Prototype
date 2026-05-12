using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatHUD : MonoBehaviour
{
    [Header("Health")]
    public Slider playerHealthBar;
    public TextMeshProUGUI playerHealthText;

    [Header("Weapon Info")]
    public TextMeshProUGUI weaponNameText;
    public Image weaponIcon;

    [Header("Wave")]
    public TextMeshProUGUI waveText;

    [Header("Game Over")]
    public GameObject gameOverPanel;

    void Start()
    {
        // Hook into HealthSystem and CombatManager events
        var player = FindObjectOfType<PlayerController>();
        if (player)
        {
            var health = player.GetComponent<HealthSystem>();
            if (health)
            {
                health.onHealthChanged.AddListener(SetPlayerHealth);
                SetPlayerHealth(1f);
            }
        }

        var cm = CombatManager.Instance;
        if (cm)
        {
            cm.onWaveStarted.AddListener(SetWave);
            cm.onWeaponSwapped.AddListener(SetWeapon);
            cm.onPlayerDeath.AddListener(ShowGameOver);
        }

        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void SetPlayerHealth(float normalised)
    {
        if (playerHealthBar) playerHealthBar.value = normalised;
        if (playerHealthText) playerHealthText.text = Mathf.Round(normalised * 100) + "%";
    }

    public void SetWeapon(WeaponData data)
    {
        if (weaponNameText) weaponNameText.text = data.weaponName;
    }

    public void SetWave(int wave)
    {
        if (waveText) waveText.text = "Wave " + wave;
    }

    public void ShowGameOver()
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }
}
