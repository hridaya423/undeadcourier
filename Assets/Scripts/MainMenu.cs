using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    string newGameScene = "SampleScene";
    public TMP_Text highScoreUI;

    
    public Image fadePanel;
    public float fadeTime = 1.0f;
    public MenuSequencer sequencer;
    private bool isTransitioning = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        int highScore = SaveLoadManager.Instance != null ? SaveLoadManager.Instance.LoadHighScore() : 0;

        if (sequencer != null)
        {
            sequencer.PlayIntro(highScore);
            return;
        }

        if (highScoreUI != null) highScoreUI.text = $"Most waves survived: " + highScore;

        if (fadePanel != null)
        {

            Color startColor = fadePanel.color;
            startColor.a = 1f;
            fadePanel.color = startColor;


            fadePanel.raycastTarget = false;

            StartCoroutine(FadeIn());
        }
    }

    public void StartNewGame()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        DitherController.Instance?.DisableForGameplay();
        SceneManager.LoadScene(newGameScene);
    }

    IEnumerator FadeIn()
    {
        float elapsedTime = 0;
        Color panelColor = fadePanel.color;

        while (elapsedTime < fadeTime)
        {
            panelColor.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
            fadePanel.color = panelColor;
            elapsedTime += Time.deltaTime;
            yield return null;
        }


        panelColor.a = 0f;
        fadePanel.color = panelColor;
    }

    public void ExitApplication()
    {
#if !UNITY_WEBGL
        Application.Quit();
#endif
    }
}
    
