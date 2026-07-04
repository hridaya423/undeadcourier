using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public TMP_Text label;
    public Color hoverLabelColor = Color.white;
    public RectTransform underline;
    public Color normalColor = Color.white;
    public Color accentColor = Color.red;
    public float hoverOffsetX = 12f;
    public AudioSource audioSource;
    public AudioClip confirmClip;
    public float waveCharDelay = 0.018f;
    public float waveCharRise = 0.06f;
    public float waveHold = 0.2f;
    public float waveFade = 0.25f;
    public float hoverJitterX = 1.5f;
    public float hoverJitterY = 0.6f;
    public float hoverJitterSpeed = 18f;
    public float hoverLiftY = -2f;
    public float hoverScale = 1.035f;

    const float DampSpeed = 12f;

    RectTransform rectTransform;
    CanvasGroup canvasGroup;
    Vector2 restAnchoredPos;
    bool hovered;
    bool pressed;
    bool selected;
    bool revealed;
    bool revealStarted;
    bool revealing;
    bool waving;
    Coroutine waveRoutine;

    float labelColorT;
    float offsetT;
    float underlineT;
    float scaleT;
    float hoverScaleT;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        restAnchoredPos = rectTransform.anchoredPosition;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (underline != null)
        {
            Vector3 s = underline.localScale;
            s.x = 0f;
            underline.localScale = s;
        }

        if (label != null) label.color = normalColor;
    }

    void Start()
    {
        if (!revealStarted) StartCoroutine(FallbackRevealAfter(2f));
    }

    IEnumerator FallbackRevealAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!revealed) Reveal(0f);
    }

    void Update()
    {
        bool active = hovered || selected;
        float targetLabelT = active ? 1f : 0f;
        float targetOffsetT = active ? 1f : 0f;
        float targetUnderlineT = active ? 1f : 0f;
        float targetScaleT = pressed ? 1f : 0f;
        float targetHoverScaleT = active ? 1f : 0f;

        float k = 1f - Mathf.Exp(-DampSpeed * Time.unscaledDeltaTime);
        labelColorT = Mathf.Lerp(labelColorT, targetLabelT, k);
        offsetT = Mathf.Lerp(offsetT, targetOffsetT, k);
        underlineT = Mathf.Lerp(underlineT, targetUnderlineT, k);
        scaleT = Mathf.Lerp(scaleT, targetScaleT, k);
        hoverScaleT = Mathf.Lerp(hoverScaleT, targetHoverScaleT, k);

        if (label != null && !waving) label.color = Color.Lerp(normalColor, hoverLabelColor, labelColorT);
        if (underline != null)
        {
            Vector3 s = underline.localScale;
            s.x = underlineT;
            underline.localScale = s;
        }

        if (!revealing)
        {
            Vector2 pos = rectTransform.anchoredPosition;
            float jitterX = 0f;
            float jitterY = 0f;
            if (active)
            {
                float time = Time.unscaledTime * hoverJitterSpeed + restAnchoredPos.y * 0.013f;
                jitterX = Mathf.Sin(time) * hoverJitterX;
                jitterY = Mathf.Cos(time * 0.8f) * hoverJitterY;
            }

            pos.x = restAnchoredPos.x + hoverOffsetX * offsetT + jitterX * offsetT;
            pos.y = restAnchoredPos.y + hoverLiftY * offsetT + jitterY * offsetT;
            rectTransform.anchoredPosition = pos;
        }

        float scale = Mathf.Lerp(1f, hoverScale, hoverScaleT);
        scale = Mathf.Lerp(scale, 0.97f, scaleT);
        rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
        if (label != null)
        {
            if (waveRoutine != null) StopCoroutine(waveRoutine);
            waveRoutine = StartCoroutine(WaveRoutine());
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        PlayClip(confirmClip, false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
    }

    IEnumerator WaveRoutine()
    {
        waving = true;
        label.ForceMeshUpdate();
        var ti = label.textInfo;

        float sweepDuration = ti.characterCount * waveCharDelay + waveCharRise;
        float t = 0f;
        while (t < sweepDuration)
        {
            t += Time.unscaledDeltaTime;
            for (int c = 0; c < ti.characterCount; c++)
            {
                float on = Mathf.Clamp01((t - c * waveCharDelay) / waveCharRise);
                SetCharColor(ti, c, Color.Lerp(hoverLabelColor, accentColor, on));
            }
            label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            yield return null;
        }

        for (int c = 0; c < ti.characterCount; c++)
            SetCharColor(ti, c, accentColor);
        label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        yield return new WaitForSecondsRealtime(waveHold);

        t = 0f;
        while (t < waveFade)
        {
            t += Time.unscaledDeltaTime;
            Color32 c32 = Color.Lerp(accentColor, hoverLabelColor, Mathf.Clamp01(t / waveFade));
            for (int c = 0; c < ti.characterCount; c++)
                SetCharColor(ti, c, c32);
            label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            yield return null;
        }

        waving = false;
    }

    static void SetCharColor(TMP_TextInfo ti, int charIndex, Color32 color)
    {
        var charInfo = ti.characterInfo[charIndex];
        if (!charInfo.isVisible) return;
        var colors = ti.meshInfo[charInfo.materialReferenceIndex].colors32;
        int v = charInfo.vertexIndex;
        colors[v] = colors[v + 1] = colors[v + 2] = colors[v + 3] = color;
    }

    void PlayClip(AudioClip clip, bool randomizePitch)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = randomizePitch ? Random.Range(0.95f, 1.05f) : 1f;
        audioSource.PlayOneShot(clip);
    }

    public void Reveal(float delay)
    {
        revealStarted = true;
        StopAllCoroutines();
        waving = false;
        waveRoutine = null;
        StartCoroutine(RevealRoutine(delay));
    }

    IEnumerator RevealRoutine(float delay)
    {
        if (canvasGroup == null)
        {
            revealed = true;
            yield break;
        }

        if (delay > 0f) yield return new WaitForSeconds(delay);

        revealing = true;
        Vector2 startPos = restAnchoredPos + new Vector2(-24f, 0f);
        Vector2 endPos = restAnchoredPos;
        canvasGroup.alpha = 0f;
        rectTransform.anchoredPosition = startPos;

        float duration = 0.25f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            k = 1f - (1f - k) * (1f - k);
            canvasGroup.alpha = k;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, k);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        rectTransform.anchoredPosition = endPos;
        revealing = false;
        revealed = true;
    }
}
