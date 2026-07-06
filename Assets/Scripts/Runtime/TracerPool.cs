using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public static class TracerPool
{
    private const int PoolSize = 16;

    private static LineRenderer[] pool;
    private static Coroutine[] fadeRoutines;
    private static int nextIndex;
    private static MonoBehaviour runner;

    private static void EnsurePool()
    {
        if (pool != null) return;

        GameObject runnerObject = new GameObject("TracerPoolRunner");
        Object.DontDestroyOnLoad(runnerObject);
        runner = runnerObject.AddComponent<TracerPoolRunner>();

        pool = new LineRenderer[PoolSize];
        fadeRoutines = new Coroutine[PoolSize];

        Material tracerMaterial = new Material(Shader.Find("Sprites/Default"));
        tracerMaterial.color = new Color(1f, 0.72f, 0.35f, 0.22f);

        for (int i = 0; i < PoolSize; i++)
        {
            GameObject tracerObject = new GameObject("Tracer_" + i);
            tracerObject.transform.SetParent(runnerObject.transform);

            LineRenderer lr = tracerObject.AddComponent<LineRenderer>();
            lr.material = tracerMaterial;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.positionCount = 2;
            lr.alignment = LineAlignment.View;
            lr.numCapVertices = 2;
            lr.enabled = false;

            pool[i] = lr;
        }
    }

    public static void Fire(Vector3 start, Vector3 end, float width)
    {
        EnsurePool();

        int index = nextIndex;
        nextIndex = (nextIndex + 1) % PoolSize;

        LineRenderer lr = pool[index];

        if (fadeRoutines[index] != null)
        {
            runner.StopCoroutine(fadeRoutines[index]);
        }

        lr.startWidth = width * 0.32f;
        lr.endWidth = 0.001f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startColor = new Color(1f, 0.72f, 0.35f, 0.25f);
        lr.endColor = new Color(1f, 0.45f, 0.18f, 0f);
        lr.enabled = true;

        fadeRoutines[index] = runner.StartCoroutine(FadeAndDisable(lr, index));
    }

    private static IEnumerator FadeAndDisable(LineRenderer lr, int index)
    {
        float duration = 0.035f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / duration);
            lr.startColor = new Color(1f, 0.72f, 0.35f, 0.25f * t);
            lr.endColor = new Color(1f, 0.45f, 0.18f, 0f);
            yield return null;
        }

        lr.enabled = false;
        fadeRoutines[index] = null;
    }

    private class TracerPoolRunner : MonoBehaviour
    {
    }
}
