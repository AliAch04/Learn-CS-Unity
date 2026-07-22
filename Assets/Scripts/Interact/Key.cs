using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Key : MonoBehaviour, IInteractable
{
    public KeyCode keyCode = KeyCode.E;
    public Sprite keySprite;
    public void interact()
    {
        Debug.Log("Interact!");
    }

    public Sprite GetSprite()
    {
        return keySprite;
    }

    public bool CanInteract()
    {
        return (Input.GetKeyDown(keyCode));
    }

    public Transform GetHandle()
    {
        return gameObject.transform;
    }
}
