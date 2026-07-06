using System.Collections.Generic;
using UnityEngine;

public static class CorpseRegistry
{
    private static readonly List<GameObject> corpses = new List<GameObject>();

    public static void Register(GameObject corpse)
    {
        corpses.Add(corpse);

        while (corpses.Count > QualityTier.MaxCorpses)
        {
            GameObject oldest = corpses[0];
            corpses.RemoveAt(0);

            if (oldest != null)
            {
                Object.Destroy(oldest);
            }
        }
    }

    public static void Unregister(GameObject corpse)
    {
        corpses.Remove(corpse);
    }
}
