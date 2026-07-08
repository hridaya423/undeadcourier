using System;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAttackState : StateMachineBehaviour
{
    Transform player;
    NavMeshAgent agent;
    float nextDamageTime;

    public float stopAttackingDistance = 2.5f;
    public float damageInterval = 1.1f;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
        agent = animator.GetComponent<NavMeshAgent>();
        nextDamageTime = 0f;
    }
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null || agent == null) return;

        if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel.isPlaying == false)
        {
            SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.zombieAttacking);
        }

        LookAtPlayer();

        float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);

        if (distanceFromPlayer > stopAttackingDistance)
        {
            animator.SetBool("isAttacking", false);
        }

        if (distanceFromPlayer <= stopAttackingDistance && Time.time >= nextDamageTime)
        {
            nextDamageTime = Time.time + damageInterval;
            Player playerHealth = player.GetComponent<Player>();
            Zombie zombie = animator.GetComponent<Zombie>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(zombie != null && zombie.zombieDamage > 0 ? zombie.zombieDamage : 18);
            }
        }
    }

    private void LookAtPlayer()
    {
        Vector3 direction = player.position - agent.transform.position;
        agent.transform.rotation = Quaternion.LookRotation(direction);

        var yRotation = agent.transform.eulerAngles.y;
        agent.transform.rotation = Quaternion.Euler(0, yRotation, 0);

    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel.Stop();
        }
    }
}
