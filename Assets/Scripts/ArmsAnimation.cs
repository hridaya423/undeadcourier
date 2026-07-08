using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections.Generic;

public class ArmsAnimation : MonoBehaviour
{
    private TwoBoneIKConstraint rightHandIK;
    private TwoBoneIKConstraint leftHandIK;
    private RigBuilder rigBuilder;
    private WeaponManager weaponManager;
    private Weapon currentWeapon;
    private Transform armsRoot;
    [SerializeField] private Transform handTarget;
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private float targetFollowSpeed = 18f;
    [SerializeField] private float ikBlendSpeed = 10f;

    
    [System.Serializable]
    public class WeaponOffsets
    {
        public Weapon.WeaponModel weaponModel;
        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;
    }

    [SerializeField]
    private List<WeaponOffsets> weaponOffsetsList = new List<WeaponOffsets>
    {
        new WeaponOffsets { weaponModel = Weapon.WeaponModel.M1911 },
        new WeaponOffsets { weaponModel = Weapon.WeaponModel.AK74 },
        new WeaponOffsets { weaponModel = Weapon.WeaponModel.Uzi },
        new WeaponOffsets { weaponModel = Weapon.WeaponModel.Shotgun }
    };

    
    [SerializeField] private Vector3 defaultPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 defaultRotationOffset = Vector3.zero;

    [Header("Left Hand")]
    [SerializeField] private bool createLeftHandIK = true;
    [SerializeField] private Vector3 defaultLeftHandOffset = new Vector3(-0.03f, -0.01f, 0.14f);
    [SerializeField] private Vector3 defaultLeftHandRotation = new Vector3(90f, 180f, 0f);

    
    [SerializeField] private bool showDebugVisuals = false;
    [SerializeField] private float debugSphereRadius = 0.02f;
    [SerializeField] private Color targetColor = Color.green;
    [SerializeField] private Color gripColor = Color.red;
    [SerializeField] private Color leftTargetColor = Color.cyan;

    void Start()
    {
        rightHandIK = GetComponent<TwoBoneIKConstraint>();
        rigBuilder = FindAnyObjectByType<RigBuilder>();
        weaponManager = WeaponManager.Instance;
        GameObject armsObject = GameObject.Find("OpenGameArt_FPS_Arms");
        armsRoot = armsObject != null ? armsObject.transform : transform.root;
        EnsureRightHandIK();
        EnsureRightHandTarget();
        EnsureLeftHandIK();

        if (rightHandIK != null)
        {
            rightHandIK.weight = 0f;
        }

        if (leftHandIK != null)
        {
            leftHandIK.weight = 0f;
        }
    }

    void LateUpdate()
    {
        currentWeapon = weaponManager != null ? weaponManager.ActiveWeapon : null;
        WeaponActionAnimator actionAnimator = currentWeapon != null ? currentWeapon.GetComponent<WeaponActionAnimator>() : null;
        bool weaponActive = currentWeapon != null && actionAnimator != null;

        if (rightHandIK != null)
        {
            rightHandIK.weight = Mathf.Lerp(rightHandIK.weight, weaponActive ? 1f : 0f, Time.deltaTime * ikBlendSpeed);
        }

        if (leftHandIK != null)
        {
            leftHandIK.weight = Mathf.Lerp(leftHandIK.weight, weaponActive ? 1f : 0f, Time.deltaTime * ikBlendSpeed);
        }

        if (!weaponActive)
        {
            return;
        }

        Transform rightGrip = actionAnimator.RightHandGrip;
        Transform leftGrip = actionAnimator.LeftHandGrip;

        UpdateTarget(handTarget, rightGrip, GetPositionOffset(currentWeapon.transform), GetRotationOffset(currentWeapon.transform));
        UpdateTarget(leftHandTarget, leftGrip, defaultLeftHandOffset, defaultLeftHandRotation);

        if (showDebugVisuals && handTarget != null && rightGrip != null)
        {
            Debug.DrawLine(handTarget.position, rightGrip.position, Color.yellow);
        }

        if (showDebugVisuals && leftHandTarget != null && leftGrip != null)
        {
            Debug.DrawLine(leftHandTarget.position, leftGrip.position, Color.cyan);
        }
    }

    void OnDrawGizmos()
    {
        if (showDebugVisuals)
        {
            if (handTarget != null)
            {
                Gizmos.color = targetColor;
                Gizmos.DrawSphere(handTarget.position, debugSphereRadius);
            }

            if (leftHandTarget != null)
            {
                Gizmos.color = leftTargetColor;
                Gizmos.DrawSphere(leftHandTarget.position, debugSphereRadius);
            }
        }
    }

    private void EnsureRightHandTarget()
    {
        if (handTarget == null && rightHandIK != null && rightHandIK.data.target != null)
        {
            handTarget = rightHandIK.data.target;
        }

        if (handTarget == null)
        {
            GameObject targetObj = new GameObject(gameObject.name + "_RightHandTarget");
            handTarget = targetObj.transform;
            handTarget.SetParent(transform.parent != null ? transform.parent : transform, false);
        }

        if (rightHandIK != null)
        {
            var data = rightHandIK.data;
            data.target = handTarget;
            rightHandIK.data = data;
        }
    }

    private void EnsureRightHandIK()
    {
        if (rightHandIK == null)
        {
            rightHandIK = gameObject.AddComponent<TwoBoneIKConstraint>();
        }

        Transform armRoot = FindBoneByNames("mixamorig2:RightArm", "mixamorig:RightArm", "upper_arm.R");
        Transform forearm = FindBoneByNames("mixamorig2:RightForeArm", "mixamorig:RightForeArm", "forearm.R");
        Transform hand = FindBoneByNames("mixamorig2:RightHand", "mixamorig:RightHand", "hand.R");

        if (armRoot == null || forearm == null || hand == null) return;

        var data = rightHandIK.data;
        data.root = armRoot;
        data.mid = forearm;
        data.tip = hand;
        data.targetPositionWeight = 1f;
        data.targetRotationWeight = 1f;
        rightHandIK.data = data;
    }

