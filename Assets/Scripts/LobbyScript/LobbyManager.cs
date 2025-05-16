using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Fusion.Sockets;
using static Unity.Collections.Unicode;
using System.Collections;

public class LobbyManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    public NetworkRunner runner;
    public Button exitButton;
    public Button playButton;
    public TextMeshProUGUI roomIDText;
    public GameObject playerListContainer;
    public GameObject playerInfoPrefab;
    public AvatarImageManager avatarImageManager;
    public GameObject notificationPanel;
    public TMP_Text notificationText;

    private Dictionary<PlayerRef, GameObject> playerInfoSlots = new Dictionary<PlayerRef, GameObject>();
    private Dictionary<PlayerRef, PlayerLobbyData> playerLobbyDatas = new Dictionary<PlayerRef, PlayerLobbyData>();

    public struct PlayerLobbyData : INetworkStruct
    {
        public NetworkString<_32> PlayerName;
        public int AvatarIndex;
    }

    private void Start()
    {
        Debug.Log("LobbyManager Start");
        // Các thiết lập UI và non-network được thực hiện ở đây

        if (PlayerData.Instance != null)
        {
            roomIDText.text = "Room ID: " + PlayerData.Instance.RoomID;
            Debug.Log($"Room ID set to: {PlayerData.Instance.RoomID}");
        }
        else
        {
            Debug.LogError("PlayerData.Instance is null in LobbyScene!");
        }

        exitButton.onClick.AddListener(OnExitClicked);
        playButton.onClick.AddListener(OnPlayClicked);

        // Các thiết lập Network-related sẽ được thực hiện trong Spawned

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Đây là lifecycle của Fusion 2 - sẽ được gọi khi NetworkObject được khởi tạo hoàn chỉnh
    public override void Spawned()
    {
        Debug.Log("LobbyManager Spawned - This is a network object now");

        // Lấy tham chiếu runner
        runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("NetworkRunner not found after spawning!");
            return;
        }

        // Đăng ký callbacks
        runner.AddCallbacks(this);

        // Xóa danh sách người chơi cũ nếu có
        ClearPlayerList();

        // Thiết lập UI dựa trên vai trò
        playButton.gameObject.SetActive(runner.IsServer);
        playButton.interactable = false;

        // Thiết lập thông tin người chơi local
        if (runner.LocalPlayer != default)
        {
            SetupLocalPlayerInfo();

            // Client yêu cầu danh sách người chơi
            if (!runner.IsServer)
            {
                Debug.Log("Client requesting player list");
                RPC_RequestPlayerList();
            }
        }
        else
        {
            Debug.LogError("LocalPlayer is default in Spawned");
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Debug.Log("LobbyManager Despawned");

        // Hủy đăng ký callbacks để tránh memory leak
        if (runner != null)
        {
            runner.RemoveCallbacks(this);
        }

        // Xóa tham chiếu
        this.runner = null;
    }

    private void SetupLocalPlayerInfo()
    {
        if (PlayerData.Instance == null)
        {
            Debug.LogError("PlayerData.Instance is null in SetupLocalPlayerInfo");
            return;
        }

        Debug.Log($"Setting up local player: {PlayerData.Instance.PlayerName}, Avatar: {PlayerData.Instance.AvatarIndex}");

        if (runner.IsServer)
        {
            PlayerLobbyData data = new PlayerLobbyData
            {
                PlayerName = PlayerData.Instance.PlayerName,
                AvatarIndex = PlayerData.Instance.AvatarIndex
            };

            // Thêm vào cache
            playerLobbyDatas[runner.LocalPlayer] = data;

            // Tạo UI
            SpawnPlayerInfo(runner.LocalPlayer, PlayerData.Instance.PlayerName, PlayerData.Instance.AvatarIndex);

            // Broadcast
            RPC_BroadcastPlayerInfo(runner.LocalPlayer, PlayerData.Instance.PlayerName, PlayerData.Instance.AvatarIndex);
        }
        else
        {
            Debug.Log("Client sends info to server");
            // An toàn để gọi RPC vì đang trong Spawned
            RPC_ClientSendsInfo(PlayerData.Instance.PlayerName, PlayerData.Instance.AvatarIndex);
        }
    }

    private void RefreshPlayerList()
    {
        if (runner == null) return;

        Debug.Log("Refreshing player list");
        // Clear existing UI
        ClearPlayerList();

        // Rebuild the list from networked data
        foreach (var kvp in playerLobbyDatas)
        {
            SpawnPlayerInfo(kvp.Key, kvp.Value.PlayerName.ToString(), kvp.Value.AvatarIndex);
        }
    }

    private void ClearPlayerList()
    {
        Debug.Log($"Clearing player list. Slots count: {playerInfoSlots.Count}");

        if (playerListContainer != null)
        {
            // Destroy all children of playerListContainer
            foreach (Transform child in playerListContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (var playerSlot in playerInfoSlots.Values)
        {
            if (playerSlot != null)
            {
                Destroy(playerSlot);
            }
        }
        playerInfoSlots.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (scene.name == "InGame")
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void SpawnPlayerInfo(PlayerRef player, string playerName, int avatarIndex)
    {
        if (playerInfoPrefab == null || playerListContainer == null)
        {
            Debug.LogError($"playerInfoPrefab: {playerInfoPrefab}, playerListContainer: {playerListContainer}");
            return;
        }

        Debug.Log($"Spawning player info UI for: {playerName}, Avatar: {avatarIndex}");

        // Xóa slot hiện có nếu có
        if (playerInfoSlots.TryGetValue(player, out GameObject existingSlot) && existingSlot != null)
        {
            Destroy(existingSlot);
        }

        // Tạo slot mới
        GameObject playerSlot = Instantiate(playerInfoPrefab, playerListContainer.transform);
        playerInfoSlots[player] = playerSlot;

        PlayerSetInfo playerSetInfo = playerSlot.GetComponent<PlayerSetInfo>();
        if (playerSetInfo != null)
        {
            playerSetInfo.SetPlayerName(playerName);

            if (avatarImageManager != null)
            {
                Sprite avatarSprite = avatarImageManager.SetImage(avatarIndex);
                if (avatarSprite != null)
                {
                    playerSetInfo.SetPlayerImage(avatarSprite);
                }
                else
                {
                    Debug.LogError($"Avatar sprite is null for index: {avatarIndex}");
                }
            }
            else
            {
                Debug.LogError("avatarImageManager is not assigned!");
            }

            // Đánh dấu người chơi hiện tại
            bool isLocalPlayer = (runner != null && runner.LocalPlayer == player);
            playerSetInfo.SetIsLocalPlayer(isLocalPlayer);
        }
        else
        {
            Debug.LogError("PlayerSetInfo component not found on playerInfoPrefab!");
        }

        // Cập nhật trạng thái nút Play nếu là server
        if (runner != null && runner.IsServer)
        {
            UpdatePlayButtonState();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ClientSendsInfo(string name, int avatar, RpcInfo info = default)
    {
        Debug.Log($"RPC_ClientSendsInfo from {info.Source}, Name: {name}, Avatar: {avatar}");

        PlayerLobbyData data = new PlayerLobbyData
        {
            PlayerName = name,
            AvatarIndex = avatar
        };

        // Thêm vào cache server
        playerLobbyDatas[info.Source] = data;

        // Broadcast
        RPC_BroadcastPlayerInfo(info.Source, name, avatar);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestPlayerList(RpcInfo info = default)
    {
        Debug.Log($"RPC_RequestPlayerList from {info.Source}. Player count: {playerLobbyDatas.Count}");

        foreach (var kvp in playerLobbyDatas)
        {
            RPC_SendPlayerInfoToClient(info.Source, kvp.Key, kvp.Value.PlayerName.ToString(), kvp.Value.AvatarIndex);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BroadcastPlayerInfo(PlayerRef player, string name, int avatar)
    {
        Debug.Log($"RPC_BroadcastPlayerInfo: Player: {player}, Name: {name}, Avatar: {avatar}");

        // Cập nhật cache
        PlayerLobbyData data = new PlayerLobbyData
        {
            PlayerName = name,
            AvatarIndex = avatar
        };
        playerLobbyDatas[player] = data;

        // Cập nhật UI
        SpawnPlayerInfo(player, name, avatar);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SendPlayerInfoToClient(PlayerRef targetPlayer, PlayerRef playerToShow, string name, int avatar, RpcInfo info = default)
    {
        Debug.Log($"RPC_SendPlayerInfoToClient: Target: {targetPlayer}, PlayerToShow: {playerToShow}, Name: {name}, Avatar: {avatar}");

        // Only process this RPC if we are the target client
        if (runner.LocalPlayer != targetPlayer) return;

        // Update local cache
        PlayerLobbyData data = new PlayerLobbyData
        {
            PlayerName = name,
            AvatarIndex = avatar
        };
        playerLobbyDatas[playerToShow] = data;

        // Update UI
        SpawnPlayerInfo(playerToShow, name, avatar);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UpdatePlayerInfo(PlayerRef player, string name, int avatar)
    {
        Debug.Log($"RPC_UpdatePlayerInfo: Player: {player}, Name: {name}, Avatar: {avatar}");

        // Update local cache
        PlayerLobbyData data = new PlayerLobbyData
        {
            PlayerName = name,
            AvatarIndex = avatar
        };
        playerLobbyDatas[player] = data;

        // Update UI
        SpawnPlayerInfo(player, name, avatar);
    }

    private void OnExitClicked()
    {
        Debug.Log("Exit clicked");
        runner.Shutdown();
        SceneManager.LoadScene("JoinScene");
    }

    private void OnPlayClicked()
    {
        Debug.Log("Play clicked");
        if (runner.IsServer)
        {
            // Kiểm tra số lượng người chơi, cần ít nhất 2 người chơi
            if (playerLobbyDatas.Count >= 2)
            {
                runner.LoadScene("InGame");
            }
            else
            {
                Debug.Log("Cần ít nhất 2 người chơi để bắt đầu game!");
                // Tạo thông báo cho người chơi
                ShowNotification("Cần ít nhất 2 người chơi để bắt đầu game!");
            }
        }
    }

    // Phương thức hiển thị thông báo (thêm mới)
    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);

            // Tự động ẩn thông báo sau 3 giây
            StartCoroutine(HideNotification(3f));
        }
    }

    // Coroutine ẩn thông báo (thêm mới)
    private IEnumerator HideNotification(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} joined the lobby.");

        if (runner.IsServer)
        {
            // When a player joins, send them information about all existing players
            foreach (var kvp in playerLobbyDatas)
            {
                RPC_SendPlayerInfoToClient(player, kvp.Key, kvp.Value.PlayerName.ToString(), kvp.Value.AvatarIndex);
            }

            // Hiển thị thông báo có người chơi mới tham gia
            ShowNotification($"Người chơi mới đã tham gia!");

            // Kiểm tra số lượng người chơi
            UpdatePlayButtonState();
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} left the lobby.");

        // Lấy tên người chơi trước khi xóa để hiển thị thông báo
        string playerLeftName = "Someone";
        if (playerLobbyDatas.TryGetValue(player, out PlayerLobbyData data))
        {
            playerLeftName = data.PlayerName.ToString();
        }

        // Remove from UI
        if (playerInfoSlots.TryGetValue(player, out GameObject playerSlot) && playerSlot != null)
        {
            Destroy(playerSlot);
            playerInfoSlots.Remove(player);
        }

        // Remove from data cache
        if (playerLobbyDatas.ContainsKey(player))
        {
            playerLobbyDatas.Remove(player);
        }

        // Hiển thị thông báo người chơi đã rời phòng
        ShowNotification($"{playerLeftName} đã rời phòng!");

        // If server, broadcast the removal to ensure all clients are in sync
        if (runner.IsServer)
        {
            RPC_RemovePlayer(player);

            // Kiểm tra số lượng người chơi
            UpdatePlayButtonState();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RemovePlayer(PlayerRef player)
    {
        Debug.Log($"RPC_RemovePlayer: {player}");

        // Remove from UI
        if (playerInfoSlots.TryGetValue(player, out GameObject playerSlot) && playerSlot != null)
        {
            Destroy(playerSlot);
            playerInfoSlots.Remove(player);
        }

        // Remove from data cache
        if (playerLobbyDatas.ContainsKey(player))
        {
            playerLobbyDatas.Remove(player);
        }
    }

    // Phương thức cập nhật trạng thái nút Play dựa vào số lượng người chơi
    private void UpdatePlayButtonState()
    {
        if (runner.IsServer && playButton != null)
        {
            bool enoughPlayers = playerLobbyDatas.Count >= 2;
            playButton.interactable = enoughPlayers;

            if (!enoughPlayers && playButton.gameObject.activeInHierarchy)
            {
                ShowNotification("Cần ít nhất 2 người chơi để bắt đầu game!");
            }
        }
    }

    // Implement other required INetworkRunnerCallbacks methods
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Lobby Shutdown: {shutdownReason}");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected to server in Lobby.");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected from server in Lobby: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Failed to connect in Lobby: {reason}");
    }
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