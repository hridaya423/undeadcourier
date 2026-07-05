using UnityEngine;

public class RunDirector : MonoBehaviour
{
    public static RunDirector Instance { get; set; }

    public int zombiesKilled;
    public int cash;
    public float runTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.EnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        GameEvents.EnemyDied -= HandleEnemyDied;
    }

    private void Update()
    {
        runTime += Time.deltaTime;
    }

    private void HandleEnemyDied(Enemy e)
    {
        zombiesKilled++;
    }
}
