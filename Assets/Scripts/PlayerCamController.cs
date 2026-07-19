using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamController : MonoBehaviour
{
    [SerializeField] float sensX; 
    [SerializeField] float sensY;

    [SerializeField] Transform playerOrient;

    float rotXCam;
    float rotYCam;
    void Start()
    {
        // Lock the cursor in the center of the screen AND make it invisible 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
        
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        //Debug.Log(mouseX);
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        rotYCam += mouseX;
        rotXCam -= mouseY;
        Debug.Log($"(X : {rotXCam}, Y : {rotYCam})");

        // Set limit to X rotation
        rotXCam = Mathf.Clamp(rotXCam, -80f, 80f);

        // Apply the movements to the Cam & player
        transform.rotation = Quaternion.Euler(rotXCam, rotYCam, 0);
        playerOrient.rotation = Quaternion.Euler(0, rotYCam, 0);


    }
}
