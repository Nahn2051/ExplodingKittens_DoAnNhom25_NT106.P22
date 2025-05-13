using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarImageManager : MonoBehaviour
{
    public Sprite[] avatarImages;
    public Sprite SetImage(int index)
    {
        if (index < 0 || index >= avatarImages.Length)
        {
            Debug.LogError("Index out of bounds");
            return null;
        }
        return avatarImages[index];
    }
}
