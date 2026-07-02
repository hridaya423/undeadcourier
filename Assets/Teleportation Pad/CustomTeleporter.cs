using UnityEngine;
using System.Collections;

public class SimpleTeleporter : MonoBehaviour
{
    // The destination pad to teleport to
    public Transform destinationPad;

    // Time it takes to teleport
    public float teleportDelay = 1.5f;

    // Height offset for teleportation
    public float teleportationHeightOffset = 1f;

    // Audio for teleport effect (optional)
    public AudioSource teleportSound;

    // Teleporter settings
    public float validTeleportDistance = 2.5f; // How close the player needs to be to the center
    public string playerTag = "Player"; // Tag for the player object

    // Debug settings
    public bool forceDetect = false;  // For testing - press space to force detection

    // Private variables
    private bool isCurrentlyTeleporting = false;
    private Coroutine currentTeleportCoroutine = null;
    private Transform objectBeingTeleported = null;
    private Collider teleporterCollider;

    void Start()
    {
        // Get the collider component
        teleporterCollider = GetComponent<Collider>();

        if (teleporterCollider == null)
        {
            Debug.LogError("TELEPORTER ERROR: No collider attached to " + gameObject.name);
        }

        // Display critical setup info
        Debug.LogError("TELEPORTER SETUP: " + gameObject.name);

        if (destinationPad == null)
        {
            Debug.LogError("ERROR: No destination pad assigned to " + gameObject.name);
        }
        else
        {
            Debug.LogError("Destination pad is: " + destinationPad.name);
        }
    }

