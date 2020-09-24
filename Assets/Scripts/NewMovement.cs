using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMovement : MonoBehaviour {

    private Rigidbody2D rb; // The character's Rigidbody
    private bool facingRight = true;  // For determining which way the player is currently facing.
    public Vector2 direction;

    [Header("Movement")]
    public float moveSpeed = 10f;
    //public float maxSpeed = 7f;
    public float currentSpeed = 0f;

    [Header("Jumping")]
    public bool isGrounded; // Whether or not the player is on ground.
    public bool isJumping;

    public float jumpForce = 28f;
    public float fallMultiplier = 2f;

    private float jumpTimeCounter;
    public float jumpTime = 0.35f;

    private int extraJumps;
    public int extraJumpValue = 1;

    [SerializeField] private bool airControl = true; // Whether or not a player can steer while jumping;

    [Header("Climbing")]
    public bool isWalled;
    public bool isTouchingWall;
    public bool isClimbing;
    public float climbingSpeed = 0f;

    public bool isWallJumping;

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
        direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool isPressingJump = Input.GetButtonDown("Jump");

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        isWalled = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, whatIsWall);

        if (isGrounded) extraJumps = extraJumpValue;

        if (Input.GetButtonDown("Jump")) {
            isJumping = true;
            jumpTimeCounter = jumpTime;

            if (extraJumps > 0) {
                rb.velocity = Vector2.up * jumpForce;
                extraJumps--;
            } else if (isGrounded) {
                rb.velocity = Vector2.up * jumpForce;
            }
        }

        if (Input.GetButton("Jump") && isJumping) {

            if (jumpTimeCounter > 0) {
                rb.velocity = Vector2.up * jumpForce;
                jumpTimeCounter -= Time.deltaTime;
            } else {
                isJumping = false;
            }
        }

        if (Input.GetButtonUp("Jump")) {
            isJumping = false;
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
            }
        }

        if (isGrounded || airControl) {
            Move();
        }
    }

    public void Move() {
        float computedSpeed = moveSpeed;

        rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);

        if (direction.x > 0 && !facingRight || direction.x < 0 && facingRight) {
            Flip();
        }
    }

    private void Flip() {
        facingRight = !facingRight;
        transform.rotation = Quaternion.Euler(0, facingRight ? 0 : 180, 0);
    }
}