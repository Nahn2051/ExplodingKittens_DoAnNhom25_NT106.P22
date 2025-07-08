using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image playerAvatarImage;
    [SerializeField] private TMP_Text cardCountText;
    [SerializeField] private Image avatarBackground;
    
    [Header("Avatar References")]
    [SerializeField] private AvatarImageManager avatarManager;
    
    [HideInInspector] public int PlayerActorNumber { get; private set; }
    
    private void Start()
    {
        // Tìm AvatarImageManager nếu không được set
        if (avatarManager == null)
        {
            avatarManager = FindObjectOfType<AvatarImageManager>();
        }
        
        // Tìm background của avatar nếu không được set
        if (avatarBackground == null && playerAvatarImage != null)
        {
            // Tìm component Image cha của playerAvatarImage
            avatarBackground = playerAvatarImage.transform.parent.GetComponent<Image>();
        }
    }
    
    public void Initialize(string playerName, int avatarIndex, int actorNumber)
    {
        PlayerActorNumber = actorNumber;
        
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }
        
        if (playerAvatarImage != null && avatarManager != null)
        {
            playerAvatarImage.sprite = avatarManager.SetImage(avatarIndex);
        }
        
        // Mặc định số lá bài là 5 (số lá bài phát ban đầu)
        UpdateCardCount(5);
    }
    
    public void UpdateCardCount(int count)
    {
        if (cardCountText != null)
        {
            cardCountText.text = count.ToString();
        }
    }
    
    public void SetActiveState(bool isActive, Color color)
    {
        if (avatarBackground == null && playerAvatarImage != null)
        {
            // Tìm component Image cha của playerAvatarImage nếu chưa có
            avatarBackground = playerAvatarImage.transform.parent.GetComponent<Image>();
        }
        
        if (avatarBackground != null)
        {
            avatarBackground.color = color;
        }
    }
    
    public void SetEliminatedState(bool isEliminated)
    {
        if (avatarBackground != null)
        {
            // Đổi màu thành đỏ hoặc xám để báo hiệu player bị loại
            avatarBackground.color = isEliminated ? Color.red : Color.white;
        }
        
        // Có thể thêm hiệu ứng khác như làm mờ
        if (playerAvatarImage != null)
        {
            playerAvatarImage.color = isEliminated ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
        }
        
        if (playerNameText != null)
        {
            playerNameText.color = isEliminated ? Color.gray : Color.white;
        }
    }
}