    void Update()
    {
        // Test feature - press Space to force teleportation (for debugging)
        if (forceDetect && Input.GetKeyDown(KeyCode.Space) && !isCurrentlyTeleporting)
        {
            Debug.LogError("TELEPORTER: Force detect triggered by Space key");
            StartTeleportation();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only teleport player-tagged objects
        if (collision.gameObject.CompareTag(playerTag))
        {
            // Verify the collision is actually on the teleporter with distance check
            if (IsObjectOnTeleporter(collision.transform))
            {
                Debug.LogError("TELEPORTER: Valid collision with " + collision.gameObject.name);
                if (!isCurrentlyTeleporting)
                {
                    StartTeleportation(collision.transform);
                }
            }
            else
            {
                Debug.LogError("TELEPORTER: Ignoring distant collision with " + collision.gameObject.name);
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Cancel teleportation if the object leaves the teleporter
        if (objectBeingTeleported == collision.transform)
        {
            Debug.LogError("TELEPORTER: Collision exited with " + collision.gameObject.name);
            CancelTeleportation();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Only teleport player-tagged objects
        if (other.CompareTag(playerTag))
        {
            // Verify the trigger is actually on the teleporter with distance check
            if (IsObjectOnTeleporter(other.transform))
            {
                Debug.LogError("TELEPORTER: Valid trigger with " + other.gameObject.name);
                if (!isCurrentlyTeleporting)
                {
                    StartTeleportation(other.transform);
                }
            }
            else
            {
                Debug.LogError("TELEPORTER: Ignoring distant trigger with " + other.gameObject.name);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Cancel teleportation if the object leaves the teleporter
        if (objectBeingTeleported == other.transform)
        {
            Debug.LogError("TELEPORTER: Trigger exited with " + other.gameObject.name);
            CancelTeleportation();
        }
    }

    // Check if an object is actually on the teleporter (not just colliding from far away)
    bool IsObjectOnTeleporter(Transform objectTransform)
    {
        // Check horizontal distance to center
        Vector2 teleporterPos = new Vector2(transform.position.x, transform.position.z);
        Vector2 objectPos = new Vector2(objectTransform.position.x, objectTransform.position.z);
        float horizontalDistance = Vector2.Distance(teleporterPos, objectPos);

        // Check vertical alignment - object should be above the teleporter
        bool isAboveTeleporter = objectTransform.position.y >= transform.position.y;

        // Check if the point is inside the collider bounds (projected onto the XZ plane)
        bool isWithinDistance = horizontalDistance <= validTeleportDistance;

        Debug.LogError("TELEPORTER: Distance check - " + objectTransform.name +
                       " Distance: " + horizontalDistance +
                       " Within bounds: " + isWithinDistance +
                       " Above pad: " + isAboveTeleporter);

        return isWithinDistance && isAboveTeleporter;
    }

    // Manual detection - can be called from another script if needed
    public void DetectPlayer(Transform player)
    {
        if (!isCurrentlyTeleporting && IsObjectOnTeleporter(player))
        {
            Debug.LogError("TELEPORTER: Manual detection for " + player.name);
            StartTeleportation(player);
        }
    }

    // Start the teleportation process
    void StartTeleportation(Transform objectToTeleport = null)
    {
        if (destinationPad == null)
        {
            Debug.LogError("TELEPORTER ERROR: No destination pad assigned!");
            return;
        }

        // If no specific object was provided, try to find the player
        if (objectToTeleport == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null && IsObjectOnTeleporter(player.transform))
            {
                objectToTeleport = player.transform;
                Debug.LogError("TELEPORTER: Auto-found player: " + player.name);
            }
            else
            {
                Debug.LogError("TELEPORTER ERROR: No valid object to teleport!");
                return;
            }
        }

        // Store reference to the object being teleported
        objectBeingTeleported = objectToTeleport;

        // Start the teleportation coroutine and store the reference
        currentTeleportCoroutine = StartCoroutine(TeleportWithDelay(objectToTeleport));
    }

    // Cancel the current teleportation if needed
    void CancelTeleportation()
    {
        if (isCurrentlyTeleporting && currentTeleportCoroutine != null)
        {
            Debug.LogError("TELEPORTER: Cancelling teleportation for " + objectBeingTeleported.name);
            StopCoroutine(currentTeleportCoroutine);
            currentTeleportCoroutine = null;
            objectBeingTeleported = null;
            isCurrentlyTeleporting = false;
            Debug.LogError("TELEPORTER: Teleportation cancelled, ready for next teleportation");
        }
    }

    // Coroutine to handle the delayed teleportation
    IEnumerator TeleportWithDelay(Transform objectToTeleport)
    {
        isCurrentlyTeleporting = true;

        Debug.LogError("TELEPORTER: Starting delay of " + teleportDelay + " seconds");

        // Wait for the specified delay
        yield return new WaitForSeconds(teleportDelay);

        // Double-check if the object is still being teleported and is still on the teleporter
        if (objectBeingTeleported == objectToTeleport && IsObjectOnTeleporter(objectToTeleport))
        {
            // Calculate the teleport position
            Vector3 teleportPosition = destinationPad.position + new Vector3(0, teleportationHeightOffset, 0);

            // Actually teleport the object
            Debug.LogError("TELEPORTER: Teleporting " + objectToTeleport.name + " to " + teleportPosition);
            objectToTeleport.position = teleportPosition;

            // Play sound if available
            if (teleportSound != null)
            {
                teleportSound.Play();
                Debug.LogError("TELEPORTER: Playing teleport sound");
            }
        }
        else
        {
            Debug.LogError("TELEPORTER: Object no longer valid for teleportation!");
        }

        // Set a short cooldown before allowing another teleportation
        yield return new WaitForSeconds(0.5f);

        isCurrentlyTeleporting = false;
        objectBeingTeleported = null;
        currentTeleportCoroutine = null;
        Debug.LogError("TELEPORTER: Ready for next teleportation");
    }

    // Visual debugging
    void OnDrawGizmos()
    {
        // Draw the teleporter pad
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1, 0.1f, 1));

        // Draw the valid teleport area
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, validTeleportDistance);

        // Draw the connection to destination
        if (destinationPad != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, destinationPad.position);

            // Draw the destination and height offset
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(destinationPad.position, new Vector3(1, 0.1f, 1));
            Gizmos.DrawLine(
                destinationPad.position,
                destinationPad.position + new Vector3(0, teleportationHeightOffset, 0)
            );
            Gizmos.DrawWireSphere(
                destinationPad.position + new Vector3(0, teleportationHeightOffset, 0),
                0.3f
            );
        }
    }
}