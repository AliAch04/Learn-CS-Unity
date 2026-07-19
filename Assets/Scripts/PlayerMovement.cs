using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public Transform orient;
    public float groundDrag = 5f;

    [Header("Check Ground")]
    public float playerHeight = 2;
    public LayerMask whatIsGound;
    bool grounded;

    [Header("Jump Setting")]
    public float jumpForce = 13f;
    public float jumpCoolDown = 0.25f;
    public float gravityMultiplier = 2.5f;
    public float airMultiplier = 0.1f;
    bool ableToJump;
    public KeyCode JumpKey = KeyCode.Space;

    float hozInput;
    float verInput;
    Vector3 direction;

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        ableToJump = true;
        rb.useGravity = false;
    }

    private void Update()
    {
        // Ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGound);
        Debug.DrawRay(transform.position, Vector3.down * (playerHeight * 0.5f + 0.2f), grounded ? Color.green : Color.red);
        
        // Apply the drag
        if (grounded) rb.drag = groundDrag;
        else rb.drag = 0.5f;

        MyInput();
        SpeedControlle();

    }

    private void FixedUpdate()
    {
        MovePlayer();
        ApplyCustomGravity();
    }

    private void MyInput()
    {
        hozInput = Input.GetAxisRaw("Horizontal");
        verInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(JumpKey) && ableToJump && grounded)
        {
            ableToJump = false;
            Jump();
            // Call Reset function after delay (jumpCoolDown)
            Invoke(nameof(ResetJump), jumpCoolDown);
        }
    }

    private void MovePlayer()
    {
        // Calculate the vector direction
        direction = orient.right * hozInput + orient.forward * verInput;

        // On ground
        if (grounded)
            rb.AddForce(direction.normalized * moveSpeed * 10f, ForceMode.Force);

        // In Air
        else if (!grounded)
        {
            rb.AddForce(direction.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

    }

    private void ApplyCustomGravity()
    {
        // Applies a stronger realistic downward force
        if (!grounded)
        {
            rb.AddForce(Vector3.down * (9.81f * gravityMultiplier), ForceMode.Force);
        }
        else
        {
            // Standard light gravity
            rb.AddForce(Vector3.down * 9.81f, ForceMode.Force);
        }
    }

    private void SpeedControlle()
    {
        Vector3 flatVeclocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        //Debug.Log(flatVeclocity.magnitude);

        if(flatVeclocity.magnitude > moveSpeed)
        {
            Vector3 limitedSpeed = flatVeclocity.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedSpeed.x, rb.velocity.y, limitedSpeed.z);
        }
    }

    private void Jump()
    {
        // Reset the y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse); 
            
    }

    private void ResetJump()
    {
        ableToJump = true;
    }

}
