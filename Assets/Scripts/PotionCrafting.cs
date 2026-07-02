using System.Collections;
using UnityEngine;
using TMPro;

public class TempleInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string interactMessage = "Press E to convert essences to a potion";
    [SerializeField] private string insufficientMessage = "Need 4 essences to craft a potion";

    [Header("Effect Settings")]
    [SerializeField] private GameObject conversionEffect;
    [SerializeField] private AudioClip conversionSound;
    [SerializeField] private float effectDuration = 2f;

    [Header("Optional Path Reference")]
    [SerializeField] private PathToTemple pathToTemple;

    private Transform player;
    private bool isPlayerInRange = false;
    private bool isCoolingDown = false;

    private void Start()
    {
        
        if (GlobalReferences.Instance != null)
        {
            player = GlobalReferences.Instance.player.transform;
        }
        else
        {
            Debug.LogError("GlobalReferences not found!");
        }

        
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        
        if (pathToTemple == null)
        {
            pathToTemple = FindFirstObjectByType<PathToTemple>();
        }
    }

    private void Update()
    {
        if (player == null) return;

        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool wasInRange = isPlayerInRange;
        isPlayerInRange = distanceToPlayer <= interactionDistance;

        
        if (isPlayerInRange != wasInRange)
        {
            UpdatePromptVisibility();
        }

        
        if (isPlayerInRange && Input.GetKeyDown(interactionKey) && !isCoolingDown)
        {
            HandleInteraction();
        }
    }

    private void UpdatePromptVisibility()
    {
        if (interactionPrompt == null) return;

        interactionPrompt.SetActive(isPlayerInRange);

        
        if (promptText != null && GlobalReferences.Instance != null)
        {
            bool hasEnoughEssences = GlobalReferences.Instance.essenceCount >= GlobalReferences.Instance.essencesPerPotion;
            promptText.text = hasEnoughEssences ? interactMessage : insufficientMessage;
        }
    }

    private void HandleInteraction()
    {
        if (GlobalReferences.Instance == null) return;

        
        if (GlobalReferences.Instance.essenceCount >= GlobalReferences.Instance.essencesPerPotion)
        {
            StartCoroutine(PerformConversion());
        }
        else
        {
            
            Debug.Log("Not enough essences to craft a potion");
        }
    }

    private IEnumerator PerformConversion()
    {
        isCoolingDown = true;

        
        if (pathToTemple != null)
        {
            pathToTemple.HidePath();
        }

        
        if (conversionEffect != null)
        {
            GameObject effect = Instantiate(conversionEffect, transform.position + Vector3.up, Quaternion.identity);
            Destroy(effect, effectDuration);
        }

        
        if (conversionSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.playerChannel.PlayOneShot(conversionSound);
        }

        
        yield return new WaitForSeconds(effectDuration * 0.5f);

        
        int essencesToUse = GlobalReferences.Instance.essencesPerPotion;
        GlobalReferences.Instance.essenceCount -= essencesToUse;
        GlobalReferences.Instance.AddPotion(1);

        Debug.Log($"Converted {essencesToUse} essences into 1 potion");

        
        yield return new WaitForSeconds(effectDuration * 0.5f);

        isCoolingDown = false;
        UpdatePromptVisibility();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
