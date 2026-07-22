using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractionUI : MonoBehaviour
{
    public Image image;
    public Transform handle;
    public void setUI(Sprite sprite, Transform handleTransform)
    {
        image.sprite = sprite;
        handle = handleTransform;
    }

    private void FixedUpdate()
    {
        if(handle != null)
        {
            image.rectTransform.position = InteractionManager.instance.playerCam.WorldToScreenPoint(handle.position);
        }
    }
}
