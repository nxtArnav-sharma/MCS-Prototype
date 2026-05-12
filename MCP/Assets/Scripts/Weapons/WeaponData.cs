using UnityEngine;

// Right-click in Project → Create → Combat/Weapon Data to make a weapon asset
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName = "Weapon";
    public WeaponType weaponType;
    public GameObject weaponPrefab;

    [Header("Stats")]
    public float damage = 25f;
    public float attackRange = 1.5f;
    public float attackCooldown = 0.4f;
    public float comboWindow = 0.8f;      // time to input next combo hit

    [Header("Combo")]
    public int maxComboHits = 3;
    public float[] comboDamageMultipliers = { 1f, 1.2f, 1.8f };

    [Header("Special Attack")]
    public float specialDamage = 80f;
    public float specialCooldown = 5f;
    public GameObject specialVFXPrefab;

    [Header("Animations")]
    public string[] attackAnimNames = { "Attack1", "Attack2", "Attack1" };
    public string specialAnimName = "SpecialAttack";

    [Header("Audio")]
    public AudioClip swingSound;
    public AudioClip hitSound;
}

public enum WeaponType { Melee, Magic, Ranged }
