using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class LobbySceneManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_Text roomIdText;
    public Button copyRoomIdButton;
    public Transform playerListContainer;
    public GameObject playerItemPrefab;
    public Button startGameButton;
    public Button leaveButton;
    public AudioMixer MainAudioMixer;
    [Header("Avatar Display")]
    public LobbyPlayerDisplay localPlayerDisplay;
    
    [NonSerialized]
    private PhotonView _photonView;
    [NonSerialized]
    private string _roomId;
    
    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        if (_photonView == null)
        {
            Debug.LogError("PhotonView không tìm thấy trên LobbySceneManager. Vui lòng thêm component PhotonView.");
            _photonView = gameObject.AddComponent<PhotonView>();
        }
    }
    
    private void Start()
    {
        StartCoroutine(InitializeAfterConnection());
        float vol = PlayerPrefs.GetFloat("MusicVol", 0.75f); // Giá trị mặc định 0.75
        MainAudioMixer.SetFloat("MusicVol", vol);
    }
    
    private IEnumerator InitializeAfterConnection()
    {
        // Đợi cho đến khi kết nối đến phòng hoàn tất
        float timeout = 10f;
        float elapsed = 0f;
        
        while (elapsed < timeout && (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null))
        {
            Debug.Log("Đang đợi kết nối Photon hoàn tất...");
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        
        if (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogError("Kết nối đến phòng thất bại sau khi timeout. Quay lại màn hình chính.");
            LeaveRoom();
            yield break;
        }
        
        // Thiết lập UI
        SetupLobbyUI();
        
        // Đăng ký người chơi
        TryRegisterPlayer();
    }
    
    private void SetupLobbyUI()
    {
        // Setup ID phòng
        _roomId = PhotonNetwork.CurrentRoom.Name;
        if (roomIdText != null)
            roomIdText.text = $"Room ID: {_roomId}";
            
        // Thiết lập nút copy
        if (copyRoomIdButton != null)
        {
            copyRoomIdButton.onClick.AddListener(CopyRoomIdToClipboard);
        }
        
        // Thiết lập các nút
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            
            // Hiển thị nút cho tất cả người chơi nhưng vô hiệu hóa nếu không phải host
            startGameButton.gameObject.SetActive(true);
            UpdateStartGameButton();
        }
        
        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(OnLeaveClicked);
        }
        
        // Hiển thị thông tin người chơi
        UpdatePlayerAvatarDisplay();
        
        // Cập nhật danh sách người chơi
        UpdatePlayerList();
    }
    
    private void UpdateStartGameButton()
    {
        if (startGameButton != null)
        {
            bool isHost = PhotonNetwork.IsMasterClient;
            bool hasEnoughPlayers = PhotonNetwork.CurrentRoom.PlayerCount >= 2;
            
            // Chỉ cho phép start nếu là host và có ít nhất 2 người chơi
            startGameButton.interactable = isHost && hasEnoughPlayers;
            
            // Cập nhật text trên nút nếu không đủ người chơi
            TextMeshProUGUI buttonText = startGameButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (!hasEnoughPlayers && isHost)
                {
                    buttonText.text = "1/2";
                }
                else
                {
                    buttonText.text = "Play";
                }
            }
        }
    }
    
    private void TryRegisterPlayer()
    {
        try
        {
            // Kiểm tra kết nối trước khi đăng ký
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("Không thể đăng ký người chơi: Chưa kết nối với Photon.");
                return;
            }
            
            if (PhotonNetwork.CurrentRoom == null)
            {
                Debug.LogError("Không thể đăng ký người chơi: Không có phòng nào đang hoạt động.");
            return;
        }
        
            // Đăng ký thông tin người chơi
            Player player = PhotonNetwork.LocalPlayer;
            int avatarIndex = PlayerData.Instance != null ? PlayerData.Instance.AvatarIndex : 0;
            
            // Sử dụng CustomProperties để lưu trữ thông tin người chơi
            ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
            playerProps["AvatarIndex"] = avatarIndex;
            player.SetCustomProperties(playerProps);
            
            // Thông báo đến tất cả người chơi có người mới
            if (_photonView != null && _photonView.IsMine)
            {
                _photonView.RPC("RPC_PlayerJoined", RpcTarget.AllBuffered);
            }
            
            Debug.Log($"Đã đăng ký người chơi: {player.NickName} với Avatar: {avatarIndex}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Lỗi khi đăng ký người chơi: {e.Message}\n{e.StackTrace}");
        }
    }
    
    public void UpdatePlayerAvatarDisplay()
    {
        if (localPlayerDisplay != null && PlayerData.Instance != null)
        {
            localPlayerDisplay.SetPlayerInfo(PlayerData.Instance.PlayerName, PlayerData.Instance.AvatarIndex);
        }
    }
    
    public void UpdatePlayerList()
    {
        if (playerListContainer == null || playerItemPrefab == null) return;
        
        // Xóa danh sách hiện tại
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Tạo lại danh sách từ tất cả người chơi trong phòng
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject playerItemGO = Instantiate(playerItemPrefab, playerListContainer);
            LobbyPlayerDisplay playerDisplay = playerItemGO.GetComponent<LobbyPlayerDisplay>();
            
            if (playerDisplay != null)
            {
                int avatarIndex = 0;
                if (player.CustomProperties.ContainsKey("AvatarIndex"))
                    avatarIndex = (int)player.CustomProperties["AvatarIndex"];
                
                playerDisplay.SetPlayerInfo(player.NickName, avatarIndex);
                
                // Đánh dấu chủ phòng
                if (player.IsMasterClient)
                {
                    playerDisplay.MarkAsHost();
                }
            }
        }
        
        // Cập nhật trạng thái nút bắt đầu game
        UpdateStartGameButton();
    }
    
    [PunRPC]
    private void RPC_PlayerJoined()
    {
        try
        {
            Debug.Log("RPC_PlayerJoined được gọi");
            UpdatePlayerList();
        }
        catch (Exception e)
        {
            Debug.LogError($"Lỗi trong RPC_PlayerJoined: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void OnStartGameClicked()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            try
            {
                _photonView.RPC("RPC_StartGame", RpcTarget.All);
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi gọi RPC_StartGame: {e.Message}");
                // Fallback nếu RPC thất bại
                PhotonNetwork.LoadLevel("InGame");
            }
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Không thể bắt đầu game: Cần ít nhất 2 người chơi.");
        }
    }
    
    [PunRPC]
    private void RPC_StartGame()
    {
        try
        {
            Debug.Log("Bắt đầu chuyển cảnh đến InGame");
            // Tất cả người chơi đều load scene chứ không chỉ host
            PhotonNetwork.LoadLevel("InGame");
        }
        catch (Exception e)
        {
            Debug.LogError($"Lỗi trong RPC_StartGame: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void OnLeaveClicked()
    {
        LeaveRoom();
    }
    
    private void LeaveRoom()
    {
        StartCoroutine(LeaveRoomRoutine());
    }
    
    private IEnumerator LeaveRoomRoutine()
    {
        if (PhotonNetwork.InRoom)
        {
            // Nếu là chủ phòng, thông báo cho người chơi khác
            if (PhotonNetwork.IsMasterClient && _photonView != null)
            {
                try
                {
                    _photonView.RPC("RPC_HostLeaving", RpcTarget.Others);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Lỗi khi gọi RPC_HostLeaving: {e.Message}");
                }
            }
            
            PhotonNetwork.LeaveRoom();
        }
        
        // Đợi ngắt kết nối khỏi phòng
        while (PhotonNetwork.InRoom)
        {
            yield return null;
        }
        
        // Chuyển về màn hình JoinScene
        SceneManager.LoadScene("JoinScene");
    }
    
    [PunRPC]
    private void RPC_HostLeaving()
    {
        try
        {
            Debug.Log("Chủ phòng đã rời đi.");
            // Hiển thị thông báo chủ phòng đã rời đi
            HostLeaveNotification notification = FindObjectOfType<HostLeaveNotification>();
            if (notification != null)
            {
                notification.ShowNotification();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Lỗi trong RPC_HostLeaving: {e.Message}\n{e.StackTrace}");
        }
    }
    
    // Photon Callbacks
    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Cập nhật danh sách người chơi khi có thay đổi
        UpdatePlayerList();
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Người chơi {newPlayer.NickName} đã vào phòng.");
        UpdatePlayerList();
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Người chơi {otherPlayer.NickName} đã rời phòng.");
        UpdatePlayerList();
        
        // Nếu chủ phòng rời đi, hiển thị thông báo
        if (otherPlayer.IsMasterClient)
        {
            HostLeaveNotification notification = FindObjectOfType<HostLeaveNotification>();
            if (notification != null)
            {
                notification.ShowNotification();
            }
        }
    }
    
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"Chủ phòng mới: {newMasterClient.NickName}");
        UpdatePlayerList();
        
        // Cập nhật quyền bắt đầu game
        UpdateStartGameButton();
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Ngắt kết nối khỏi Photon: {cause}");
        SceneManager.LoadScene("JoinScene");
    }
    
    public void CopyRoomIdToClipboard()
    {
        if (string.IsNullOrEmpty(_roomId)) return;
        
        GUIUtility.systemCopyBuffer = _roomId;
        
        // Hiển thị thông báo (tùy chọn)
        Debug.Log($"Đã sao chép Room ID: {_roomId} vào clipboard");
        
        // Nếu có một UI thông báo, bạn có thể hiển thị nó ở đây
        // ShowCopyNotification();
    }
}
