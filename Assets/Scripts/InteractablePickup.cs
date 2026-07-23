using UnityEngine;

public class InteractablePickup : MonoBehaviour, IInteractable
{
    [Header("UI Settings")]
    public Sprite itemSprite;
    public Transform handle;

    public KeyCode keyCode = KeyCode.E;

    public Sprite GetSprite()
    {
        return itemSprite;
    }

    public Transform GetHandle()
    {
        return handle != null ? handle : transform;
    }

    public bool CanInteract()
    {
        return Input.GetKeyDown(keyCode);
    }

    public void interact()
    {
        if (PickUpScript.instance != null)
        {
            PickUpScript.instance.PickUpObject(this.gameObject);
        }
    }
}