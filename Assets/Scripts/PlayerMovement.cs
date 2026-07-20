using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Animations;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float baseMoveSpeed = 8f;
    private float targetMoveSpeed;
    private float currentMoveSpeed;
    public Transform orient;

    [Header("Acceleration Settings")]
    public float normalAcceleration = 5f;
    public float iceAcceleration = 2f;
    private float currentAcceleration;

    [Header("Friction & Braking")]
    public float normalGroundDrag = 0f;
    public float iceGroundDrag = 0f;

    [Header("Smooth Stop Settings")]
    [Tooltip("Higher numbers = stops faster. Lower numbers = slides further")]
    public float normalBrakingSpeed = 15f;  
    public float iceBrakingSpeed = 6f;
    private float currentBrakingSpeed;

    [Header("Check Ground")]
    public float playerHeight = 2;
    public LayerMask whatIsGound;
    bool grounded;

    [Header("Jump Setting")]
    public float jumpForce = 13f;
    public float jumpCoolDown = 0.25f;
    public float gravityMultiplier = 2.5f;
    [Range(0f, 1f)] public float airMultiplier = 0.1f;
    bool ableToJump;
    public KeyCode JumpKey = KeyCode.Space;

    [Header("UI Display")]
    public TextMeshProUGUI velocityText;

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

        targetMoveSpeed = baseMoveSpeed;
    }

    private void Update()
    {
        HandleGroundCheckAndFriction();
        MyInput();
        SpeedController();
        UpdateVelocityUI();

    }

    private void FixedUpdate()
    {
        MovePlayer();
        ApplyCustomGravity();

        if (hozInput == 0 && verInput == 0 && grounded)
        {
            DeceleratePlayer();
        }
    }

    private void HandleGroundCheckAndFriction()
    {
        float sphereRadius = 0.4f;
        float castDistance = (playerHeight * 0.5f) - sphereRadius + 0.2f;

        RaycastHit hit;
        grounded = Physics.SphereCast(transform.position, sphereRadius, Vector3.down, out hit, castDistance, whatIsGound);

        if (grounded)
        {
            Debug.DrawRay(transform.position, Vector3.down * (castDistance + sphereRadius), Color.green);

            // Check using Unity Tags 
            if (hit.collider.CompareTag("Ice"))
            {
                rb.drag = iceGroundDrag; // Slidiness effect
                currentMoveSpeed = baseMoveSpeed * 1.2f; // move faster on ice
                currentBrakingSpeed = iceBrakingSpeed;
                currentAcceleration = iceAcceleration;
            }
            //// Check using Physics Material to managing it via assets
            //else if (hit.collider.sharedMaterial != null && hit.collider.sharedMaterial.name == "IceMaterial")
            //{
            //    rb.drag = iceGroundDrag;
            //}
            else
            {
                // Normal surface: high drag acts like artificial brakes when you stop pressing keys
                rb.drag = normalGroundDrag;
                currentMoveSpeed = baseMoveSpeed;
                currentBrakingSpeed = normalBrakingSpeed;
                currentAcceleration = normalAcceleration;
            }
        }
        else
        {
            // Air state
            //Debug.DrawRay(transform.position, Vector3.down * (castDistance + sphereRadius), Color.red);
            rb.drag = 0.5f;
        }

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
        {
            // Forces accelerate the player naturally
            rb.AddForce(direction.normalized * targetMoveSpeed * currentAcceleration, ForceMode.Force);
        }
        // In Air
        else
        {
            rb.AddForce(direction.normalized * currentMoveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

    }

    private void DeceleratePlayer()
    {
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // Calculate the braking vector using standard physics deceleration
        Vector3 smoothedVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, currentBrakingSpeed * Time.fixedDeltaTime);

        rb.velocity = new Vector3(smoothedVelocity.x, rb.velocity.y, smoothedVelocity.z);
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

    private void SpeedController()
    {
        Vector3 flatVeclocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        //Debug.Log(flatVeclocity.magnitude);

        if (flatVeclocity.magnitude > currentMoveSpeed)
        {
            Vector3 limitedSpeed = flatVeclocity.normalized * currentMoveSpeed;
            rb.velocity = new Vector3(limitedSpeed.x, rb.velocity.y, limitedSpeed.z);
        }
    }

    private void Jump()
    {
        // Reset the y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse); 
            
    }

    private void UpdateVelocityUI()
    {
        if (velocityText != null)
        {
            // Calculate horizontal speed (ignoring falling/jumping speed)
            Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            float speed = flatVelocity.magnitude;

            // Formating
            velocityText.text = "Speed: " + speed.ToString("F1") + " m/s";
        }
    }

    private void ResetJump()
    {
        ableToJump = true;
    }

}
