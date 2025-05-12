using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerSetInfo : MonoBehaviour
{
    public TMP_Text playerName;
    public Image playerImage;

    public void SetPlayerName(string name)
    {
        playerName.text = name;
    }
    public void SetPlayerImage(Sprite image)
    {
        playerImage.sprite = image;
    }
}
