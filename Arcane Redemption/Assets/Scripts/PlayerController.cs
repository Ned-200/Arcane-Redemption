using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] public bool canMove = true;
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float sprintStaminaCost = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpStaminaCost = 15f;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashStaminaCost = 20f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundMask = -1;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showGroundCheckGizmo = true;

    // Components
    private CharacterController characterController;
    private BaseCharacter baseCharacter;

    // Movement state
    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool isDashing;
    private float dashTimeRemaining;
    private float dashCooldownRemaining;
    private Vector3 dashDirection;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        baseCharacter = GetComponent<BaseCharacter>();
        
        if (enableDebugLogs)
        {
            Debug.Log("PlayerController: Awake - CharacterController: " + (characterController != null) + 
                      ", BaseCharacter: " + (baseCharacter != null));
        }
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleGravity();
        if (canMove) {
            HandleDash();
            HandleMovement();
            HandleJump();
        }
    }

    private void HandleGroundCheck()
    {
        wasGroundedLastFrame = isGrounded;

        // Cast from center of character downward
        Vector3 spherePosition = transform.position + Vector3.up * (groundCheckRadius + 0.1f);
        
        // Use SphereCast for more reliable ground detection
        RaycastHit hit;
        bool sphereCastHit = Physics.SphereCast(
            spherePosition,
            groundCheckRadius,
            Vector3.down,
            out hit,
            groundCheckDistance + groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        // Also use CharacterController's built-in ground check as backup
        bool controllerGrounded = characterController.isGrounded;

        // Consider grounded if either method detects ground
        isGrounded = sphereCastHit || controllerGrounded;

        if (enableDebugLogs && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"Ground Check - SphereCast: {sphereCastHit}, ControllerGrounded: {controllerGrounded}, " +
                      $"Final IsGrounded: {isGrounded}, Player Y: {transform.position.y}, " +
                      $"Hit Distance: {(sphereCastHit ? hit.distance.ToString() : "N/A")}");
        }

        // Reset downward velocity when grounded
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        // Just landed
        if (isGrounded && !wasGroundedLastFrame)
        {
            if (enableDebugLogs)
            {
                Debug.Log("Player landed!");
            }
        }
    }

    private void HandleGravity()
    {
        if (!isGrounded && !isDashing)
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        if (isDashing) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && CanSprint();
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        if (isSprinting && move.magnitude > 0f)
        {
            ConsumeStamina(sprintStaminaCost * Time.deltaTime);
        }

        characterController.Move(move * currentSpeed * Time.deltaTime);
        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"Space pressed! IsGrounded: {isGrounded}, IsDashing: {isDashing}");
            }

            if (isGrounded && !isDashing)
            {
                TryJump();
            }
            else if (!isGrounded)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("Can't jump - not grounded!");
                }
            }
        }
    }

    private void HandleDash()
    {
        if (dashCooldownRemaining > 0f)
        {
            dashCooldownRemaining -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.C) && !isDashing && dashCooldownRemaining <= 0f)
        {
            if (enableDebugLogs)
            {
                Debug.Log("Dash pressed!");
            }
            TryDash();
        }

        if (isDashing)
        {
            dashTimeRemaining -= Time.deltaTime;

            if (dashTimeRemaining > 0f)
            {
                float dashSpeed = dashDistance / dashDuration;
                characterController.Move(dashDirection * dashSpeed * Time.deltaTime);
            }
            else
            {
                isDashing = false;
                if (enableDebugLogs)
                {
                    Debug.Log("Dash complete");
                }
            }
        }
    }

    private void TryJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        ConsumeStamina(jumpStaminaCost);
        
        if (enableDebugLogs)
        {
            Debug.Log("Jumping! Velocity Y: " + velocity.y);
        }
    }

    private void TryDash()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        if (move.magnitude < 0.1f)
        {
            dashDirection = transform.forward;
        }
        else
        {
            dashDirection = move.normalized;
        }

        isDashing = true;
        dashTimeRemaining = dashDuration;
        dashCooldownRemaining = dashCooldown;
        ConsumeStamina(dashStaminaCost);
        
        if (enableDebugLogs)
        {
            Debug.Log("Dashing! Direction: " + dashDirection);
        }
    }

    private bool CanSprint()
    {
        // Add stamina check when BaseCharacter exposes the method
        return true;
    }

    private void ConsumeStamina(float amount)
    {
        // This is a placeholder - you'll need to add a public method to BaseCharacter
        // baseCharacter.ConsumeStamina(amount);
    }

    // Visualize the ground check in Scene view
    private void OnDrawGizmos()
    {
        if (!showGroundCheckGizmo) return;

        Vector3 spherePosition = transform.position + Vector3.up * (groundCheckRadius + 0.1f);
        Vector3 sphereEnd = spherePosition + Vector3.down * (groundCheckDistance + groundCheckRadius);

        // Draw the sphere cast
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
        Gizmos.DrawWireSphere(sphereEnd, groundCheckRadius);
        
        // Draw line showing the cast direction
        Gizmos.DrawLine(spherePosition, sphereEnd);

        // Draw marker at player feet
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}
