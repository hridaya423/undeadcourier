using System;
using UnityEngine;
using UnityEngine.AI;

public class ZombieRagdoll : MonoBehaviour
{
    enum HitZone
    {
        Head,
        Torso,
        Hips,
        Legs,
        Arms
    }

    [Serializable]
    public struct Part
    {
        public HumanBodyBones bone;
        public Rigidbody body;
        public Collider collider;
    }

    [SerializeField] Part[] parts;
    [SerializeField] float bodyImpulse = 4.5f;
    [SerializeField] float headImpulse = 6f;

    Animator animator;
    NavMeshAgent navAgent;
    CapsuleCollider rootCollider;
    Collider[] handColliders;
    Vector3[] previousPositions;
    Quaternion[] previousRotations;
    Vector3[] cachedLinearVelocities;
    Vector3[] cachedAngularVelocities;
    bool active;

    public bool IsReady => parts != null && parts.Length > 0;

    void Awake()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        rootCollider = GetComponent<CapsuleCollider>();
        CacheHandColliders();
        IgnoreSelfCollisions();
        SetAliveState();
    }

    public void SetParts(Part[] configuredParts)
    {
        parts = configuredParts;
        AllocateCaches();
    }

    public void SetAliveState()
    {
        active = false;

        if (animator != null) animator.enabled = true;
        if (rootCollider != null) rootCollider.enabled = true;

        SetHandColliders(false);
        SetPartPhysics(true, false);
        CapturePose(true);
    }

    public bool EnableRagdoll(Vector3 hitPoint, Vector3 hitDirection, bool headshot, Collider hitCollider)
    {
        if (!IsReady || active) return false;

        active = true;

        if (navAgent != null) navAgent.enabled = false;
        if (rootCollider != null) rootCollider.enabled = false;
        SetHandColliders(false);
        if (animator != null) animator.enabled = false;

        SetPartPhysics(false, true);
        ApplyCachedMotion();
        ApplyImpulse(hitPoint, hitDirection, headshot, hitCollider);
        return true;
    }

    void LateUpdate()
    {
        if (!active) CapturePose(false);
    }

    void SetPartPhysics(bool kinematic, bool collisions)
    {
        if (parts == null) return;

        for (int i = 0; i < parts.Length; i++)
        {
            Rigidbody body = parts[i].body;
            Collider collider = parts[i].collider;

            if (body != null)
            {
                body.isKinematic = kinematic;
                body.detectCollisions = collisions;
            }

            if (collider != null) collider.enabled = collisions;
        }
    }

    void IgnoreSelfCollisions()
    {
        if (parts == null) return;

        for (int i = 0; i < parts.Length; i++)
        {
            Rigidbody body = parts[i].body;
            Collider collider = parts[i].collider;
            if (body == null || collider == null) continue;

            CharacterJoint joint = body.GetComponent<CharacterJoint>();
            if (joint == null || joint.connectedBody == null) continue;

            Collider connectedCollider = FindCollider(joint.connectedBody);
            if (connectedCollider != null) Physics.IgnoreCollision(collider, connectedCollider, true);
        }
    }

    Collider FindCollider(Rigidbody body)
    {
        if (parts == null) return null;

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].body == body) return parts[i].collider;
        }

        return null;
    }

    void ApplyImpulse(Vector3 hitPoint, Vector3 hitDirection, bool headshot, Collider hitCollider)
    {
        Rigidbody target = ResolveTargetBody(hitPoint, hitCollider, headshot);
        if (target == null) return;

        Vector3 direction = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : -transform.forward;
        HitZone zone = ZoneFor(target, headshot);
        float impulse = (headshot ? headImpulse : bodyImpulse) * ImpulseMultiplier(zone);
        target.AddForceAtPosition(direction * impulse, hitPoint, ForceMode.Impulse);

        Rigidbody hips = FindBody(HumanBodyBones.Hips);
        Rigidbody chest = FindBody(HumanBodyBones.Chest);

        switch (zone)
        {
            case HitZone.Head:
                if (chest != null && chest != target) chest.AddForce(direction * impulse * 0.14f, ForceMode.Impulse);
                break;
            case HitZone.Torso:
                break;
            case HitZone.Hips:
                break;
            case HitZone.Legs:
                if (hips != null && hips != target) hips.AddForce(direction * impulse * 0.18f, ForceMode.Impulse);
                break;
            case HitZone.Arms:
                break;
        }
    }

    void AllocateCaches()
    {
        int count = parts != null ? parts.Length : 0;
        previousPositions = new Vector3[count];
        previousRotations = new Quaternion[count];
        cachedLinearVelocities = new Vector3[count];
        cachedAngularVelocities = new Vector3[count];
    }

    void CapturePose(bool force)
    {
        if (parts == null) return;
        if (previousPositions == null || previousPositions.Length != parts.Length) AllocateCaches();

        float dt = Time.deltaTime;
        for (int i = 0; i < parts.Length; i++)
        {
            Rigidbody body = parts[i].body;
            if (body == null) continue;

            Vector3 position = body.worldCenterOfMass;
            Quaternion rotation = body.rotation;

            if (!force && dt > 0.0001f)
            {
                cachedLinearVelocities[i] = (position - previousPositions[i]) / dt;

                Quaternion delta = rotation * Quaternion.Inverse(previousRotations[i]);
                delta.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f) angle -= 360f;
                cachedAngularVelocities[i] = axis.sqrMagnitude > 0.0001f ? axis.normalized * angle * Mathf.Deg2Rad / dt : Vector3.zero;
            }

            previousPositions[i] = position;
            previousRotations[i] = rotation;
        }
    }

    void ApplyCachedMotion()
    {
        if (parts == null || cachedLinearVelocities == null) return;

        for (int i = 0; i < parts.Length; i++)
        {
            Rigidbody body = parts[i].body;
            if (body == null) continue;
            if (body.isKinematic) continue;

            body.linearVelocity = cachedLinearVelocities[i];
            body.angularVelocity = cachedAngularVelocities[i];
        }
    }

    Rigidbody ResolveTargetBody(Vector3 hitPoint, Collider hitCollider, bool headshot)
    {
        if (hitCollider != null && hitCollider.attachedRigidbody != null) return hitCollider.attachedRigidbody;
        if (headshot) return FindBody(HumanBodyBones.Head);
        return FindClosestBody(hitPoint);
    }

    HitZone ZoneFor(Rigidbody body, bool headshot)
    {
        if (body == null) return HitZone.Torso;
        if (headshot || body == FindBody(HumanBodyBones.Head)) return HitZone.Head;

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].body != body) continue;

            switch (parts[i].bone)
            {
                case HumanBodyBones.Hips:
                    return HitZone.Hips;
                case HumanBodyBones.Spine:
                case HumanBodyBones.Chest:
                    return HitZone.Torso;
                case HumanBodyBones.LeftUpperLeg:
                case HumanBodyBones.LeftLowerLeg:
                case HumanBodyBones.LeftFoot:
                case HumanBodyBones.RightUpperLeg:
                case HumanBodyBones.RightLowerLeg:
                case HumanBodyBones.RightFoot:
                    return HitZone.Legs;
                case HumanBodyBones.LeftUpperArm:
                case HumanBodyBones.LeftLowerArm:
                case HumanBodyBones.RightUpperArm:
                case HumanBodyBones.RightLowerArm:
                    return HitZone.Arms;
                default:
                    return HitZone.Torso;
            }
        }

        return HitZone.Torso;
    }

    float ImpulseMultiplier(HitZone zone)
    {
        switch (zone)
        {
            case HitZone.Head:
                return 1.18f;
            case HitZone.Torso:
                return 1f;
            case HitZone.Hips:
                return 0.95f;
            case HitZone.Legs:
                return 0.78f;
            case HitZone.Arms:
                return 0.72f;
            default:
                return 1f;
        }
    }

    Rigidbody FindBody(HumanBodyBones bone)
    {
        if (parts == null) return null;

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].bone == bone) return parts[i].body;
        }

        return null;
    }

    Rigidbody FindClosestBody(Vector3 point)
    {
        Rigidbody best = null;
        float bestDistance = float.MaxValue;

        if (parts == null) return null;

        for (int i = 0; i < parts.Length; i++)
        {
            Rigidbody body = parts[i].body;
            if (body == null) continue;

            float distance = (body.worldCenterOfMass - point).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = body;
            }
        }

        return best;
    }

    void CacheHandColliders()
    {
        ZombieHand[] hands = GetComponentsInChildren<ZombieHand>(true);
        handColliders = new Collider[hands.Length];

        for (int i = 0; i < hands.Length; i++)
        {
            handColliders[i] = hands[i].GetComponent<Collider>();
        }
    }

    void SetHandColliders(bool enabled)
    {
        if (handColliders == null) return;

        for (int i = 0; i < handColliders.Length; i++)
        {
            if (handColliders[i] != null) handColliders[i].enabled = enabled;
        }
    }
}
