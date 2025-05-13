using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyPlayerDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image playerAvatarImage;
    
    public void Initialize(string playerName, Sprite avatarSprite)
    {
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }
        
        if (playerAvatarImage != null)
        {
            playerAvatarImage.sprite = avatarSprite;
        }
    }
} 