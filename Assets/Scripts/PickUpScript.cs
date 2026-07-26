using System.Collections;
using UnityEngine;

public class PickUpScript : MonoBehaviour
{
    public static PickUpScript instance;

    public GameObject player;
    public Transform holdPos;

    [Header("Camera & Controls")]
    [Tooltip("Drag the PlayerCamController script here.")]
    public PlayerCamController cameraLookScript;

    [Header("Physics Settings")]
    public float throwForce = 500f;
    public float pickUpRange = 5f;
    public string holdLayerName = "HoldLayer";
    private int holdLayerIndex;

    [Tooltip("How fast the object rotates with the mouse")]
    public float rotationSensitivity = 2f;

    [Header("UI Panels")]
    public CanvasGroup holdUIGroup;
    public CanvasGroup rotateUIGroup;
    public float uiFadeSpeed = 8f;

    [Header("Animation Settings")]
    public float pickupSpeed = 0.2f;
    public float takeSpeed = 0.3f;

    public GameObject heldObj;
    private Rigidbody heldObjRb;
    private bool canDrop = true;
    private bool isAnimating = false;
    private Vector3 localCenterOffset;
    private float dropCooldown = 0f;

    private float targetHoldAlpha = 0f;
    private float targetRotateAlpha = 0f;

    private void Awake()
    {
        instance = this;

        if (holdUIGroup != null) holdUIGroup.alpha = 0f;
        if (rotateUIGroup != null) rotateUIGroup.alpha = 0f;
    }

    void Start()
    {
        holdLayerIndex = LayerMask.NameToLayer(holdLayerName);
        if (holdLayerIndex == -1)
        {
            Debug.LogError($"[PickUpScript] Layer '{holdLayerName}' does not exist! Defaulting to layer 0.");
            holdLayerIndex = 0;
        }
    }

    void Update()
    {
        if (heldObj != null && !isAnimating)
        {
            MoveObject();
            RotateObject();

            // 1. DROP ITEM ('X')
            if (Input.GetKeyDown(KeyCode.X) && canDrop == true)
            {
                StopClipping();
                DropObject();
            }
            // 2. TAKE ITEM ('E')
            else if (Input.GetKeyDown(KeyCode.E) && canDrop == true)
            {
                TakeObject();
            }
            // 3. THROW ITEM (Left Click)
            else if (Input.GetKeyDown(KeyCode.Mouse0) && canDrop == true)
            {
                StopClipping();
                ThrowObject();
            }
        }

        // Smooth UI Fading Logic
        if (holdUIGroup != null)
        {
            holdUIGroup.alpha = Mathf.Lerp(holdUIGroup.alpha, targetHoldAlpha, Time.deltaTime * uiFadeSpeed);
        }
        if (rotateUIGroup != null)
        {
            rotateUIGroup.alpha = Mathf.Lerp(rotateUIGroup.alpha, targetRotateAlpha, Time.deltaTime * uiFadeSpeed);
        }
    }

    public void PickUpObject(GameObject pickUpObj)
    {
        if (Time.time < dropCooldown) return;

        Rigidbody rb = pickUpObj.GetComponent<Rigidbody>();
        if (rb == null) rb = pickUpObj.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            heldObj = pickUpObj;
            heldObjRb = rb;

            Renderer[] renderers = heldObj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
                localCenterOffset = heldObj.transform.InverseTransformPoint(combinedBounds.center);
            }
            else
            {
                localCenterOffset = Vector3.zero;
            }

            heldObjRb.isKinematic = true;
            heldObj.transform.parent = holdPos.transform;
            SetLayerRecursively(heldObj, holdLayerIndex);

            Collider[] colliders = heldObj.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            targetHoldAlpha = 1f;
            targetRotateAlpha = 0f;

            StartCoroutine(AnimatePickup());
        }
    }

    private IEnumerator AnimatePickup()
    {
        isAnimating = true;
        float timeElapsed = 0f;
        Vector3 startPos = heldObj.transform.position;

        while (timeElapsed < pickupSpeed)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / pickupSpeed;

            Vector3 targetPos = holdPos.position - heldObj.transform.TransformDirection(localCenterOffset);
            heldObj.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        isAnimating = false;
    }

    void TakeObject()
    {
        dropCooldown = Time.time + 0.2f;
        StartCoroutine(AnimateTake());
    }

    private IEnumerator AnimateTake()
    {
        isAnimating = true;
        HideAllUI();

        float timeElapsed = 0f;
        Vector3 startPos = heldObj.transform.position;
        Vector3 startScale = heldObj.transform.localScale;

        while (timeElapsed < takeSpeed)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / takeSpeed;

            Vector3 targetPos = holdPos.position - holdPos.right * 0.5f - holdPos.up * 0.6f;
            heldObj.transform.position = Vector3.Lerp(startPos, targetPos, t);
            heldObj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        Destroy(heldObj);
        heldObj = null;
        isAnimating = false;
    }

    void DropObject()
    {
        dropCooldown = Time.time + 0.2f;
        HideAllUI();

        Collider[] colliders = heldObj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }

        SetLayerRecursively(heldObj, 0);
        heldObjRb.isKinematic = false;
        heldObj.transform.parent = null;
        heldObj = null;
    }

    void ThrowObject()
    {
        dropCooldown = Time.time + 0.2f;
        HideAllUI();

        Collider[] colliders = heldObj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }

        SetLayerRecursively(heldObj, 0);
        heldObjRb.isKinematic = false;
        heldObj.transform.parent = null;
        heldObjRb.AddForce(transform.forward * throwForce);
        heldObj = null;
    }

    void MoveObject()
    {
        heldObj.transform.position = holdPos.position - heldObj.transform.TransformDirection(localCenterOffset);
    }

    // --- NEW: Locked Axis Rotation Logic ---
    void RotateObject()
    {
        if (Input.GetKey(KeyCode.R))
        {
            canDrop = false; // Disables Throw/Drop/Take while inspecting
            if (cameraLookScript != null) cameraLookScript.enabled = false;

            targetHoldAlpha = 0f;
            targetRotateAlpha = 1f;

            // Hold LEFT CLICK to rotate Horizontally
            if (Input.GetMouseButton(0))
            {
                float xAxisRotation = Input.GetAxis("Mouse X") * rotationSensitivity;
                heldObj.transform.Rotate(transform.up, -xAxisRotation, Space.World);
            }

            // Hold RIGHT CLICK to rotate Vertically
            if (Input.GetMouseButton(1))
            {
                float yAxisRotation = Input.GetAxis("Mouse Y") * rotationSensitivity;
                heldObj.transform.Rotate(transform.right, yAxisRotation, Space.World);
            }
        }
        else
        {
            canDrop = true; // Enables actions again
            if (cameraLookScript != null) cameraLookScript.enabled = true;

            targetHoldAlpha = 1f;
            targetRotateAlpha = 0f;
        }
    }

    void StopClipping()
    {
        var clipRange = Vector3.Distance(heldObj.transform.position, transform.position);
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, clipRange))
        {
            heldObj.transform.position = hit.point + (hit.normal * 0.1f);
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void HideAllUI()
    {
        targetHoldAlpha = 0f;
        targetRotateAlpha = 0f;
    }
}