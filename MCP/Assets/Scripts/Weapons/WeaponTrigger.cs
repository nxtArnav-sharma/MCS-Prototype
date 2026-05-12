using UnityEngine;

// Attach this to the weapon collider object
public class WeaponTrigger : MonoBehaviour
{
    [HideInInspector] public float damage;
    private string ownerTag;

    public void SetOwner(string tag) => ownerTag = tag;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ownerTag)) return;         // don't hit yourself

        var health = other.GetComponentInParent<HealthSystem>();
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage);
            CombatManager.Instance?.SpawnHitVFX(transform.position);
        }
    }
}
