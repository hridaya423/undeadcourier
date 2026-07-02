using UnityEngine;

public class ConstrainedFollower : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;

    [Header("Position Settings")]
    public bool followX = true;
    public bool followY = true;
    public bool followZ = true;
    public Vector3 positionOffset = Vector3.zero;
    public float smoothFollowSpeed = 0f; 

    [Header("Rotation Settings")]
    public bool followRotationX = false;
    public bool followRotationY = true;
    public bool followRotationZ = false;
    public Vector3 rotationOffset = Vector3.zero;

    private Vector3 initialRotation;

    void Start()
    {
        
        initialRotation = transform.eulerAngles;

        
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("No target assigned and no object with 'Player' tag found!");
            }
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        
        Vector3 targetPosition = transform.position;

        if (followX) targetPosition.x = target.position.x + positionOffset.x;
        if (followY) targetPosition.y = target.position.y + positionOffset.y;
        if (followZ) targetPosition.z = target.position.z + positionOffset.z;

        
        if (smoothFollowSpeed > 0)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothFollowSpeed);
        }
        else
        {
            transform.position = targetPosition;
        }

        
        Vector3 newRotation = initialRotation;

        if (followRotationX) newRotation.x = target.eulerAngles.x + rotationOffset.x;
        if (followRotationY) newRotation.y = target.eulerAngles.y + rotationOffset.y;
        if (followRotationZ) newRotation.z = target.eulerAngles.z + rotationOffset.z;

        transform.rotation = Quaternion.Euler(newRotation);
    }
}
