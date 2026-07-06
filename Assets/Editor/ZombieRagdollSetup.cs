using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ZombieRagdollSetup
{
    const string PrefabPath = "Assets/Prefab/Zombie.prefab";

    static readonly HumanBodyBones[] Bones =
    {
        HumanBodyBones.Hips,
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.Head,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.LeftFoot,
        HumanBodyBones.RightUpperLeg,
        HumanBodyBones.RightLowerLeg,
        HumanBodyBones.RightFoot
    };

    [MenuItem("Tools/Undead Courier/Setup Zombie Ragdoll")]
    public static void Run()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

        try
        {
            Configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("Zombie ragdoll setup complete.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void Configure(GameObject root)
    {
        Animator animator = root.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogError("Zombie prefab needs a humanoid Animator for ragdoll setup.");
            return;
        }

        ZombieRagdoll ragdoll = root.GetComponent<ZombieRagdoll>();
        if (ragdoll == null) ragdoll = root.AddComponent<ZombieRagdoll>();
        SerializedObject serializedRagdoll = new SerializedObject(ragdoll);
        serializedRagdoll.FindProperty("bodyImpulse").floatValue = 4.5f;
        serializedRagdoll.FindProperty("headImpulse").floatValue = 6f;
        serializedRagdoll.ApplyModifiedPropertiesWithoutUndo();

        Dictionary<HumanBodyBones, Rigidbody> bodies = new Dictionary<HumanBodyBones, Rigidbody>();
        List<ZombieRagdoll.Part> parts = new List<ZombieRagdoll.Part>();

        for (int i = 0; i < Bones.Length; i++)
        {
            HumanBodyBones bone = Bones[i];
            Transform transform = animator.GetBoneTransform(bone);
            if (transform == null) continue;

            Rigidbody body = ConfigureBody(transform.gameObject, bone);
            Collider collider = ConfigureCollider(animator, transform.gameObject, bone);
            bodies[bone] = body;

            parts.Add(new ZombieRagdoll.Part
            {
                bone = bone,
                body = body,
                collider = collider
            });
        }

        ConfigureJoints(animator, bodies);
        ragdoll.SetParts(parts.ToArray());
        EditorUtility.SetDirty(root);
    }

    static Rigidbody ConfigureBody(GameObject gameObject, HumanBodyBones bone)
    {
        Rigidbody body = gameObject.GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();

        body.mass = MassFor(bone);
        body.isKinematic = true;
        body.useGravity = true;
        body.linearDamping = 0.08f;
        body.angularDamping = AngularDampingFor(bone);
        body.detectCollisions = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.solverIterations = 12;
        body.solverVelocityIterations = 12;
        body.maxAngularVelocity = 7f;
        body.maxDepenetrationVelocity = 1.5f;

        return body;
    }

    static Collider ConfigureCollider(Animator animator, GameObject gameObject, HumanBodyBones bone)
    {
        Collider existing = null;
        Collider[] colliders = gameObject.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] is CapsuleCollider || colliders[i] is SphereCollider || colliders[i] is BoxCollider)
            {
                existing = colliders[i];
                break;
            }
        }

        Collider collider = existing;

        if (bone == HumanBodyBones.Head)
        {
            SphereCollider sphere = collider as SphereCollider;
            if (sphere == null)
            {
                if (collider != null) Object.DestroyImmediate(collider);
                sphere = gameObject.AddComponent<SphereCollider>();
            }
            Vector3 neckUp = HeadCenter(animator, gameObject.transform);
            sphere.radius = 0.11f;
            sphere.center = neckUp;
            collider = sphere;
        }
        else if (bone == HumanBodyBones.Hips)
        {
            BoxCollider box = collider as BoxCollider;
            if (box == null)
            {
                if (collider != null) Object.DestroyImmediate(collider);
                box = gameObject.AddComponent<BoxCollider>();
            }
            box.size = new Vector3(0.32f, 0.22f, 0.24f);
            box.center = new Vector3(0f, 0.02f, 0f);
            collider = box;
        }
        else if (bone == HumanBodyBones.LeftFoot || bone == HumanBodyBones.RightFoot)
        {
            BoxCollider box = collider as BoxCollider;
            if (box == null)
            {
                if (collider != null) Object.DestroyImmediate(collider);
                box = gameObject.AddComponent<BoxCollider>();
            }
            box.size = new Vector3(0.11f, 0.04f, 0.18f);
            box.center = new Vector3(0f, 0f, 0.05f);
            collider = box;
        }
        else
        {
            CapsuleCollider capsule = collider as CapsuleCollider;
            if (capsule == null)
            {
                if (collider != null) Object.DestroyImmediate(collider);
                capsule = gameObject.AddComponent<CapsuleCollider>();
            }
            ConfigureCapsule(animator, capsule, bone);
            collider = capsule;
        }

        collider.enabled = false;
        collider.isTrigger = false;
        return collider;
    }

    static void ConfigureCapsule(Animator animator, CapsuleCollider capsule, HumanBodyBones bone)
    {
        Transform transform = capsule.transform;
        Transform end = EndBone(animator, bone);
        Vector3 localEnd = end != null ? transform.InverseTransformPoint(end.position) : CenterFor(bone) * 2f;
        float length = Mathf.Max(localEnd.magnitude, 0.12f);
        int direction = MainAxis(localEnd);
        float radius = Mathf.Min(RadiusFor(bone), length * 0.34f);

        capsule.direction = direction;
        capsule.radius = radius;
        capsule.height = Mathf.Max(length + radius, radius * 2f);
        capsule.center = localEnd * 0.5f;
    }

    static Transform EndBone(Animator animator, HumanBodyBones bone)
    {
        HumanBodyBones end;
        switch (bone)
        {
            case HumanBodyBones.Spine: end = HumanBodyBones.Chest; break;
            case HumanBodyBones.Chest: end = HumanBodyBones.Head; break;
            case HumanBodyBones.LeftUpperArm: end = HumanBodyBones.LeftLowerArm; break;
            case HumanBodyBones.LeftLowerArm: end = HumanBodyBones.LeftHand; break;
            case HumanBodyBones.RightUpperArm: end = HumanBodyBones.RightLowerArm; break;
            case HumanBodyBones.RightLowerArm: end = HumanBodyBones.RightHand; break;
            case HumanBodyBones.LeftUpperLeg: end = HumanBodyBones.LeftLowerLeg; break;
            case HumanBodyBones.LeftLowerLeg: end = HumanBodyBones.LeftFoot; break;
            case HumanBodyBones.RightUpperLeg: end = HumanBodyBones.RightLowerLeg; break;
            case HumanBodyBones.RightLowerLeg: end = HumanBodyBones.RightFoot; break;
            default: return null;
        }
        return animator.GetBoneTransform(end);
    }

    static Vector3 HeadCenter(Animator animator, Transform head)
    {
        Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) return new Vector3(0f, 0.06f, 0f);

        Vector3 awayFromChest = (head.position - chest.position).normalized;
        Vector3 local = head.InverseTransformDirection(awayFromChest) * 0.06f;
        return local;
    }

    static int MainAxis(Vector3 v)
    {
        v = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        if (v.x > v.y && v.x > v.z) return 0;
        if (v.y > v.z) return 1;
        return 2;
    }

    static void ConfigureJoints(Animator animator, Dictionary<HumanBodyBones, Rigidbody> bodies)
    {
        Link(animator, bodies, HumanBodyBones.Spine, HumanBodyBones.Hips);
        Link(animator, bodies, HumanBodyBones.Chest, HumanBodyBones.Spine);
        Link(animator, bodies, HumanBodyBones.Head, HumanBodyBones.Chest);
        Link(animator, bodies, HumanBodyBones.LeftUpperArm, HumanBodyBones.Chest);
        Link(animator, bodies, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftUpperArm);
        Link(animator, bodies, HumanBodyBones.RightUpperArm, HumanBodyBones.Chest);
        Link(animator, bodies, HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm);
        Link(animator, bodies, HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips);
        Link(animator, bodies, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftUpperLeg);
        Link(animator, bodies, HumanBodyBones.LeftFoot, HumanBodyBones.LeftLowerLeg);
        Link(animator, bodies, HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips);
        Link(animator, bodies, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg);
        Link(animator, bodies, HumanBodyBones.RightFoot, HumanBodyBones.RightLowerLeg);
    }

    static void Link(Animator animator, Dictionary<HumanBodyBones, Rigidbody> bodies, HumanBodyBones bone, HumanBodyBones parentBone)
    {
        if (!bodies.TryGetValue(bone, out Rigidbody body)) return;
        if (!bodies.TryGetValue(parentBone, out Rigidbody parentBody)) return;

        Transform transform = animator.GetBoneTransform(bone);
        if (transform == null) return;

        CharacterJoint joint = transform.GetComponent<CharacterJoint>();
        if (joint == null) joint = transform.gameObject.AddComponent<CharacterJoint>();

        joint.connectedBody = parentBody;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector3.zero;
        joint.connectedAnchor = parentBody.transform.InverseTransformPoint(transform.position);
        joint.enableCollision = false;
        joint.enablePreprocessing = false;
        Vector3 axis = JointAxis(animator, bone, transform);
        joint.axis = axis;
        joint.swingAxis = SwingAxis(axis);
        joint.lowTwistLimit = Limit(LowTwistFor(bone));
        joint.highTwistLimit = Limit(HighTwistFor(bone));
        joint.swing1Limit = Limit(SwingFor(bone));
        joint.swing2Limit = Limit(Swing2For(bone));
        joint.massScale = 1f;
        joint.connectedMassScale = 1f;
    }

    static SoftJointLimit Limit(float value)
    {
        SoftJointLimit limit = new SoftJointLimit();
        limit.limit = value;
        return limit;
    }

    static float MassFor(HumanBodyBones bone)
    {
        switch (bone)
        {
            case HumanBodyBones.Hips: return 4.5f;
            case HumanBodyBones.Spine: return 4.4f;
            case HumanBodyBones.Chest: return 5.4f;
            case HumanBodyBones.Head: return 1.8f;
            case HumanBodyBones.LeftUpperLeg:
            case HumanBodyBones.RightUpperLeg: return 2.2f;
            case HumanBodyBones.LeftLowerLeg:
            case HumanBodyBones.RightLowerLeg: return 1.4f;
            case HumanBodyBones.LeftFoot:
            case HumanBodyBones.RightFoot: return 0.7f;
            case HumanBodyBones.LeftUpperArm:
            case HumanBodyBones.RightUpperArm: return 1f;
            default: return 0.8f;
        }
    }

    static float AngularDampingFor(HumanBodyBones bone)
    {
        switch (bone)
        {
            case HumanBodyBones.Hips:
            case HumanBodyBones.Spine:
            case HumanBodyBones.Chest: return 0.32f;
            case HumanBodyBones.Head: return 0.38f;
            default: return 0.22f;
        }
    }

    static float RadiusFor(HumanBodyBones bone)
    {
        switch (bone)
        {
            case HumanBodyBones.Hips: return 0.13f;
            case HumanBodyBones.Spine:
            case HumanBodyBones.Chest: return 0.14f;
            case HumanBodyBones.LeftUpperLeg:
            case HumanBodyBones.RightUpperLeg: return 0.105f;
            case HumanBodyBones.LeftLowerLeg:
            case HumanBodyBones.RightLowerLeg: return 0.085f;
            default: return 0.055f;
        }
    }

    static Vector3 CenterFor(HumanBodyBones bone)
    {
        switch (bone)
        {
            case HumanBodyBones.LeftUpperArm:
            case HumanBodyBones.LeftLowerArm: return new Vector3(-0.08f, 0f, 0f);
            case HumanBodyBones.RightUpperArm:
            case HumanBodyBones.RightLowerArm: return new Vector3(0.08f, 0f, 0f);
            case HumanBodyBones.LeftUpperLeg:
            case HumanBodyBones.RightUpperLeg:
            case HumanBodyBones.LeftLowerLeg:
            case HumanBodyBones.RightLowerLeg: return new Vector3(0f, 0.12f, 0f);
            default: return Vector3.zero;
        }
    }

    static float SwingFor(HumanBodyBones bone)
    {
        switch (bone)
        {
            case HumanBodyBones.Spine: return 58f;
            case HumanBodyBones.Chest: return 65f;
            case HumanBodyBones.Head: return 44f;
            case HumanBodyBones.LeftUpperArm:
            case HumanBodyBones.RightUpperArm: return 55f;
            case HumanBodyBones.LeftUpperLeg:
            case HumanBodyBones.RightUpperLeg: return 75f;
            case HumanBodyBones.LeftLowerLeg:
            case HumanBodyBones.RightLowerLeg: return 95f;
            case HumanBodyBones.LeftFoot:
            case HumanBodyBones.RightFoot: return 70f;
            default: return 38f;
        }
    }

    static float Swing2For(HumanBodyBones bone)
    {
        switch (bone)
        {
            case HumanBodyBones.Spine:
            case HumanBodyBones.Chest: return 48f;
            case HumanBodyBones.LeftUpperLeg:
            case HumanBodyBones.RightUpperLeg: return 70f;
            case HumanBodyBones.LeftLowerLeg:
            case HumanBodyBones.RightLowerLeg: return 65f;
            default: return SwingFor(bone) * 0.65f;
        }
    }

    static float LowTwistFor(HumanBodyBones bone)
    {
        switch (bone)
        {
            case HumanBodyBones.Spine:
            case HumanBodyBones.Chest: return -35f;
            case HumanBodyBones.LeftUpperLeg:
            case HumanBodyBones.RightUpperLeg: return -35f;
            case HumanBodyBones.LeftLowerLeg:
            case HumanBodyBones.RightLowerLeg: return -30f;
            default: return -24f;
        }
    }

    static float HighTwistFor(HumanBodyBones bone)
    {
        switch (bone)
        {
            case HumanBodyBones.Spine:
            case HumanBodyBones.Chest: return 35f;
            case HumanBodyBones.LeftUpperLeg:
            case HumanBodyBones.RightUpperLeg: return 35f;
            case HumanBodyBones.LeftLowerLeg:
            case HumanBodyBones.RightLowerLeg: return 30f;
            default: return 24f;
        }
    }

    static Vector3 JointAxis(Animator animator, HumanBodyBones bone, Transform transform)
    {
        Transform end = EndBone(animator, bone);
        Vector3 local = end != null ? transform.InverseTransformPoint(end.position) : Vector3.up;
        if (local.sqrMagnitude < 0.0001f) local = Vector3.up;
        return local.normalized;
    }

    static Vector3 SwingAxis(Vector3 axis)
    {
        Vector3 candidate = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) < 0.85f ? Vector3.up : Vector3.forward;
        Vector3 swing = Vector3.Cross(axis, candidate);
        if (swing.sqrMagnitude < 0.0001f) swing = Vector3.right;
        return swing.normalized;
    }
}
