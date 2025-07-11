using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Linq;

public class CardEffectManager : MonoBehaviourPunCallbacks
{
    public static CardEffectManager Instance;
    // [SerializeField] private TMP_Text CurrentEffectText;
    
    [Header("UI Components")]
    [SerializeField] private ExplodingKittenUI explodingKittenUI;
    [SerializeField] private NormalCardComboUI normalCardComboUI;
    [SerializeField] private GameSetUI gameSetUI;
    
    // Biến để kiểm soát việc chơi bài khi có exploding
    // QUAN TRỌNG: Đây là nguồn dữ liệu chính cho trạng thái exploding trong game
    [HideInInspector] public static bool IsExplodingInProgress = false;
    [HideInInspector] public static int ExplodingPlayerId = -1;
    
    // Track backup elimination coroutines so we can cancel them
    private Coroutine backupEliminationCoroutine = null;
    private Coroutine finalSafetyEliminationCoroutine = null;
    
    // Store the exploding player ID before it gets reset to handle turn transitions correctly
    private int explodingPlayerIdForTurnTransition = -1;
    
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
    }
    
    private void Start()
    {
        // Thiết lập UI component events
        if (explodingKittenUI != null)
        {
            explodingKittenUI.OnDefuseConfirmed += OnDefuseConfirmed;
            explodingKittenUI.OnPlayerEliminated += OnPlayerEliminated;
        }
        
        if (normalCardComboUI != null)
        {
            normalCardComboUI.OnTwoCardComboExecuted += OnTwoCardComboExecuted;
            normalCardComboUI.OnThreeCardComboExecuted += OnThreeCardComboExecuted;
        }
        
        if (gameSetUI != null)
        {
            gameSetUI.OnRestartGame += OnRestartGame;
            gameSetUI.OnReturnToMainMenu += OnReturnToMainMenu;
        }
    }
    
    // Public method để xử lý combo normal cards
    public void HandleNormalCardCombo(List<Card> comboCards)
    {
        if (normalCardComboUI != null && comboCards.Count >= 2)
        {
            // Thực hiện combo ngay lập tức
            normalCardComboUI.HandleNormalCardCombo(comboCards);
            
            // Log để debug
            string comboType = comboCards.Count == 2 ? "Combo2" : "Combo3";
            
            Debug.Log($"CardEffectManager: Triggered {comboType} UI for {comboCards.Count} cards of type {comboCards[0].data.effect}");
        }
    }
    
    // Public method để hiển thị thông báo hướng dẫn combo
    public void ShowComboHelpMessage()
    {
        if (normalCardComboUI != null)
        {
            normalCardComboUI.ShowComboHelpMessage();
        }
    }
    
    // Public method để hiển thị trạng thái combo selection
    public void ShowComboSelectionStatus(int selectedCount, string cardType)
    {
        if (normalCardComboUI != null)
        {
            normalCardComboUI.ShowComboSelectionStatus(selectedCount, cardType);
        }
    }
    
    // Event handlers cho UI components
    private void OnTwoCardComboExecuted(int fromPlayerId, int toPlayerId)
    {
        // Gửi RPC để thực hiện combo 2 lá
        photonView.RPC("RPC_ExecuteTwoCardCombo", RpcTarget.All, fromPlayerId, toPlayerId);
    }
    
    private void OnThreeCardComboExecuted(int fromPlayerId, int toPlayerId, string cardType)
    {
        // Gửi RPC để thực hiện combo 3 lá
        photonView.RPC("RPC_ExecuteThreeCardCombo", RpcTarget.All, fromPlayerId, toPlayerId, cardType);
    }
    
    private void OnDefuseConfirmed(int position)
    {
        Debug.Log($"OnDefuseConfirmed called with position {position}");
        
        // Lưu lại ExplodingPlayerId trước khi nó bị reset để sử dụng trong việc chuyển lượt
        explodingPlayerIdForTurnTransition = ExplodingPlayerId;
        
        // Cancel any active backup elimination timers immediately
        CancelBackupEliminationTimers();
        
        // Gửi RPC để đặt lại exploding card vào deck
        // RPC_ReinsertExplodingCard sẽ gọi RPC_EndExplodingState để reset trạng thái cho tất cả client
        photonView.RPC("RPC_ReinsertExplodingCard", RpcTarget.MasterClient, position);
        
        // Chỉ chuyển lượt nếu là MasterClient
        // (trạng thái exploding sẽ được reset bởi RPC_EndExplodingState)
        if (PhotonNetwork.IsMasterClient)
        {
            // Đây là thời điểm tốt để chuyển lượt vì exploding state chưa bị reset
            StartCoroutine(ResumeTurnAfterDelay(0.3f));
        }
    }
    
    private void OnRestartGame()
    {
        Debug.Log("Restarting game...");
        
        // Make sure all players are notified about restart
        photonView.RPC("RPC_RestartGame", RpcTarget.All);
        
        // Host will load the level
        if (PhotonNetwork.IsMasterClient)
        {
            // Wait a moment to let the RPC propagate
            StartCoroutine(RestartGameAfterDelay(1.0f));
        }
    }
    
    private IEnumerator RestartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Master client loading game scene...");
            PhotonNetwork.LoadLevel("LobbyScene");
        }
    }
    
    [PunRPC]
    private void RPC_RestartGame()
    {
        Debug.Log("Received restart game RPC");
        // Any cleanup needed before scene reload can be done here
    }
    
    private IEnumerator ResumeTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Chỉ master client xử lý việc chuyển lượt sau khi defuse
        if (PhotonNetwork.IsMasterClient && GameManager.Instance != null)
        {
            Debug.Log("Master client resuming turn after defuse");
            
            // Sau khi defuse thành công, lượt chơi phải chuyển sang người chơi tiếp theo
            // Theo luật Exploding Kittens: nếu rút được Exploding Kitten và defuse thành công, lượt kết thúc
            
            // Tìm index của người chơi đã rút exploding card trong playerList
            int explodingPlayerIndex = -1;
            if (explodingPlayerIdForTurnTransition > 0)
            {
                for (int i = 0; i < GameManager.Instance.playerList.Count; i++)
                {
                    if (GameManager.Instance.playerList[i].ActorNumber == explodingPlayerIdForTurnTransition)
                    {
                        explodingPlayerIndex = i;
                        break;
                    }
                }
            }
            
            // Nếu không tìm thấy exploding player, sử dụng current turn index làm fallback
            int currentPlayerIndex = (explodingPlayerIndex >= 0) ? explodingPlayerIndex : GameManager.Instance.GetCurrentTurnIndex();
            int nextPlayerIndex = GameManager.Instance.GetNextAlivePlayerIndex(currentPlayerIndex);
            
            Debug.Log($"Defuse successful: explodingPlayerIdForTurnTransition={explodingPlayerIdForTurnTransition}, explodingPlayerIndex={explodingPlayerIndex}, turn passing from player index {currentPlayerIndex} to player index {nextPlayerIndex}");
            
            // Đảm bảo rằng next player không phải là chính player đã defuse (trừ khi chỉ có 1 player còn lại)
            if (nextPlayerIndex == currentPlayerIndex && GameManager.Instance.playerList.Count > 1)
            {
                Debug.LogWarning("Next player is same as current player, this shouldn't happen unless only 1 player remains");
                // Thử tìm next player khác một lần nữa
                nextPlayerIndex = GameManager.Instance.GetNextAlivePlayerIndex(nextPlayerIndex);
            }
            
            // Reset transition tracking variable
            explodingPlayerIdForTurnTransition = -1;
            
            // Chuyển lượt với 1 turn bình thường (không còn attack turns)
            GameManager.Instance.StartTurn(nextPlayerIndex, 1);
        }
        else
        {
            Debug.Log("Non-master client waiting for turn change from master");
        }
    }
    
    private void OnReturnToMainMenu()
    {
        Debug.Log("Returning to main menu...");
        
        // Notify all clients to leave room
        photonView.RPC("RPC_ReturnToMainMenu", RpcTarget.All);
    }
    
    [PunRPC]
    private void RPC_ReturnToMainMenu()
    {
        Debug.Log("Leaving room and returning to main menu");
        StartCoroutine(LeaveRoomAndReturnToMenu());
    }
    
    private IEnumerator LeaveRoomAndReturnToMenu()
    {
        // Disable all UI interactions during disconnect process
        if (GameManager.Instance != null)
        {
            GameManager.Instance.enabled = false;
        }
        
        // Clean up local state first
        CleanupGameState();
        
        // Leave the room properly
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            Debug.Log("Leaving Photon room...");
            PhotonNetwork.LeaveRoom();
            
            // Wait for room leave confirmation
            float timeout = 5f;
            float timer = 0f;
            
            while (PhotonNetwork.InRoom && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("Failed to leave room within timeout, forcing disconnect");
                PhotonNetwork.Disconnect();
                
                // Wait for disconnect
                timer = 0f;
                while (PhotonNetwork.IsConnected && timer < timeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    timer += 0.1f;
                }
            }
            else
            {
                Debug.Log("Successfully left Photon room");
            }
        }
        else if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Not in room, disconnecting from Photon...");
            PhotonNetwork.Disconnect();
            
            // Wait for disconnect
            float timeout = 3f;
            float timer = 0f;
            while (PhotonNetwork.IsConnected && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
        }
        
        Debug.Log("Photon cleanup completed, loading main menu scene");
        
        // Load the main menu scene
        SceneManager.LoadScene("Main Menu");
    }
    
    private void CleanupGameState()
    {
        Debug.Log("Cleaning up game state before leaving room");
        
        // Reset exploding state
        SetExplodingState(false);
        
        // Stop all running coroutines
        StopAllCoroutines();
        
        // Hide all UI panels
        HideAllComboPanels();
        HideExplodingPanels();
        
        // Reset UI states
        if (SeeTheFutureUI.Instance != null)
        {
            SeeTheFutureUI.Instance.gameObject.SetActive(false);
        }
        
        if (FavorTargetSelectUI.Instance != null)
        {
            FavorTargetSelectUI.Instance.gameObject.SetActive(false);
        }
        
        if (FavorGiveCardUI.Instance != null)
        {
            FavorGiveCardUI.Instance.gameObject.SetActive(false);
        }
        
        // Clear card holder
        if (CardHolder.Instance != null)
        {
            // Remove all cards from hand
            var cards = CardHolder.Instance.Cards.ToList();
            foreach (var card in cards)
            {
                CardHolder.Instance.RemoveCard(card);
            }
        }
        
        Debug.Log("Game state cleanup completed");
    }
    
    // Public method to leave room (can be called from UI buttons)
    public void LeaveRoomImmediate()
    {
        Debug.Log("Immediate room leave requested");
        OnReturnToMainMenu();
    }
    
    // Method to force leave room without waiting for UI confirmation
    public void ForceLeaveRoom()
    {
        Debug.Log("Force leaving room immediately");
        StartCoroutine(ForceLeaveRoomCoroutine());
    }
    
    private IEnumerator ForceLeaveRoomCoroutine()
    {
        // Immediate cleanup
        CleanupGameState();
        
        // Force disconnect without waiting
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Force disconnecting from Photon");
            PhotonNetwork.Disconnect();
            
            // Short wait for disconnect
            yield return new WaitForSeconds(1f);
        }
        
        // Load main menu immediately
        Debug.Log("Force loading main menu scene");
        SceneManager.LoadScene("JoinScene");
    }
    
    [PunRPC]
    private void RPC_ExecuteTwoCardCombo(int fromPlayerId, int toPlayerId)
    {
        Debug.Log($"Combo 2 lá: Người chơi {fromPlayerId} lấy 1 lá bài ngẫu nhiên từ người chơi {toPlayerId}");
        
        // Hiển thị thông báo cho người bị chọn
        if (PhotonNetwork.LocalPlayer.ActorNumber == toPlayerId)
        {
            Debug.Log("You have been selected! A random card will be taken.");
            // Có thể thêm UI notification ở đây nếu cần
        }
        
        // Logic để lấy 1 lá bài ngẫu nhiên từ target player
        if (PhotonNetwork.LocalPlayer.ActorNumber == toPlayerId && CardManager.Instance != null)
        {
            // Người bị lấy bài - chọn ngẫu nhiên 1 lá từ tay
            var cards = CardManager.Instance.cardHolder.Cards;
            if (cards.Count > 0)
            {
                int randomIndex = Random.Range(0, cards.Count);
                Card randomCard = cards[randomIndex];
                
                // Gửi thông tin lá bài cho người chơi nhận
                photonView.RPC("RPC_ReceiveCardFromCombo", RpcTarget.All, 
                               randomCard.data.cardName, randomCard.data.effect, 
                               CardManager.Instance.GetSpriteIndex(randomCard.data.sprite), 
                               fromPlayerId, toPlayerId);
                
                // Xóa lá bài khỏi tay
                CardManager.Instance.cardHolder.RemoveCard(randomCard);
            }
        }
        
        // Ensure UI interactions are restored after combo execution
        StartCoroutine(RestoreUIAfterEffect("TwoCardCombo"));
        Debug.Log("Two-card combo completed, UI restoration started");
    }
    
    [PunRPC]
    private void RPC_ExecuteThreeCardCombo(int fromPlayerId, int toPlayerId, string requestedCardType)
    {
        Debug.Log($"Combo 3 lá: Người chơi {fromPlayerId} yêu cầu lá {requestedCardType} từ người chơi {toPlayerId}");
        
        // Hiển thị thông báo cho người bị chọn
        if (PhotonNetwork.LocalPlayer.ActorNumber == toPlayerId)
        {
            Debug.Log($"You have been selected! Player requests {requestedCardType}.");
            // Có thể thêm UI notification ở đây nếu cần
        }
        
        // Logic để lấy lá bài cụ thể từ target player
        if (PhotonNetwork.LocalPlayer.ActorNumber == toPlayerId && CardManager.Instance != null)
        {
            // Tìm lá bài yêu cầu trong tay
            var cards = CardManager.Instance.cardHolder.Cards;
            Card targetCard = null;
            
            foreach (Card card in cards)
            {
                if (card.data.effect == requestedCardType)
                {
                    targetCard = card;
                    break;
                }
            }
            
            if (targetCard != null)
            {
                // Gửi thông tin lá bài cho người chơi nhận
                photonView.RPC("RPC_ReceiveCardFromCombo", RpcTarget.All, 
                               targetCard.data.cardName, targetCard.data.effect, 
                               CardManager.Instance.GetSpriteIndex(targetCard.data.sprite), 
                               fromPlayerId, toPlayerId);
                
                // Xóa lá bài khỏi tay
                CardManager.Instance.cardHolder.RemoveCard(targetCard);
            }
            else
            {
                // Không có lá bài yêu cầu
                photonView.RPC("RPC_ComboCardNotFound", RpcTarget.All, requestedCardType, fromPlayerId, toPlayerId);
            }
        }
        
        // Ensure UI interactions are restored after combo execution
        StartCoroutine(RestoreUIAfterEffect("ThreeCardCombo"));
        Debug.Log("Three-card combo completed, UI restoration started");
    }
    
    [PunRPC]
    private void RPC_ReceiveCardFromCombo(string cardName, string cardEffect, int spriteIndex, int toPlayerId, int fromPlayerId)
    {
        Debug.Log($"Người chơi {toPlayerId} nhận được lá {cardName} từ người chơi {fromPlayerId}");
        
        // Chỉ người nhận mới thêm lá bài vào tay
        if (PhotonNetwork.LocalPlayer.ActorNumber == toPlayerId && CardManager.Instance != null)
        {
            CardData cardData = new CardData
            {
                cardName = cardName,
                sprite = CardManager.Instance.allCardSprites[spriteIndex],
                effect = cardEffect
            };
            
            CardManager.Instance.cardHolder.DrawCard(CardManager.Instance.cardPrefab, cardData);
        }
    }
    
    [PunRPC]
    private void RPC_ComboCardNotFound(string requestedCardType, int fromPlayerId, int toPlayerId)
    {
        Debug.Log($"Người chơi {toPlayerId} không có lá {requestedCardType} mà người chơi {fromPlayerId} yêu cầu");
        
        // Ensure UI interactions are restored even when card is not found
        StartCoroutine(RestoreUIAfterEffect("ThreeCardComboNotFound"));
        Debug.Log("Three-card combo card not found, UI restoration started");
    }
    
    // Xử lý kích hoạt hiệu ứng thẻ bài
    public void ActivateCardEffect(string effectType, int activatingPlayerId)
    {
        // Đảm bảo chỉ có người chơi đến lượt mới kích hoạt hiệu ứng
        if (GameManager.Instance != null)
        {
            photonView.RPC("RPC_ActivateCardEffect", RpcTarget.All, effectType, 0, activatingPlayerId);
        }
    }
    
    [PunRPC]
    private void RPC_ActivateCardEffect(string effectType, int cardId, int activatingPlayerId)
    {
        Debug.Log($"Hiệu ứng '{effectType}' được kích hoạt bởi người chơi {activatingPlayerId}");
        
        // Xử lý các hiệu ứng khác nhau dựa trên loại thẻ
        switch (effectType)
        {
            case "Exploding":
                HandleExplodingEffect(activatingPlayerId);
                break;
                
            case "Defuse":
                HandleDefuseEffect(activatingPlayerId);
                break;
                
            case "Attack":
                HandleAttackEffect(activatingPlayerId);
                break;
                
            case "Favor":
                HandleFavorEffect(activatingPlayerId);
                break;
                
            case "Shuffle":
                HandleShuffleEffect(activatingPlayerId);
                break;
                
            case "Skip":
                HandleSkipEffect(activatingPlayerId);
                break;
                
            case "SeeTheFuture":
                HandleSeeTheFutureEffect(activatingPlayerId);
                break;
                
            case "HairyPotatoCat":
            case "BeardCat":
            case "Cattermelon":
            case "Tacocat":
            case "RainbowRalphingCat":
                // Normal cards sẽ được xử lý bởi combo system
                Debug.Log($"Normal card {effectType} được chơi bởi người chơi {activatingPlayerId}");
                
                // Ensure UI interactions are restored after normal card is played individually
                if (GameManager.Instance != null)
                {
                    StartCoroutine(RestoreUIAfterEffect(effectType));
                    Debug.Log($"Normal card {effectType} played individually, UI restoration started");
                }
                break;
                
            default:
                Debug.LogWarning($"Effect '{effectType}' is not defined");
                break;
        }
    }
    
    // Các hàm xử lý hiệu ứng
    private void HandleExplodingEffect(int playerId)
    {
        Debug.Log($"HandleExplodingEffect called for player {playerId}");
        Debug.Log($"Local player ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
        
        // Đặt trạng thái exploding đang diễn ra cho toàn bộ game
        SetExplodingState(true, playerId);
        
        // Chỉ người chơi rút bài exploding mới thấy UI
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId)
        {
            Debug.Log("This is the local player who drew exploding card - starting sequence");
            StartExplodingKittenSequence();
        }
        else
        {
            Debug.Log("This is not the local player who drew exploding card");
        }
    }
    
    private void StartExplodingKittenSequence()
    {
        Debug.Log("Starting exploding kitten sequence!");
        
        // Cancel any existing backup timers first
        if (backupEliminationCoroutine != null)
        {
            StopCoroutine(backupEliminationCoroutine);
            backupEliminationCoroutine = null;
        }
        if (finalSafetyEliminationCoroutine != null)
        {
            StopCoroutine(finalSafetyEliminationCoroutine);
            finalSafetyEliminationCoroutine = null;
        }
        
        // Thông báo GameManager rằng exploding đang diễn ra
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetExplodingInProgress(true);
        }
        
        // Hiển thị ExplodingKittenUI
        if (explodingKittenUI != null)
        {
            explodingKittenUI.StartExplodingKittenSequence();
        }
        
        // Start backup elimination timers and track them
        backupEliminationCoroutine = StartCoroutine(BackupEliminationTimer(11f)); // 11 seconds - 1 second more than the UI countdown
        finalSafetyEliminationCoroutine = StartCoroutine(FinalSafetyEliminationTimer(15f)); // 15 seconds - absolute maximum
    }
    
    private IEnumerator BackupEliminationTimer(float timeLimit)
    {
        yield return new WaitForSeconds(timeLimit);
        
        // If we're still in exploding state after time limit, force eliminate the player
        if (IsExplodingInProgress)
        {
            Debug.LogWarning($"BACKUP ELIMINATION: Player took more than {timeLimit} seconds to defuse - triggering elimination");
            
            // Force elimination through UI first
            if (explodingKittenUI != null)
            {
                explodingKittenUI.ForcePlayerElimination();
            }
            
            // Wait a moment for UI elimination to process
            yield return new WaitForSeconds(1f);
            
            // If still in exploding state, force direct elimination
            if (IsExplodingInProgress)
            {
                Debug.LogError("UI elimination failed, forcing direct elimination");
                OnPlayerEliminated();
            }
        }
    }
    
    private IEnumerator FinalSafetyEliminationTimer(float timeLimit)
    {
        yield return new WaitForSeconds(timeLimit);
        
        // If we're still in exploding state after absolute time limit, force eliminate immediately
        if (IsExplodingInProgress)
        {
            Debug.LogError($"FINAL SAFETY ELIMINATION: Player took more than {timeLimit} seconds to defuse - forcing elimination immediately!");
            
            // Reset exploding state first
            SetExplodingState(false);
            
            // Force eliminate the player who was supposed to be exploding
            int playerToEliminate = PhotonNetwork.LocalPlayer.ActorNumber;
            
            // Send elimination RPC directly
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_PlayerEliminated", RpcTarget.All, playerToEliminate);
            }
            else
            {
                // If not master client, trigger local elimination
                OnPlayerEliminated();
            }
            
            // Hide all exploding UI
            if (explodingKittenUI != null)
            {
                explodingKittenUI.HideExplodingPanel();
                explodingKittenUI.HidePositionInputPanel();
            }
        }
    }
    
    public void OnDefuseCardDropped(Card defuseCard)
    {
        // QUAN TRỌNG: Ngay lập tức hủy các timer backup elimination khi defuse card được thả
        Debug.Log("[CardEffectManager] Defuse card dropped - canceling elimination timers immediately");
        CancelBackupEliminationTimers();
        
        // Delegate to ExplodingKittenUI
        if (explodingKittenUI != null)
        {
            explodingKittenUI.HandleDefuseCardDropped(defuseCard);
        }
    }
    
    private void OnPlayerEliminated()
    {
        // Kiểm tra xem player có defuse card trong tay không
        bool hasDefuseInHand = false;
        if (CardManager.Instance != null)
        {
            hasDefuseInHand = CardManager.Instance.HasDefuseCardInHand();
        }
        
        // Hiển thị UI elimination
        if (gameSetUI != null)
        {
            string message = "You have been eliminated from the game!";
            if (hasDefuseInHand)
            {
                message += "\n(You had a Defuse card in your hand but forgot to use it!)";
            }
            
            gameSetUI.ShowPlayerEliminated(message, true);
        }
        
        // Gửi RPC để loại bỏ player
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_PlayerEliminated", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
    
    private void HandleDefuseEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Defuse từ người chơi {playerId}");
        
        // Defuse là thẻ gỡ bom đặc biệt
        // Chỉ được sử dụng khi rút phải Exploding Kitten
        Debug.Log("Defuse cards cannot be played directly - only used to defuse Exploding Kittens");
    }
    
    private void HandleAttackEffect(int playerId)
    {
        Debug.Log($"[ATTACK] Starting Attack effect handler for player {playerId}");
        Debug.Log($"[ATTACK] Is Local Player: {PhotonNetwork.LocalPlayer.ActorNumber == playerId}");
        
        // Thực hiện hiệu ứng ngay
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId && GameManager.Instance != null)
        {
            Debug.Log($"[ATTACK] Processing Attack effect for local player");
            GameManager.Instance.ProcessAttackPlayed();
            
            // Ghi log để debug
            Debug.Log("[ATTACK] Attack card effect processed, turn should change");
        }
        
        // Ensure UI interactions are restored after Attack effect with longer delay
        Debug.Log("[ATTACK] Starting UI restoration coroutine");
        StartCoroutine(RestoreUIAfterAttack());
    }
    
    private void HandleFavorEffect(int playerId)
    {
        Debug.Log($"[Favor] Xử lý hiệu ứng Favor từ player {playerId}");
        
        // Thực hiện hiệu ứng ngay
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId && FavorTargetSelectUI.Instance != null)
        {
            // Ensure the panel is visible and on top
            FavorTargetSelectUI.Instance.gameObject.SetActive(true);
            FavorTargetSelectUI.Instance.transform.SetAsLastSibling();
            
            Debug.Log("[Favor] Showing target selection UI");
            
            // Ensure UI interactions are enabled before showing the panel
            if (GameManager.Instance != null)
            {
                // Use the new method that doesn't close active dialogs
                GameManager.Instance.EnableUIInteractionsOnly();
                Debug.Log("[Favor] UI interactions enabled, Favor panel should be interactive");
            }
            
            FavorTargetSelectUI.Instance.Show(
                GameManager.Instance.playerList,
                PhotonNetwork.LocalPlayer.ActorNumber,
                (targetPlayerId) =>
                {
                    Debug.Log("[Favor] Đã chọn người chọi có ID: " + targetPlayerId);
                    Debug.Log($"[Favor] Sending RPC_RequestFavorCard from {playerId} to {targetPlayerId}");
                    
                    // Send RPC to request favor card
                    photonView.RPC("RPC_RequestFavorCard", RpcTarget.All, playerId, targetPlayerId);
                    
                    // Additional direct call for the target player (fallback)
                    if (PhotonNetwork.LocalPlayer.ActorNumber == targetPlayerId)
                    {
                        Debug.Log("[Favor] Direct fallback call for local target player");
                        StartCoroutine(DirectShowFavorGiveUI(playerId, targetPlayerId));
                    }
                    
                    // Start UI restoration after favor is initiated
                    StartCoroutine(RestoreUIAfterEffect("Favor"));
                }
            );
        }
    }

    // Direct fallback method to show FavorGiveCardUI without relying on RPC
    private IEnumerator DirectShowFavorGiveUI(int fromPlayerId, int targetPlayerId)
    {
        yield return new WaitForSeconds(0.1f); // Small delay to let RPC process first
        
        Debug.Log($"[Favor] DirectShowFavorGiveUI called - from {fromPlayerId} to {targetPlayerId}, local: {PhotonNetwork.LocalPlayer.ActorNumber}");
        
        if (PhotonNetwork.LocalPlayer.ActorNumber != targetPlayerId)
        {
            Debug.Log("[Favor] DirectShowFavorGiveUI: This player is not the target");
            yield break;
        }
        
        Debug.Log("[Favor] DirectShowFavorGiveUI: Attempting to show UI for target player");
        
        // Check if CardHolder instance exists
        if (CardHolder.Instance == null)
        {
            Debug.LogError("[Favor] DirectShowFavorGiveUI: CardHolder.Instance is null!");
            yield break;
        }
        
        var cards = CardHolder.Instance.Cards.Select(c => c.data).ToList();
        Debug.Log($"[Favor] DirectShowFavorGiveUI: Player has {cards.Count} cards to choose from");
        
        // Try to find FavorGiveCardUI
        if (FavorGiveCardUI.Instance == null)
        {
            FavorGiveCardUI favorUI = FindObjectOfType<FavorGiveCardUI>();
            if (favorUI != null)
            {
                FavorGiveCardUI.Instance = favorUI;
                Debug.Log("[Favor] DirectShowFavorGiveUI: Found and assigned FavorGiveCardUI instance");
            }
            else
            {
                Debug.LogError("[Favor] DirectShowFavorGiveUI: No FavorGiveCardUI found in scene!");
                yield break;
            }
        }
        
        // Activate and show the UI
        FavorGiveCardUI.Instance.gameObject.SetActive(true);
        FavorGiveCardUI.Instance.transform.SetAsLastSibling();
        
        // Force enable canvas and raycaster
        Canvas canvas = FavorGiveCardUI.Instance.GetComponent<Canvas>();
        if (canvas != null) canvas.enabled = true;
        
        UnityEngine.UI.GraphicRaycaster raycaster = FavorGiveCardUI.Instance.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster != null) raycaster.enabled = true;
        
        Debug.Log("[Favor] DirectShowFavorGiveUI: Showing FavorGiveCardUI for target player");
        
        FavorGiveCardUI.Instance.Show(cards, (selectedCardName) =>
        {
            Debug.Log($"[Favor] DirectShowFavorGiveUI: Target player selected card: {selectedCardName}");
            
            CardData selectedCard = cards.FirstOrDefault(c => c.cardName == selectedCardName);
            if (selectedCard == null)
            {
                Debug.LogError("❌ DirectShowFavorGiveUI: Không tìm thấy cardData với tên: " + selectedCardName);
                return;
            }

            int spriteIndex = CardManager.Instance.GetSpriteIndex(selectedCard.sprite);
            photonView.RPC("RPC_ReceiveFavorCardByData", RpcTarget.All,
                fromPlayerId,
                targetPlayerId,
                selectedCard.cardName,
                spriteIndex,
                selectedCard.effect);
                
            // Start UI restoration after card is given
            StartCoroutine(RestoreUIAfterEffect("FavorGiveDirect"));
        });
    }

    
    private void HandleShuffleEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Shuffle từ người chơi {playerId}");
        
        // Thực hiện hiệu ứng ngay
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId)
        {
            CardManager.Instance.PhotonView.RPC("RPC_RequestShuffle", RpcTarget.MasterClient);
        }
        
        // Ensure UI interactions are restored after Shuffle effect
        StartCoroutine(RestoreUIAfterEffect("Shuffle"));
    }
    
    private void HandleSkipEffect(int playerId)
    {
        Debug.Log($"[SKIP] Starting Skip effect handler for player {playerId}");
        Debug.Log($"[SKIP] Is Local Player: {PhotonNetwork.LocalPlayer.ActorNumber == playerId}");
        
        // Check if player is still in the game before proceeding
        if (GameManager.Instance != null && GameManager.Instance.IsPlayerEliminated(playerId))
        {
            Debug.LogError($"Player {playerId} is already eliminated! Cannot process Skip card.");
            return;
        }
        
        // Thực hiện hiệu ứng ngay
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId && GameManager.Instance != null)
        {
            Debug.Log($"[SKIP] Processing Skip effect for local player");
            GameManager.Instance.ProcessSkipPlayed();
            
            // Ghi log để debug
            Debug.Log("[SKIP] Skip card effect processed, turn should change");
        }
        
        // Ensure UI interactions are restored after Skip effect with multiple attempts
        Debug.Log("[SKIP] Starting UI restoration coroutine");
        StartCoroutine(RestoreUIAfterSkip());
    }
    
    private IEnumerator RestoreUIAfterAttack()
    {
        Debug.Log("[Attack] Starting UI restoration sequence");
        
        // Wait longer for Attack effects to settle
        yield return new WaitForSeconds(0.5f);
        
        if (GameManager.Instance != null)
        {
            Debug.Log("[Attack] Enabling player interactions after Attack effect");
            GameManager.Instance.EnablePlayerInteractions();
            Debug.Log("[Attack] UI interactions restored");
        }
    }
    
    private IEnumerator RestoreUIAfterSkip()
    {
        Debug.Log("[Skip] Starting UI restoration sequence");
        
        // Wait longer for Skip effects to settle  
        yield return new WaitForSeconds(0.5f);
        
        if (GameManager.Instance != null)
        {
            Debug.Log("[Skip] Enabling player interactions after Skip effect");
            GameManager.Instance.EnablePlayerInteractions();
            Debug.Log("[Skip] UI interactions restored");
        }
    }
    
    private IEnumerator RestoreUIAfterEffect(string effectName)
    {
        Debug.Log($"[{effectName}] Starting UI restoration sequence");
        
        // Single restoration after delay to avoid multiple calls
        yield return new WaitForSeconds(0.2f);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnablePlayerInteractions();
            Debug.Log($"[{effectName}] UI interactions restored");
        }
    }
    
    [PunRPC]
    private void RPC_PlayerEliminated(int eliminatedPlayerId)
    {
        Debug.Log($"Người chơi {eliminatedPlayerId} đã bị loại!");
        
        // Reset trạng thái exploding cho tất cả sử dụng phương thức trung tâm
        SetExplodingState(false);
        
        // Thông báo GameManager về việc loại bỏ player
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EliminatePlayer(eliminatedPlayerId);
        }
    }
    
    [PunRPC]
    private void RPC_ReinsertExplodingCard(int position)
    {
        Debug.Log($"RPC_ReinsertExplodingCard called with position {position}");
        
        // Chỉ master client xử lý việc chèn lại exploding card
        if (PhotonNetwork.IsMasterClient && CardManager.Instance != null)
        {
            // Tạo exploding card data
            CardData explodingCard = new CardData
            {
                cardName = "Exploding_1",
                sprite = CardManager.Instance.allCardSprites[0], // Assuming exploding sprite is at index 0
                effect = "Exploding"
            };
            
            CardManager.Instance.InsertCardIntoDeck(explodingCard, position);
            Debug.Log($"Master client inserted exploding card at position {position}");
            
            // Thông báo cho tất cả client rằng exploding đã kết thúc
            photonView.RPC("RPC_EndExplodingState", RpcTarget.All);
        }
        else
        {
            Debug.Log($"Non-master client received RPC_ReinsertExplodingCard - waiting for RPC_EndExplodingState");
        }
    }
    
    [PunRPC]
    private void RPC_EndExplodingState()
    {
        Debug.Log("RPC_EndExplodingState called - resetting exploding state for all clients");
        
        // Reset trạng thái exploding cho tất cả client sử dụng phương thức trung tâm
        SetExplodingState(false);
        
        // Đảm bảo UI được reset
        if (explodingKittenUI != null)
        {
            explodingKittenUI.HideExplodingPanel();
            explodingKittenUI.HidePositionInputPanel();
        }
    }
    
    [PunRPC]
    public void RPC_ShowWinner(string winnerName)
    {
        Debug.Log($"[RPC_ShowWinner] Winner announced: {winnerName}");
        
        if (gameSetUI != null)
        {
            // Kiểm tra xem local player có phải là winner không với nhiều cách so sánh
            string localPlayerName = PhotonNetwork.LocalPlayer.NickName;
            bool isLocalPlayerWinner = false;
            
            // So sánh tên chính xác
            if (localPlayerName == winnerName)
            {
                isLocalPlayerWinner = true;
            }
            // So sánh case-insensitive để tránh lỗi do viết hoa/thường
            else if (string.Equals(localPlayerName, winnerName, System.StringComparison.OrdinalIgnoreCase))
            {
                isLocalPlayerWinner = true;
            }
            
            Debug.Log($"[RPC_ShowWinner] Local player: '{localPlayerName}', Winner: '{winnerName}', IsLocalWinner: {isLocalPlayerWinner}");
            
            // Hiển thị UI với thông tin chính xác
            gameSetUI.ShowGameOver(winnerName, isLocalPlayerWinner);
        }
        else
        {
            Debug.LogError("[RPC_ShowWinner] GameSetUI is null!");
        }
    }
    
    // Method để ẩn effect ngay lập tức
    public void HideEffect()
    {
        // Effect text has been removed, this method is kept for compatibility
        Debug.Log("HideEffect called - effect text display has been removed");
    }
    
    // Event handlers cho UI components
    
    [PunRPC]
    private void RPC_ReceiveFutureCards(int[] spriteIndexes)
    {
        Debug.Log("You see the future! Top cards are: " + string.Join(", ", spriteIndexes));

        //Gọi UI để hiển thị các lá bài
        if (SeeTheFutureUI.Instance != null)
        {
            // Register for the completion event
            SeeTheFutureUI.Instance.OnSeeTheFutureComplete = () => {
                Debug.Log("SeeTheFuture effect completed, enabling interaction");
                
                // Ensure UI interaction is restored
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.EnablePlayerInteractions();
                }
                
                // Clear the reference to avoid memory leaks
                SeeTheFutureUI.Instance.OnSeeTheFutureComplete = null;
            };
            
            // Show the cards
            SeeTheFutureUI.Instance.ShowFutureCards(spriteIndexes);
        }
    }
    [PunRPC]
    private void RPC_RequestFavorCard(int fromPlayerId, int toPlayerId)
    {
        Debug.Log($"[Favor] RPC_RequestFavorCard received - fromPlayer: {fromPlayerId}, toPlayer: {toPlayerId}, localPlayer: {PhotonNetwork.LocalPlayer.ActorNumber}");
        
        if (PhotonNetwork.LocalPlayer.ActorNumber == toPlayerId)
        {
            Debug.Log("[Favor] This local player is the target - showing card selection UI");
            StartCoroutine(ShowFavorGiveUIWithDelay(fromPlayerId, toPlayerId));
        }
        else
        {
            Debug.Log($"[Favor] This player ({PhotonNetwork.LocalPlayer.ActorNumber}) is not the target ({toPlayerId})");
        }
    }
    
    private IEnumerator ShowFavorGiveUIWithDelay(int fromPlayerId, int toPlayerId)
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure UI system is ready
        
        Debug.Log($"[Favor] ShowFavorGiveUIWithDelay - starting UI display for target player {toPlayerId}");
        
        // Check if CardHolder instance exists
        if (CardHolder.Instance == null)
        {
            Debug.LogError("[Favor] CardHolder.Instance is null!");
            yield break;
        }
        
        var cards = CardHolder.Instance.Cards.Select(c => c.data).ToList();
        Debug.Log($"[Favor] Player has {cards.Count} cards to choose from");
        
        // Check if FavorGiveCardUI instance exists, try to find it if null
        if (FavorGiveCardUI.Instance == null)
        {
            Debug.LogError("[Favor] FavorGiveCardUI.Instance is null! Trying to find it in scene...");
            
            // Try to find FavorGiveCardUI in the scene
            FavorGiveCardUI favorUI = FindObjectOfType<FavorGiveCardUI>();
            if (favorUI != null)
            {
                Debug.Log("[Favor] Found FavorGiveCardUI in scene, using it");
                FavorGiveCardUI.Instance = favorUI;
            }
            else
            {
                Debug.LogError("[Favor] No FavorGiveCardUI found in scene! Cannot show card selection UI.");
                yield break;
            }
        }
        
        // Make sure the FavorGiveCardUI is active and on top
        FavorGiveCardUI.Instance.gameObject.SetActive(true);
        FavorGiveCardUI.Instance.transform.SetAsLastSibling();
        
        // Force enable canvas and raycaster
        Canvas canvas = FavorGiveCardUI.Instance.GetComponent<Canvas>();
        if (canvas != null) 
        {
            canvas.enabled = true;
            Debug.Log("[Favor] Canvas enabled for FavorGiveCardUI");
        }
        
        UnityEngine.UI.GraphicRaycaster raycaster = FavorGiveCardUI.Instance.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster != null) 
        {
            raycaster.enabled = true;
            Debug.Log("[Favor] GraphicRaycaster enabled for FavorGiveCardUI");
        }
        
        Debug.Log("[Favor] Showing FavorGiveCardUI for target player");
        
        FavorGiveCardUI.Instance.Show(cards, (selectedCardName) =>
        {
            Debug.Log($"[Favor] Target player selected card: {selectedCardName}");
            
            CardData selectedCard = cards.FirstOrDefault(c => c.cardName == selectedCardName);
            if (selectedCard == null)
            {
                Debug.LogError("❌ Không tìm thấy cardData với tên: " + selectedCardName);
                return;
            }

            int spriteIndex = CardManager.Instance.GetSpriteIndex(selectedCard.sprite);
            photonView.RPC("RPC_ReceiveFavorCardByData", RpcTarget.All,
                fromPlayerId,
                toPlayerId,
                selectedCard.cardName,
                spriteIndex,
                selectedCard.effect);
                
            // Start UI restoration after card is given
            StartCoroutine(RestoreUIAfterEffect("FavorGive"));
        });
    }
    [PunRPC]
    private void RPC_ReceiveFavorCardByData(int fromPlayerId, int toPlayerId, string cardName, int spriteIndex, string effect)
    {
        // Người bị yêu cầu: xoá bài
        if (PhotonNetwork.LocalPlayer.ActorNumber == toPlayerId)
        {
            if (CardHolder.Instance != null)
            {
                CardHolder.Instance.RemoveCardByName(cardName);
                CardHolder.Instance.ArrangeCards();
                GameManager.Instance?.UpdatePlayerCardCount();
                Debug.Log($"❌ Người chơi {toPlayerId} đã đưa lá {cardName}");
            }
        }

        // Người yêu cầu: nhận bài
        if (PhotonNetwork.LocalPlayer.ActorNumber == fromPlayerId)
        {
            CardData cardData = new CardData
            {
                cardName = cardName,
                sprite = CardManager.Instance.allCardSprites[spriteIndex],
                effect = effect
            };

            if (CardHolder.Instance != null)
            {
                CardHolder.Instance.AddCard(CardManager.Instance.cardPrefab, cardData);
                Debug.Log($"🎁 Người chơi {fromPlayerId} đã nhận được lá {cardName}");
            }
        }
        
        // Ensure UI is restored after favor card exchange is complete
        StartCoroutine(RestoreUIAfterEffect("FavorCardExchange"));
    }

    private void HandleSeeTheFutureEffect(int playerId) 
    { 
        Debug.Log("See the future effect handled");
        
        // Thực hiện hiệu ứng ngay
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId && CardManager.Instance != null)
        {
            Debug.Log("Processing SeeTheFuture effect - showing top 3 cards");
            CardManager.Instance.PhotonView.RPC("RPC_RequestSeeTheFuture", RpcTarget.MasterClient, playerId);
        }
        
        // Add a fallback UI restoration in case SeeTheFuture UI doesn't appear
        StartCoroutine(EnsureSeeTheFutureUIRestoration());
        
        Debug.Log("SeeTheFuture effect setup completed");
    }
    
    private IEnumerator EnsureSeeTheFutureUIRestoration()
    {
        // Wait a bit for SeeTheFuture UI to appear
        yield return new WaitForSeconds(2f);
        
        // If SeeTheFuture UI is not active, restore interactions
        if (SeeTheFutureUI.Instance == null || !SeeTheFutureUI.Instance.IsPanelActive())
        {
            if (GameManager.Instance != null)
            {
                Debug.Log("SeeTheFuture UI fallback: ensuring interactions are enabled");
                GameManager.Instance.EnablePlayerInteractions();
            }
        }
    }

    // Phương thức trung tâm để thay đổi trạng thái exploding với debug logging
    public static void SetExplodingState(bool inProgress, int playerId = -1)
    {
        string previousState = $"IsExplodingInProgress={IsExplodingInProgress}, ExplodingPlayerId={ExplodingPlayerId}";
        
        IsExplodingInProgress = inProgress;
        if (inProgress)
        {
            ExplodingPlayerId = playerId;
        }
        else
        {
            ExplodingPlayerId = -1;
            // KHÔNG reset transition tracking variable ở đây vì ResumeTurnAfterDelay cần sử dụng nó
            // explodingPlayerIdForTurnTransition sẽ được reset trong ResumeTurnAfterDelay sau khi sử dụng
        }
        
        Debug.Log($"[EXPLODING STATE] Changed from {previousState} to IsExplodingInProgress={IsExplodingInProgress}, ExplodingPlayerId={ExplodingPlayerId}");
        
        // Đồng bộ với GameManager
        if (Instance != null && GameManager.Instance != null)
        {
            GameManager.Instance.SetExplodingInProgress(inProgress);
        }
    }

    // Method để force reset UI state khi có vấn đề UI bị đứng
    private void ForceResetUIState()
    {
        Debug.Log("ForceResetUIState: Resetting all UI states");
        
        // Reset effect text
        HideEffect();
        
        // Đảm bảo tất cả UI panels được reset
        if (normalCardComboUI != null)
        {
            normalCardComboUI.HideAllPanels();
        }
        
        // Reset các UI component khác
        if (explodingKittenUI != null)
        {
            explodingKittenUI.HideExplodingPanel();
            explodingKittenUI.HidePositionInputPanel();
        }
        
        // Reset SeeTheFuture UI nếu có
        if (SeeTheFutureUI.Instance != null)
        {
            SeeTheFutureUI.Instance.ForceClosePanel();
        }
        
        // Reset Favor UI nếu có
        if (FavorTargetSelectUI.Instance != null)
        {
            FavorTargetSelectUI.Instance.gameObject.SetActive(false);
        }
        
        // Reset FavorGiveCard UI nếu có
        if (FavorGiveCardUI.Instance != null)
        {
            FavorGiveCardUI.Instance.gameObject.SetActive(false);
        }
        
        Debug.Log("ForceResetUIState completed");
    }

    // Public method để reset UI state - có thể được gọi từ GameManager hoặc các component khác
    public void ResetAllUIState()
    {
        Debug.Log("ResetAllUIState called from external component");
        ForceResetUIState();
        
        // Make sure player interactions are enabled
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnablePlayerInteractions();
        }
    }
    
    // Hàm phụ trợ
    public void HideAllComboPanels()
    {
        if (normalCardComboUI != null)
        {
            normalCardComboUI.HideAllPanels();
        }
    }
    
    public void HideExplodingPanels()
    {
        if (explodingKittenUI != null)
        {
            explodingKittenUI.HideExplodingPanel();
            explodingKittenUI.HidePositionInputPanel();
        }
    }

    // Method to aggressively restore card interactions after any effect
    public void ForceRestoreCardInteractions()
    {
        Debug.Log("[CardEffectManager] Force restoring card interactions");
        
        // Enable card holder interactions
        if (CardHolder.Instance != null)
        {
            CardHolder.Instance.EnableCardInteraction(true);
            Debug.Log("[CardEffectManager] Card holder interactions enabled");
        }
        
        // Enable draw card button if it's the player's turn
        if (GameManager.Instance != null)
        {
            bool isLocalTurn = GameManager.Instance.IsLocalPlayerTurn();
            var drawButton = GameManager.Instance.drawCardButtonComponent;
            
            if (drawButton != null && isLocalTurn && !IsExplodingInProgress)
            {
                drawButton.interactable = true;
                Debug.Log("[CardEffectManager] Draw button enabled");
            }
        }
        
        // Force enable all buttons
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button button in allButtons)
        {
            // Skip buttons that should remain disabled during exploding
            if (IsExplodingInProgress && button.name.Contains("Draw"))
                continue;
                
            button.interactable = true;
        }
        
        Debug.Log($"[CardEffectManager] Force enabled {allButtons.Length} buttons");
    }

    // Method to cancel backup elimination timers when defuse is successful
    public void CancelBackupEliminationTimers()
    {
        if (backupEliminationCoroutine != null)
        {
            StopCoroutine(backupEliminationCoroutine);
            backupEliminationCoroutine = null;
            Debug.Log("Backup elimination timer canceled - defuse successful");
        }
        if (finalSafetyEliminationCoroutine != null)
        {
            StopCoroutine(finalSafetyEliminationCoroutine);
            finalSafetyEliminationCoroutine = null;
            Debug.Log("Final safety elimination timer canceled - defuse successful");
        }
    }

    // Debug method to check if all required UI components are present
    [ContextMenu("Debug Favor UI Components")]
    public void DebugFavorUIComponents()
    {
        Debug.Log("=== FAVOR UI COMPONENTS DEBUG ===");
        
        // Check FavorTargetSelectUI
        if (FavorTargetSelectUI.Instance != null)
        {
            Debug.Log("✓ FavorTargetSelectUI.Instance exists");
        }
        else
        {
            FavorTargetSelectUI targetUI = FindObjectOfType<FavorTargetSelectUI>();
            Debug.Log($"✗ FavorTargetSelectUI.Instance is null, found in scene: {targetUI != null}");
        }
        
        // Check FavorGiveCardUI
        if (FavorGiveCardUI.Instance != null)
        {
            Debug.Log("✓ FavorGiveCardUI.Instance exists");
        }
        else
        {
            FavorGiveCardUI giveUI = FindObjectOfType<FavorGiveCardUI>();
            Debug.Log($"✗ FavorGiveCardUI.Instance is null, found in scene: {giveUI != null}");
        }
        
        // Check CardHolder
        if (CardHolder.Instance != null)
        {
            Debug.Log($"✓ CardHolder.Instance exists with {CardHolder.Instance.Cards.Count} cards");
        }
        else
        {
            Debug.Log("✗ CardHolder.Instance is null");
        }
        
        Debug.Log("=== END FAVOR UI DEBUG ===");
    }

    // Method to handle unexpected disconnection
    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.Log($"Disconnected from Photon. Cause: {cause}");
        
        // Cleanup and return to main menu
        CleanupGameState();
        
        // Load main menu scene after brief delay
        StartCoroutine(ReturnToMainMenuAfterDisconnect());
    }
    
    private IEnumerator ReturnToMainMenuAfterDisconnect()
    {
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("Returning to main menu after disconnect");
        SceneManager.LoadScene("JoinScene");
    }
    
    // Method to handle room leaving events
    public override void OnLeftRoom()
    {
        Debug.Log("Successfully left Photon room");
        // This will be called when we successfully leave the room
        // The coroutine in RPC_ReturnToMainMenu will handle the scene loading
    }
    
    // Method to handle when other players leave the room
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"Player {otherPlayer.NickName} left the room");
        
        // You can add additional logic here if needed
        // For example, check if we should end the game due to insufficient players
        if (PhotonNetwork.PlayerList.Length < 2 && GameManager.Instance != null)
        {
            Debug.Log("Not enough players remaining, considering ending game");
            // Could show a UI asking if player wants to continue waiting or leave
        }
    }
    
    // Debug method to check connection status
    [ContextMenu("Debug Photon Connection")]
    public void DebugPhotonConnection()
    {
        Debug.Log("=== PHOTON CONNECTION DEBUG ===");
        Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"Room Name: {(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "null")}");
        Debug.Log($"Player Count: {PhotonNetwork.PlayerList.Length}");
        Debug.Log($"Local Player: {PhotonNetwork.LocalPlayer?.NickName ?? "null"}");
        Debug.Log($"Is Master Client: {PhotonNetwork.IsMasterClient}");
        Debug.Log("===============================");
    }

    // Static method to leave room from anywhere in the game
    public static void RequestLeaveRoom()
    {
        if (Instance != null)
        {
            Debug.Log("Static request to leave room");
            Instance.LeaveRoomImmediate();
        }
        else
        {
            Debug.LogWarning("CardEffectManager instance not found, using direct PhotonNetwork disconnect");
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
            SceneManager.LoadScene("JoinScene");
        }
    }
    
    // Method to check if it's safe to leave room (no important operations in progress)
    public bool CanSafelyLeaveRoom()
    {
        // Don't leave during exploding sequence
        if (IsExplodingInProgress)
        {
            Debug.Log("Cannot leave room safely - exploding sequence in progress");
            return false;
        }
        
        // Don't leave if other critical operations are running
        if (backupEliminationCoroutine != null || finalSafetyEliminationCoroutine != null)
        {
            Debug.Log("Cannot leave room safely - elimination timers running");
            return false;
        }
        
        return true;
    }
    
    // Method to leave room with safety check
    public void LeaveRoomSafely()
    {
        if (CanSafelyLeaveRoom())
        {
            Debug.Log("Safe to leave room, proceeding with normal leave");
            LeaveRoomImmediate();
        }
        else
        {
            Debug.LogWarning("Not safe to leave room immediately, forcing leave");
            ForceLeaveRoom();
        }
    }
}