    private void EnsureLeftHandIK()
    {
        if (!createLeftHandIK) return;

        if (leftHandTarget == null)
        {
            GameObject targetObj = new GameObject(gameObject.name + "_LeftHandTarget");
            leftHandTarget = targetObj.transform;
            leftHandTarget.SetParent(transform.parent != null ? transform.parent : transform, false);
        }

        if (leftHandIK != null) return;

        if (rigBuilder == null) return;

        Transform armRoot = FindBoneByNames("mixamorig2:LeftArm", "mixamorig:LeftArm", "upper_arm.L");
        Transform forearm = FindBoneByNames("mixamorig2:LeftForeArm", "mixamorig:LeftForeArm", "forearm.L");
        Transform hand = FindBoneByNames("mixamorig2:LeftHand", "mixamorig:LeftHand", "hand.L");

        if (armRoot == null || forearm == null || hand == null) return;

        Transform rigParent = transform.parent != null ? transform.parent : transform;
        GameObject leftRigObject = new GameObject("Arm_IK_Left");
        leftRigObject.transform.SetParent(rigParent, false);

        leftHandIK = leftRigObject.AddComponent<TwoBoneIKConstraint>();
        var data = leftHandIK.data;
        data.root = armRoot;
        data.mid = forearm;
        data.tip = hand;
        data.target = leftHandTarget;
        data.targetPositionWeight = 1f;
        data.targetRotationWeight = 1f;
        leftHandIK.data = data;

        Rig rig = rigParent.GetComponent<Rig>();
        if (rig == null)
        {
            rig = rigParent.gameObject.AddComponent<Rig>();
        }

        bool rigRegistered = false;
        for (int i = 0; i < rigBuilder.layers.Count; i++)
        {
            if (rigBuilder.layers[i].rig == rig)
            {
                rigRegistered = true;
                break;
            }
        }

        if (!rigRegistered)
        {
            rigBuilder.layers.Add(new RigLayer(rig));
        }

        rigBuilder.Build();
    }

    private Transform FindBoneByNames(params string[] boneNames)
    {
        if (armsRoot == null) return null;

        foreach (Transform child in armsRoot.GetComponentsInChildren<Transform>(true))
        {
            for (int i = 0; i < boneNames.Length; i++)
            {
                if (child.name == boneNames[i]) return child;
            }
        }

        return null;
    }

    private void UpdateTarget(Transform target, Transform grip, Vector3 positionOffset, Vector3 rotationOffset)
    {
        if (grip == null || target == null) return;

        Vector3 desiredPosition = grip.position + grip.TransformDirection(positionOffset);
        Quaternion desiredRotation = grip.rotation * Quaternion.Euler(rotationOffset);

        target.position = Vector3.Lerp(target.position, desiredPosition, Time.deltaTime * targetFollowSpeed);
        target.rotation = Quaternion.Slerp(target.rotation, desiredRotation, Time.deltaTime * targetFollowSpeed);
    }

    private Vector3 GetPositionOffset(Transform weapon)
    {
        Weapon weaponComponent = weapon.GetComponent<Weapon>();
        if (weaponComponent == null)
            return defaultPositionOffset;

        
        WeaponOffsets matchingOffset = weaponOffsetsList.Find(wo => wo.weaponModel == weaponComponent.thisWeaponModel);
        return matchingOffset != null ? matchingOffset.positionOffset : defaultPositionOffset;
    }

    private Vector3 GetRotationOffset(Transform weapon)
    {
        Weapon weaponComponent = weapon.GetComponent<Weapon>();
        if (weaponComponent == null)
            return defaultRotationOffset;

        
        WeaponOffsets matchingOffset = weaponOffsetsList.Find(wo => wo.weaponModel == weaponComponent.thisWeaponModel);
        return matchingOffset != null ? matchingOffset.rotationOffset : defaultRotationOffset;
    }

    public void CalibrateHandPosition()
    {
        if (currentWeapon == null || handTarget == null || rightHandIK == null)
            return;

        WeaponActionAnimator actionAnimator = currentWeapon.GetComponent<WeaponActionAnimator>();
        if (actionAnimator == null || actionAnimator.RightHandGrip == null) return;

        Transform handBone = rightHandIK.data.tip;
        if (handBone == null)
            return;

        WeaponOffsets matchingOffset = weaponOffsetsList.Find(wo => wo.weaponModel == currentWeapon.thisWeaponModel);
        if (matchingOffset == null)
        {
            matchingOffset = new WeaponOffsets { weaponModel = currentWeapon.thisWeaponModel };
            weaponOffsetsList.Add(matchingOffset);
        }

        matchingOffset.positionOffset = actionAnimator.RightHandGrip.InverseTransformPoint(handBone.position);

        Quaternion relativeRotation = Quaternion.Inverse(actionAnimator.RightHandGrip.rotation) * handBone.rotation;
        matchingOffset.rotationOffset = relativeRotation.eulerAngles;

        Debug.Log($"Calibrated hand position for {currentWeapon.thisWeaponModel}. " +
                  $"Position offset: {matchingOffset.positionOffset}, " +
                  $"Rotation offset: {matchingOffset.rotationOffset}");

        UpdateTarget(handTarget, actionAnimator.RightHandGrip, matchingOffset.positionOffset, matchingOffset.rotationOffset);
    }
}
