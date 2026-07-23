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
    private float rotationSensitivity = 2f;

    [Header("UI Panels")]
    [Tooltip("Attach the Canvas Group of the Hold UI here")]
    public CanvasGroup holdUIGroup;
    [Tooltip("Attach the Canvas Group of the Rotate UI here")]
    public CanvasGroup rotateUIGroup;
    public float uiFadeSpeed = 8f; // How fast the UI fades in and out

    public GameObject heldObj;
    private Rigidbody heldObjRb;
    private bool canDrop = true;
    private Vector3 localCenterOffset;
    private float dropCooldown = 0f;

    // Internal targets for the smooth fade
    private float targetHoldAlpha = 0f;
    private float targetRotateAlpha = 0f;

    private void Awake()
    {
        instance = this;

        // Ensure UI is completely invisible on start
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
        if (heldObj != null)
        {
            MoveObject();
            RotateObject();

            if (Input.GetKeyDown(KeyCode.E) && canDrop == true)
            {
                StopClipping();
                DropObject();
            }
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

            // Tell Hold UI to fade in
            targetHoldAlpha = 1f;
            targetRotateAlpha = 0f;
        }
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

    void RotateObject()
    {
        if (Input.GetKey(KeyCode.R))
        {
            canDrop = false;
            if (cameraLookScript != null) cameraLookScript.enabled = false;

            // Trigger Rotate UI fade in, Hold UI fade out
            targetHoldAlpha = 0f;
            targetRotateAlpha = 1f;

            float XaxisRotation = Input.GetAxis("Mouse X") * rotationSensitivity;
            float YaxisRotation = Input.GetAxis("Mouse Y") * rotationSensitivity;

            heldObj.transform.Rotate(transform.up, -XaxisRotation, Space.World);
            heldObj.transform.Rotate(transform.right, YaxisRotation, Space.World);
        }
        else
        {
            canDrop = true;
            if (cameraLookScript != null) cameraLookScript.enabled = true;

            // Trigger Hold UI fade in, Rotate UI fade out
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
        // Target 0 alpha for both
        targetHoldAlpha = 0f;
        targetRotateAlpha = 0f;
    }
}