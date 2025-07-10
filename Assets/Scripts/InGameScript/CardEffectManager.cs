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
    [SerializeField] private TMP_Text CurrentEffectText;
    
    [Header("UI Components")]
    [SerializeField] private ExplodingKittenUI explodingKittenUI;
    [SerializeField] private NormalCardComboUI normalCardComboUI;
    [SerializeField] private GameSetUI gameSetUI;
    
    // Biến để kiểm soát việc chơi bài khi có exploding
    // QUAN TRỌNG: Đây là nguồn dữ liệu chính cho trạng thái exploding trong game
    // GameManager và NopeManager đều tham chiếu đến các biến này
    [HideInInspector] public static bool IsExplodingInProgress = false;
    [HideInInspector] public static int ExplodingPlayerId = -1;
    
    // Biến để quản lý hiển thị effect
    private Coroutine currentEffectCoroutine;
    
    // Màu sắc cho từng loại effect
    private Dictionary<string, Color> effectColors = new Dictionary<string, Color>
    {
        {"Exploding", Color.red},           // Đỏ cho Exploding
        {"Defuse", Color.green},            // Xanh lá cho Defuse  
        {"Attack", new Color(1f, 0.5f, 0f)},          // Tím cho Attack
        {"Skip", Color.cyan},               // Xanh dương cho Skip
        {"Favor", Color.yellow},            // Vàng cho Favor
        {"Shuffle", Color.blue},            // Xanh đậm cho Shuffle
        {"SeeTheFuture", Color.magenta}, // Cam cho SeeTheFuture
        {"Nope", Color.red},              // Đen cho Nope
        {"Combo2", new Color(0.5f, 0f, 1f)}, // Tím nhạt cho Combo 2
        {"Combo3", new Color(1f, 0f, 0.5f)}  // Hồng cho Combo 3
    };
    
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
        
        // Khởi tạo màu sắc cho các effect
        InitializeEffectColors();
    }
    
    private void InitializeEffectColors()
    {
        if (effectColors == null)
        {
            effectColors = new Dictionary<string, Color>
            {
                {"Exploding", Color.red},           // Đỏ cho Exploding
                {"Defuse", Color.green},            // Xanh lá cho Defuse  
                {"Attack", Color.magenta},          // Tím cho Attack
                {"Skip", Color.cyan},               // Xanh dương cho Skip
                {"Favor", Color.yellow},            // Vàng cho Favor
                {"Shuffle", Color.blue},            // Xanh đậm cho Shuffle
                {"SeeTheFuture", new Color(1f, 0.5f, 0f)}, // Cam cho SeeTheFuture
                {"Nope", Color.black},              // Đen cho Nope
                {"Combo2", new Color(0.5f, 0f, 1f)}, // Tím nhạt cho Combo 2
                {"Combo3", new Color(1f, 0f, 0.5f)}  // Hồng cho Combo 3
            };
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
            // Hiển thị combo effect với countdown
            string comboType = comboCards.Count == 2 ? "Combo2" : "Combo3";
            ShowCountdownEffect(comboType, 5);
            
            // Cho phép Nope trong 5 giây trước khi thực hiện combo
            if (NopeManager.Instance != null)
            {
                // Sử dụng string thay vì object để dễ so sánh
                string comboKey = $"Combo_{comboCards.Count}_{PhotonNetwork.LocalPlayer.ActorNumber}";
                NopeManager.Instance.StartComboNopeWindow(comboCards, comboKey);
            }
            else
            {
                // Nếu không có NopeManager, thực hiện ngay
                normalCardComboUI.HandleNormalCardCombo(comboCards);
            }
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
        
        // Gửi RPC để đặt lại exploding card vào deck
        // RPC_ReinsertExplodingCard sẽ gọi RPC_EndExplodingState để reset trạng thái cho tất cả client
        photonView.RPC("RPC_ReinsertExplodingCard", RpcTarget.MasterClient, position);
        
        // Chỉ chuyển lượt nếu là MasterClient
        // (trạng thái exploding sẽ được reset bởi RPC_EndExplodingState)
        if (PhotonNetwork.IsMasterClient)
        {
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
            PhotonNetwork.LoadLevel("SampleScene");
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
            // Đảm bảo exploding state đã được reset trước khi chuyển lượt
            Debug.Log("Master client resuming turn after defuse");
            
            // Lấy người chơi hiện tại và chuyển sang người tiếp theo
            int currentTurn = GameManager.Instance.GetCurrentTurnIndex();
            int nextPlayer = GameManager.Instance.GetNextAlivePlayerIndex(currentTurn);
            
            // Chuyển lượt với 1 turn bình thường (không còn attack turns)
            GameManager.Instance.StartTurn(nextPlayer, 1);
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
        
        // Clean up and disconnect
        if (PhotonNetwork.IsConnected)
        {
            // Leave the room but stay connected
            PhotonNetwork.LeaveRoom();
        }
        
        // Load the main menu scene
        SceneManager.LoadScene("JoinRoomScene");
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
                
            case "Nope":
                HandleNopeEffect(activatingPlayerId);
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
        
        // Hiển thị effect Exploding ngay lập tức
        ShowInstantEffect("Exploding");
        
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
    }
    
    public void OnDefuseCardDropped(Card defuseCard)
    {
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
            photonView.RPC("RPC_EliminatePlayer", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
    
    private void HandleDefuseEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Defuse từ người chơi {playerId}");
        
        // Hiển thị effect Defuse ngay lập tức
        ShowInstantEffect("Defuse");
        
        // Defuse KHÔNG thể bị Nope - đây là thẻ gỡ bom đặc biệt
        // Chỉ được sử dụng khi rút phải Exploding Kitten
        Debug.Log("Defuse cards cannot be played directly - only used to defuse Exploding Kittens");
    }
    
    private void HandleAttackEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Attack từ người chơi {playerId}");
        
        // Hiển thị effect Attack ngay lập tức
        ShowInstantEffect("Attack");
        
        // Attack kích hoạt NGAY LẬP TỨC
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId)
        {
            GameManager.Instance.ProcessAttackPlayed();
        }
        
        // Mở Nope window cho đến khi có người rút bài (nhưng không ảnh hưởng đến việc kích hoạt)
        if (NopeManager.Instance != null)
        {
            NopeManager.Instance.StartNopeWindow("Attack", playerId);
        }
    }

    private void HandleFavorEffect(int playerId)
    {
        Debug.Log($"[Favor] Xử lý hiệu ứng Favor từ player {playerId}");
        
        // Hiển thị effect Favor với countdown 5 giây
        ShowCountdownEffect("Favor", 5);
        
        // Cho phép Nope trong 5 giây
        if (NopeManager.Instance != null)
        {
            NopeManager.Instance.StartFavorNopeWindow(playerId);
        }
        else
        {
            // Nếu không có NopeManager, thực hiện ngay
            if (PhotonNetwork.LocalPlayer.ActorNumber == playerId && FavorTargetSelectUI.Instance != null)
            {
                FavorTargetSelectUI.Instance.Show(
                    GameManager.Instance.playerList,
                    PhotonNetwork.LocalPlayer.ActorNumber,
                    (targetPlayerId) =>
                    {
                        Debug.Log("[Favor] Đã chọn người chơi có ID: " + targetPlayerId);
                        photonView.RPC("RPC_RequestFavorCard", RpcTarget.All, playerId, targetPlayerId);
                    }
                );
            }
        }
    }

    private void HandleNopeEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Nope từ người chơi {playerId}");
        
        // Hiển thị effect Nope ngay lập tức
        ShowInstantEffect("Nope");
        
        // Gọi NopeManager xử lý logic Nope
        // Nope có thể được chơi bởi bất kỳ ai (không chỉ người có lượt)
        if (NopeManager.Instance != null)
        {
            NopeManager.Instance.PlayNopeCard(playerId);
        }
        else
        {
            Debug.LogWarning("NopeManager Instance is null when trying to play Nope!");
        }
    }
    
    private void HandleShuffleEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Shuffle từ người chơi {playerId}");
        
        // Hiển thị effect Shuffle với countdown 5 giây
        ShowCountdownEffect("Shuffle", 5);
        
        // Cho phép Nope trong 5 giây
        if (NopeManager.Instance != null)
        {
            NopeManager.Instance.StartShuffleNopeWindow(playerId);
        }
        else
        {
            // Nếu không có NopeManager, thực hiện ngay
            CardManager.Instance.PhotonView.RPC("RPC_RequestShuffle", RpcTarget.MasterClient);
        }
    }
    
    private void HandleSkipEffect(int playerId)
    {
        Debug.Log($"Xử lý hiệu ứng Skip từ người chơi {playerId}");
        
        // Check if player is still in the game before proceeding
        if (GameManager.Instance.IsPlayerEliminated(playerId))
        {
            Debug.LogError($"Player {playerId} is already eliminated! Cannot process Skip card.");
            return;
        }
        
        // Hiển thị effect Skip ngay lập tức
        ShowInstantEffect("Skip");
        
        // Skip kích hoạt NGAY LẬP TỨC
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId)
        {
            GameManager.Instance.ProcessSkipPlayed();
        }
        
        // Mở Nope window cho đến khi có người rút bài (nhưng không ảnh hưởng đến việc kích hoạt)
        if (NopeManager.Instance != null)
        {
            NopeManager.Instance.StartNopeWindow("Skip", playerId);
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
        Debug.Log($"Winner announced: {winnerName}");
        
        if (gameSetUI != null)
        {
            // Kiểm tra xem local player có phải là winner không
            bool isLocalPlayerWinner = PhotonNetwork.LocalPlayer.NickName == winnerName;
            gameSetUI.ShowGameOver(winnerName, isLocalPlayerWinner);
        }
    }
    
    // Method để hiển thị effect ngay lập tức (không có countdown)
    private void ShowInstantEffect(string effectName)
    {
        if (CurrentEffectText != null)
        {
            // Dừng coroutine hiện tại nếu có
            if (currentEffectCoroutine != null)
            {
                StopCoroutine(currentEffectCoroutine);
            }
            
            CurrentEffectText.text = effectName;
            
            // Đặt màu cho effect
            if (effectColors.ContainsKey(effectName))
            {
                CurrentEffectText.color = effectColors[effectName];
            }
            else
            {
                CurrentEffectText.color = Color.white; // Màu mặc định
            }
            
            // Hiển thị trong 3 giây rồi ẩn
            currentEffectCoroutine = StartCoroutine(HideEffectAfterDelay(3f));
        }
    }
    
    // Method để hiển thị effect với countdown
    private void ShowCountdownEffect(string effectName, int countdown)
    {
        if (CurrentEffectText != null)
        {
            // Dừng coroutine hiện tại nếu có
            if (currentEffectCoroutine != null)
            {
                StopCoroutine(currentEffectCoroutine);
            }
            
            // Đặt màu cho effect
            string effectKey = effectName.Contains("Combo") ? effectName : effectName;
            if (effectColors.ContainsKey(effectKey))
            {
                CurrentEffectText.color = effectColors[effectKey];
            }
            else
            {
                CurrentEffectText.color = Color.white; // Màu mặc định
            }
            
            // Bắt đầu countdown
            currentEffectCoroutine = StartCoroutine(CountdownEffect(effectName, countdown));
        }
    }
    
    // Coroutine để đếm ngược
    private IEnumerator CountdownEffect(string effectName, int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            if (CurrentEffectText != null)
            {
                CurrentEffectText.text = $"{effectName}: {i}s";
            }
            yield return new WaitForSeconds(1f);
        }
        
        // Khi hết thời gian, hiển thị effect đang kích hoạt
        if (CurrentEffectText != null)
        {
            CurrentEffectText.text = $"{effectName} Activated!";
            yield return new WaitForSeconds(2f);
            CurrentEffectText.text = "";
        }
    }
    
    // Coroutine để ẩn effect sau một khoảng thời gian
    private IEnumerator HideEffectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (CurrentEffectText != null)
        {
            CurrentEffectText.text = "";
        }
    }
    
    // Method để ẩn effect ngay lập tức
    public void HideEffect()
    {
        if (currentEffectCoroutine != null)
        {
            StopCoroutine(currentEffectCoroutine);
            currentEffectCoroutine = null;
        }
        
        if (CurrentEffectText != null)
        {
            CurrentEffectText.text = "";
        }
    }
    
    // Event handlers cho UI components
    
    [PunRPC]
    private void RPC_ReceiveFutureCards(int[] spriteIndexes)
    {
        Debug.Log("You see the future! Top cards are: " + string.Join(", ", spriteIndexes));

        //Gọi UI để hiển thị các lá bài
        if (SeeTheFutureUI.Instance != null)
        {
            SeeTheFutureUI.Instance.ShowFutureCards(spriteIndexes);
        }
    }
    [PunRPC]
    private void RPC_RequestFavorCard(int fromPlayerId, int toPlayerId)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == toPlayerId)
        {
            var cards = CardHolder.Instance.Cards.Select(c => c.data).ToList();

            FavorGiveCardUI.Instance.Show(cards, (selectedCardName) =>
            {
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
            });
        }
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
    }

    private void HandleSeeTheFutureEffect(int playerId) 
    { 
        Debug.Log("See the future effect handled");
        
        // Hiển thị effect SeeTheFuture với countdown 5 giây
        ShowCountdownEffect("SeeTheFuture", 5);
        
        // Cho phép Nope trong 5 giây
        if (NopeManager.Instance != null)
        {
            NopeManager.Instance.StartSeeTheFutureNopeWindow(playerId);
        }
        else
        {
            // Nếu không có NopeManager, thực hiện ngay
            if (CardManager.Instance != null)
            {
                Debug.Log("Processing SeeTheFuture effect - showing top 3 cards");
                CardManager.Instance.PhotonView.RPC("RPC_RequestSeeTheFuture", RpcTarget.MasterClient, playerId);
            }
        }
        
        Debug.Log("SeeTheFuture effect setup completed");
    }

    // Khi có người rút bài, reset trạng thái Nope - handled in new Nope system

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
        
        // Reset Nope state
        if (NopeManager.Instance != null)
        {
            NopeManager.Instance.EndNopeWindow();
        }
        
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
            SeeTheFutureUI.Instance.gameObject.SetActive(false);
        }
        
        // Reset Favor UI nếu có
        if (FavorTargetSelectUI.Instance != null)
        {
            FavorTargetSelectUI.Instance.gameObject.SetActive(false);
        }
        
        // Đảm bảo NopeManager được reset
        if (NopeManager.Instance != null)
        {
            NopeManager.Instance.EndNopeWindow();
        }
        
        Debug.Log("ForceResetUIState completed");
    }

    // ==== END NOPE SYSTEM ====

    // Public method để reset UI state - có thể được gọi từ GameManager hoặc các component khác
    public void ResetAllUIState()
    {
        Debug.Log("ResetAllUIState called from external component");
        ForceResetUIState();
    }

    // ==== PUBLIC METHODS FOR NOPEMANAGER ACCESS ===
    public void HideAllComboPanels()
    {
        if (normalCardComboUI != null)
        {
            normalCardComboUI.HideAllPanels();
        }
    }
    
    public void ExecuteCombo(List<Card> comboCards)
    {
        if (normalCardComboUI != null)
        {
            normalCardComboUI.HandleNormalCardCombo(comboCards);
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
}