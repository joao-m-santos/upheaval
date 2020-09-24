using UnityEngine;
using UnityEngine.Events;

public class CharacterController : MonoBehaviour {

    private Rigidbody2D rb; // The character's Rigidbody
    private bool facingRight = true;  // For determining which way the player is currently facing.

    private Vector3 Velocity = Vector3.zero;

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float maxSpeed = 7f;
    public float currentSpeed = 0f;

    [Range(0, 1)] [SerializeField] private float crouchSpeed = .36f; // Amount of maxSpeed applied to crouching movement. 1 = 100%
    private float movementSmoothing = .01f; // How much to smooth out the movement

    [Header("Jumping")]
    public float jumpForce = 10f;
    public float fallMultiplier = 4f;
    public float lowJumpMultiplier = 4f;

    // Prevent 1 frame jump
    private float jumpDelay = 0.01f;
    private float jumpTimer;

    [SerializeField] private bool airControl = true; // Whether or not a player can steer while jumping;

    [Header("Climbing")]
    public bool isWalled;
    public bool isClimbing;
    public float climbingSpeed = 0f;

    public bool isWallJumping;

    [Header("Physics")]
    public float gravity = 1f;

    [Header("Collision")]
    public bool isGrounded; // Whether or not the player is on ground.

    [SerializeField] private LayerMask whatIsGround; // A mask determining what is ground to the character
    [SerializeField] private LayerMask whatIsWall; // A mask determining what is wall to the character

    [SerializeField] private Transform groundCheck; // A position marking where to check if the player is grounded.
    [SerializeField] private Transform ceilingCheck; // A position marking where to check for ceilings
    [SerializeField] private Transform wallCheck; // A position marking where to check for walls.

    const float groundCheckRadius = .5f; // Radius of the overlap circle to determine if grounded
    const float ceilingCheckRadius = .2f; // Radius of the overlap circle to determine if the player can stand up
    const float wallCheckRadius = .2f; // Radius of the overlap circle to determine if climbing

    [Header("Events")]
    public UnityEvent OnLandEvent;
    [System.Serializable] public class BoolEvent : UnityEvent<bool> { }

    // UNUSED (for now)
    [SerializeField] private Collider2D CrouchDisableCollider; // A collider that will be disabled when crouching
    private bool wasCrouching = false;
    public BoolEvent OnCrouchEvent;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();

        if (OnLandEvent == null)
            OnLandEvent = new UnityEvent();

        if (OnCrouchEvent == null)
            OnCrouchEvent = new BoolEvent();
    }

    private void Update() {
        bool wasGrounded = isGrounded;
        isGrounded = false;
        isWallJumping = false;

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, whatIsGround);
        for (int i = 0; i < colliders.Length; i++) {
            if (colliders[i].gameObject != gameObject) {
                isGrounded = true;
                if (!wasGrounded)
                    OnLandEvent.Invoke();
            }
        }

        if (Input.GetButton("Jump")) {
            jumpTimer = Time.time + jumpDelay;
        }

        isWalled = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, whatIsWall);
        isClimbing = isWalled && !isGrounded;
        isWallJumping = Input.GetButton("Jump") && isClimbing;
    }

    private void FixedUpdate() {
        if (isGrounded) {
            rb.gravityScale = 0;
        } else if (isClimbing) {
            rb.velocity = new Vector2(0f, 0f);
            rb.gravityScale = 0;
        } else {
            rb.gravityScale = gravity;

            // Fall acceleration
            if (rb.velocity.y < 0) {
                rb.gravityScale = gravity * fallMultiplier;
            } else if (rb.velocity.y > 0 && !Input.GetButton("Jump")) {
                rb.gravityScale = gravity * lowJumpMultiplier;
            }
        }

        if (jumpTimer > Time.time && (isGrounded || isWallJumping)) {
            Jump(isWallJumping);
        }
    }

    public void Move(Vector2 direction, bool crouch, bool jump) {
        float computedSpeed = moveSpeed;

        // If crouching, check to see if the character can stand up
        if (!crouch) {
            // If the character has a ceiling preventing them from standing up, keep them crouching
            if (Physics2D.OverlapCircle(ceilingCheck.position, ceilingCheckRadius, whatIsGround)) {
                crouch = true;
            }
        }

        //only control the player if grounded or airControl is turned on
        if (isGrounded || airControl) {
            // If crouching
            //if (crouch) {
            //    if (!wasCrouching) {
            //        wasCrouching = true;
            //        OnCrouchEvent.Invoke(true);
            //    }

            //    // Reduce the speed by the crouchSpeed multiplier
            //    computedSpeed *= crouchSpeed;

            //    // Disable one of the colliders when crouching
            //    if (CrouchDisableCollider != null)
            //        CrouchDisableCollider.enabled = false;
            //} else {
            //    // Enable the collider when not crouching
            //    if (CrouchDisableCollider != null)
            //        CrouchDisableCollider.enabled = true;

            //    if (wasCrouching) {
            //        wasCrouching = false;
            //        OnCrouchEvent.Invoke(false);
            //    }
            //}
            if (isClimbing) {
                Debug.Log("CARALHO WTF");
                Vector3 targetVelocity = new Vector2(0f, direction.y * moveSpeed);
                // And then smoothing it out and applying it to the character
                rb.velocity = Vector3.SmoothDamp(rb.velocity, targetVelocity, ref Velocity, movementSmoothing);
            } else {
                Debug.Log("ENTRA AQUI LOL");
                // Move the character by finding the target velocity
                Vector3 targetVelocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
                // And then smoothing it out and applying it to the character
                rb.velocity = Vector3.SmoothDamp(rb.velocity, targetVelocity, ref Velocity, movementSmoothing);
            }

            currentSpeed = rb.velocity.x;

            if (direction.x > 0 && !facingRight || direction.x < 0 && facingRight) {
                Flip();
            }
        }

        // If the player should jump...
        if (isGrounded && jump) {
            Jump();
        }
    }

    private void Jump(bool wallJump = false) {
        float input = Input.GetAxisRaw("Horizontal");

        if (wallJump) Debug.Log("WALL JUMP!");
        else Debug.Log("JUMP!");

        // Reset vertical movement
        rb.velocity = new Vector2(rb.velocity.x, 0);

        // Add a vertical force to the player
        Vector2 jumpVector;

        //if (wallJump) jumpVector = new Vector2(input, jumpForce);
        //else 
        jumpVector = Vector2.up * jumpForce;

        rb.AddForce(jumpVector, ForceMode2D.Impulse);
        //rb.velocity = Vector2.up * jumpForce;
        //rb.velocity = new Vector2(input, jumpForce);

        isGrounded = false;
        isClimbing = false;

        jumpTimer = 0;

        Debug.Log("JUMP END!");
    }

    // Switch the way the player is labelled as facing.
    private void Flip() {
        facingRight = !facingRight;
        transform.rotation = Quaternion.Euler(0, facingRight ? 0 : 180, 0);
    }

    // Visualization
    private void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
    }
}