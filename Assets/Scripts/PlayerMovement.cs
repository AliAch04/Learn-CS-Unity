using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public Transform orient;
    public float groundDrag = 5f;

    [Header("Check Ground")]
    public float playerHeight = 2;
    public LayerMask whatIsGound;
    bool grounded;

    [Header("Jump Setting")]
    public float jumpForce = 5f;
    public float jumpCoolDown;
    public float airMultiplier;
    bool ableToJump;

    float hozInput;
    float verInput;
    Vector3 direction;

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // Ground check
        grounded = Physics.Raycast(transform.position, -transform.up, playerHeight * 0.5f + 0.2f, whatIsGound);

        // Apply the drag
        if (grounded) rb.drag = groundDrag;
        else rb.drag = 0f;

        MyInput();
        SpeedControlle();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        hozInput = Input.GetAxisRaw("Horizontal");
        verInput = Input.GetAxisRaw("Vertical");
    }

    private void MovePlayer()
    {
        // Calculate the vector direction
        direction = orient.right * hozInput + new Vector3(0,0,0) + orient.forward * verInput;

        rb.AddForce(direction.normalized * moveSpeed * 10f, ForceMode.Force);
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
        if (grounded) ableToJump = true;
        else ableToJump = false;

        //Debug.Log($"Able to Jump: {ableToJump}");
        if (ableToJump)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse); 
        }
            
    }

}
