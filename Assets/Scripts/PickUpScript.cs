using UnityEngine;

public class PickUpScript : MonoBehaviour
{
    public static PickUpScript instance; 

    public GameObject player;
    public Transform holdPos;
    public float throwForce = 500f;
    public float pickUpRange = 5f;
    public LayerMask holdLayer = LayerMask.NameToLayer("holdLayer");
    private float rotationSensitivity = 1f;

    public GameObject heldObj; 
    private Rigidbody heldObjRb;
    private bool canDrop = true;
    private int LayerNumber;


    private void Awake()
    {
        instance = this; 
    }

    void Start()
    {
        LayerNumber = holdLayer;
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
        if (pickUpObj.GetComponentInChildren<Rigidbody>())
        {
            heldObj = pickUpObj;
            heldObjRb = pickUpObj.GetComponentInChildren<Rigidbody>();
            heldObjRb.isKinematic = true;
            heldObjRb.transform.parent = holdPos.transform;
            heldObj.layer = LayerNumber;
            Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), player.GetComponent<Collider>(), true);
        }
    }

    void DropObject()
    {
        Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), player.GetComponent<Collider>(), false);
        heldObj.layer = 0;
        heldObjRb.isKinematic = false;
        heldObj.transform.parent = null;
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
            float XaxisRotation = Input.GetAxis("Mouse X") * rotationSensitivity;
            float YaxisRotation = Input.GetAxis("Mouse Y") * rotationSensitivity;
            heldObj.transform.Rotate(Vector3.down, XaxisRotation);
            heldObj.transform.Rotate(Vector3.right, YaxisRotation);
        }
        else
        {
            canDrop = true;
        }
    }

    void ThrowObject()
    {
        Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), player.GetComponent<Collider>(), false);
        heldObj.layer = 0;
        heldObjRb.isKinematic = false;
        heldObj.transform.parent = null;
        heldObjRb.AddForce(transform.forward * throwForce);
        heldObj = null;
    }

    void StopClipping()
    {
        var clipRange = Vector3.Distance(heldObj.transform.position, transform.position);
        RaycastHit[] hits;
        hits = Physics.RaycastAll(transform.position, transform.TransformDirection(Vector3.forward), clipRange);

        if (hits.Length > 1)
        {
            heldObj.transform.position = transform.position + new Vector3(0f, -0.5f, 0f);
        }
    }
}