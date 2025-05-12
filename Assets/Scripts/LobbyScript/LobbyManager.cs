using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Fusion.Sockets;
using static Unity.Collections.Unicode;

public class LobbySceneManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkRunner runner;
    public Button exitButton;
    public Button playButton;
    public TextMeshProUGUI roomIDText;
    public GameObject playerListContainer; // Parent GameObject of player info slots
    public GameObject playerInfoPrefab;    // Prefab containing PlayerSetInfo
    public AvatarImageManager avatarImageManager;

    private Dictionary<PlayerRef, GameObject> playerInfoSlots = new Dictionary<PlayerRef, GameObject>();
    private Dictionary<PlayerRef, (string playerName, int avatarIndex)> playerInfos = new();

    void Start()
    {
        // Get the NetworkRunner (Important: Assumes it exists in the scene or DontDestroyOnLoad)
        runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("NetworkRunner not found in LobbyScene!");
        }

        exitButton.onClick.AddListener(OnExitClicked);
        playButton.onClick.AddListener(OnPlayClicked);

        // Initially hide the play button
        playButton.gameObject.SetActive(false);

        // Display the Room ID
        if (PlayerData.Instance != null)
        {
            roomIDText.text = "Room ID: " + PlayerData.Instance.RoomID;
        }
        else
        {
            Debug.LogError("PlayerData.Instance is null in LobbyScene!");
        }

        // Check if we are the host and show the Play button
        if (runner.IsServer)
        {
            playButton.gameObject.SetActive(true);
        }

        if (!runner.IsServer && PlayerData.Instance != null)
        {
            RPC_ClientSendsInfo(PlayerData.Instance.PlayerName, PlayerData.Instance.AvatarIndex);
        }

        // Fusion 2 doesn't use SimulationMessageReceived anymore
        // Register to scene load completion event
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Spawn existing players
        foreach (var player in runner.ActivePlayers)
        {
            SpawnPlayerInfo(player);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (scene.name == "InGame")
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void SpawnPlayerInfo(PlayerRef player)
    {
        if (playerInfoPrefab != null && playerListContainer != null)
        {
            GameObject playerSlot = Instantiate(playerInfoPrefab, playerListContainer.transform);
            playerInfoSlots.Add(player, playerSlot);

            // Request player info from the client
            if (runner.IsServer)
            {
                // Use RPC instead of SimulationMessage
                RPC_SendPlayerInfo(player, PlayerData.Instance.PlayerName, PlayerData.Instance.AvatarIndex);
            }
        }
        else
        {
            Debug.LogError("playerInfoPrefab or playerListContainer is not assigned!");
        }
    }

    // Modified to use RPCs in Fusion 2
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SendPlayerInfo(PlayerRef player, string playerName, int avatarIndex)
    {
        UpdatePlayerInfo(player, playerName, avatarIndex);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ClientSendsInfo(string name, int avatar)
    {
        playerInfos[runner.LocalPlayer] = (name, avatar);

        // Gửi thông tin cho tất cả mọi người
        RPC_BroadcastPlayerInfo(runner.LocalPlayer, name, avatar);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BroadcastPlayerInfo(PlayerRef player, string name, int avatar)
    {
        UpdatePlayerInfo(player, name, avatar);
    }

    private void UpdatePlayerInfo(PlayerRef player, string playerName, int avatarIndex)
    {
        if (playerInfoSlots.TryGetValue(player, out GameObject playerSlot))
        {
            PlayerSetInfo playerSetInfo = playerSlot.GetComponent<PlayerSetInfo>();
            if (playerSetInfo != null)
            {
                playerSetInfo.SetPlayerName(playerName);
                playerSetInfo.SetPlayerImage(avatarImageManager.SetImage(avatarIndex));
            }
            else
            {
                Debug.LogError("PlayerSetInfo component not found on playerInfoPrefab!");
            }
        }
    }

    // Moved implementation directly into the INetworkRunnerCallbacks methods

    private void OnExitClicked()
    {
        runner.Disconnect(runner.LocalPlayer);
        SceneManager.LoadScene("JoinScene");
    }

    private void OnPlayClicked()
    {
        if (runner.IsServer)
        {
            // In Fusion 2, use this method to load scenes
            runner.LoadScene("InGame");
        }
    }

    // Fusion Event Callbacks (Important!)
    private void OnEnable()
    {
        if (runner != null)
        {
            runner.AddCallbacks(this);
        }
    }

    private void OnDisable()
    {
        if (runner != null)
        {
            runner.RemoveCallbacks(this);
        }
    }

    // INetworkRunnerCallbacks implementation
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Gửi danh sách playerInfos cho người mới vào
            foreach (var kvp in playerInfos)
            {
                RPC_BroadcastPlayerInfo(kvp.Key, kvp.Value.playerName, kvp.Value.avatarIndex);
            }
        }
        SpawnPlayerInfo(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (playerInfoSlots.TryGetValue(player, out GameObject playerSlot))
        {
            Destroy(playerSlot);
            playerInfoSlots.Remove(player);
        }
    }

    // Implement other required INetworkRunnerCallbacks methods
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
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}

// In Fusion 2, the RPC message struct is simpler
// No need for IFusionSerializable implementation as Fusion 2
// automatically serializes primitive types and strings
// You can define NetworkObject behavior that handles this instead