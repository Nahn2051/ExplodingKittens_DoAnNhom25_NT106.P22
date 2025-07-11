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
    [SerializeField] public Button drawCardButtonComponent; // Made public for external access
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
    public bool isExplodingInProgress = false; // Made public for external access
    
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
        // QUAN TRỌNG: Không cho phép rút bài khi có exploding sequence đang diễn ra
        if (CardEffectManager.IsExplodingInProgress)
        {
            Debug.LogWarning("Cannot draw card - exploding sequence in progress!");
            return;
        }
        
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
        // Thông báo CardEffectManager rằng có người rút bài
        if (CardEffectManager.Instance != null)
        {
            // Có thể thêm xử lý khi người chơi rút bài nếu cần
        }
        
        // Giảm thời gian chờ từ 0.5s xuống 0.1s
        yield return new WaitForSeconds(0.1f);

        // Đồng bộ việc giảm attackTurns cho tất cả clients trước khi kiểm tra exploding
        photonView.RPC("RPC_DecrementAttackTurns", RpcTarget.All);
        
        // Wait thêm một chút để RPC được xử lý
        yield return new WaitForSeconds(0.1f);

        // QUAN TRỌNG: Không chuyển lượt nếu exploding sequence đang diễn ra
        if (CardEffectManager.IsExplodingInProgress)
        {
            Debug.Log("Exploding sequence in progress - not changing turn");
            yield break;
        }
        
        // THÊM: Kiểm tra xem có exploding sequence nào được trigger sau khi draw không
        yield return new WaitForSeconds(0.2f); // Wait thêm để exploding sequence có thể start
        
        if (CardEffectManager.IsExplodingInProgress)
        {
            Debug.Log("Exploding sequence started after draw - aborting turn change");
            yield break;
        }

        // Chỉ Master Client xử lý turn management
        if (PhotonNetwork.IsMasterClient)
        {
            if (attackTurns > 0)
            {
                // Nếu vẫn còn lượt tấn công, không chuyển người, chỉ reset lượt của chính mình
                photonView.RPC("RPC_StartTurn", RpcTarget.All, currentTurnIndex, attackTurns);
            }
            else // Hết lượt, chuyển cho người tiếp theo
            {
                // Tìm người chơi tiếp theo chưa bị loại
                int nextPlayerIndex = GetNextAlivePlayerIndex(currentTurnIndex);
                
                // Chuyển lượt cho người chơi tiếp theo với 1 lượt bình thường
                photonView.RPC("RPC_StartTurn", RpcTarget.All, nextPlayerIndex, 1);
            }
        }
        else
        {
            // Non-master clients request turn management từ Master Client
            Debug.Log($"[CLIENT] Requesting turn management from Master Client. attackTurns: {attackTurns}");
            if (attackTurns > 0)
            {
                // Vẫn còn lượt, yêu cầu Master Client reset lượt hiện tại
                Debug.Log($"[CLIENT] Requesting continue turn for player {currentTurnIndex} with {attackTurns} turns");
                photonView.RPC("RPC_RequestContinueTurn", RpcTarget.MasterClient, currentTurnIndex, attackTurns);
            }
            else
            {
                // Hết lượt, yêu cầu Master Client chuyển lượt
                int nextPlayerIndex = GetNextAlivePlayerIndex(currentTurnIndex);
                Debug.Log($"[CLIENT] Requesting start turn for next player {nextPlayerIndex} with 1 turn");
                photonView.RPC("RPC_RequestStartTurn", RpcTarget.MasterClient, nextPlayerIndex, 1);
            }
        }
    }
    
    [PunRPC]
    private void RPC_DecrementAttackTurns()
    {
        // Đồng bộ việc giảm attackTurns cho tất cả clients
        attackTurns--;
        Debug.Log($"[DRAW] attackTurns decremented to: {attackTurns}");
    }
    
    // Xử lý khi exploding kitten được defuse - cần giảm attack turns vì đã rút 1 lá
    public void ProcessExplodingKittenDefused()
    {
        Debug.Log($"[EXPLODING DEFUSED] Processing exploding kitten defuse. Current attackTurns: {attackTurns}");
        
        // Reset exploding state trước tiên
        SetExplodingInProgress(false);
        Debug.Log("[EXPLODING DEFUSED] Reset exploding state after successful defuse");
        
        // Đồng bộ việc giảm attackTurns cho tất cả người chơi thông qua RPC
        photonView.RPC("RPC_ProcessDefuseAndCheckTurn", RpcTarget.All);
        
        // Enable UI interactions cho tất cả người chơi
        photonView.RPC("RPC_EnableUIAfterDefuse", RpcTarget.All);
    }
    
    [PunRPC]
    private void RPC_ProcessDefuseAndCheckTurn()
    {
        // Giảm số lượt attack vì đã rút 1 lá (exploding kitten) - đồng bộ cho tất cả clients
        attackTurns--;
        
        Debug.Log($"[EXPLODING DEFUSED] After defuse, attackTurns remaining: {attackTurns}");
        
        // Chỉ Master Client xử lý turn management để tránh duplicate
        if (PhotonNetwork.IsMasterClient)
        {
            if (attackTurns > 0)
            {
                // Vẫn còn lượt phải rút, tiếp tục lượt của player hiện tại
                Debug.Log($"[EXPLODING DEFUSED] Player still needs to draw {attackTurns} more cards");
                photonView.RPC("RPC_StartTurn", RpcTarget.All, currentTurnIndex, attackTurns);
            }
            else
            {
                // Đã hết lượt (attackTurns == 0), chuyển cho người tiếp theo
                Debug.Log("[EXPLODING DEFUSED] attackTurns == 0, advancing to next player");
                int nextPlayerIndex = GetNextAlivePlayerIndex(currentTurnIndex);
                photonView.RPC("RPC_StartTurn", RpcTarget.All, nextPlayerIndex, 1);
            }
        }
        else
        {
            // Non-master clients request turn management từ Master Client sau defuse
            if (attackTurns > 0)
            {
                // Vẫn còn lượt, yêu cầu Master Client tiếp tục lượt hiện tại
                photonView.RPC("RPC_RequestContinueTurn", RpcTarget.MasterClient, currentTurnIndex, attackTurns);
            }
            else
            {
                // Hết lượt, yêu cầu Master Client chuyển lượt
                int nextPlayerIndex = GetNextAlivePlayerIndex(currentTurnIndex);
                photonView.RPC("RPC_RequestStartTurn", RpcTarget.MasterClient, nextPlayerIndex, 1);
            }
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
            Debug.Log($"[MASTER] Received request to start turn for player {nextPlayerIndex} with {newAttackTurns} turns");
            StartTurn(nextPlayerIndex, newAttackTurns);
        }
    }
    
    [PunRPC]
    private void RPC_RequestContinueTurn(int playerIndex, int remainingAttackTurns)
    {
        // Chỉ host xử lý yêu cầu tiếp tục lượt
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[MASTER] Received request to continue turn for player {playerIndex} with {remainingAttackTurns} turns");
            photonView.RPC("RPC_StartTurn", RpcTarget.All, playerIndex, remainingAttackTurns);
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
        
        Debug.Log($"[RPC_StartTurn] Player: {activePlayerName} (index: {playerIndex}), attackTurns: {attackTurns}");
        
        bool isLocalPlayerTurn = (playerIndex == localPlayerIndex);
        
        // Bật/tắt nút Draw Card dựa trên lượt và attackTurns
        if (drawCardButton != null && drawCardButtonComponent != null)
        {
            // QUAN TRỌNG: Chỉ enable nút draw khi:
            // 1. Là lượt của local player
            // 2. Không có exploding sequence đang diễn ra  
            // 3. Còn attackTurns > 0 (còn lượt cần rút)
            bool shouldEnableDrawButton = isLocalPlayerTurn && 
                                        !isExplodingInProgress && 
                                        !CardEffectManager.IsExplodingInProgress &&
                                        attackTurns > 0;
            
            drawCardButtonComponent.interactable = shouldEnableDrawButton;
            
            Debug.Log($"[RPC_StartTurn] Draw button state: isLocalTurn={isLocalPlayerTurn}, attackTurns={attackTurns}, shouldEnable={shouldEnableDrawButton}");
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
        
        // QUAN TRỌNG: Enable UI interactions sau khi start turn để tránh UI conflicts
        if (!isExplodingInProgress && !CardEffectManager.IsExplodingInProgress)
        {
            Debug.Log("[RPC_StartTurn] Enabling UI interactions after turn start");
            // Delay nhỏ để đảm bảo tất cả state đã được sync
            StartCoroutine(DelayedEnablePlayerInteractions(0.1f));
        }
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
        Debug.Log($"[ELIMINATION] EliminatePlayer called for player {playerId}");
        
        if (!IsPlayerEliminated(playerId))
        {
            eliminatedPlayerIds.Add(playerId);
            Debug.Log($"[ELIMINATION] Player {playerId} đã bị loại bỏ. Total eliminated: {eliminatedPlayerIds.Count}");
            
            // In ra danh sách những người bị eliminate
            string eliminatedList = string.Join(", ", eliminatedPlayerIds);
            Debug.Log($"[ELIMINATION] Current eliminated players: [{eliminatedList}]");
            
            // QUAN TRỌNG: Reset exploding state ngay sau khi eliminate để unblock UI
            SetExplodingInProgress(false);
            Debug.Log($"[ELIMINATION] Reset exploding state after player {playerId} elimination");
            
            // QUAN TRỌNG: Ẩn CardHolder cho player bị eliminate
            if (PhotonNetwork.LocalPlayer.ActorNumber == playerId && CardHolder.Instance != null)
            {
                CardHolder.Instance.SetEliminatedState(true);
                Debug.Log($"[ELIMINATION] CardHolder hidden for eliminated local player {playerId}");
            }
            
            // Tìm và update UI của player bị loại
            UpdateEliminatedPlayerUI(playerId);
            
            // Kiểm tra winner - quan trọng phải gọi sau khi cập nhật danh sách
            CheckForWinner();
            
            // Nếu game chưa kết thúc, tiếp tục chuyển lượt
            if (!IsGameEnded())
            {
                // Chuyển lượt nếu người bị loại đang có lượt hoặc nếu exploding state đang active
                bool isEliminatedPlayerCurrentTurn = GetCurrentPlayerActorNumber() == playerId;
                bool shouldAdvanceTurn = isEliminatedPlayerCurrentTurn || isExplodingInProgress;
                
                if (shouldAdvanceTurn && PhotonNetwork.IsMasterClient)
                {
                    Debug.Log($"[ELIMINATION] Advancing turn after player {playerId} elimination. IsCurrentTurn: {isEliminatedPlayerCurrentTurn}, IsExploding: {isExplodingInProgress}");
                    int nextPlayerIndex = GetNextAlivePlayerIndex(currentTurnIndex);
                    StartTurn(nextPlayerIndex);
                }
                else
                {
                    Debug.Log($"[ELIMINATION] Not advancing turn. IsMasterClient: {PhotonNetwork.IsMasterClient}, ShouldAdvance: {shouldAdvanceTurn}");
                }
                
                // QUAN TRỌNG: Enable UI interactions cho tất cả người chơi sau khi elimination
                photonView.RPC("RPC_EnableUIAfterElimination", RpcTarget.All);
            }
        }
        else
        {
            Debug.LogWarning($"[ELIMINATION] Player {playerId} is already eliminated! Skipping duplicate elimination.");
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
        
        // Chỉ Master Client xử lý turn transition
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_StartTurn", RpcTarget.All, nextPlayerIndex, turnsToPass);
        }
        else
        {
            photonView.RPC("RPC_RequestStartTurn", RpcTarget.MasterClient, nextPlayerIndex, turnsToPass);
        }
    }

    public void ProcessSkipPlayed()
    {
        if (!IsLocalPlayerTurn()) return;

        // Đồng bộ việc giảm attackTurns cho tất cả clients
        photonView.RPC("RPC_ProcessSkipEffect", RpcTarget.All);
    }
    
    [PunRPC]
    private void RPC_ProcessSkipEffect()
    {
        // Giảm số lượt phải chơi đi 1 - đồng bộ cho tất cả clients
        this.attackTurns--;
        
        Debug.Log($"[SKIP] attackTurns decremented to: {this.attackTurns}");
        
        // Chỉ Master Client xử lý turn management
        if (PhotonNetwork.IsMasterClient)
        {
            if (this.attackTurns > 0)
            {
                // Nếu vẫn còn lượt, bắt đầu lại lượt của chính người chơi này với số lượt còn lại
                photonView.RPC("RPC_StartTurn", RpcTarget.All, this.currentTurnIndex, this.attackTurns);
            }
            else
            {
                // Nếu đã hết lượt, chuyển cho người chơi tiếp theo với 1 lượt bình thường
                int nextPlayerIndex = GetNextAlivePlayerIndex(currentTurnIndex);
                photonView.RPC("RPC_StartTurn", RpcTarget.All, nextPlayerIndex, 1);
            }
        }
        else
        {
            // Non-master clients request turn management từ Master Client
            if (this.attackTurns > 0)
            {
                // Vẫn còn lượt, yêu cầu Master Client tiếp tục lượt hiện tại
                photonView.RPC("RPC_RequestContinueTurn", RpcTarget.MasterClient, this.currentTurnIndex, this.attackTurns);
            }
            else
            {
                // Hết lượt, yêu cầu Master Client chuyển lượt
                int nextPlayerIndex = GetNextAlivePlayerIndex(currentTurnIndex);
                photonView.RPC("RPC_RequestStartTurn", RpcTarget.MasterClient, nextPlayerIndex, 1);
            }
        }
    }

    // Kiểm tra winner
    private void CheckForWinner()
    {
        int alivePlayers = 0;
        Player winner = null;
        List<Player> alivePlayersList = new List<Player>();
        
        // Đếm số người chơi còn sống và tìm winner
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!IsPlayerEliminated(player.ActorNumber))
            {
                alivePlayers++;
                winner = player; // Người cuối cùng còn sống sẽ là winner
                alivePlayersList.Add(player);
            }
        }
        
        Debug.Log($"CheckForWinner: {alivePlayers} players alive");
        
        // In ra danh sách những người còn sống để debug
        foreach (Player alivePlayer in alivePlayersList)
        {
            Debug.Log($"Alive player: {alivePlayer.NickName} (ActorNumber: {alivePlayer.ActorNumber})");
        }
        
        // Kiểm tra điều kiện thắng - chỉ khi còn đúng 1 người
        if (alivePlayers == 1 && winner != null)
        {
            Debug.Log($"Game Over! Winner: {winner.NickName} (ActorNumber: {winner.ActorNumber})");
            // Thông báo winner cho tất cả người chơi
            photonView.RPC("RPC_AnnounceWinner", RpcTarget.All, winner.NickName);
            
            // Dừng game
            EndGame();
        }
        else if (alivePlayers == 0)
        {
            Debug.Log("Game Over! All players eliminated - Draw game");
            photonView.RPC("RPC_AnnounceWinner", RpcTarget.All, "Draw");
            EndGame();
        }
        else
        {
            Debug.Log($"Game continues with {alivePlayers} players remaining");
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
    
    [PunRPC]
    private void RPC_EnableUIAfterElimination()
    {
        Debug.Log("[ELIMINATION] RPC_EnableUIAfterElimination - enabling UI for all players");
        
        // Reset exploding state cho tất cả clients
        SetExplodingInProgress(false);
        
        // Enable player interactions
        EnablePlayerInteractions();
        
        Debug.Log("[ELIMINATION] UI enabled for all players after elimination");
    }
    
    [PunRPC]
    private void RPC_EnableUIAfterDefuse()
    {
        Debug.Log("[DEFUSE] RPC_EnableUIAfterDefuse - enabling UI for all players");
        
        // Reset exploding state cho tất cả clients
        SetExplodingInProgress(false);
        
        // Enable player interactions
        EnablePlayerInteractions();
        
        Debug.Log("[DEFUSE] UI enabled for all players after successful defuse");
    }

    // Phương thức để set trạng thái exploding
    public void SetExplodingInProgress(bool inProgress)
    {
        isExplodingInProgress = inProgress;
        // Đồng bộ với CardEffectManager để tránh desync
        CardEffectManager.IsExplodingInProgress = inProgress;
    }
    
    // Phương thức để đồng bộ attackTurns cho tất cả clients
    public void SyncAttackTurns(int newAttackTurns)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SyncAttackTurns", RpcTarget.All, newAttackTurns);
        }
    }
    
    [PunRPC]
    private void RPC_SyncAttackTurns(int newAttackTurns)
    {
        attackTurns = newAttackTurns;
        Debug.Log($"[SYNC] attackTurns synchronized to: {attackTurns}");
    }
    
    // Public getter cho attackTurns
    public int GetAttackTurns()
    {
        return attackTurns;
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

    // Thêm phương thức để quay lại lượt của một người chơi cụ thể
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
    
    // Method để reset CardHolder visibility cho tất cả players (useful cho game restart)
    public void ResetAllCardHolders()
    {
        if (CardHolder.Instance != null)
        {
            CardHolder.Instance.SetCardHolderVisible(true);
            CardHolder.Instance.SetEliminatedState(false);
            Debug.Log("All CardHolders reset to visible state");
        }
    }
    
    // Flag to prevent multiple simultaneous UI restoration calls
    private bool isRestoringUI = false;
    
    // Coroutine để delay enable player interactions
    private IEnumerator DelayedEnablePlayerInteractions(float delay)
    {
        Debug.Log($"[DelayedEnablePlayerInteractions] Waiting {delay}s before enabling UI, current attackTurns: {attackTurns}");
        yield return new WaitForSeconds(delay);
        Debug.Log($"[DelayedEnablePlayerInteractions] About to enable UI, final attackTurns: {attackTurns}");
        EnablePlayerInteractions();
    }
    
    // Method to explicitly re-enable player interactions
    // Can be called after UI effects that might block interaction
    public void EnablePlayerInteractions()
    {
        // Prevent multiple simultaneous restoration calls
        if (isRestoringUI)
        {
            Debug.Log("UI restoration already in progress, skipping duplicate call");
            return;
        }
        
        isRestoringUI = true;
        Debug.Log("Explicitly enabling player interactions");
        
        // Re-enable drawing cards if it's the player's turn AND no exploding is in progress AND attackTurns > 0
        bool isLocalTurn = IsLocalPlayerTurn();
        if (drawCardButtonComponent != null)
        {
            // QUAN TRỌNG: Chỉ enable nút draw khi có lượt cần rút
            bool shouldEnableDrawButton = isLocalTurn && 
                                        !isExplodingInProgress && 
                                        !CardEffectManager.IsExplodingInProgress &&
                                        attackTurns > 0;
            
            drawCardButtonComponent.interactable = shouldEnableDrawButton;
            
            Debug.Log($"[EnablePlayerInteractions] Draw button state: isLocalTurn={isLocalTurn}, attackTurns={attackTurns}, shouldEnable={shouldEnableDrawButton}");
        }
        
        // IMPORTANT: Always enable card interactions for the local player
        // This allows them to continue playing cards even if it's not their draw turn
        if (CardHolder.Instance != null)
        {
            CardHolder.Instance.EnableCardInteraction(true);
            Debug.Log("Card interactions enabled for local player regardless of turn");
        }
        
        // Ensure all UI blocking elements are properly cleaned up
        if (SeeTheFutureUI.Instance != null && SeeTheFutureUI.Instance.IsPanelActive())
        {
            SeeTheFutureUI.Instance.ForceClosePanel();
        }
        
        // Only close Favor UI panels if they're not actively being used
        // Check if favor UI should remain open
        bool shouldCloseFavorUI = true;
        if (FavorTargetSelectUI.Instance != null && FavorTargetSelectUI.Instance.gameObject.activeInHierarchy)
        {
            // Don't close if the target selection is currently active
            shouldCloseFavorUI = false;
            Debug.Log("Favor target selection UI is active, keeping it open");
        }
        
        if (shouldCloseFavorUI)
        {
            // Hide Favor UI panels
            if (FavorTargetSelectUI.Instance != null)
            {
                FavorTargetSelectUI.Instance.gameObject.SetActive(false);
            }
            
            if (FavorGiveCardUI.Instance != null)
            {
                FavorGiveCardUI.Instance.gameObject.SetActive(false);
            }
        }
        
        // Reset card effect UI
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.HideEffect();
            CardEffectManager.Instance.HideAllComboPanels();
            CardEffectManager.Instance.HideExplodingPanels();
        }
        
        Debug.Log("All UI interactions should now be restored");
        
        // Reset the restoration flag
        isRestoringUI = false;
    }

    // Method to enable UI interactions without closing active dialogs
    public void EnableUIInteractionsOnly()
    {
        Debug.Log("Enabling UI interactions only (keeping active dialogs open)");
        
        // Re-enable drawing cards if it's the player's turn AND no exploding is in progress AND attackTurns > 0
        bool isLocalTurn = IsLocalPlayerTurn();
        if (drawCardButtonComponent != null)
        {
            // QUAN TRỌNG: Chỉ enable nút draw khi có lượt cần rút
            bool shouldEnableDrawButton = isLocalTurn && 
                                        !isExplodingInProgress && 
                                        !CardEffectManager.IsExplodingInProgress &&
                                        attackTurns > 0;
            
            drawCardButtonComponent.interactable = shouldEnableDrawButton;
            
            Debug.Log($"[EnableUIInteractionsOnly] Draw button state: isLocalTurn={isLocalTurn}, attackTurns={attackTurns}, shouldEnable={shouldEnableDrawButton}");
        }
        
        // If this is the local player's turn, make sure their cards are interactive
        if (isLocalTurn && CardHolder.Instance != null)
        {
            CardHolder.Instance.EnableCardInteraction(true);
        }
        
        Debug.Log("UI interactions enabled without closing dialogs");
    }
    
    // Method to force enable all UI interactions (for combo panels)
    public void ForceEnableAllUIInteractions()
    {
        Debug.Log("Force enabling ALL UI interactions for combo panels");
        
        // Enable all canvas components
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            canvas.enabled = true;
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = true;
            }
        }
        
        // Enable all buttons
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button button in allButtons)
        {
            button.interactable = true;
        }
        
        Debug.Log($"Force enabled {allCanvases.Length} canvases and {allButtons.Length} buttons");
    }
}
