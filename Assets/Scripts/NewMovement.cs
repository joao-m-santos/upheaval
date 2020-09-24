using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMovement : MonoBehaviour {

    private Rigidbody2D rb; // The character's Rigidbody
    private bool facingRight = true;  // For determining which way the player is currently facing.
    public bool shouldRotateOnClimb;
    public Vector2 direction;
    public Vector2 currentSpeed;

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float maxSpeed = 20f;

    [Header("Jumping")]
    public bool isGrounded; // Whether or not the player is on ground.
    public bool isJumping;
    public bool isWallJumping;

    public float jumpForce = 28f;
    public float fallMultiplier = 2f;

    private float jumpTimeCounter;
    public float jumpTime = 0.35f;

    private int extraJumps;
    public int extraJumpValue = 1;

    [SerializeField] private bool airControl = true; // Whether or not a player can steer while jumping;

    [Header("Climbing")]
    public bool isClimbing;
    public float climbingSpeed = 10f;

    [Header("Physics")]
    public int gravity = 8;

    [Header("Collision")]
    [SerializeField] private LayerMask whatIsGround; // A mask determining what is ground to the character
    [SerializeField] private LayerMask whatIsWall; // A mask determining what is wall to the character

    [SerializeField] private Transform groundCheck; // A position marking where to check if the player is grounded.
    [SerializeField] private Transform wallCheck; // A position marking where to check for walls.

    const float groundCheckRadius = .5f; // Radius of the overlap circle to determine if grounded
    const float wallCheckRadius = .2f; // Radius of the overlap circle to determine if climbing

    void Start() {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update() {
        currentSpeed = rb.velocity;
        direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        isClimbing = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, whatIsWall);

        if (isGrounded) {
            extraJumps = extraJumpValue;
        }

        if (Input.GetButtonDown("Jump")) {
            jumpTimeCounter = jumpTime;

            if (extraJumps > 0) {
                isJumping = true;
                extraJumps--;
            } else if (isGrounded) {
                isJumping = true;
            }

            if (isClimbing) {
                isWallJumping = true;
            }
        }

        if (Input.GetButtonUp("Jump")) {
            isJumping = false;
            isWallJumping = false;
        }
    }

    void FixedUpdate() {
        if (isGrounded || isClimbing) {
            rb.gravityScale = 0;
        } else {
            rb.gravityScale = gravity;

            // Faster fall
            if (rb.velocity.y < 0) {
                rb.gravityScale = gravity * fallMultiplier;

                if (Mathf.Abs(rb.velocity.y) >= maxSpeed) {
                    rb.velocity = new Vector2(rb.velocity.x, -maxSpeed);
                }
            }
        }

        if (isClimbing) {
            Climb();
        } else if (isGrounded || airControl) {
            Move();
        }

        if (isJumping) {
            Jump();
        }

        if (isWallJumping) {
            WallJump();
        }

        if (Input.GetButton("Jump") && isJumping) {
            if (jumpTimeCounter > 0) {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                jumpTimeCounter -= Time.fixedDeltaTime;
            } else {
                isJumping = false;
            }
        }
    }

    public void Move() {
        float computedSpeed = moveSpeed;

        rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);

        if (direction.x > 0 && !facingRight || direction.x < 0 && facingRight) {
            Flip();
        }
    }

    public void Climb() {
        float computedSpeed = climbingSpeed;

        float xForce;
        if (facingRight && direction.x > 0 || !facingRight && direction.x < 0)
            xForce = 0f;
        else xForce = direction.x * moveSpeed;

        rb.velocity = new Vector2(xForce, direction.y * climbingSpeed);

        //if (direction.x > 0 && !facingRight || direction.x < 0 && facingRight) {
        //    Flip();
        //}
    }

    public void Jump() {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    public void WallJump() {
        if (isClimbing && direction.x != 0) {
            if (facingRight && direction.x < 0 || !facingRight && direction.x > 0) {
                rb.velocity = new Vector2(direction.x * moveSpeed, jumpForce);
            }
        }
    }

    private void Flip() {
        facingRight = !facingRight;
        transform.rotation = Quaternion.Euler(0, facingRight ? 0 : 180, 0);
    }
}