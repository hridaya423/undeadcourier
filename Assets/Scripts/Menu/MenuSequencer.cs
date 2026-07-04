using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSequencer : MonoBehaviour
{
    public Image fadePanel;
    public LogoFlicker logo;
    public MenuButton[] buttons;
    public TMP_Text statText;
    public CanvasGroup footer;
    public string statLabelFormat = "MOST WAVES SURVIVED - {0}";

    void Awake()
    {
        ApplyRuntimeStyling();
    }

    void ApplyRuntimeStyling()
    {
        TMP_FontAsset distressedFont = FindDistressedMenuFont();

        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null || button.label == null) continue;

                if (distressedFont != null) button.label.font = distressedFont;
                button.label.fontSize = 30f;
                button.label.characterSpacing = 2.5f;
                button.hoverOffsetX = 20f;
                button.hoverJitterX = 0.9f;
                button.hoverJitterY = 0.35f;
                button.hoverJitterSpeed = 10f;
                button.hoverLiftY = -2f;
                button.hoverScale = 1.04f;
            }
        }

        if (footer != null)
        {
            var footerTexts = footer.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < footerTexts.Length; i++) footerTexts[i].text = string.Empty;
        }
    }

    TMP_FontAsset FindDistressedMenuFont()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || text.font == null) continue;
            if (text.text.Contains("UNDEAD") || text.text.Contains("COURIER")) return text.font;
        }

        return null;
    }

    public void PlayIntro(int highScore)
    {
        StopAllCoroutines();
        StartCoroutine(IntroRoutine(highScore));
    }

    IEnumerator IntroRoutine(int highScore)
    {
        if (statText != null) statText.text = string.Empty;

        if (fadePanel != null)
        {
            fadePanel.raycastTarget = false;
            Color c = fadePanel.color;
            c.a = 0f;
            fadePanel.color = c;
        }

        if (logo != null) logo.PlayReveal();

        yield return new WaitForSeconds(0.5f);

        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null) buttons[i].Reveal(i * 0.08f);
            }
        }

        yield return new WaitForSeconds(0.3f);
        if (statText != null && highScore > 0) StartCoroutine(CountUp(highScore, 0.8f));

        yield return new WaitForSeconds(0.2f);
        if (footer != null) StartCoroutine(FadeCanvasGroup(footer, 0f, 0.6f, 0.5f));
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            group.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }
        group.alpha = to;
    }

    IEnumerator FadePanel(float from, float to, float duration)
    {
        if (fadePanel == null) yield break;
        Color c = fadePanel.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            c.a = Mathf.Lerp(from, to, k);
            fadePanel.color = c;
            yield return null;
        }
        c.a = to;
        fadePanel.color = c;
    }

    IEnumerator CountUp(int target, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            int value = Mathf.RoundToInt(Mathf.Lerp(0, target, k));
            statText.text = string.Format(statLabelFormat, value);
            yield return null;
        }
        statText.text = string.Format(statLabelFormat, target);
    }

    public IEnumerator TransitionOut()
    {
        if (fadePanel != null) fadePanel.raycastTarget = true;
        if (logo != null && logo.secondaryGroup != null)
            StartCoroutine(FadeCanvasGroup(logo.secondaryGroup, logo.secondaryGroup.alpha, 0f, 0.45f));
        yield return FadePanel(fadePanel != null ? fadePanel.color.a : 0f, 1f, 0.45f);
    }
}
