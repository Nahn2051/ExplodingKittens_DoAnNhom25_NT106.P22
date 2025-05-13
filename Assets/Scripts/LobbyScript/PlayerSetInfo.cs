using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion;

public class PlayerSetInfo : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Text playerName;
    public Image playerImage;
    public Image backgroundImage; // Optional background for player card
    
    [Header("Visual Options")]
    public Color localPlayerColor = new Color(0.8f, 1f, 0.8f, 1f); // Highlight color for local player
    public Color otherPlayerColor = Color.white;
    
    private bool isLocalPlayer = false;
    
    public void SetPlayerName(string name)
    {
        Debug.Log($"Setting player name to: {name}");
        if (playerName == null)
        {
            Debug.LogError("playerName Text component is null!");
            return;
        }
        playerName.text = name;
    }
    
    public void SetPlayerImage(Sprite image)
    {
        Debug.Log($"Setting player image: {(image != null ? image.name : "null")}");
        if (playerImage == null)
        {
            Debug.LogError("playerImage Image component is null!");
            return;
        }
        playerImage.sprite = image;
    }
    
    public void SetIsLocalPlayer(bool isLocal)
    {
        isLocalPlayer = isLocal;
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        // Nếu có backgroundImage, đổi màu để làm nổi bật người chơi hiện tại
        if (backgroundImage != null)
        {
            backgroundImage.color = isLocalPlayer ? localPlayerColor : otherPlayerColor;
        }
        
        // Thêm hiệu ứng hoặc văn bản nếu cần, ví dụ: "YOU" cho người chơi hiện tại
        if (playerName != null && isLocalPlayer && !playerName.text.EndsWith(" (YOU)"))
        {
            playerName.text += " (YOU)";
        }
    }
    
    // Có thể thêm các phương thức khác tùy vào yêu cầu, như hiệu ứng động, hiển thị trạng thái, v.v.
}
