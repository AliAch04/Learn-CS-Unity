using UnityEngine;

public class PickUpScript : MonoBehaviour
{
    public static PickUpScript instance;

    public GameObject player;
    public Transform holdPos;

    [Header("Camera & Controls")]
    [Tooltip("Drag the script that controls your mouse looking here so we can disable it while rotating objects.")]
    public PlayerCamController cameraLookScript;

    [Header("Physics Settings")]
    public float throwForce = 500f;
    public float pickUpRange = 5f;
    public string holdLayerName = "HoldLayer";
    private int holdLayerIndex;

    private float rotationSensitivity = 2f;

    public GameObject heldObj;
    private Rigidbody heldObjRb;
    private bool canDrop = true;

    private void Awake()
    {
        instance = this;
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

            if (Input.GetKeyDown(KeyCode.Mouse0) && canDrop == true)
            {
                StopClipping();
                ThrowObject();
            }
        }
    }

    public void PickUpObject(GameObject pickUpObj)
    {
        Rigidbody rb = pickUpObj.GetComponent<Rigidbody>();
        if (rb == null) rb = pickUpObj.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            heldObj = pickUpObj;
            heldObjRb = rb;

            heldObjRb.isKinematic = true;
            heldObj.transform.parent = holdPos.transform;

            SetLayerRecursively(heldObj, holdLayerIndex);

            // FIX 1: Completely disable colliders while holding to prevent ALL physics glitches
            Collider[] colliders = heldObj.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
        }
    }

    void DropObject()
    {
        // Re-enable colliders when dropping
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
        // Re-enable colliders when throwing
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
        heldObj.transform.position = holdPos.transform.position;
    }

    void RotateObject()
    {
        if (Input.GetKey(KeyCode.R))
        {
            canDrop = false;

            // Disable the player's ability to look around
            if (cameraLookScript != null) cameraLookScript.enabled = false;

            float XaxisRotation = Input.GetAxis("Mouse X") * rotationSensitivity;
            float YaxisRotation = Input.GetAxis("Mouse Y") * rotationSensitivity;

            // FIX 2: Rotate around the exact center of the hold position, using the camera's up/right directions
            heldObj.transform.RotateAround(holdPos.position, transform.up, -XaxisRotation);
            heldObj.transform.RotateAround(holdPos.position, transform.right, YaxisRotation);
        }
        else
        {
            canDrop = true;

            // Re-enable the player's ability to look around
            if (cameraLookScript != null) cameraLookScript.enabled = true;
        }
    }

    void StopClipping()
    {
        var clipRange = Vector3.Distance(heldObj.transform.position, transform.position);

        // Much cleaner clipping check since the object's colliders are disabled!
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, clipRange))
        {
            // If there's a wall between the camera and the hold point, drop it slightly in front of the wall
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
}