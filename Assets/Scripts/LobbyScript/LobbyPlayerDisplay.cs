using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyPlayerDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image playerAvatarImage;

    [Header("Avatar References")]
    [SerializeField] private AvatarImageManager avatarManager;
    
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

    public void SetPlayerInfo(string playerName, int avatarIndex)
    {
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        if (playerAvatarImage != null)
        {
            // Tìm AvatarImageManager nếu không có
            if (avatarManager == null)
            {
                avatarManager = FindObjectOfType<AvatarImageManager>();
            }

            // Lấy sprite từ AvatarImageManager
            if (avatarManager != null)
            {
                playerAvatarImage.sprite = avatarManager.SetImage(avatarIndex);
            }
            else
            {
                Debug.LogError("Không tìm thấy AvatarImageManager");
            }
        }
    }

    public void MarkAsHost()
    {
        if (playerNameText != null)
        {
            playerNameText.text = playerNameText.text + " (Host)";
        }
    }
} 