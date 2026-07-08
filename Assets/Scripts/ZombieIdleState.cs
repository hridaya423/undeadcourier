using UnityEngine;

public class ZombieIdleState : StateMachineBehaviour
{

    float timer;
    public float idleTime = 0f;

    Transform player;

    public float detectionAreaRadius = 200f;


    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer = 0;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;

    }
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer += Time.deltaTime;
        if (timer > idleTime)
        {
            animator.SetBool("isPatrolling", true);
        }

        if (player == null) return;

        float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);
        if (distanceFromPlayer < detectionAreaRadius * PlayerDetectionMultiplier() || HeardRecentNoise(animator.transform.position))
        {
            animator.SetBool("isChasing", true);
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
