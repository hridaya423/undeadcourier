using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections.Generic;

public class ArmsAnimation : MonoBehaviour
{
    private TwoBoneIKConstraint twoBoneIK;
    private WeaponManager weaponManager;
    private Transform currentGrip;
    [SerializeField] private Transform handTarget; 

    
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

    
    [SerializeField] private bool showDebugVisuals = true;
    [SerializeField] private float debugSphereRadius = 0.02f;
    [SerializeField] private Color targetColor = Color.green;
    [SerializeField] private Color gripColor = Color.red;

    void Start()
    {
        twoBoneIK = GetComponent<TwoBoneIKConstraint>();
        weaponManager = WeaponManager.Instance;

        
        if (handTarget == null && twoBoneIK != null && twoBoneIK.data.target != null)
        {
            handTarget = twoBoneIK.data.target;
        }

        
        if (handTarget == null)
        {
            GameObject targetObj = new GameObject(gameObject.name + "_Target");
            handTarget = targetObj.transform;

            
            var data = twoBoneIK.data;
            data.target = handTarget;
            twoBoneIK.data = data;
        }

        
        twoBoneIK.weight = 0f;
    }

    void Update()
    {
        bool weaponActive = UpdateWeaponReference();
        twoBoneIK.weight = Mathf.Lerp(twoBoneIK.weight, weaponActive ? 1f : 0f, Time.deltaTime * 10f);

        
        if (showDebugVisuals && currentGrip != null && handTarget != null)
        {
            Debug.DrawLine(handTarget.position, currentGrip.position, Color.yellow);
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

            if (currentGrip != null)
            {
                Gizmos.color = gripColor;
                Gizmos.DrawSphere(currentGrip.position, debugSphereRadius);
            }
        }
    }

    private bool UpdateWeaponReference()
    {
        if (!weaponManager || !weaponManager.activeWeaponSlot || weaponManager.activeWeaponSlot.transform.childCount == 0)
        {
            currentGrip = null;
            return false;
        }

        Transform weapon = weaponManager.activeWeaponSlot.transform.GetChild(0);
        Transform newGrip = FindGripTransform(weapon);

        if (newGrip != currentGrip)
        {
            UpdateIKTarget(newGrip, weapon);
            currentGrip = newGrip;
        }
        else if (currentGrip != null)
        {
            
            
            UpdateIKTarget(currentGrip, weapon);
        }

        return currentGrip != null;
    }

    private void UpdateIKTarget(Transform grip, Transform weapon)
    {
        if (grip == null || handTarget == null)
            return;

        
        Vector3 positionOffset = GetPositionOffset(weapon);
        Vector3 rotationOffset = GetRotationOffset(weapon);

        
        handTarget.position = grip.position + grip.TransformDirection(positionOffset);


        handTarget.rotation = grip.rotation * Quaternion.Euler(rotationOffset);
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

    private Transform FindGripTransform(Transform weapon)
    {
        foreach (Transform child in weapon.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("WeaponGrip"))
            {
                return child;
            }
        }

        return null;
    }

    
    
    public void CalibrateHandPosition()
    {
        if (currentGrip == null || handTarget == null || twoBoneIK == null)
            return;

        Transform handBone = twoBoneIK.data.tip;
        if (handBone == null)
            return;

        
        Weapon currentWeapon = weaponManager.activeWeaponSlot.GetComponentInChildren<Weapon>();
        if (currentWeapon == null)
            return;

        
        WeaponOffsets matchingOffset = weaponOffsetsList.Find(wo => wo.weaponModel == currentWeapon.thisWeaponModel);
        if (matchingOffset == null)
        {
            matchingOffset = new WeaponOffsets { weaponModel = currentWeapon.thisWeaponModel };
            weaponOffsetsList.Add(matchingOffset);
        }

        
        matchingOffset.positionOffset = currentGrip.InverseTransformPoint(handBone.position);

        
        Quaternion relativeRotation = Quaternion.Inverse(currentGrip.rotation) * handBone.rotation;
        matchingOffset.rotationOffset = relativeRotation.eulerAngles;

        Debug.Log($"Calibrated hand position for {currentWeapon.thisWeaponModel}. " +
                  $"Position offset: {matchingOffset.positionOffset}, " +
                  $"Rotation offset: {matchingOffset.rotationOffset}");

        
        UpdateIKTarget(currentGrip, currentWeapon.transform);
    }
}
