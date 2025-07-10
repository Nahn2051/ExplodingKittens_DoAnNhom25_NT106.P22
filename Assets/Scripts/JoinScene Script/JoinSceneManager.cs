using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System;
using System.Collections;
using UnityEngine.Audio;
using Firebase.Auth;

public class JoinSceneManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public Image Avatar;
    public AvatarImageManager avatarImageManager;
    public GameObject avatarContain;
    public TMP_InputField nameInput;
    public TMP_InputField roomIdInput;
    public Button avatarButton;
    public Button hostButton;
    public TMP_Text hostRoomFailed;
    public Button joinButton;
    public TMP_Text noRoomFoundText;
    public Button exitButton;
    public AudioMixer MainAudioMixer;
    public TextMeshProUGUI uidText;

    [Header("Network Settings")]
    public int playerLimit = 5;
    
    private bool _isJoiningOrHosting = false;
    private bool _isInitialized = false;

    void Awake()
    {
        Debug.Log("JoinSceneManager Awake - Checking Photon connection status");
        
        // Đảm bảo rằng đã ngắt kết nối hoàn toàn khỏi Photon nếu đã kết nối trước đó
        if (PhotonNetwork.IsConnected || PhotonNetwork.InRoom)
        {
            Debug.Log($"Photon đang kết nối - IsConnected: {PhotonNetwork.IsConnected}, InRoom: {PhotonNetwork.InRoom}, InLobby: {PhotonNetwork.InLobby}");
            Debug.Log("Đang thực hiện ngắt kết nối hoàn toàn...");
            StartCoroutine(CompleteDisconnectFromPhoton());
        }
        else
        {
            Debug.Log("Photon chưa kết nối - có thể khởi tạo ngay");
            _isInitialized = true;
        }
    }
    
    private IEnumerator CompleteDisconnectFromPhoton()
    {
        Debug.Log("Bắt đầu quá trình ngắt kết nối hoàn toàn");
        
        // Bước 1: Thoát khỏi room nếu đang trong room
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Đang trong room - thoát khỏi room trước");
            PhotonNetwork.LeaveRoom();
            
            // Đợi cho đến khi thoát khỏi room
            float roomTimeout = 3f;
            float roomElapsed = 0f;
            
            while (roomElapsed < roomTimeout && PhotonNetwork.InRoom)
            {
                Debug.Log("Đang đợi thoát khỏi room...");
                roomElapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
            
            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("Quá thời gian thoát khỏi room - thực hiện disconnect cưỡng bức");
            }
            else
            {
                Debug.Log("Đã thoát khỏi room thành công");
            }
        }
        
        // Bước 2: Ngắt kết nối hoàn toàn khỏi Photon
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Đang ngắt kết nối khỏi Photon Master Server");
            PhotonNetwork.Disconnect();
            
            // Đợi cho đến khi ngắt kết nối hoàn toàn
            float connectionTimeout = 5f;
            float connectionElapsed = 0f;
            
            while (connectionElapsed < connectionTimeout && PhotonNetwork.IsConnected)
            {
                Debug.Log($"Đang đợi Photon ngắt kết nối... Status: {PhotonNetwork.NetworkClientState}");
                connectionElapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
            
            if (PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("Quá thời gian ngắt kết nối Photon - có thể cần restart ứng dụng");
            }
            else
            {
                Debug.Log("Đã ngắt kết nối Photon thành công");
            }
        }
        
        Debug.Log($"Trạng thái cuối cùng - IsConnected: {PhotonNetwork.IsConnected}, InRoom: {PhotonNetwork.InRoom}, NetworkState: {PhotonNetwork.NetworkClientState}");
        _isInitialized = true;
    }
    
    private void Start()
    {
        // Vô hiệu hóa các nút cho đến khi sẵn sàng
        hostButton.interactable = false;
        joinButton.interactable = false;
        avatarButton.interactable = false;
        
        StartCoroutine(WaitForInitializationAndConnect());
    }
    
    private IEnumerator WaitForInitializationAndConnect()
    {
        // Đợi cho đến khi quá trình disconnect hoàn tất
        while (!_isInitialized)
        {
            Debug.Log("Đang đợi quá trình disconnect hoàn tất...");
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("Quá trình disconnect hoàn tất - bắt đầu kết nối mới");
        
        // Thiết lập kết nối với Photon Cloud
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Bắt đầu kết nối đến Photon với settings mới...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            Debug.Log("Photon đã kết nối - bật các nút");
            EnableButtons();
        }
        
        SetupUIListeners();

        // Đảm bảo PlayerData tồn tại
        if (PlayerData.Instance == null)
        {
            Debug.LogError("Không tìm thấy PlayerData singleton! Đang tạo mới.");
            GameObject playerDataObj = new GameObject("PlayerData");
            playerDataObj.AddComponent<PlayerData>();
        }
        
        // Tự động điền UI với dữ liệu người chơi hiện có
        InitializeUIFromPlayerData();

        float vol = PlayerPrefs.GetFloat("MusicVol", 0.75f); // Giá trị mặc định 0.75
        MainAudioMixer.SetFloat("MusicVol", vol);
        
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            string firebaseUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            Debug.Log("Firebase User ID: " + firebaseUserId);

            if (PlayerData.Instance != null)
                PlayerData.Instance.UserId = firebaseUserId;
        }
        else
        {
            Debug.LogWarning("Chưa đăng nhập Firebase! UserId không có.");
        }
    }
    
    private void EnableButtons()
    {
        hostButton.interactable = true;
        joinButton.interactable = true;
        avatarButton.interactable = true;
        Debug.Log("Đã bật tất cả các nút - sẵn sàng tạo/tham gia phòng");
    }
    void Update()
    {
        if (PlayerData.Instance != null && uidText != null)
        {
            uidText.text = "UID: " + PlayerData.Instance.UserId;
        }
    }
    private void SetupUIListeners()
    {
        avatarButton.onClick.AddListener(OnAvatarClicked);
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        exitButton.onClick.AddListener(OnExitClicked);
        
        // Ẩn thông báo lỗi ban đầu
        if (hostRoomFailed) hostRoomFailed.gameObject.SetActive(false);
        if (noRoomFoundText) noRoomFoundText.gameObject.SetActive(false);
    }
    
    private void InitializeUIFromPlayerData()
    {
        // Tự động điền tên nếu có
        if (PlayerData.Instance != null)
        {
            if (!string.IsNullOrEmpty(PlayerData.Instance.PlayerName))
            {
                nameInput.text = PlayerData.Instance.PlayerName;
                nameInput.interactable = false;
            }
                SetAvatarImage(PlayerData.Instance.AvatarIndex);
        }
    }

    private void OnAvatarClicked()
    {
        if (avatarImageManager != null && avatarContain != null)
        {
            avatarContain.SetActive(!avatarContain.activeSelf);
        }
    }
    
    public void SetAvatarImage(int index)
    {
        if (avatarImageManager != null && Avatar != null)
        {
            Avatar.sprite = avatarImageManager.SetImage(index);
            
            if (PlayerData.Instance != null)
                PlayerData.Instance.AvatarIndex = index;
                
            if (avatarContain != null)
                avatarContain.SetActive(false);
        }
    }

    private void OnHostClicked()
    {
        if (_isJoiningOrHosting) return;
        _isJoiningOrHosting = true;
        
        Debug.Log("Đã nhấp nút Host");
        hostButton.interactable = false;
        
        if (hostRoomFailed)
            hostRoomFailed.gameObject.SetActive(false);
        
        // Lấy thông tin người chơi
        string playerName = nameInput.text.Trim();
        if (string.IsNullOrEmpty(playerName) && PlayerData.Instance != null)
            playerName = PlayerData.Instance.PlayerName;
            
        if (string.IsNullOrEmpty(playerName))
            playerName = "Host_" + UnityEngine.Random.Range(1000, 9999);

        // Tạo ID phòng
        string roomID = UnityEngine.Random.Range(1000, 9999).ToString();

        // Lưu vào PlayerData
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.PlayerName = playerName;
            PlayerData.Instance.RoomID = roomID;
        }
        
        // Đặt tên người chơi cho Photon
        PhotonNetwork.NickName = playerName;
        
        Debug.Log($"Tạo phòng với ID: {roomID}, Tên người chơi: {playerName}");

        // Tạo cấu hình phòng
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = (byte)playerLimit,
            IsVisible = true,
            IsOpen = true
        };

        // Tạo hoặc tham gia phòng
        PhotonNetwork.CreateRoom(roomID, roomOptions);
    }

    private void OnJoinClicked()
    {
        if (_isJoiningOrHosting) return;
        _isJoiningOrHosting = true;
        
        Debug.Log("Đã nhấp nút Tham gia");
        joinButton.interactable = false;
        
        if (noRoomFoundText)
            noRoomFoundText.gameObject.SetActive(false);
        
        // Lấy thông tin người chơi
        string playerName = nameInput.text.Trim();
        string roomID = roomIdInput.text.Trim();

        if (string.IsNullOrEmpty(playerName) && PlayerData.Instance != null)
            playerName = PlayerData.Instance.PlayerName;
            
        if (string.IsNullOrEmpty(playerName))
            playerName = "Player_" + UnityEngine.Random.Range(1000, 9999);

        if (string.IsNullOrEmpty(roomID))
        {
            Debug.LogWarning("ID phòng trống!");
            joinButton.interactable = true;
            _isJoiningOrHosting = false;
            return;
        }

        // Lưu vào PlayerData
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.PlayerName = playerName;
            PlayerData.Instance.RoomID = roomID;
        }
        
        // Đặt tên người chơi cho Photon
        PhotonNetwork.NickName = playerName;
        
        Debug.Log($"Tham gia phòng với ID: {roomID}, Tên người chơi: {playerName}");

        // Tham gia phòng
        PhotonNetwork.JoinRoom(roomID);
    }

    private void OnExitClicked()
    {
        SceneManager.LoadScene("Main Menu");
    }
    
    // Callbacks Photon PUN
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("Đã kết nối đến Photon Master Server - sẵn sàng tạo/tham gia phòng");
        EnableButtons();
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Đã ngắt kết nối khỏi Photon: {cause}");
        
        // Reset trạng thái
        _isJoiningOrHosting = false;
        
        // Vô hiệu hóa nút để ngăn spam
        hostButton.interactable = false;
        joinButton.interactable = false;
        avatarButton.interactable = false;
        
        // Nếu disconnect không mong muốn, thử kết nối lại
        if (cause != DisconnectCause.DisconnectByClientLogic && cause != DisconnectCause.ApplicationQuit)
        {
            Debug.Log("Disconnect không mong muốn - thử kết nối lại sau 2 giây");
            StartCoroutine(ReconnectAfterDelay(2f));
        }
    }
    
    private IEnumerator ReconnectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (!PhotonNetwork.IsConnected && !_isJoiningOrHosting)
        {
            Debug.Log("Thử kết nối lại đến Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }
    
    public override void OnCreatedRoom()
    {
        Debug.Log($"Đã tạo phòng thành công: {PhotonNetwork.CurrentRoom.Name}");
    }
    
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Tạo phòng thất bại: {message} (mã: {returnCode})");
        if (hostRoomFailed) 
            hostRoomFailed.gameObject.SetActive(true);
        hostButton.interactable = true;
        _isJoiningOrHosting = false;
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log($"Đã tham gia phòng: {PhotonNetwork.CurrentRoom.Name}");
        // Chuyển đến cảnh Lobby
        PhotonNetwork.LoadLevel("LobbyScene");
    }
    
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Tham gia phòng thất bại: {message} (mã: {returnCode})");
        if (noRoomFoundText) 
            noRoomFoundText.gameObject.SetActive(true);
        joinButton.interactable = true;
        _isJoiningOrHosting = false;
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Đã thoát khỏi room thành công");
    }
    
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"Người chơi {otherPlayer.NickName} đã thoát khỏi room");
    }
    
    // Method để manually disconnect (có thể gọi từ UI nếu cần)
    [ContextMenu("Force Disconnect")]
    public void ForceDisconnect()
    {
        Debug.Log("Thực hiện ngắt kết nối cưỡng bức");
        _isInitialized = false;
        StartCoroutine(CompleteDisconnectFromPhoton());
    }
}
