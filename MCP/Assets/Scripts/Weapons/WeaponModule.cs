using UnityEngine;
using System.Collections;

public class WeaponModule : MonoBehaviour
{
    public WeaponData data;

    [Header("Hitbox")]
    public Collider attackCollider;           // trigger collider on weapon
    public Transform trailStart, trailEnd;    // from Blender empties

    [Header("VFX")]
    public TrailRenderer weaponTrail;
    public ParticleSystem hitParticles;

    private bool isAttacking;
    private float lastAttackTime;

    // Called by PlayerController — activate this weapon's hitbox for one frame window
    public void ActivateHitbox(float duration, float damageMultiplier = 1f)
    {
        StartCoroutine(HitboxRoutine(duration, damageMultiplier));
    }

    private IEnumerator HitboxRoutine(float duration, float mult)
    {
        isAttacking = true;
        if (attackCollider) attackCollider.enabled = true;
        if (weaponTrail) weaponTrail.emitting = true;

        // Store multiplied damage on the collider's trigger handler
        var trigger = attackCollider?.GetComponent<WeaponTrigger>();
        if (trigger) trigger.damage = data.damage * mult;

        yield return new WaitForSeconds(duration);

        isAttacking = false;
        if (attackCollider) attackCollider.enabled = false;
        if (weaponTrail) weaponTrail.emitting = false;
    }

    public void PlayHitEffect(Vector3 position)
    {
        if (hitParticles)
        {
            hitParticles.transform.position = position;
            hitParticles.Play();
        }
    }

    public bool IsOnCooldown => Time.time - lastAttackTime < data.attackCooldown;
    public void MarkAttackTime() => lastAttackTime = Time.time;
}
