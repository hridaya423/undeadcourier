using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action<Enemy> EnemyDied;
    public static event Action<Vector3, float> NoiseEmitted;
    public static event Action<int> PlayerDamaged;

    public static void RaiseEnemyDied(Enemy e)
    {
        EnemyDied?.Invoke(e);
    }

    public static void RaiseNoiseEmitted(Vector3 position, float radius)
    {
        NoiseEmitted?.Invoke(position, radius);
    }

    public static void RaisePlayerDamaged(int currentHP)
    {
        PlayerDamaged?.Invoke(currentHP);
    }
}
