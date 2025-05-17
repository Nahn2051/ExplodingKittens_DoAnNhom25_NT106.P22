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

    void Awake()
    {
        // Đảm bảo rằng đã ngắt kết nối Photon nếu đã kết nối trước đó
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Đã kết nối Photon - ngắt kết nối trước khi bắt đầu lại");
            StartCoroutine(DisconnectFromPhoton());
        }
    }
    
    private IEnumerator DisconnectFromPhoton()
    {
        Debug.Log("Đang ngắt kết nối Photon");
        PhotonNetwork.Disconnect();
        
        // Đợi cho đến khi ngắt kết nối hoàn toàn
        float timeout = 5f;
        float elapsed = 0f;
        
        while (elapsed < timeout && PhotonNetwork.IsConnected)
        {
            Debug.Log("Đang đợi Photon ngắt kết nối...");
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        if (PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Quá thời gian ngắt kết nối Photon");
        }
        
        Debug.Log("Đã ngắt kết nối Photon thành công");
    }
    
    private void Start()
    {
        hostButton.interactable = false;
        joinButton.interactable = false;
        avatarButton.interactable = false;
        // Thiết lập kết nối với Photon Cloud
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Đang kết nối đến Photon...");
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
        Debug.Log("Đã kết nối đến Photon Master Server");
        hostButton.interactable = true;
        joinButton.interactable = true;
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Đã ngắt kết nối khỏi Photon: {cause}");
        hostButton.interactable = true;
        joinButton.interactable = true;
        _isJoiningOrHosting = false;
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
}
