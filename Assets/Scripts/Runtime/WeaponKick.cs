using UnityEngine;

public class WeaponKick : MonoBehaviour
{
    private Vector3 restPosition;
    
    private Quaternion restRotation;
    private bool restPoseCaptured;

    private Vector3 positionOffset;
    private Vector3 rotationOffsetEuler;
    private float recoverSpeed = 10f;

    public void Kick(Vector3 positionKick, Vector3 rotationKickEuler, float recoverSpeed)
    {
        if (!restPoseCaptured)
        {
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
            restPoseCaptured = true;
        }

        positionOffset += positionKick;
        rotationOffsetEuler += rotationKickEuler;
        this.recoverSpeed = recoverSpeed;
    }

    private void Update()
    {
        if (!restPoseCaptured) return;

        positionOffset = Vector3.MoveTowards(positionOffset, Vector3.zero, recoverSpeed * Time.deltaTime);
        rotationOffsetEuler = Vector3.MoveTowards(rotationOffsetEuler, Vector3.zero, recoverSpeed * 20f * Time.deltaTime);

        transform.localPosition = restPosition + positionOffset;

        transform.localRotation = restRotation * Quaternion.Euler(rotationOffsetEuler);
    }
}
