using UnityEngine;

public interface IInteractable
{
    public void interact();
    public Sprite GetSprite();
    public bool CanInteract();
    public Transform GetHandle();
}
