using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

public class LobbySceneManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Header("UI References")]
    public TMP_Text roomIdText;
    public Button playButton;
    public Button exitButton;
    public Transform playerContainer;
    public HostLeaveNotification hostLeaveNotification;
    
    [Header("Prefabs")]
    public GameObject playerAvatarPrefab;
    public AvatarImageManager avatarImageManager;
    
    // Networking
    private NetworkRunner _runner;
    private bool _isHost = false;
    private Dictionary<PlayerRef, GameObject> _playerAvatars = new Dictionary<PlayerRef, GameObject>();
    
    private void Awake()
    {
        _runner = FindObjectOfType<NetworkRunner>();
        
        if (_runner == null)
        {
            Debug.LogError("NetworkRunner not found in scene!");
            return;
        }
        
        // Determine if we're the host
        _isHost = _runner.IsServer;
    }
    
    private void Start()
    {
        // Display the Room ID
        if (PlayerData.Instance != null)
        {
            roomIdText.text = "Room ID: " + PlayerData.Instance.RoomID;
        }
        
        // Only host can see the play button
        playButton.gameObject.SetActive(_isHost);
        
        // Setup Button Listeners
        playButton.onClick.AddListener(OnPlayButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
        
        // Register network events
        _runner.AddCallbacks(this);
        
        // Spawn our player avatar (this will be called on all clients)
        SpawnPlayerAvatar();
    }
    
    private void OnPlayButtonClicked()
    {
        if (_isHost)
        {
            // TODO: Start the game
            // _runner.SetActiveScene("InGame");
            SceneManager.LoadScene("InGame");
        }
    }
    
    private void OnExitButtonClicked()
    {
        // If host is leaving, notify everyone and shut down the session
        if (_isHost)
        {
            RPC_HostLeft();
        }
        
        // Leave the network session
        LeaveSession();
    }
    
    private void LeaveSession()
    {
        if (_runner != null)
        {
            _runner.Shutdown();
        }
        
        SceneManager.LoadScene("JoinScene");
    }
    
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_HostLeft()
    {
        if (!_isHost) // Only non-hosts should show message and return to join scene
        {
            Debug.Log("Host left the game!");
            
            // Show notification
            if (hostLeaveNotification != null)
            {
                hostLeaveNotification.ShowHostLeftNotification();
                Invoke("LeaveSession", 3f); // Give time to see the notification
            }
            else
            {
                LeaveSession();
            }
        }
    }
    
    private void SpawnPlayerAvatar()
    {
        if (playerAvatarPrefab == null)
        {
            Debug.LogError("Player Avatar Prefab is not assigned!");
            return;
        }
        
        // Create local player avatar
        GameObject avatarInstance = Instantiate(playerAvatarPrefab, playerContainer);
        
        // Get the display component
        LobbyPlayerDisplay playerDisplay = avatarInstance.GetComponent<LobbyPlayerDisplay>();
        
        if (playerDisplay != null && PlayerData.Instance != null && avatarImageManager != null)
        {
            // Initialize with player data
            playerDisplay.Initialize(
                PlayerData.Instance.PlayerName,
                avatarImageManager.SetImage(PlayerData.Instance.AvatarIndex)
            );
        }
        else
        {
            // Fallback to directly setting components if LobbyPlayerDisplay isn't available
            SetPlayerAvatarDisplay(avatarInstance, PlayerData.Instance.PlayerName, PlayerData.Instance.AvatarIndex);
        }
        
        // Add to dictionary if we have a valid runner
        if (_runner != null && _runner.LocalPlayer != PlayerRef.None)
        {
            _playerAvatars[_runner.LocalPlayer] = avatarInstance;
            
            // Notify others about our player
            RPC_PlayerJoined(PlayerData.Instance.PlayerName, PlayerData.Instance.AvatarIndex);
        }
    }
    
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PlayerJoined(string playerName, int avatarIndex, RpcInfo info = default)
    {
        // Don't duplicate our own avatar
        if (info.Source == _runner.LocalPlayer)
            return;
            
        // Create avatar for the other player
        GameObject avatarInstance = Instantiate(playerAvatarPrefab, playerContainer);
        
        // Get the display component
        LobbyPlayerDisplay playerDisplay = avatarInstance.GetComponent<LobbyPlayerDisplay>();
        
        if (playerDisplay != null && avatarImageManager != null)
        {
            // Initialize with player data
            playerDisplay.Initialize(
                playerName,
                avatarImageManager.SetImage(avatarIndex)
            );
        }
        else
        {
            // Fallback to directly setting components if LobbyPlayerDisplay isn't available
            SetPlayerAvatarDisplay(avatarInstance, playerName, avatarIndex);
        }
        
        // Add to dictionary
        _playerAvatars[info.Source] = avatarInstance;
    }
    
    // Fallback method to set player avatar display directly
    private void SetPlayerAvatarDisplay(GameObject avatarObject, string playerName, int avatarIndex)
    {
        // Set player name
        TMP_Text nameText = avatarObject.GetComponentInChildren<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = playerName;
        }
        
        // Set player avatar
        SetAvatarImage(avatarObject, avatarIndex);
    }
    
    // Set the avatar image based on the index
    private void SetAvatarImage(GameObject avatarObject, int avatarIndex)
    {
        Image avatarImage = avatarObject.GetComponentInChildren<Image>(true);
        
        if (avatarImageManager != null && avatarImage != null)
        {
            avatarImage.sprite = avatarImageManager.SetImage(avatarIndex);
        }
    }
    
    // INetworkRunnerCallbacks implementation
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} left the game");
        
        // If this player is the host and we're not the host, return to join scene
        if (runner.IsServer && player == runner.LocalPlayer && !_isHost)
        {
            Debug.Log("Host left the game!");
            
            // Show notification
            if (hostLeaveNotification != null)
            {
                hostLeaveNotification.ShowHostLeftNotification();
                Invoke("LeaveSession", 3f); // Give time to see the notification
            }
            else
            {
                LeaveSession();
            }
            
            return;
        }
        
        // Remove the player's avatar
        if (_playerAvatars.TryGetValue(player, out GameObject avatar))
        {
            Destroy(avatar);
            _playerAvatars.Remove(player);
        }
    }
    
    // Implement required INetworkRunnerCallbacks methods
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}