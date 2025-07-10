using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    
    [Header("UI References")]
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private Transform playerSlotsContainer;
    [SerializeField] private GameObject drawCardButton;
    [SerializeField] private Button drawCardButtonComponent;
    [SerializeField] private Color activePlayerColor = Color.green;
    [SerializeField] private Color inactivePlayerColor = Color.white;
    
    [Header("Game State")]
    [SerializeField] private int currentTurnIndex = 0;
    [SerializeField] public List<Player> playerList = new List<Player>();
    private int localPlayerIndex = -1;
    private List<PlayerSlot> playerSlots = new List<PlayerSlot>();
    private int lastPlayerDrawCardIndex = -1;
    
    [Header("Player Management")]
    private List<int> eliminatedPlayerIds = new List<int>();
    
    [Header("Turn Management")]
    private bool isExplodingInProgress = false;
    
    [Header("Effect States")]
    private int attackTurns = 1; // Số lượt phải chơi, bình thường là 1, bị Attack sẽ là 2
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
        
        // Lấy component Button của drawCardButton nếu có
        if (drawCardButton != null && drawCardButtonComponent == null)
        {
            drawCardButtonComponent = drawCardButton.GetComponent<Button>();
        }
    }
    
    private void Start()
    {
        InitializeGame();
        Vector3 spawnPos = new Vector3(UnityEngine.Random.Range(-2f, 2f), 0, UnityEngine.Random.Range(-2f, 2f));
        PhotonNetwork.Instantiate("VoicePlayer", spawnPos, Quaternion.identity);
        //yield return new WaitUntil(() => PhotonNetwork.InRoom);

        // Giờ mới gọi Instantiate
        PhotonNetwork.Instantiate("VoicePlayer", Vector3.zero, Quaternion.identity);
    }
    
    public int GetCurrentTurnIndex()
    {
        return currentTurnIndex;
    }
    
    public bool IsLocalPlayerTurn()
    {
        // Kiểm tra xem lượt hiện tại có phải là của player local hay không
        if (currentTurnIndex < 0 || currentTurnIndex >= playerList.Count)
        {
            Debug.LogError($"currentTurnIndex ({currentTurnIndex}) nằm ngoài phạm vi playerList ({playerList.Count})!");
            return false;
        }
        
        // Lấy ActorNumber của người chơi đang có lượt
        int currentPlayerActorNumber = playerList[currentTurnIndex].ActorNumber;
        
        // So sánh với ActorNumber của local player
        bool result = (currentPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber);
        
        // Log để debug
        Debug.Log($"IsLocalPlayerTurn: currTurnIdx={currentTurnIndex}, currPlayerActorNum={currentPlayerActorNumber}, localPlayerActorNum={PhotonNetwork.LocalPlayer.ActorNumber}, result={result}");
        
        return result;
    }
    
    private void InitializeGame()
    {
        // Lấy danh sách người chơi
        playerList = new List<Player>(PhotonNetwork.PlayerList);
        
        // Tìm vị trí của người chơi local
        for (int i = 0; i < playerList.Count; i++)
        {
            if (playerList[i].IsLocal)
            {
                localPlayerIndex = i;
                break;
            }
        }
        
        // Nếu là master client, bắt đầu game và chia bài
        if (PhotonNetwork.IsMasterClient)
        {
            // Chia bài đặc biệt cho tất cả người chơi (1 Defuse + 4 lá khác)
            photonView.RPC("RPC_DealInitialCards", RpcTarget.AllBuffered);
            
            // Bắt đầu lượt đầu tiên
            StartTurn(0);
        }
        
        // Tạo PlayerSlot UI cho mỗi người chơi
        CreatePlayerSlots();
    }
    
    private void CreatePlayerSlots()
    {
        playerSlots.Clear();
        
        // Tạo danh sách người chơi theo thứ tự lượt chơi (bắt đầu từ người chơi tiếp theo của local player)
        List<Player> orderedPlayers = new List<Player>();
        
        // Bắt đầu từ người chơi tiếp theo của local player
        for (int i = 1; i < playerList.Count; i++)
        {
            int playerIndex = (localPlayerIndex + i) % playerList.Count;
            orderedPlayers.Add(playerList[playerIndex]);
        }
        
        // Tạo slot cho mỗi người chơi theo thứ tự đã sắp xếp
        foreach (Player player in orderedPlayers)
        {
            GameObject slotObject = Instantiate(playerSlotPrefab, playerSlotsContainer);
            PlayerSlot playerSlot = slotObject.GetComponent<PlayerSlot>();
            
            if (playerSlot != null)
            {
                // Lấy thông tin avatar
                int avatarIndex = 0;
                if (player.CustomProperties.ContainsKey("AvatarIndex"))
                {
                    avatarIndex = (int)player.CustomProperties["AvatarIndex"];
                }
                
                // Thiết lập thông tin player slot
                playerSlot.Initialize(player.NickName, avatarIndex, player.ActorNumber);
                playerSlots.Add(playerSlot);
            }
        }
    }
    
    [PunRPC]
    private void RPC_DealInitialCards()
    {
        Debug.Log("Phát bài đặc biệt: mỗi người 1 Defuse + 4 lá khác (không có Exploding)");
        
        // Chỉ có master client phát bài để đồng bộ
        if (PhotonNetwork.IsMasterClient && CardManager.Instance != null)
        {
            // Sử dụng phương thức phát bài đặc biệt
            CardManager.Instance.DealInitialCardsSpecial(playerList);
        }
    }
    
    public void OnDrawCardButtonClicked()
    {
        if (IsLocalPlayerTurn())
        {
            Debug.Log("Đang rút bài và chuyển lượt...");
            
            // Vô hiệu hóa nút rút bài ngay lập tức để tránh nhấn nhiều lần
            if (drawCardButtonComponent != null)
            {
                drawCardButtonComponent.interactable = false;
            }
            
            // Rút bài thông qua CardManager
            if (CardManager.Instance != null)
            {
                // Yêu cầu host xử lý việc rút bài
                CardManager.Instance.PhotonView.RPC("RPC_RequestDrawCard", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
                
                // Xử lý lượt sau khi rút bài
                StartCoroutine(ProcessTurnAfterDrawing());
            }
        }
        else
        {
            Debug.LogWarning("Not your turn! Current turn: " + currentTurnIndex + ", your turn: " + localPlayerIndex);
        }
    }

    private IEnumerator ProcessTurnAfterDrawing()
    {
        // Thông báo CardEffectManager rằng có người rút bài (để reset Nope window cho Attack/Skip)
        if (CardEffectManager.Instance != null)
        {
            NopeManager.Instance.OnPlayerDrawCard();
        }
        
        // Giảm thời gian chờ từ 0.5s xuống 0.1s
        yield return new WaitForSeconds(0.1f);

        attackTurns--; // Giảm số lượt tấn công còn lại

        if (attackTurns > 0)
        {
            // Nếu vẫn còn lượt tấn công, không chuyển người, chỉ reset lượt của chính mình
            photonView.RPC("RPC_StartTurn", RpcTarget.All, currentTurnIndex, attackTurns);
        }
        else // Hết lượt, chuyển cho người tiếp theo
        {
            // Tìm người chơi tiếp theo chưa bị loại
            int nextPlayerIndex = GetNextAlivePlayerIndex(currentTurnIndex);
            
            // Yêu cầu Master Client chuyển lượt cho người chơi tiếp theo với 1 lượt bình thường
            photonView.RPC("RPC_RequestStartTurn", RpcTarget.MasterClient, nextPlayerIndex, 1);
        }
    }
    
    // Tìm người chơi tiếp theo chưa bị loại
    public int GetNextAlivePlayerIndex(int currentIndex)
    {
        int nextIndex = currentIndex;
        int attempts = 0;
        int maxAttempts = playerList.Count;
        
        do
        {
            nextIndex = (nextIndex + 1) % playerList.Count;
            attempts++;
            
            // Tránh vòng lặp vô hạn
            if (attempts >= maxAttempts)
            {
                break;
            }
        }
        while (IsPlayerEliminated(playerList[nextIndex].ActorNumber));
        
        return nextIndex;
    }
    
    [PunRPC]
    private void RPC_RequestStartTurn(int nextPlayerIndex, int newAttackTurns)
    {
        // Chỉ host xử lý yêu cầu chuyển lượt
        if (PhotonNetwork.IsMasterClient)
        {
            StartTurn(nextPlayerIndex, newAttackTurns);
        }
    }
    
    public void StartTurn(int playerIndex, int newAttackTurns = 1)
    {
        // MasterClient sẽ gọi hàm này và RPC cho tất cả
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_StartTurn", RpcTarget.All, playerIndex, newAttackTurns);
        }
    }
    
    [PunRPC]
    private void RPC_StartTurn(int playerIndex, int newAttackTurns)
    {
        // Cập nhật biến lượt và trạng thái attack hiện tại
        currentTurnIndex = playerIndex;
        attackTurns = newAttackTurns;  

        // Lấy thông tin người chơi đang có lượt
        string activePlayerName = "Unknown";
        if (playerIndex >= 0 && playerIndex < playerList.Count)
        {
            activePlayerName = playerList[playerIndex].NickName;
        }
        
        Debug.Log("Bắt đầu lượt chơi của người chơi: " + activePlayerName + " (index: " + playerIndex + ")");
        
        bool isLocalPlayerTurn = (playerIndex == localPlayerIndex);
        
        // Bật/tắt nút Draw Card dựa trên lượt
        if (drawCardButton != null && drawCardButtonComponent != null)
        {
            // Không ẩn nút, chỉ vô hiệu hóa khi không phải lượt của người chơi
            drawCardButtonComponent.interactable = isLocalPlayerTurn;
        }
        
        // Hiển thị thông báo lượt chơi
        if (isLocalPlayerTurn)
        {
            Debug.Log("Đến lượt của bạn! Hãy rút một lá bài.");
            // Hiển thị thông báo "Đến lượt của bạn!" - bạn có thể thêm code UI thông báo ở đây
            
            // Kiểm tra nếu đây là lượt đầu tiên (chưa ai từng rút bài)
            if (lastPlayerDrawCardIndex == -1)
            {
                // Hiển thị thông báo đặc biệt cho lượt đầu tiên
                Debug.Log("Lượt đầu tiên của trò chơi! Hãy rút một lá bài để bắt đầu.");
            }
        }
        else
        {
            Debug.Log("Đang đến lượt của " + activePlayerName);
            // Hiển thị thông báo "Đang đến lượt của [tên]" - bạn có thể thêm code UI thông báo ở đây
        }
        
        // Cập nhật màu background cho tất cả player slot
        UpdatePlayerSlotColors(playerIndex);
    }
    
    private void UpdatePlayerSlotColors(int currentPlayerIndex)
    {
        // Duyệt qua tất cả PlayerSlot và cập nhật màu
        foreach (PlayerSlot slot in playerSlots)
        {
            // Tìm index của player slot này trong danh sách
            int slotPlayerIndex = -1;
            for (int i = 0; i < playerList.Count; i++)
            {
                if (playerList[i].ActorNumber == slot.PlayerActorNumber)
                {
                    slotPlayerIndex = i;
                    break;
                }
            }
            
            // Nếu đây là người chơi đang có lượt, đổi màu nền sang xanh
            if (slotPlayerIndex == currentPlayerIndex)
            {
                slot.SetActiveState(true, activePlayerColor);
            }
            else
            {
                slot.SetActiveState(false, inactivePlayerColor);
            }
        }
    }
    
    [PunRPC]
    private void RPC_PlayerDrawCard(int playerActorNumber)
    {
        // Cập nhật người chơi cuối cùng rút bài
        for (int i = 0; i < playerList.Count; i++)
        {
            if (playerList[i].ActorNumber == playerActorNumber)
            {
                lastPlayerDrawCardIndex = i;
                break;
            }
        }
        
        // Chỉ người chơi đang thực hiện action mới rút bài
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerActorNumber)
        {
            if (CardManager.Instance != null)
            {
                Debug.Log($"Người chơi {playerActorNumber} đang rút bài");
                CardManager.Instance.OnDrawButtonClicked();
                
                // Cập nhật số lượng thẻ bài cho người chơi
                UpdatePlayerCardCount();
            }
            else
            {
                Debug.LogError("CardManager.Instance là null khi người chơi rút bài!");
            }
        }
        
        Debug.Log("Người chơi " + playerActorNumber + " đã rút 1 lá bài");
    }
    
    public void UpdatePlayerCardCount()
    {
        // Lấy số lượng thẻ bài trong tay
        int cardCount = 0;
        if (CardManager.Instance != null && CardManager.Instance.cardHolder != null)
        {
            cardCount = CardManager.Instance.cardHolder.Cards.Count;
        }
        
        // Đồng bộ số lượng thẻ bài qua network
        photonView.RPC("RPC_UpdatePlayerCardCount", RpcTarget.Others, PhotonNetwork.LocalPlayer.ActorNumber, cardCount);
    }
    
    [PunRPC]
    private void RPC_UpdatePlayerCardCount(int playerActorNumber, int cardCount)
    {
        // Cập nhật số lượng thẻ bài cho người chơi tương ứng
        PlayerSlot[] playerSlots = playerSlotsContainer.GetComponentsInChildren<PlayerSlot>();
        foreach (PlayerSlot slot in playerSlots)
        {
            if (slot.PlayerActorNumber == playerActorNumber)
            {
                slot.UpdateCardCount(cardCount);
                break;
            }
        }
    }
    
    // Callbacks Photon
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Người chơi {otherPlayer.NickName} đã rời phòng.");
        
        // Cập nhật danh sách người chơi
        playerList.Clear();
        playerList.AddRange(PhotonNetwork.PlayerList);
        
        // Nếu đang lượt người chơi đã rời phòng, chuyển lượt
        int leftPlayerIndex = -1;
        for (int i = 0; i < playerList.Count; i++)
        {
            if (playerList[i].ActorNumber == otherPlayer.ActorNumber)
            {
                leftPlayerIndex = i;
                break;
            }
        }
        
        if (leftPlayerIndex == currentTurnIndex && PhotonNetwork.IsMasterClient)
        {
            int nextPlayerIndex = (currentTurnIndex + 1) % playerList.Count;
            StartTurn(nextPlayerIndex);
        }
    }
    
    // Player Management
    
    // Thêm phương thức kiểm tra player còn sống
    public bool IsPlayerEliminated(int playerId)
    {
        return eliminatedPlayerIds.Contains(playerId);
    }
    
    // Thêm phương thức loại bỏ player
    public void EliminatePlayer(int playerId)
    {
        if (!IsPlayerEliminated(playerId))
        {
            eliminatedPlayerIds.Add(playerId);
            Debug.Log($"Player {playerId} đã bị loại bỏ");
            
            // Tìm và update UI của player bị loại
            UpdateEliminatedPlayerUI(playerId);
            
            // Kiểm tra winner - quan trọng phải gọi sau khi cập nhật danh sách
            CheckForWinner();
            
            // Nếu game chưa kết thúc, tiếp tục chuyển lượt
            if (!IsGameEnded())
            {
                // Chuyển lượt nếu người bị loại đang có lượt
                if (GetCurrentPlayerActorNumber() == playerId && PhotonNetwork.IsMasterClient)
                {
                    int nextPlayerIndex = GetNextAlivePlayerIndex(currentTurnIndex);
                    StartTurn(nextPlayerIndex);
                }
            }
        }
    }
    
    // Update UI cho player bị loại
    private void UpdateEliminatedPlayerUI(int playerId)
    {
        foreach (PlayerSlot slot in playerSlots)
        {
            if (slot.PlayerActorNumber == playerId)
            {
                slot.SetEliminatedState(true);
                Debug.Log($"Player {playerId} UI marked as eliminated");
                break;
            }
        }
    }
    
    // Kiểm tra game đã kết thúc chưa
    private bool IsGameEnded()
    {
        int alivePlayers = 0;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!IsPlayerEliminated(player.ActorNumber))
            {
                alivePlayers++;
            }
        }
        return alivePlayers <= 1;
    }
    
    // Lấy ActorNumber của player hiện tại
    private int GetCurrentPlayerActorNumber()
    {
        if (currentTurnIndex >= 0 && currentTurnIndex < playerList.Count)
        {
            return playerList[currentTurnIndex].ActorNumber;
        }
        return -1;
    }
  

    public void ProcessAttackPlayed()
    {
        if (!IsLocalPlayerTurn()) return;

        int turnsToPass;

        // Kiểm tra xem đây có phải là một lượt tấn công bình thường không
        if (this.attackTurns <= 1)
        {
            // Nếu là lượt bình thường, người tiếp theo chỉ phải chịu 2 lượt.
            turnsToPass = 2;
        }
        else
        {
            // Nếu đang bị tấn công sẵn, thực hiện cộng dồn theo yêu cầu.
            // Ví dụ: đang chịu 2 lượt, đánh Attack -> người sau chịu 2+2=4 lượt.
            turnsToPass = this.attackTurns + 2;
        }

        // Sử dụng GetNextAlivePlayerIndex để tìm người chơi tiếp theo còn sống
        int nextPlayerIndex = GetNextAlivePlayerIndex(currentTurnIndex);
        photonView.RPC("RPC_RequestStartTurn", RpcTarget.MasterClient, nextPlayerIndex, turnsToPass);
    }

    public void ProcessSkipPlayed()
    {
        if (!IsLocalPlayerTurn()) return;

        // Giảm số lượt phải chơi đi 1
        this.attackTurns--;

        if (this.attackTurns > 0)
        {
            // Nếu vẫn còn lượt, bắt đầu lại lượt của chính người chơi này với số lượt còn lại
            photonView.RPC("RPC_StartTurn", RpcTarget.All, this.currentTurnIndex, this.attackTurns);
        }
        else
        {
            // Nếu đã hết lượt, chuyển cho người chơi tiếp theo với 1 lượt bình thường
            int nextPlayerIndex = GetNextAlivePlayerIndex(currentTurnIndex);
            photonView.RPC("RPC_RequestStartTurn", RpcTarget.MasterClient, nextPlayerIndex, 1);
        }
    }

    // Kiểm tra winner
    private void CheckForWinner()
    {
        int alivePlayers = 0;
        Player winner = null;
        List<Player> alivePlayersList = new List<Player>();
        
        // Đếm số người chơi còn sống
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!IsPlayerEliminated(player.ActorNumber))
            {
                alivePlayers++;
                winner = player;
                alivePlayersList.Add(player);
            }
        }
        
        Debug.Log($"CheckForWinner: {alivePlayers} players alive");
        
        // Kiểm tra điều kiện thắng
        if (alivePlayers <= 1)
        {
            if (winner != null)
            {
                Debug.Log($"Game Over! Winner: {winner.NickName}");
                // Thông báo winner cho tất cả người chơi
                photonView.RPC("RPC_AnnounceWinner", RpcTarget.All, winner.NickName);
            }
            else
            {
                Debug.Log("Game Over! No winner (all players eliminated)");
                // Trường hợp đặc biệt: không có ai thắng
                photonView.RPC("RPC_AnnounceWinner", RpcTarget.All, "No One");
            }
            
            // Dừng game
            EndGame();
        }
        else if (alivePlayers == 0)
        {
            Debug.Log("Game Over! All players eliminated - Draw game");
            photonView.RPC("RPC_AnnounceWinner", RpcTarget.All, "Draw");
            EndGame();
        }
    }
    
    // Kết thúc game
    private void EndGame()
    {
        Debug.Log("Game ended. Disabling gameplay.");
        
        // Vô hiệu hóa draw card button
        if (drawCardButtonComponent != null)
        {
            drawCardButtonComponent.interactable = false;
        }
        
        // Dừng tất cả turn management
        isExplodingInProgress = true; // Tạm dừng turn switching
        
        // Có thể thêm logic khác như disable UI, save stats, etc.
    }
    
    [PunRPC]
    private void RPC_AnnounceWinner(string winnerName)
    {
        Debug.Log($"Winner: {winnerName}");
        if (CardEffectManager.Instance != null)
        {
            // Gọi method ShowWinner từ CardEffectManager
            CardEffectManager.Instance.photonView.RPC("RPC_ShowWinner", RpcTarget.All, winnerName);
        }
    }

    // Phương thức để set trạng thái exploding
    public void SetExplodingInProgress(bool inProgress)
    {
        isExplodingInProgress = inProgress;
        // Đồng bộ với CardEffectManager để tránh desync
        CardEffectManager.IsExplodingInProgress = inProgress;
    }

    // Public method để test việc phát bài đặc biệt
    public void TestSpecialCardDealing()
    {
        if (PhotonNetwork.IsMasterClient && CardManager.Instance != null)
        {
            Debug.Log("=== TESTING SPECIAL CARD DEALING ===");
            CardManager.Instance.LogDeckComposition();
            CardManager.Instance.DealInitialCardsSpecial(playerList);
            Debug.Log("=== AFTER DEALING CARDS ===");
            CardManager.Instance.LogDeckComposition();
        }
    }

    // Public method để force check winner (for testing)
    public void ForceCheckWinner()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CheckForWinner();
        }
    }
    
    // Public method để get số players còn sống
    public int GetAlivePlayersCount()
    {
        int count = 0;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!IsPlayerEliminated(player.ActorNumber))
            {
                count++;
            }
        }
        return count;
    }

    // Add this method for NopeManager/Skip effect
    public void ReturnTurnToPlayer(int playerId)
    {
        // Find the index of the player in playerList by ActorNumber
        int idx = playerList.FindIndex(p => p.ActorNumber == playerId);
        if (idx >= 0)
        {
            currentTurnIndex = idx;
            StartTurn(idx);
        }
        else
        {
            Debug.LogWarning($"ReturnTurnToPlayer: playerId {playerId} not found in playerList");
        }
    }
}
