using UnityEngine;

public class Flashlight : MonoBehaviour
{
    public GameObject lightGO;
    public float pickupDistance = 3f;
    public KeyCode pickupKey = KeyCode.E;
    public bool isHeld = false;

    
    public Vector3 spawnPosition = new Vector3(0.3f, -0.2f, 0.5f);
    public Vector3 spawnRotation = new Vector3(0, 0, 0);

    
    public Transform flashlightSpawnPoint;

    
    public Transform handTransform; 
    public Transform[] fingerTransforms; 
    public Vector3[] fingerPositions; 
    public Vector3[] fingerRotations; 

    
    

    private Transform playerTransform;
    private Outline outlineComponent;
    private Vector3[] originalFingerPositions;
    private Quaternion[] originalFingerRotations;

    private void Start()
    {
        playerTransform = Camera.main.transform;
        
        lightGO.SetActive(true);
        outlineComponent = GetComponent<Outline>();

        
        if (fingerTransforms != null && fingerTransforms.Length > 0)
        {
            originalFingerPositions = new Vector3[fingerTransforms.Length];
            originalFingerRotations = new Quaternion[fingerTransforms.Length];

            for (int i = 0; i < fingerTransforms.Length; i++)
            {
                if (fingerTransforms[i] != null)
                {
                    originalFingerPositions[i] = fingerTransforms[i].localPosition;
                    originalFingerRotations[i] = fingerTransforms[i].localRotation;
                }
            }
        }
    }

    public void PickUp()
    {
        if (isHeld) return;

        
        Transform parentTransform = flashlightSpawnPoint != null ? flashlightSpawnPoint : playerTransform;
        transform.SetParent(parentTransform);

        
        transform.localPosition = spawnPosition;
        transform.localRotation = Quaternion.Euler(spawnRotation);

        isHeld = true;

        
        PositionFingers(true);

        
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
        }

        
        if (InteractionManager.Instance.hoveredFlashlight == this)
        {
            InteractionManager.Instance.hoveredFlashlight = null;
        }

        
        Destroy(GetComponent<Collider>());
        Destroy(GetComponent<Rigidbody>());
    }

    private void PositionFingers(bool grip)
    {
        if (fingerTransforms == null || fingerPositions == null || fingerRotations == null)
            return;

        int count = Mathf.Min(fingerTransforms.Length, fingerPositions.Length, fingerRotations.Length);

        for (int i = 0; i < count; i++)
        {
            if (fingerTransforms[i] != null)
            {
                if (grip)
                {
                    
                    fingerTransforms[i].localPosition = fingerPositions[i];
                    fingerTransforms[i].localRotation = Quaternion.Euler(fingerRotations[i]);
                }
                else
                {
                    
                    fingerTransforms[i].localPosition = originalFingerPositions[i];
                    fingerTransforms[i].localRotation = originalFingerRotations[i];
                }
            }
        }
    }

    
    public void Drop(Vector3 dropPosition, Vector3 dropRotation)
    {
        if (!isHeld) return;

        transform.SetParent(null);
        transform.position = dropPosition;
        transform.rotation = Quaternion.Euler(dropRotation);

        
        PositionFingers(false);

        
        gameObject.AddComponent<Rigidbody>();
        gameObject.AddComponent<BoxCollider>();

        isHeld = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupDistance);
    }
}
