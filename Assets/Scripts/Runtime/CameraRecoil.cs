using UnityEngine;

public class CameraRecoil : MonoBehaviour
{
    public float pitchClampMin = -4f;
    public float pitchClampMax = 0.5f;
    public float yawClampMin = -1.5f;
    public float yawClampMax = 1.5f;

    private Vector2 currentKick;
    private Quaternion lastApplied = Quaternion.identity;

    public void Kick(float pitchMin, float pitchMax, float yawMin, float yawMax, float snapSpeed, float recoverSpeed)
    {
        float pitchAmount = Random.Range(pitchMin, pitchMax);
        float yawAmount = Random.Range(yawMin, yawMax);

        currentKick.x -= pitchAmount;
        currentKick.y += yawAmount;

        currentKick.x = Mathf.Clamp(currentKick.x, pitchClampMin, pitchClampMax);
        currentKick.y = Mathf.Clamp(currentKick.y, yawClampMin, yawClampMax);

        this.recoverSpeed = recoverSpeed;
    }

    private float recoverSpeed = 10f;

    private void LateUpdate()
    {
        transform.localRotation *= Quaternion.Inverse(lastApplied);
        currentKick = Vector2.MoveTowards(currentKick, Vector2.zero, recoverSpeed * Time.deltaTime);
        lastApplied = Quaternion.Euler(currentKick.x, currentKick.y, 0f);
        transform.localRotation *= lastApplied;
    }
}
