using System.Collections;
using UnityEngine;

public class LogoFlicker : MonoBehaviour
{
    public CanvasGroup group;
    public CanvasGroup secondaryGroup;
    public RectTransform jitterTarget;
    public AudioSource audioSource;
    public AudioClip buzzClip;
    public RectTransform rule;
    public float glitchIntervalMin = 8f;
    public float glitchIntervalMax = 14f;

    Vector2 jitterRestPos;
    Coroutine loopRoutine;

    void Awake()
    {
        SetAlpha(0f);
        if (jitterTarget != null) jitterRestPos = jitterTarget.anchoredPosition;
        if (rule != null) rule.localScale = new Vector3(0f, rule.localScale.y, rule.localScale.z);
    }

    public void PlayReveal()
    {
        StopAllCoroutines();
        StartCoroutine(RevealRoutine());
    }

    IEnumerator RevealRoutine()
    {
        if (group == null) yield break;

        if (audioSource != null && buzzClip != null) audioSource.PlayOneShot(buzzClip);

        yield return SetAlphaFor(0f, 0.02f);
        yield return SetAlphaFor(1f, 0.05f);
        yield return SetAlphaFor(0.1f, 0.06f);
        yield return SetAlphaFor(1f, Random.Range(0.05f, 0.09f));
        yield return SetAlphaFor(0.25f, 0.08f);
        yield return SetAlphaFor(1f, Random.Range(0.08f, 0.12f));
        yield return SetAlphaFor(0.6f, 0.05f);
        yield return SetAlphaFor(1f, 0.4f);

        SetAlpha(1f);

        if (rule != null) StartCoroutine(RuleDrawIn());

        loopRoutine = StartCoroutine(GlitchLoop());
    }

    void SetAlpha(float alpha)
    {
        if (group != null) group.alpha = alpha;
        if (secondaryGroup != null) secondaryGroup.alpha = alpha;
    }

    IEnumerator SetAlphaFor(float alpha, float duration)
    {
        SetAlpha(alpha);
        yield return new WaitForSeconds(duration);
    }

    IEnumerator RuleDrawIn()
    {
        float duration = 0.45f;
        float t = 0f;
        Vector3 scale = rule.localScale;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            k = 1f - Mathf.Pow(1f - k, 3f);
            rule.localScale = new Vector3(k, scale.y, scale.z);
            yield return null;
        }
        rule.localScale = new Vector3(1f, scale.y, scale.z);
    }

    IEnumerator GlitchLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(glitchIntervalMin, glitchIntervalMax));
            yield return Glitch();
        }
    }

    IEnumerator Glitch()
    {
        float duration = 0.08f;
        float t = 0f;
        Vector2 jitterOffset = jitterTarget != null
            ? new Vector2(Random.Range(-3f, 3f), 0f)
            : Vector2.zero;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            float dip = Mathf.Sin(k * Mathf.PI);
            SetAlpha(Mathf.Lerp(1f, 0.75f, dip));
            if (jitterTarget != null) jitterTarget.anchoredPosition = jitterRestPos + jitterOffset * dip;
            yield return null;
        }

        SetAlpha(1f);
        if (jitterTarget != null) jitterTarget.anchoredPosition = jitterRestPos;
    }
}
