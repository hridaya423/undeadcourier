using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] internal int HP = 100;
    private Animator animator;
    private NavMeshAgent navAgent;
    private float bloodLossMl;
    private Vector3 lastHitPoint;
    private Vector3 lastHitDirection = Vector3.back;
    private Collider lastHitCollider;
    private bool lastHitWasHeadshot;
    internal bool isDead;

    private void Start()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        SetupHeadHitbox();
    }

    private void SetupHeadHitbox()
    {
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return;

        Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        if (headBone == null) return;

        if (headBone.GetComponent<HeadHitbox>() != null) return;

        SphereCollider headCollider = headBone.GetComponent<SphereCollider>();
        if (headCollider == null) headCollider = headBone.gameObject.AddComponent<SphereCollider>();
        headCollider.radius = 0.14f;
        headCollider.isTrigger = false;
        headBone.gameObject.layer = gameObject.layer;

        HeadHitbox headHitbox = headBone.gameObject.AddComponent<HeadHitbox>();
        headHitbox.owner = this;
    }

    public virtual void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        HP -= damageAmount;

        if (HP <= 0)
        {
            if (!isDead) 
            {
                isDead = true;
                GlobalReferences.Instance?.IncrementZombiesKilled();
                GameEvents.RaiseEnemyDied(this);

                if (navAgent != null) navAgent.enabled = false;
                if (TryGetComponent(out CapsuleCollider capsule)) capsule.enabled = false;

                ZombieRagdoll ragdoll = GetComponent<ZombieRagdoll>();
                bool didRagdoll = ragdoll != null && ragdoll.EnableRagdoll(lastHitPoint, lastHitDirection, lastHitWasHeadshot, lastHitCollider);

                if (!didRagdoll)
                {
                    int randomValue = Random.Range(0, 2);
                    if (animator != null) animator.SetTrigger(randomValue == 0 ? "DIE1" : "DIE2");
                }

                if (SoundManager.Instance != null)
                    SoundManager.Instance.zombieChannel1.PlayOneShot(SoundManager.Instance.zombieDeath);

                HitstopController.RequestHitstop(Random.Range(0.06f, 0.08f));
                SpawnDeathPool();
                CorpseRegistry.Register(gameObject);
                StartCoroutine(DespawnAfterDelay(15f));
            }
        }
        else
        {
            if (animator != null) animator.SetTrigger("DAMAGE");
            if (SoundManager.Instance != null)
                SoundManager.Instance.zombieChannel1.PlayOneShot(SoundManager.Instance.zombieHurt);
        }
    }

    public float RegisterBloodHit(int damageAmount, bool headshot, Weapon.WeaponModel weaponModel)
    {
        if (isDead) return 0f;

        float baseLoss;
        switch (weaponModel)
        {
            case Weapon.WeaponModel.AK74:
                baseLoss = 720f;
                break;
            case Weapon.WeaponModel.Shotgun:
                baseLoss = 140f;
                break;
            case Weapon.WeaponModel.Uzi:
                baseLoss = 220f;
                break;
            default:
                baseLoss = 320f;
                break;
        }

        float damageScale = Mathf.Clamp(damageAmount / 22f, 0.65f, 2.4f);
        float loss = baseLoss * damageScale * (headshot ? 1.75f : 1f) * Random.Range(0.8f, 1.25f);
        bloodLossMl = Mathf.Min(5000f, bloodLossMl + loss);
        return loss;
    }

    public void RegisterHitContext(Vector3 hitPoint, Vector3 hitDirection, Collider hitCollider, bool headshot)
    {
        lastHitPoint = hitPoint;
        lastHitCollider = hitCollider;
        lastHitWasHeadshot = headshot;
        if (hitDirection.sqrMagnitude > 0.0001f) lastHitDirection = hitDirection.normalized;
    }

    private void SpawnDeathPool()
    {
        Vector3 origin = transform.position + Vector3.up * 0.4f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
        RaycastHit? best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit candidate = hits[i];
            if (candidate.collider.GetComponentInParent<Enemy>() != null) continue;
            if (candidate.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (candidate.normal.y < 0.72f) continue;
            if (candidate.distance < bestDistance)
            {
                bestDistance = candidate.distance;
                best = candidate;
            }
        }

        if (best.HasValue)
        {
            BloodDecalPool.SpawnDeathPool(best.Value.point, best.Value.normal, bloodLossMl);
        }
        else
        {
            BloodDecalPool.SpawnDeathPool(transform.position, Vector3.up, bloodLossMl);
        }
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CorpseRegistry.Unregister(gameObject);
        Destroy(gameObject);
    }
}
