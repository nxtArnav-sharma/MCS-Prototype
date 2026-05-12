using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Combat Mechanics")]
    public bool isBlocking;
    public float blockMitigation = 0.8f; // 80% damage reduction when blocking

    [Header("Events")]
    public UnityEvent<float> onHealthChanged;   // passes normalised 0-1
    public UnityEvent onDeath;
    public UnityEvent<float> onDamageTaken;

    [Header("Visual Feedback")]
    public Renderer[] flashRenderers;
    private Color[] originalColors;
    public Color hitFlashColor = Color.red;
    public float flashDuration = 0.1f;

    private bool isDead;

    void Awake()
    {
        currentHealth = maxHealth;
        if (flashRenderers.Length > 0)
        {
            originalColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
                originalColors[i] = flashRenderers[i].material.color;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        if (isBlocking) amount *= (1f - blockMitigation);

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
        onDamageTaken?.Invoke(amount);
        StartCoroutine(FlashRoutine());
        if (currentHealth <= 0) Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    private void Die()
    {
        isDead = true;
        onDeath?.Invoke();
    }

    public bool IsDead => isDead;

    private System.Collections.IEnumerator FlashRoutine()
    {
        foreach (var r in flashRenderers)
            r.material.color = hitFlashColor;
        yield return new WaitForSeconds(flashDuration);
        for (int i = 0; i < flashRenderers.Length; i++)
            flashRenderers[i].material.color = originalColors[i];
    }
}
