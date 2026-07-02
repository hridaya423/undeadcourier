using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    string newGameScene = "SampleScene";
    public TMP_Text highScoreUI;
    public AudioSource mainmenusource;

    
    public Image fadePanel;
    public float fadeTime = 1.0f;
    private bool isTransitioning = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        mainmenusource.Play();
        int highScore = SaveLoadManager.Instance.LoadHighScore();
        highScoreUI.text = $"Most waves survived: " + highScore;

        
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
        if (!isTransitioning)
        {
            isTransitioning = true;
            StartCoroutine(TransitionToNewGame());
        }
    }

    IEnumerator TransitionToNewGame()
    {
        
        if (fadePanel != null)
        {
            fadePanel.raycastTarget = true;
            yield return StartCoroutine(FadeOut());
        }

        
        mainmenusource.Stop();

        
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

    IEnumerator FadeOut()
    {
        float elapsedTime = 0;
        Color panelColor = fadePanel.color;

        while (elapsedTime < fadeTime)
        {
            panelColor.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeTime);
            fadePanel.color = panelColor;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        
        panelColor.a = 1f;
        fadePanel.color = panelColor;
    }

    public void ExitApplication()
    {
        Application.Quit();
    }
}
    