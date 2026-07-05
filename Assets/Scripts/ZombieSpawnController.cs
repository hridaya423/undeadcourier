using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ZombieSpawnController : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float spawnRadius = 50f;
    public float minSpawnDistance = 35f;
    public int initialZombiesPerWave = 5;
    public float spawnDelay = 0.5f;
    public float waveCooldown = 10f;
    public float spawnHeightOffset = 0.5f;

    [Header("Boss Settings")]
    public GameObject bossPrefab;
    public int bossWaveInterval = 5;
    public float bossSpawnDelay = 3f;
    public AudioClip bossWarningSound;

    [Header("UI References")]
    public Slider waveProgressBar;
    public Image progressBarFill;
    public Color progressBarColor = Color.green;
    public TextMeshProUGUI currentWaveText; 
    public Image currentWaveImage;
    public TextMeshProUGUI nextWaveText;    

    [Header("Progress Bar Settings")]
    public float progressBarSmoothSpeed = 5f; 

    private int currentZombiesPerWave;
    private int currentWave = 0;
    private bool inCooldown;
    private float cooldownCounter;
    private List<Enemy> currentZombiesAlive = new List<Enemy>();
    private bool isBossWave;
    private Enemy currentBoss;
    private int currentWaveZombieCount;
    private bool waveJustStarted = false; 
    private float targetProgressValue = 0f; 
    private float currentProgressValue = 0f; 

    private void OnEnable()
    {
        GameEvents.EnemyDied += OnEnemyDeath;
    }

    private void OnDisable()
    {
        GameEvents.EnemyDied -= OnEnemyDeath;
    }

    private void Start()
    {
        if (waveProgressBar != null)
        {
            waveProgressBar.gameObject.SetActive(true);
            waveProgressBar.minValue = 0f;
            waveProgressBar.maxValue = 1f;
            waveProgressBar.value = 0f;

            if (progressBarFill != null)
                progressBarFill.color = progressBarColor;
        }
        currentZombiesPerWave = initialZombiesPerWave;
        cooldownCounter = waveCooldown;

        StartNextWave();
    }

    
    private void StartNextWave()
    {
        currentZombiesAlive.RemoveAll(item => item == null);
        currentWave++;

        spawnRadius += 2f;
        minSpawnDistance = Mathf.Min(minSpawnDistance + 1f, spawnRadius - 5f);
        currentZombiesPerWave = Mathf.RoundToInt(initialZombiesPerWave * (1 + currentWave * 0.7f));

        if (GlobalReferences.Instance != null)
            GlobalReferences.Instance.waveNumber = currentWave;

        
        UpdateWaveUI(false);

        
        waveJustStarted = true;

        
        targetProgressValue = 0f;
        currentProgressValue = 0f;
        if (waveProgressBar != null)
        {
            waveProgressBar.value = 0f;
        }

        isBossWave = currentWave % bossWaveInterval == 0;

        if (isBossWave && bossPrefab != null)
        {
            int reducedZombieCount = Mathf.Max(2, currentZombiesPerWave / 3);
            currentWaveZombieCount = reducedZombieCount + 1;
            StartCoroutine(StartBossWave(reducedZombieCount));
        }
        else
        {
            currentWaveZombieCount = currentZombiesPerWave;
            StartCoroutine(SpawnWave());
        }
    }

    
    private IEnumerator WaveCooldown()
    {
        inCooldown = true;
        cooldownCounter = waveCooldown;

        
        UpdateWaveUI(true);

        
        targetProgressValue = 0f;
        currentProgressValue = 0f;
        if (waveProgressBar != null)
        {
            waveProgressBar.value = 0f;
        }

        while (cooldownCounter > 0)
        {
            targetProgressValue = 1f - (cooldownCounter / waveCooldown);

            cooldownCounter -= Time.deltaTime;
            yield return null;
        }

        
        targetProgressValue = 0f;
        currentProgressValue = 0f;
        if (waveProgressBar != null)
        {
            waveProgressBar.value = 0f;
        }

        inCooldown = false;
        StartNextWave();
    }

    private IEnumerator SpawnWave()
    {
        
        targetProgressValue = 0f;
        currentProgressValue = 0f;
        if (waveProgressBar != null)
        {
            waveProgressBar.value = 0f;
        }

        if (GlobalReferences.Instance == null || GlobalReferences.Instance.zombiePrefab == null)
            yield break;

        for (int i = 0; i < currentZombiesPerWave; i++)
        {
            if (GlobalReferences.Instance.player == null)
                yield break;

            Vector3 spawnPosition = GetValidSpawnPosition();
            GameObject zombie = Instantiate(
                GlobalReferences.Instance.zombiePrefab,
                spawnPosition,
                Quaternion.identity
            );

            Enemy enemyComponent = zombie.GetComponent<Enemy>();
            if (enemyComponent != null)
                currentZombiesAlive.Add(enemyComponent);

            yield return new WaitForSeconds(spawnDelay);
        }

        
        waveJustStarted = false;
    }

    private IEnumerator StartBossWave(int zombieCount)
    {
        
        targetProgressValue = 0f;
        currentProgressValue = 0f;
        if (waveProgressBar != null)
        {
            waveProgressBar.value = 0f;
        }

        if (bossWarningSound != null && SoundManager.Instance != null)
            SoundManager.Instance.playerChannel.PlayOneShot(bossWarningSound);

        for (int i = 0; i < zombieCount; i++)
        {
            if (GlobalReferences.Instance == null || GlobalReferences.Instance.zombiePrefab == null)
                yield break;

            Vector3 spawnPosition = GetValidSpawnPosition();
            GameObject zombie = Instantiate(
                GlobalReferences.Instance.zombiePrefab,
                spawnPosition,
                Quaternion.identity
            );

            Enemy enemyComponent = zombie.GetComponent<Enemy>();
            if (enemyComponent != null)
                currentZombiesAlive.Add(enemyComponent);

            yield return new WaitForSeconds(spawnDelay);
        }

        yield return new WaitForSeconds(bossSpawnDelay);

        Vector3 bossSpawnPosition = GetValidSpawnPosition(true);
        GameObject boss = Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);

        currentBoss = boss.GetComponent<Enemy>();
        if (currentBoss != null)
            currentZombiesAlive.Add(currentBoss);

        yield return new WaitForSeconds(3f);

        
        waveJustStarted = false;
    }

    private void Update()
    {
        currentZombiesAlive.RemoveAll(item => item == null);

        if (currentZombiesAlive.Count == 0 && !inCooldown && !waveJustStarted)
            StartCoroutine(WaveCooldown());

        UpdateProgressBar();

        if (isBossWave && currentBoss != null && currentBoss.isDead)
        {
            currentZombiesAlive.Remove(currentBoss);
            currentBoss = null;
        }

        
        if (waveProgressBar != null)
        {
            
            currentProgressValue = Mathf.Lerp(currentProgressValue, targetProgressValue, Time.deltaTime * progressBarSmoothSpeed);
            waveProgressBar.value = currentProgressValue;
        }
    }

    private void UpdateProgressBar()
    {
        if (waveProgressBar == null) return;

        
        if (waveJustStarted)
        {
            targetProgressValue = 0f;
            return;
        }

        if (inCooldown)
        {
            
        }
        else
        {
            if (currentWaveZombieCount > 0)
            {
                int zombiesKilled = currentWaveZombieCount - currentZombiesAlive.Count;
                targetProgressValue = (float)zombiesKilled / currentWaveZombieCount;
            }
        }
    }

    
    
    
    private void UpdateWaveUI(bool cooldown)
    {
        if (cooldown)
        {
            
            if (currentWaveText != null)
            {
                currentWaveImage.gameObject.SetActive(true);
                currentWaveText.gameObject.SetActive(true);
                currentWaveText.text = currentWave.ToString();
            }
            if (nextWaveText != null)
            {
                nextWaveText.gameObject.SetActive(true);
                nextWaveText.text = (currentWave + 1).ToString();
            }
        }
        else
        {
            
            if (currentWaveText != null)
            {
                currentWaveText.gameObject.SetActive(false);
                currentWaveImage.gameObject.SetActive(false);
            }
            if (nextWaveText != null)
            {
                nextWaveText.gameObject.SetActive(true);
                nextWaveText.text = currentWave.ToString();
            }
        }
    }

    
    public void OnEnemyDeath(Enemy enemy)
    {
        if (currentZombiesAlive.Contains(enemy))
        {
            currentZombiesAlive.Remove(enemy);
            UpdateProgressBar();
        }
    }

    private Vector3 GetValidSpawnPosition(bool isBoss = false)
    {
        if (GlobalReferences.Instance?.player == null)
            return Vector3.zero;

        Vector3 playerPosition = GlobalReferences.Instance.player.transform.position;
        int maxAttempts = 100;
        float bossMinDistance = isBoss ? minSpawnDistance + 10f : minSpawnDistance;
        float bossMaxDistance = isBoss ? minSpawnDistance + 20f : spawnRadius;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(bossMinDistance, bossMaxDistance);

            Vector3 spawnCandidate = playerPosition + new Vector3(
                randomDir.x * randomDist,
                playerPosition.y + spawnHeightOffset,
                randomDir.y * randomDist
            );

            if (NavMesh.SamplePosition(spawnCandidate, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(playerPosition, hit.position) >= minSpawnDistance)
                    return hit.position;
            }
        }

        Vector3 fallbackPos = playerPosition +
                            Random.onUnitSphere * minSpawnDistance * 1.1f +
                            Vector3.up * spawnHeightOffset;

        if (NavMesh.SamplePosition(fallbackPos, out NavMeshHit fallbackHit, 50f, NavMesh.AllAreas))
            return fallbackHit.position;

        return playerPosition + Vector3.forward * minSpawnDistance;
    }

    private void OnDrawGizmosSelected()
    {
        if (GlobalReferences.Instance?.player == null) return;

        Vector3 playerPos = GlobalReferences.Instance.player.transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerPos, minSpawnDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerPos, spawnRadius);
    }
}
