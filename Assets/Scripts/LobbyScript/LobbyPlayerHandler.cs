using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion;

public class LobbyPlayerHandler : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject playerAvatarPrefab; // Prefab của PlayerAvatar
    public Transform playerContainerTransform; // Transform chứa các player avatars
    
    [Header("References")]
    public LobbyManager lobbyManager;
    public AvatarImageManager avatarManager;
    
    private Dictionary<PlayerRef, GameObject> playerAvatars = new Dictionary<PlayerRef, GameObject>();
    private bool isInitialized = false;
    
    private void Awake()
    {
        // Đảm bảo object này không bị hủy khi load scene
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        Initialize();
    }
    
    public void Initialize()
    {
        if (isInitialized)
            return;
            
        if (lobbyManager == null)
        {
            lobbyManager = FindObjectOfType<LobbyManager>();
            if (lobbyManager == null)
            {
                Debug.LogError("LobbyManager không tìm thấy! LobbyPlayerHandler sẽ không hoạt động.");
                return;
            }
        }
        
        if (avatarManager == null)
        {
            avatarManager = FindObjectOfType<AvatarImageManager>();
            if (avatarManager == null)
            {
                Debug.LogError("AvatarImageManager không tìm thấy! Avatar sẽ không hiển thị đúng.");
            }
        }
        
        if (playerContainerTransform == null)
        {
            GameObject container = GameObject.Find("PLayerContain");
            if (container != null)
            {
                playerContainerTransform = container.transform;
            }
            else
            {
                Debug.LogError("Không tìm thấy PLayerContain! Người chơi sẽ không hiển thị.");
                return;
            }
        }
        
        if (playerAvatarPrefab == null)
        {
            Debug.LogError("playerAvatarPrefab không được gán! Vui lòng gán PlayerAvatar prefab vào Inspector.");
            return;
        }
        
        // Đảm bảo container rỗng khi bắt đầu
        ClearPlayerContainer();
        
        isInitialized = true;
        Debug.Log("LobbyPlayerHandler đã khởi tạo thành công!");
    }
    
    // Phương thức để xóa tất cả player avatars
    public void ClearPlayerContainer()
    {
        if (!isInitialized)
            Initialize();
            
        if (playerContainerTransform != null)
        {
            foreach (Transform child in playerContainerTransform)
            {
                Destroy(child.gameObject);
            }
        }
        
        playerAvatars.Clear();
    }
    
    // Phương thức để thêm player vào UI
    public void AddPlayer(PlayerRef playerRef, string playerName, int avatarIndex)
    {
        if (!isInitialized)
            Initialize();
            
        if (!isInitialized)
        {
            Debug.LogError("LobbyPlayerHandler chưa được khởi tạo! Không thể thêm người chơi.");
            return;
        }
            
        // Kiểm tra nếu player đã tồn tại
        if (playerAvatars.ContainsKey(playerRef))
        {
            UpdatePlayer(playerRef, playerName, avatarIndex);
            return;
        }
        
        // Tạo player avatar mới
        GameObject playerAvatar = Instantiate(playerAvatarPrefab, playerContainerTransform);
        playerAvatars[playerRef] = playerAvatar;
        
        // Xác định xem đây có phải là local player không
        bool isLocalPlayer = (lobbyManager != null && lobbyManager.runner != null && lobbyManager.runner.LocalPlayer == playerRef);
        
        // Cập nhật thông tin player
        UpdatePlayerInfo(playerAvatar, playerName, avatarIndex, isLocalPlayer);
        
        // Kiểm tra nút Play
        CheckPlayButtonState();
    }
    
    // Phương thức để cập nhật thông tin player
    public void UpdatePlayer(PlayerRef playerRef, string playerName, int avatarIndex)
    {
        if (!isInitialized)
            Initialize();
            
        if (playerAvatars.TryGetValue(playerRef, out GameObject playerAvatar))
        {
            // Xác định xem đây có phải là local player không
            bool isLocalPlayer = (lobbyManager != null && lobbyManager.runner != null && lobbyManager.runner.LocalPlayer == playerRef);
            
            UpdatePlayerInfo(playerAvatar, playerName, avatarIndex, isLocalPlayer);
        }
    }
    
    // Phương thức để xóa player
    public void RemovePlayer(PlayerRef playerRef)
    {
        if (!isInitialized)
            Initialize();
            
        if (playerAvatars.TryGetValue(playerRef, out GameObject playerAvatar))
        {
            Destroy(playerAvatar);
            playerAvatars.Remove(playerRef);
            
            // Kiểm tra nút Play
            CheckPlayButtonState();
        }
    }
    
    // Phương thức để cập nhật thông tin avatar
    private void UpdatePlayerInfo(GameObject playerAvatar, string playerName, int avatarIndex, bool isLocalPlayer)
    {
        PlayerSetInfo playerSetInfo = playerAvatar.GetComponent<PlayerSetInfo>();
        
        if (playerSetInfo != null)
        {
            playerSetInfo.SetPlayerName(playerName);
            
            if (avatarManager != null)
            {
                Sprite avatarSprite = avatarManager.SetImage(avatarIndex);
                if (avatarSprite != null)
                {
                    playerSetInfo.SetPlayerImage(avatarSprite);
                }
            }
            
            // Đánh dấu nếu đây là local player
            playerSetInfo.SetIsLocalPlayer(isLocalPlayer);
        }
    }
    
    // Kiểm tra số lượng người chơi để kích hoạt nút Play
    private void CheckPlayButtonState()
    {
        if (!isInitialized)
            return;
            
        if (lobbyManager != null && lobbyManager.runner != null && lobbyManager.runner.IsServer)
        {
            // Kích hoạt nút Play chỉ khi có ít nhất 2 người chơi
            lobbyManager.playButton.interactable = (playerAvatars.Count >= 2);
            
            // Hiển thị thông báo nếu số lượng người chơi không đủ
            if (playerAvatars.Count < 2 && lobbyManager.playButton.gameObject.activeInHierarchy)
            {
                lobbyManager.ShowNotification("Cần ít nhất 2 người chơi để bắt đầu game!");
            }
        }
    }
}