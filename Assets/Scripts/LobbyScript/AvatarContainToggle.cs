using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarContainToggle : MonoBehaviour
{
    public CanvasGroup avatarGroup;

    private bool isOpen = false;
    public void ToggleAvatarPanel()
    {
        isOpen = !isOpen;
        avatarGroup.alpha = isOpen ? 1 : 0;
        avatarGroup.blocksRaycasts = isOpen;
        avatarGroup.interactable = isOpen;
    }
}
