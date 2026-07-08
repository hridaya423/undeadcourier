using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class Player : MonoBehaviour
{
    public static Player Active { get; private set; }

    public int HP = 100;
    public GameObject bloodyScreen;
    public TextMeshProUGUI HPText;
    public GameObject gameOverText;
    public bool isDead;
    private bool isInvulnerable = false;
    private bool playedLowHealthStinger;
    private int maxHP;

    public float Health01 => maxHP > 0 ? Mathf.Clamp01((float)HP / maxHP) : 1f;

    void Awake()
    {
        Active = this;
        maxHP = HP;
    }

    void OnDestroy()
    {
        if (Active == this) Active = null;
    }

    private void Start()
    {
        if (HPText != null)
        {
            HPText.text = $"{HP}";
        }
    }

    private void OnEnable()
    {
        
        var scientists = FindObjectsByType<ScientistNPC>();
        foreach (var scientist in scientists)
        {
            scientist.OnWorldSaved += SetInvulnerable;
        }
    }

    private void OnDisable()
    {
        
        var scientists = FindObjectsByType<ScientistNPC>();
        foreach (var scientist in scientists)
        {
            scientist.OnWorldSaved -= SetInvulnerable;
        }
    }

    private void SetInvulnerable()
    {
        isInvulnerable = true;
    }

    public void TakeDamage(int damageAmount)
    {
        
        if (isInvulnerable || isDead) return;

        HP -= damageAmount;
        if (HP <= 0)
        {
            isDead = true;
            PlayerDead();
        }
        else
        {
            if (bloodyScreen != null)
            {
                StartCoroutine(BloodyScreenEffect());
            }
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.playerChannel.PlayOneShot(SoundManager.Instance.playerHurt);
                SoundManager.Instance.SetLowHealthHeartbeat(HP <= 45);
                if (HP <= 45 && !playedLowHealthStinger)
                {
                    playedLowHealthStinger = true;
                    SoundManager.PlayRandom(SoundManager.Instance.playerChannel, SoundManager.Instance.uiStingers, 0.45f, 0.95f, 1.02f);
                }
            }
            if (HPText != null)
            {
                HPText.text = $"{HP}";
            }
        }
    }

    private void PlayerDead()
    {
        if (TryGetComponent(out MouseMovement mouseMovement)) mouseMovement.enabled = false;
        if (TryGetComponent(out PlayerMovement playerMovement)) playerMovement.enabled = false;
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetLowHealthHeartbeat(false);
            SoundManager.Instance.playerChannel.PlayOneShot(SoundManager.Instance.playerDeath);
            SoundManager.Instance.playerChannel.clip = SoundManager.Instance.gameOversfx;
            SoundManager.Instance.playerChannel.PlayDelayed(2f);
        }
        Animator playerAnimator = GetComponentInChildren<Animator>();
        if (playerAnimator != null) playerAnimator.enabled = true;
        if (HPText != null) HPText.gameObject.SetActive(false);
        if (TryGetComponent(out ScreenFader screenFader)) screenFader.StartFade();
        StartCoroutine(ShowGameOverText());
    }

    private IEnumerator ShowGameOverText()
    {
        yield return new WaitForSeconds(1f);
        if (gameOverText != null) gameOverText.gameObject.SetActive(true);
        int waveSurvived = GlobalReferences.Instance != null ? GlobalReferences.Instance.waveNumber : 1;
        int zombiesKilled = GlobalReferences.Instance != null ? GlobalReferences.Instance.zombiesKilled : 0;
        
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveScore(waveSurvived - 1, zombiesKilled);
        }
        StartCoroutine(ReturnToMainMenu());
    }

    private IEnumerator ReturnToMainMenu()
    {
        yield return new WaitForSeconds(2f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator BloodyScreenEffect()
    {
        if (bloodyScreen == null) yield break;

        if (bloodyScreen.activeInHierarchy == false)
        {
            bloodyScreen.SetActive(true);
        }
        var image = bloodyScreen.GetComponentInChildren<Image>();
        if (image == null) yield break;
        
        Color startColor = image.color;
        startColor.a = 1f;
        image.color = startColor;
        float duration = 1.5f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            
            Color newColor = image.color;
            newColor.a = alpha;
            image.color = newColor;
            
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        if (bloodyScreen.activeInHierarchy)
        {
            bloodyScreen.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("ZombieHand"))
        {
            if (!isDead && !isInvulnerable && other.gameObject.TryGetComponent(out ZombieHand zombieHand))
            {
                TakeDamage(zombieHand.damage);
            }
        }
    }
}
