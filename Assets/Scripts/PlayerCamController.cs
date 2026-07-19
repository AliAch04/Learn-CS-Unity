using UnityEngine;

public class PlayerCamController : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [SerializeField] private float sensX = 400f;
    [SerializeField] private float sensY = 400f;

    [Header("References")]
    [SerializeField] private Transform playerOrient;
    [SerializeField] private Transform camHolder; 

    private float rotXCam;
    private float rotYCam;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY * Time.deltaTime;

        rotYCam += mouseX;
        rotXCam -= mouseY;
        rotXCam = Mathf.Clamp(rotXCam, -80f, 80f);

        // Rotate the HOLDER instead of the camera child to maintain the pivot
        if (camHolder != null)
        {
            camHolder.rotation = Quaternion.Euler(rotXCam, rotYCam, 0);
        }

        // Rotate player body orientation on Y axis
        if (playerOrient != null)
        {
            playerOrient.rotation = Quaternion.Euler(0, rotYCam, 0);
        }
    }
}
