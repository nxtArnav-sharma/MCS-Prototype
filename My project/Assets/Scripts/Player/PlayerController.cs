using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;
    public float rollSpeed = 8f;
    public float rollDuration = 0.4f;

    [Header("Combat")]
    public WeaponData[] availableWeapons;
    public Transform weaponSocket;            // the WeaponAttach_R bone transform
    private int currentWeaponIndex;
    private WeaponModule equippedWeapon;
    private GameObject weaponGO;

    [Header("Combat State")]
    private int comboStep;
    private float comboTimer;
    private float specialCooldownTimer;

    [Header("References")]
    private CharacterController controller;
    private Animator animator;
    private Camera mainCam;
    private HealthSystem health;

    private Vector2 moveInput;
    private bool isRolling;
    private float rollTimer;
    private Vector3 rollDirection;

    // Animator parameter hashes (faster than string lookups)
    private static readonly int SpeedHash   = Animator.StringToHash("Speed");
    private static readonly int RollHash    = Animator.StringToHash("Roll");
    private static readonly int AttackHash  = Animator.StringToHash("AttackIndex");
    private static readonly int TriggerAtk  = Animator.StringToHash("Attack");
    private static readonly int DeathHash   = Animator.StringToHash("Death");
    private static readonly int BlockHash   = Animator.StringToHash("Block");

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator   = GetComponent<Animator>();
        health     = GetComponent<HealthSystem>();
        mainCam    = Camera.main;

        health.onDeath.AddListener(OnDeath);
        EquipWeapon(0);
    }

    void Update()
    {
        if (health.IsDead) return;

        // Block logic (Mouse 1 or Shift)
        bool isBlocking = Input.GetMouseButton(1) || Input.GetKey(KeyCode.LeftShift);
        health.isBlocking = isBlocking;
        animator.SetBool(BlockHash, isBlocking);

        HandleMovement();
        HandleComboTimer();
        HandleSpecialCooldown();
    }

    // ── Input System callbacks (wired in PlayerInput component) ──────────────
    public void OnMove(InputValue val)   => moveInput = val.Get<Vector2>();
    public void OnAttack(InputValue val) { if (val.isPressed) TryAttack(); }
    public void OnSpecial(InputValue val){ if (val.isPressed) TrySpecial(); }
    public void OnRoll(InputValue val)   { if (val.isPressed) TryRoll(); }
    public void OnSwapWeapon(InputValue val) { if (val.isPressed) CycleWeapon(); }

    // ── Movement ─────────────────────────────────────────────────────────────
    void HandleMovement()
    {
        if (isRolling)
        {
            rollTimer -= Time.deltaTime;
            controller.Move(rollDirection * rollSpeed * Time.deltaTime);
            if (rollTimer <= 0) isRolling = false;
            return;
        }

        // Camera-relative movement
        Vector3 camForward = mainCam.transform.forward;
        Vector3 camRight   = mainCam.transform.right;
        camForward.y = camRight.y = 0;
        camForward.Normalize(); camRight.Normalize();

        Vector3 move = (camForward * moveInput.y + camRight * moveInput.x);

        if (move.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot,
                                                           rotationSpeed * Time.deltaTime);
        }

        controller.Move(move * moveSpeed * Time.deltaTime);
        controller.Move(Physics.gravity * Time.deltaTime);   // keep grounded
        animator.SetFloat(SpeedHash, move.magnitude);
    }

    // ── Combat ───────────────────────────────────────────────────────────────
    void TryAttack()
    {
        if (health.isBlocking || equippedWeapon == null || equippedWeapon.IsOnCooldown) return;

        // Advance combo or reset
        if (comboTimer > 0 && comboStep < equippedWeapon.data.maxComboHits - 1)
            comboStep++;
        else
            comboStep = 0;

        comboTimer = equippedWeapon.data.comboWindow;

        float mult = equippedWeapon.data.comboDamageMultipliers[comboStep];

        animator.SetInteger(AttackHash, comboStep);
        animator.SetTrigger(TriggerAtk);

        // Activate hitbox half-way through the attack animation
        float attackWindow = equippedWeapon.data.attackCooldown * 0.4f;
        Invoke(nameof(ActivateCurrentHitbox_), equippedWeapon.data.attackCooldown * 0.3f);
        storedMult = mult;

        equippedWeapon.MarkAttackTime();
    }

    private float storedMult = 1f;
    void ActivateCurrentHitbox_() => equippedWeapon?.ActivateHitbox(0.15f, storedMult);

    void TrySpecial()
    {
        if (specialCooldownTimer > 0 || equippedWeapon == null) return;

        animator.SetTrigger(equippedWeapon.data.specialAnimName);
        specialCooldownTimer = equippedWeapon.data.specialCooldown;

        // Wider area damage — use OverlapSphere
        Collider[] hits = Physics.OverlapSphere(transform.position,
                                                 equippedWeapon.data.attackRange * 2f);
        foreach (var col in hits)
        {
            var h = col.GetComponentInParent<HealthSystem>();
            if (h != null && !h.IsDead && col.gameObject != gameObject)
                h.TakeDamage(equippedWeapon.data.specialDamage);
        }

        if (equippedWeapon.data.specialVFXPrefab)
            Instantiate(equippedWeapon.data.specialVFXPrefab, transform.position, Quaternion.identity);
    }

    void TryRoll()
    {
        if (health.isBlocking || isRolling) return;
        rollDirection = moveInput.magnitude > 0.1f
            ? (Camera.main.transform.forward * moveInput.y + Camera.main.transform.right * moveInput.x).normalized
            : transform.forward;
        rollDirection.y = 0;
        isRolling = true;
        rollTimer = rollDuration;
        animator.SetTrigger(RollHash);
    }

    void HandleComboTimer()
    {
        if (comboTimer > 0)
            comboTimer -= Time.deltaTime;
    }

    void HandleSpecialCooldown()
    {
        if (specialCooldownTimer > 0)
            specialCooldownTimer -= Time.deltaTime;
    }

    // ── Weapon System ─────────────────────────────────────────────────────────
    void EquipWeapon(int index)
    {
        if (index < 0 || index >= availableWeapons.Length) return;

        if (weaponGO) Destroy(weaponGO);
        currentWeaponIndex = index;
        WeaponData wData = availableWeapons[index];

        weaponGO = Instantiate(wData.weaponPrefab, weaponSocket);
        weaponGO.transform.localPosition = Vector3.zero;
        weaponGO.transform.localRotation = Quaternion.identity;

        equippedWeapon = weaponGO.GetComponent<WeaponModule>();
        if (equippedWeapon) equippedWeapon.data = wData;

        // Set trigger owner so it doesn't self-damage
        var trigger = weaponGO.GetComponentInChildren<WeaponTrigger>();
        if (trigger) trigger.SetOwner(gameObject.tag);

        CombatManager.Instance?.OnWeaponSwapped(wData);
    }

    void CycleWeapon()
    {
        int next = (currentWeaponIndex + 1) % availableWeapons.Length;
        EquipWeapon(next);
    }

    void OnDeath()
    {
        animator.SetTrigger(DeathHash);
        controller.enabled = false;
        this.enabled = false;
        CombatManager.Instance?.OnPlayerDeath();
    }
}
