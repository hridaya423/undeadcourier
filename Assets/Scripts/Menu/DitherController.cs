using System.Collections;
using UnityEngine;

public class DitherController : MonoBehaviour
{
    public static DitherController Instance { get; private set; }

    [System.Serializable]
    public struct DitherState
    {
        [Range(0f, 1f)] public float intensity;
        [Range(2f, 16f)] public float steps;
        [Range(0f, 1f)] public float lumaInfluence;
        [Range(1f, 8f)] public float pixelSize;
        [Range(0f, 1f)] public float duotone;

        public static DitherState Lerp(DitherState a, DitherState b, float t)
        {
            return new DitherState
            {
                intensity = Mathf.Lerp(a.intensity, b.intensity, t),
                steps = Mathf.Lerp(a.steps, b.steps, t),
                lumaInfluence = Mathf.Lerp(a.lumaInfluence, b.lumaInfluence, t),
                pixelSize = Mathf.Lerp(a.pixelSize, b.pixelSize, t),
                duotone = Mathf.Lerp(a.duotone, b.duotone, t)
            };
        }
    }

    [Tooltip("Material used by the OrderedDither Full Screen Pass renderer feature.")]
    public Material ditherMaterial;

    public DitherState resting = new DitherState { intensity = 1f, steps = 6f, lumaInfluence = 0f, pixelSize = 1f, duotone = 1f };
    public DitherState full = new DitherState { intensity = 1f, steps = 6f, lumaInfluence = 0f, pixelSize = 7f, duotone = 1f };
    public DitherState gameplay = new DitherState { intensity = 0f, steps = 6f, lumaInfluence = 1f, pixelSize = 1f, duotone = 0f };

    [Header("Breathing")]
    public float breathAmount = 0.012f;
    public float breathPeriod = 9f;

    static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    static readonly int StepsId = Shader.PropertyToID("_Steps");
    static readonly int LumaId = Shader.PropertyToID("_LumaInfluence");
    static readonly int PixelId = Shader.PropertyToID("_PixelSize");
    static readonly int DuotoneId = Shader.PropertyToID("_Duotone");
    static readonly int DuoWhiteId = Shader.PropertyToID("_DuoWhite");

    DitherState current;
    Coroutine activeAnim;
    float baseDuoWhite;

    void Awake()
    {
        Instance = this;
        if (ditherMaterial != null) baseDuoWhite = ditherMaterial.GetFloat(DuoWhiteId);
        current = resting;
        Apply(current);
    }

    void Update()
    {
        if (ditherMaterial == null) return;
        if (current.duotone <= 0.5f) return;
        float b = Mathf.Sin(Time.time * 2f * Mathf.PI / breathPeriod) * breathAmount;
        ditherMaterial.SetFloat(DuoWhiteId, baseDuoWhite + b);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (ditherMaterial != null) ditherMaterial.SetFloat(DuoWhiteId, baseDuoWhite);
        Apply(gameplay);
    }

    public void DisableForGameplay()
    {
        if (activeAnim != null) StopCoroutine(activeAnim);
        activeAnim = null;
        current = gameplay;
        Apply(gameplay);
    }

    public void ResolveIn(float duration)
    {
        StartAnim(AnimateTo(full, resting, duration));
    }

    public void Spike(float attack, float hold, float release, float peak = 0.85f)
    {
        StartAnim(SpikeRoutine(attack, hold, release, peak));
    }

    public void DissolveOut(float duration)
    {
        StartAnim(AnimateTo(current, full, duration));
    }

    void StartAnim(IEnumerator routine)
    {
        if (activeAnim != null) StopCoroutine(activeAnim);
        activeAnim = StartCoroutine(routine);
    }

    IEnumerator AnimateTo(DitherState from, DitherState to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            k = 1f - (1f - k) * (1f - k);
            current = DitherState.Lerp(from, to, k);
            Apply(current);
            yield return null;
        }
        current = to;
        Apply(current);
        activeAnim = null;
    }

    IEnumerator SpikeRoutine(float attack, float hold, float release, float peak)
    {
        DitherState start = current;
        DitherState peakState = DitherState.Lerp(resting, full, peak);
        yield return AnimateTo(start, peakState, attack);
        yield return new WaitForSecondsRealtime(hold);
        yield return AnimateTo(peakState, resting, release);
    }

    void Apply(DitherState s)
    {
        if (ditherMaterial == null) return;
        ditherMaterial.SetFloat(IntensityId, s.intensity);
        ditherMaterial.SetFloat(StepsId, s.steps);
        ditherMaterial.SetFloat(LumaId, s.lumaInfluence);
        ditherMaterial.SetFloat(PixelId, s.pixelSize);
        ditherMaterial.SetFloat(DuotoneId, s.duotone);
    }

    DitherState ReadMaterial()
    {
        return new DitherState
        {
            intensity = ditherMaterial.GetFloat(IntensityId),
            steps = ditherMaterial.GetFloat(StepsId),
            lumaInfluence = ditherMaterial.GetFloat(LumaId),
            pixelSize = ditherMaterial.GetFloat(PixelId),
            duotone = ditherMaterial.GetFloat(DuotoneId)
        };
    }
}
