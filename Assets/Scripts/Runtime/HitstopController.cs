using System.Collections;
using UnityEngine;

public class HitstopController : MonoBehaviour
{
    private static HitstopController instance;

    private int requestCount;
    private const float DippedTimeScale = 0.05f;

    private static HitstopController Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("HitstopController");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<HitstopController>();
            }
            return instance;
        }
    }

    public static void RequestHitstop(float unscaledDuration)
    {
        Instance.StartCoroutine(Instance.RunHitstop(unscaledDuration));
    }

    private IEnumerator RunHitstop(float unscaledDuration)
    {
        requestCount++;
        if (requestCount == 1)
        {
            Time.timeScale = DippedTimeScale;
        }

        yield return new WaitForSecondsRealtime(unscaledDuration);

        requestCount--;
        if (requestCount <= 0)
        {
            requestCount = 0;
            Time.timeScale = 1f;
        }
    }
}
