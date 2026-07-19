using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    [SerializeField] private Transform camPos;

    // LateUpdate prevents camera jittering/stuttering
    private void LateUpdate()
    {
        if (camPos != null)
        {
            transform.position = camPos.position;
        }
    }
}
