using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("VFX Pool")]
    public GameObject hitVFXPrefab;
    public int poolSize = 20;
    private Queue<GameObject> vfxPool = new Queue<GameObject>();

    [Header("Spawning")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int enemiesPerWave = 3;
    public float waveCooldown = 5f;
    private int currentWave;
    private int livingEnemies;
    private float waveTimer;
    private bool waitingForWave;

    [Header("Events")]
    public UnityEvent<WeaponData> onWeaponSwapped;
    public UnityEvent<int> onWaveStarted;
    public UnityEvent onPlayerDeath;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Pre-warm VFX pool
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(hitVFXPrefab);
            go.SetActive(false);
            vfxPool.Enqueue(go);
        }
    }

    void Start() => StartWave();

    void Update()
    {
        if (!waitingForWave) return;
        waveTimer -= Time.deltaTime;
        if (waveTimer <= 0) StartWave();
    }

    public void SpawnHitVFX(Vector3 position)
    {
        if (vfxPool.Count == 0) return;
        var vfx = vfxPool.Dequeue();
        vfx.transform.position = position;
        vfx.SetActive(true);
        StartCoroutine(ReturnToPool(vfx, 1.5f));
    }

    private System.Collections.IEnumerator ReturnToPool(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        go.SetActive(false);
        vfxPool.Enqueue(go);
    }

    void StartWave()
    {
        waitingForWave = false;
        currentWave++;
        onWaveStarted?.Invoke(currentWave);

        int count = enemiesPerWave + (currentWave - 1) * 2;   // scale with wave
        livingEnemies = count;

        for (int i = 0; i < count; i++)
        {
            Transform spawnPt = spawnPoints[i % spawnPoints.Length];
            var enemy = Instantiate(enemyPrefab, spawnPt.position, spawnPt.rotation);
            var health = enemy.GetComponent<HealthSystem>();
            if (health) health.onDeath.AddListener(OnEnemyDied);
        }
    }

    void OnEnemyDied()
    {
        livingEnemies--;
        if (livingEnemies <= 0)
        {
            waitingForWave = true;
            waveTimer = waveCooldown;
        }
    }

    public void OnWeaponSwapped(WeaponData data) => onWeaponSwapped?.Invoke(data);
    public void OnPlayerDeath() => onPlayerDeath?.Invoke();
}
