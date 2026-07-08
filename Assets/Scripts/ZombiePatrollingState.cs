using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class ZombiePatrollingState : StateMachineBehaviour
{
    float timer;
    public float patrollingTime = 0f;

    Transform player;
    NavMeshAgent navAgent;

    public float detectionArea = 200f;
    public float patrolSpeed = 2f;

    List<Transform> waypointsList = new List<Transform>();
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
        navAgent = animator.GetComponent<NavMeshAgent>();
        if (player == null || navAgent == null) return;

        navAgent.speed = patrolSpeed;
        timer = 0;
        waypointsList.Clear();

        GameObject waypointCluster = GameObject.FindGameObjectWithTag("Waypoints");
        if (waypointCluster == null) return;
        foreach (Transform t in waypointCluster.transform)
        {
            waypointsList.Add(t);
        }
        if (waypointsList.Count == 0) return;
        Vector3 nextPosition = waypointsList[Random.Range(0, waypointsList.Count)].position;
        navAgent.SetDestination(nextPosition);
    }
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        if (player == null || navAgent == null || waypointsList.Count == 0) return;

        if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel.isPlaying == false)
        {
            SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.zombieWalking);
            SoundManager.Instance.zombieChannel.PlayDelayed(1f);
        }
        if (navAgent.remainingDistance < navAgent.stoppingDistance)
        {
            navAgent.SetDestination(waypointsList[Random.Range(0, waypointsList.Count)].position);
        }

        timer += Time.deltaTime;
        if (timer > patrollingTime)
        {
            animator.SetBool("isPatrolling", false);
        }

        float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);
        if (distanceFromPlayer < detectionArea * PlayerDetectionMultiplier() || HeardRecentNoise(animator.transform.position))
        {
            animator.SetBool("isChasing", true);
        }
    }
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (navAgent != null)
        {
            navAgent.SetDestination(navAgent.transform.position);
        }
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel.Stop();
        }
    }

    float PlayerDetectionMultiplier()
    {
        float multiplier = 1f;
        if (PlayerFlashlight.Active != null) multiplier *= PlayerFlashlight.Active.IsOn ? 1.35f : 0.65f;
        if (PlayerMovement.Active != null && PlayerMovement.Active.IsSprinting) multiplier *= 1.2f;
        return multiplier;
    }

    bool HeardRecentNoise(Vector3 position)
    {
        return Time.time - GameEvents.LastNoiseTime < 1.4f && Vector3.Distance(position, GameEvents.LastNoisePosition) <= GameEvents.LastNoiseRadius;
    }
}
