using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Parameters")]
    [Tooltip("Basic movement speed")]
    public float moveSpeed = 6f;
    [Tooltip("Sprint multiplier")]
    public float sprintMultiplier = 1.5f;

    [Header("Gravity and Jumping")]
    [Tooltip("Gravity force applied to the player")]
    public float gravity = -19.6f;
    [Tooltip("Initial jump velocity")]
    public float jumpHeight = 2f;
    [Tooltip("Additional gravity multiplier for more responsive falling")]  
    public float fallMultiplier = 2.5f;

    [Header("Ground Detection")]
    [Tooltip("Transform used to check if player is grounded")]
    public Transform groundCheck;
    [Tooltip("Radius of ground check sphere")]
    public float groundCheckRadius = 0.3f;
    [Tooltip("Layers considered as ground")]
    public LayerMask groundMask;

    [Header("Advanced Movement")]
    [Tooltip("How quickly the player accelerates")]
    public float acceleration = 10f;
    [Tooltip("How quickly the player decelerates")]
    public float deceleration = 10f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float sprintStaminaDrain = 22f;
    public float staminaRegen = 28f;
    public float staminaRegenDelay = 0.6f;
    public float exhaustedSprintThreshold = 18f;
    public float jumpStaminaCost = 12f;

    [Header("Jump Leniency")]
    [Tooltip("Allows a jump shortly after leaving the ground")]
    public float coyoteTime = 0.12f;
    [Tooltip("Buffers jump input shortly before landing")]
    public float jumpBufferTime = 0.12f;

    [Header("Landing")]
    [Tooltip("Minimum downward speed to register a landing impact")]
    public float landingVelocityThreshold = 7f;
    [Tooltip("How quickly landing strength fades back to zero")]
    public float landingRecovery = 8f;

    
    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isMoving;
    private bool wasGrounded;
    private float lastGroundedTime;
    private float jumpPressedTime = float.NegativeInfinity;
    private float lastUngroundedVelocity;
    private float landingStrength;
    private Vector2 moveInput;
    private float stamina;
    private float lastSprintTime;
    private bool exhausted;

    public bool IsGrounded => isGrounded;
    public bool IsMoving => isMoving;
    public bool IsSprinting { get; private set; }
    public Vector2 MoveInput => moveInput;
    public float Stamina => stamina;
    public float Stamina01 => maxStamina > 0f ? stamina / maxStamina : 0f;
    public float HorizontalSpeed01 => Mathf.Clamp01(new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude / Mathf.Max(0.01f, moveSpeed * sprintMultiplier));
    public float VerticalVelocity => velocity.y;
    public float LandingStrength => landingStrength;

    void Start()
    {
        
        controller = GetComponent<CharacterController>();
        stamina = maxStamina;

        
        if (groundCheck == null)
        {
            Debug.LogWarning("Ground check transform is not set. Creating a default ground check point.");
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -controller.height / 2, 0);
            groundCheck = groundCheckObj.transform;
        }
    }

    void Update()
    {
        
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        moveInput = new Vector2(moveHorizontal, moveVertical);

        
        Vector3 desiredMoveDirection = transform.right * moveHorizontal + transform.forward * moveVertical;
        desiredMoveDirection = Vector3.ClampMagnitude(desiredMoveDirection, 1f);

        
        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && moveVertical > 0.1f;
        if (exhausted && stamina >= exhaustedSprintThreshold) exhausted = false;
        IsSprinting = wantsSprint && !exhausted && stamina > 0f;
        UpdateStamina();
        float currentMoveSpeed = IsSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

        
        moveDirection = Vector3.Lerp(moveDirection, desiredMoveDirection * currentMoveSpeed,
            Time.deltaTime * (desiredMoveDirection.magnitude > 0.1f ? acceleration : deceleration));

        
        controller.Move(moveDirection * Time.deltaTime);

        
        HandleJumping();

        
        ApplyGravity();

        
        UpdateMovementState();
        landingStrength = Mathf.MoveTowards(landingStrength, 0f, landingRecovery * Time.deltaTime);
        wasGrounded = isGrounded;
    }

    void HandleJumping()
    {
        if (Input.GetButtonDown("Jump"))
        {
            jumpPressedTime = Time.time;
        }

        bool canUseBufferedJump = Time.time - jumpPressedTime <= jumpBufferTime;
        bool canUseCoyoteTime = Time.time - lastGroundedTime <= coyoteTime;

        if (canUseBufferedJump && canUseCoyoteTime)
        {
            if (stamina < jumpStaminaCost) return;

            stamina = Mathf.Max(0f, stamina - jumpStaminaCost);
            lastSprintTime = Time.time;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpPressedTime = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
        }
    }

    void UpdateStamina()
    {
        if (IsSprinting)
        {
            stamina = Mathf.Max(0f, stamina - sprintStaminaDrain * Time.deltaTime);
            lastSprintTime = Time.time;
            if (stamina <= 0f)
            {
                exhausted = true;
                IsSprinting = false;
            }
            return;
        }

        if (Time.time - lastSprintTime >= staminaRegenDelay)
        {
            stamina = Mathf.Min(maxStamina, stamina + staminaRegen * Time.deltaTime);
        }
    }

    void ApplyGravity()
    {
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }
        else
        {
            lastUngroundedVelocity = velocity.y;
        }

        
        velocity.y += gravity * Time.deltaTime;

        
        if (velocity.y < 0)
        {
            velocity.y += gravity * (fallMultiplier - 1) * Time.deltaTime;
        }

        
        if (isGrounded && velocity.y < 0)
        {
            if (!wasGrounded && lastUngroundedVelocity < -landingVelocityThreshold)
            {
                landingStrength = Mathf.Clamp01(Mathf.Abs(lastUngroundedVelocity) / 20f);
            }

            velocity.y = -2f; 
        }

        
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateMovementState()
    {
        
        isMoving = controller.velocity.magnitude > 0.1f && isGrounded;
    }

    
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
    
