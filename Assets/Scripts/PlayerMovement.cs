using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Animations;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float baseMoveSpeed = 8f;
    public float sprintMoveSpeed = 12f;
    [Tooltip("How fast your target speed transitions between walking and sprinting.")]
    public float sprintTransitionSpeed = 10f;
    public KeyCode SprintKey = KeyCode.LeftShift;
    private float targetMoveSpeed;
    private float currentMaxSpeed;
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

    [Header("Climb / Wall Stick Settings")]
    public KeyCode ClimbKey = KeyCode.Mouse0;
    [Tooltip("Distance from camera view to detect climbable wall. Increased for magnetic dash.")]
    public float wallCheckDistance = 1.5f;
    [Tooltip("How wide the detection 'tube' is. Higher = easier to detect the wall without looking directly at it.")]
    public float wallCheckRadius = 1.0f;
    [Tooltip("Time in seconds the player can hang before falling")]
    public float maxStickTime = 5f;
    private bool isWallSticking;
    private bool isLeapingToWall;
    private float stickTimer;
    private Vector3 wallNormal;
    private Vector3 wallHitPoint;
    private bool isTouchingClimbableWall;

    [Header("Climb Prompt UI")]
    [Tooltip("Assign your UI Panel (positioned at the center of your screen Canvas)")]
    public GameObject climbPromptUI;
    private Camera mainCamera;

    [Header("Wall Timer UI")]
    public Slider wallTimerSlider;
    public Image wallTimerFillImage;
    public Color normalTimerColor = Color.white;
    public Color lowTimerColor = Color.red;
    public float colorTransitionSpeed = 5f;
    private Color targetTimerColor;

    [Header("Stamina Setting & UI")]
    public Slider staminaSlider;
    public Image staminaFillImage;
    public Color normalStaminaColor = Color.white;
    public Color lowStaminaColor = Color.red;
    private Color targetColor;
    public float maxStamina = 100f;
    public float staminaDrainRate = 25f;
    public float staminaRegenRate = 15f;
    private float currentStamina;
    private bool isExhausted;

    [Header("UI Display")]
    public TextMeshProUGUI velocityText;

    float hozInput;
    float verInput;
    Vector3 direction;
    bool isSprinting;
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        ableToJump = true;
        rb.useGravity = false;

        mainCamera = Camera.main;

        targetMoveSpeed = baseMoveSpeed;
        currentMaxSpeed = baseMoveSpeed;

        currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = maxStamina;
        }

        if (staminaFillImage != null)
        {
            staminaFillImage.color = normalStaminaColor;
        }

        if (wallTimerSlider != null)
        {
            wallTimerSlider.maxValue = maxStickTime;
            wallTimerSlider.value = maxStickTime;
            wallTimerSlider.gameObject.SetActive(false);
        }

        if (climbPromptUI != null)
        {
            climbPromptUI.SetActive(false);
        }
    }

    private void Update()
    {
        HandleGroundCheckAndFriction();
        CheckClimbableWall();
        MyInput();
        HandleStamina();
        HandleWallStickTimer();
        HandleClimbPromptUI();
        SpeedController();
        UpdateVelocityUI();
    }

    private void FixedUpdate()
    {
        // Only apply normal physics if we are not sticking and not currently dashing to the wall
        if (!isWallSticking && !isLeapingToWall)
        {
            MovePlayer();
            ApplyCustomGravity();

            if (hozInput == 0 && verInput == 0 && grounded)
            {
                DeceleratePlayer();
            }
        }
        else if (isWallSticking)
        {
            // Completely freeze position when sticking 
            rb.velocity = Vector3.zero;
        }
    }

    private void CheckClimbableWall()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        Vector3 rayOrigin = mainCamera != null ? mainCamera.transform.position : transform.position + Vector3.up;
        Vector3 rayDirection = mainCamera != null ? mainCamera.transform.forward : orient.forward;

        if (Physics.SphereCast(rayOrigin, wallCheckRadius, rayDirection, out RaycastHit hit, wallCheckDistance))
        {
            if (hit.collider.CompareTag("Climbable"))
            {
                isTouchingClimbableWall = true;
                wallNormal = hit.normal;
                wallHitPoint = hit.point;
                return;
            }
        }

        isTouchingClimbableWall = false;
    }

    private void HandleClimbPromptUI()
    {
        if (climbPromptUI == null) return;

        bool showPrompt = !grounded && isTouchingClimbableWall && !isWallSticking && !isLeapingToWall;

        if (climbPromptUI.activeSelf != showPrompt)
        {
            climbPromptUI.SetActive(showPrompt);
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
            if (isWallSticking) EndWallStick();

            float surfaceMultiplier = hit.collider.CompareTag("Ice") ? 1.3f : 1.0f;
            float desiredSpeedLimit = isSprinting ? sprintMoveSpeed : baseMoveSpeed;
            targetMoveSpeed = desiredSpeedLimit * surfaceMultiplier;

            if (hit.collider.CompareTag("Ice"))
            {
                rb.drag = iceGroundDrag;
                currentBrakingSpeed = iceBrakingSpeed;
                currentAcceleration = iceAcceleration;
            }
            else
            {
                rb.drag = normalGroundDrag;
                currentBrakingSpeed = normalBrakingSpeed;
                currentAcceleration = normalAcceleration;
            }
        }
        else
        {
            rb.drag = 0.5f;
        }
    }

    private void MyInput()
    {
        hozInput = Input.GetAxisRaw("Horizontal");
        verInput = Input.GetAxisRaw("Vertical");

        bool isMovingStraight = (hozInput != 0 && verInput == 0) || (verInput != 0 && hozInput == 0);

        if (Input.GetKey(SprintKey) && isMovingStraight && grounded && !isExhausted)
        {
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }

        // Toggle Climb/Wall stick 
        if (Input.GetKeyDown(ClimbKey))
        {
            if (isWallSticking)
            {
                EndWallStick();
            }
            else if (!grounded && isTouchingClimbableWall && !isLeapingToWall)
            {
                // Perform the magnetic leap to the wall
                StartCoroutine(LeapToWallRoutine(wallHitPoint, wallNormal));
            }
        }

        if (Input.GetKeyDown(JumpKey) && ableToJump && !isLeapingToWall)
        {
            if (grounded)
            {
                ableToJump = false;
                Jump();
                Invoke(nameof(ResetJump), jumpCoolDown);
            }
            else if (isWallSticking)
            {
                ableToJump = false;
                EndWallStick();
                WallJump();
                Invoke(nameof(ResetJump), jumpCoolDown);
            }
        }
    }

    private IEnumerator LeapToWallRoutine(Vector3 hitPoint, Vector3 normal)
    {
        isLeapingToWall = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = hitPoint + (normal * 0.5f);

        // Grab current magnitude, but enforce a minimum speed so the player doesn't float slowly
        float dashSpeed = Mathf.Max(rb.velocity.magnitude, baseMoveSpeed);
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / dashSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Nullify physics velocity during the dash to prevent gravity falling
            rb.velocity = Vector3.zero;

            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }

        // Snap precisely to the target at the end
        transform.position = targetPos;
        isLeapingToWall = false;

        StartWallStick();
    }

    private void StartWallStick()
    {
        isWallSticking = true;
        stickTimer = maxStickTime;
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;

        if (wallTimerSlider != null)
        {
            wallTimerSlider.gameObject.SetActive(true);
            wallTimerSlider.value = maxStickTime;
        }
        if (wallTimerFillImage != null)
        {
            wallTimerFillImage.color = normalTimerColor;
        }
    }

    private void EndWallStick()
    {
        isWallSticking = false;

        if (wallTimerSlider != null)
        {
            wallTimerSlider.gameObject.SetActive(false);
        }
    }

    private void HandleWallStickTimer()
    {
        if (!isWallSticking) return;

        stickTimer -= Time.deltaTime;

        if (wallTimerSlider != null)
        {
            wallTimerSlider.value = stickTimer;
        }

        if (stickTimer <= (maxStickTime * 0.5f))
        {
            targetTimerColor = lowTimerColor;
        }
        else
        {
            targetTimerColor = normalTimerColor;
        }

        if (wallTimerFillImage != null)
        {
            wallTimerFillImage.color = Color.Lerp(wallTimerFillImage.color, targetTimerColor, colorTransitionSpeed * Time.deltaTime);
        }

        if (stickTimer <= 0f)
        {
            EndWallStick();
        }
    }

    private void WallJump()
    {
        EndWallStick();

        Vector3 forceDirection = transform.up + (wallNormal * 0.1f);
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(forceDirection.normalized * jumpForce, ForceMode.Impulse);
    }

    private void HandleStamina()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }

        if (isSprinting)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isSprinting = false;
                isExhausted = true;
            }
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;

            if (isExhausted && currentStamina >= (maxStamina * 0.2f))
            {
                isExhausted = false;
            }
        }

        if (currentStamina <= (maxStamina * 0.2f))
        {
            targetColor = lowStaminaColor;
        }
        else
        {
            targetColor = normalStaminaColor;
        }

        if (staminaFillImage != null)
        {
            staminaFillImage.color = Color.Lerp(staminaFillImage.color, targetColor, colorTransitionSpeed * Time.deltaTime);
        }

        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }
    }

    private void MovePlayer()
    {
        direction = orient.right * hozInput + orient.forward * verInput;

        currentMaxSpeed = Mathf.MoveTowards(currentMaxSpeed, targetMoveSpeed, sprintTransitionSpeed * Time.fixedDeltaTime);

        if (grounded)
        {
            rb.AddForce(direction.normalized * currentMaxSpeed * currentAcceleration, ForceMode.Force);
        }
        else
        {
            rb.AddForce(direction.normalized * currentMaxSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void DeceleratePlayer()
    {
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 smoothedVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, currentBrakingSpeed * Time.fixedDeltaTime);

        rb.velocity = new Vector3(smoothedVelocity.x, rb.velocity.y, smoothedVelocity.z);
    }

    private void ApplyCustomGravity()
    {
        if (!grounded)
        {
            rb.AddForce(Vector3.down * (9.81f * gravityMultiplier), ForceMode.Force);
        }
        else
        {
            rb.AddForce(Vector3.down * 9.81f, ForceMode.Force);
        }
    }

    private void SpeedController()
    {
        Vector3 flatVeclocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (flatVeclocity.magnitude > currentMaxSpeed)
        {
            Vector3 limitedSpeed = flatVeclocity.normalized * currentMaxSpeed;
            rb.velocity = new Vector3(limitedSpeed.x, rb.velocity.y, limitedSpeed.z);
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void UpdateVelocityUI()
    {
        if (velocityText != null)
        {
            Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            float speed = flatVelocity.magnitude;

            velocityText.text = "Speed: " + speed.ToString("F1") + " m/s";
        }
    }

    private void ResetJump()
    {
        ableToJump = true;
    }
}