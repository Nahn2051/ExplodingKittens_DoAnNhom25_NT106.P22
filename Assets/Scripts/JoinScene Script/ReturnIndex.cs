using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnIndex : MonoBehaviour
{
    public int index; // Index to be returned
    public JoinSceneManager SetImageManager;
    public int GetIndex()
    {
        return index; // Return the index
    }

    public void SetIndex()
    {
        SetImageManager.SetAvatarImage(index); // Call the method to set the avatar image
    }
}
