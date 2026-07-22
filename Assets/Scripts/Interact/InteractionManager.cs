using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager instance;
    public InteractionUI ui;

    public Camera playerCam;
    public float dist = 3;

    private void Awake()
    {
        instance = this;
    }

    public void ShowUI(IInteractable interactable)
    {
        ui.gameObject.SetActive(true);
        ui.setUI(interactable.GetSprite(),interactable.GetHandle());

    }

    public void HideUI()
    {
        ui.gameObject.SetActive(false);
    }

    private void Update()
    {
        if(playerCam == default)
        {
            return;
        }

        RaycastHit hit = default;

        if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out hit, dist))
        {
            if(hit.transform.TryGetComponent(out IInteractable interactable))
            {
                ShowUI(interactable);
                if (interactable.CanInteract())
                {
                    interactable.interact();
                }

                return;
            }
        }

        HideUI();

    }
}
