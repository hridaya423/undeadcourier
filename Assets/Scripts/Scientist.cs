using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;

public class ScientistNPC : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkRadius = 10f;
    [SerializeField] private float minWalkDelay = 3f;
    [SerializeField] private float maxWalkDelay = 8f;
    [SerializeField] private float stoppingDistance = 0.5f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private GameObject interactionPrompt;

    [Header("References")]
    [SerializeField] private AudioClip receiveSound;
    [SerializeField] private AudioClip worldSavedSound;
    [SerializeField] private ParticleSystem potionEffect;

    [Header("World Saved Cutscene")]
    [SerializeField] private GameObject worldSavedOverlay;
    [SerializeField] private TextMeshProUGUI worldSavedText;
    [SerializeField] private float cutsceneDuration = 4f;

    private NavMeshAgent navAgent;
    private Animator animator;
    private Transform playerTransform;
    private bool isMoving = false;
    private Vector3 startPosition;
    private bool worldSavingInProgress = false;
    private bool hasPotionBeenGiven = false;  

    
    private readonly string walkAnimParam = "isWalking";
    private readonly string idleAnimParam = "isIdle";
    private readonly string receiveAnimParam = "receivingItem";

    
    public event Action OnPotionGiven;
    public event Action OnWorldSaved;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        startPosition = transform.position;

        
        navAgent.stoppingDistance = stoppingDistance;
        navAgent.autoBraking = true;

        
        if (worldSavedOverlay != null)
        {
            worldSavedOverlay.SetActive(false);
        }
    }

    private void Start()
    {
        
        if (GlobalReferences.Instance != null && GlobalReferences.Instance.player != null)
        {
            playerTransform = GlobalReferences.Instance.player.transform;
        }
        else
        {
            Debug.LogError("No player reference found in GlobalReferences!");
        }

        
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        
        StartCoroutine(IdleBehavior());
    }

    private void Update()
    {
        
        if (worldSavingInProgress) return;

        
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            bool isInRange = distanceToPlayer <= interactionDistance;

            
            bool isFacingScientist = false;
            if (isInRange)
            {
                Vector3 directionToScientist = (transform.position - playerTransform.position).normalized;
                isFacingScientist = Vector3.Dot(playerTransform.forward, directionToScientist) > 0.7f;
            }

            
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(isInRange && isFacingScientist && !hasPotionBeenGiven);
            }

            
            if (isInRange && isFacingScientist && Input.GetKeyDown(KeyCode.I) && !hasPotionBeenGiven)
            {
                AttemptReceivePotion();
            }
        }

        
        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        
        bool hasReachedDestination = !navAgent.pathPending &&
                                    (navAgent.remainingDistance <= navAgent.stoppingDistance) &&
                                    (!navAgent.hasPath || navAgent.velocity.sqrMagnitude < 0.1f);

        
        animator.SetBool(walkAnimParam, isMoving && !hasReachedDestination);
        animator.SetBool(idleAnimParam, !isMoving || hasReachedDestination);
    }

    private IEnumerator IdleBehavior()
    {
        while (true)
        {
            
            if (worldSavingInProgress)
            {
                yield return null;
                continue;
            }

            
            yield return new WaitForSeconds(Random.Range(minWalkDelay, maxWalkDelay));

            
            Vector3 randomDirection = Random.insideUnitSphere * walkRadius;
            randomDirection += startPosition;

            
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, walkRadius, NavMesh.AllAreas))
            {
                isMoving = true;
                navAgent.SetDestination(hit.position);

                
                while (!navAgent.pathPending && navAgent.remainingDistance > navAgent.stoppingDistance)
                {
                    
                    if (worldSavingInProgress) break;
                    yield return null;
                }

                isMoving = false;

                
                yield return new WaitForSeconds(Random.Range(1f, 3f));
            }
        }
    }

    private void AttemptReceivePotion()
    {
        
        if (worldSavingInProgress || hasPotionBeenGiven) return;

        
        navAgent.isStopped = true;
        isMoving = false;

        
        if (playerTransform != null)
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            directionToPlayer.y = 0; 
            transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        
        if (GlobalReferences.Instance != null && GlobalReferences.Instance.potionCount > 0)
        {
            
            hasPotionBeenGiven = true;

            
            GlobalReferences.Instance.AddPotion(-1); 

            
            if (animator != null)
            {
                animator.SetTrigger(receiveAnimParam);
            }

            
            if (receiveSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.playerChannel.PlayOneShot(receiveSound);
            }

            
            if (potionEffect != null)
            {
                potionEffect.Play();
            }

            
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.IncrementWorldsSaved();
            }

            
            OnPotionGiven?.Invoke();

            
            OnWorldSaved?.Invoke();

            
            StartCoroutine(ShowWorldSavedCutscene());
        }
        else
        {
            
            StartCoroutine(ResumeMovementAfterDelay(1.5f));
        }
    }

    private IEnumerator ShowWorldSavedCutscene()
    {
        
        worldSavingInProgress = true;

        
        Player playerComponent = null;
        MouseMovement mouseMovement = null;
        PlayerMovement playerMovement = null;

        if (GlobalReferences.Instance != null && GlobalReferences.Instance.player != null)
        {
            
            var player = GlobalReferences.Instance.player;
            playerComponent = player.GetComponent<Player>();
            mouseMovement = player.GetComponent<MouseMovement>();
            playerMovement = player.GetComponent<PlayerMovement>();

            if (mouseMovement != null) mouseMovement.enabled = false;
            if (playerMovement != null) playerMovement.enabled = false;

            
            if (playerComponent != null)
            {
                playerComponent.enabled = false;
            }
        }

        
        if (worldSavedSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.playerChannel.PlayOneShot(worldSavedSound);
        }

        
        if (worldSavedOverlay != null)
        {
            worldSavedOverlay.SetActive(true);

            
            if (worldSavedText != null)
            {
                worldSavedText.text = "WORLD SAVED!!!";
            }

            
            var image = worldSavedOverlay.GetComponent<Image>();
            if (image != null)
            {
                
                Color color = image.color;
                color.a = 0f;
                image.color = color;

                float fadeInDuration = 0.5f;
                float elapsedTime = 0f;

                
                while (elapsedTime < fadeInDuration)
                {
                    float alpha = Mathf.Lerp(0f, 0.8f, elapsedTime / fadeInDuration);
                    Color newColor = image.color;
                    newColor.a = alpha;
                    image.color = newColor;
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                
                color = image.color;
                color.a = 0.8f;
                image.color = color;

                
                yield return new WaitForSeconds(cutsceneDuration);

                
                elapsedTime = 0f;
                float fadeOutDuration = 1.0f;

                while (elapsedTime < fadeOutDuration)
                {
                    float alpha = Mathf.Lerp(0.8f, 0f, elapsedTime / fadeOutDuration); 
                    Color newColor = image.color;
                    newColor.a = alpha;
                    image.color = newColor;
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                
                color = image.color;
                color.a = 0f;
                image.color = color;
            }
            else
            {
                
                yield return new WaitForSeconds(cutsceneDuration);
            }

            
            worldSavedOverlay.SetActive(false);
        }
        else
        {
            
            yield return new WaitForSeconds(2f);
        }

        
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator ResumeMovementAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        navAgent.isStopped = false;
    }

    private void OnDrawGizmosSelected()
    {
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, walkRadius);

        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
