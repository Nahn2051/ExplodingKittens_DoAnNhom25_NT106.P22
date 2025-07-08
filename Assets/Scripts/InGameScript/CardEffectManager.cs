using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class CardEffectManager : MonoBehaviourPunCallbacks
{
    public static CardEffectManager Instance;
    
    [Header("UI Components")]
    [SerializeField] private ExplodingKittenUI explodingKittenUI;
    [SerializeField] private NormalCardComboUI normalCardComboUI;
    [SerializeField] private GameSetUI gameSetUI;
    
    // Biến để kiểm soát việc chơi bài khi có exploding
    [HideInInspector] public static bool IsExplodingInProgress = false;
    [HideInInspector] public static int ExplodingPlayerId = -1;
    
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
        if (normalCardComboUI != null)
        {
            normalCardComboUI.HandleNormalCardCombo(comboCards);
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
        // Gửi RPC để đặt lại exploding card vào deck
        photonView.RPC("RPC_ReinsertExplodingCard", RpcTarget.MasterClient, position);
        
        // Reset trạng thái exploding
        IsExplodingInProgress = false;
        ExplodingPlayerId = -1;
        
        // Resume turn switching - exploding đã được xử lý xong
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetExplodingInProgress(false);
            
            // Chuyển lượt sang người tiếp theo
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(ResumeTurnAfterDelay(1f));
            }
        }
    }
    
    private void OnRestartGame()
    {
        // Xử lý restart game
        PhotonNetwork.LoadLevel("SampleScene");
    }
    
    private IEnumerator ResumeTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (GameManager.Instance != null)
        {
            int currentTurn = GameManager.Instance.GetCurrentTurnIndex();
            int nextPlayer = GameManager.Instance.GetNextAlivePlayerIndex(currentTurn);
                
            GameManager.Instance.StartTurn(nextPlayer);
        }
    }
    
    private void OnReturnToMainMenu()
    {
        // Xử lý return to main menu
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("JoinRoomScene");
    }
    
    [PunRPC]
    private void RPC_ExecuteTwoCardCombo(int fromPlayerId, int toPlayerId)
    {
        Debug.Log($"Combo 2 lá: Người chơi {fromPlayerId} lấy 1 lá bài ngẫu nhiên từ người chơi {toPlayerId}");
        
        // Hiển thị thông báo cho người bị chọn
        if (PhotonNetwork.LocalPlayer.ActorNumber == toPlayerId)
        {
            ShowTargetPlayerMessage("You have been selected! A random card will be taken.");
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
            ShowTargetPlayerMessage($"You have been selected! Player requests {requestedCardType}.");
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
    public void ActivateCardEffect(string effectType, int cardId)
    {
        // Đảm bảo chỉ có người chơi đến lượt mới kích hoạt hiệu ứng
        if (GameManager.Instance != null)
        {
            photonView.RPC("RPC_ActivateCardEffect", RpcTarget.All, effectType, cardId, PhotonNetwork.LocalPlayer.ActorNumber);
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
        
        // Đặt trạng thái exploding đang diễn ra cho toàn bộ game
        IsExplodingInProgress = true;
        ExplodingPlayerId = playerId;
        
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
        
        // Reset trạng thái exploding
        IsExplodingInProgress = false;
        ExplodingPlayerId = -1;
        
        // Resume turn switching - player đã bị loại
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetExplodingInProgress(false);
        }
        
        // Gửi RPC thông báo player bị loại
        photonView.RPC("RPC_PlayerEliminated", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }
    
    // Placeholder methods cho các hiệu ứng khác
    private void HandleDefuseEffect(int playerId) { Debug.Log("Defuse effect handled"); }
    private void HandleAttackEffect(int playerId) { Debug.Log("Attack effect handled"); }
    private void HandleFavorEffect(int playerId) { Debug.Log("Favor effect handled"); }
    private void HandleNopeEffect(int playerId) { Debug.Log("Nope effect handled"); }
    private void HandleShuffleEffect(int playerId) { Debug.Log("Shuffle effect handled"); }
    private void HandleSkipEffect(int playerId) { Debug.Log("Skip effect handled"); }
    private void HandleSeeTheFutureEffect(int playerId) 
    { 
        Debug.Log("See the future effect handled");
        
        // SeeTheFuture effect: Show top 3 cards from deck
        if (CardManager.Instance != null)
        {
            Debug.Log("Processing SeeTheFuture effect - showing top 3 cards");
            // TODO: Implement actual SeeTheFuture UI
            // For now just log that effect completed
        }
        
        // Make sure UI remains interactive after effect
        Debug.Log("SeeTheFuture effect completed, UI should remain interactive");
    }
    
    [PunRPC]
    private void RPC_PlayerEliminated(int eliminatedPlayerId)
    {
        Debug.Log($"Người chơi {eliminatedPlayerId} đã bị loại!");
        
        // Reset trạng thái exploding cho tất cả
        IsExplodingInProgress = false;
        ExplodingPlayerId = -1;
        
        // Thông báo GameManager về việc loại bỏ player
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EliminatePlayer(eliminatedPlayerId);
        }
    }
    
    [PunRPC]
    private void RPC_ReinsertExplodingCard(int position)
    {
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
    
    // Method để hiển thị thông báo cho người bị chọn
    private void ShowTargetPlayerMessage(string message)
    {
        if (gameSetUI != null)
        {
            gameSetUI.ShowPlayerEliminated(message, true);
        }
        else
        {
            // Fallback: tạo temporary message
            StartCoroutine(ShowTemporaryMessage(message));
        }
    }
    
    private IEnumerator ShowTemporaryMessage(string message)
    {
        // Tạo temporary UI để hiển thị thông báo
        GameObject tempMessage = new GameObject("TargetPlayerMessage");
        tempMessage.transform.SetParent(transform);
        
        Canvas canvas = tempMessage.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        
        TMP_Text messageText = tempMessage.AddComponent<TextMeshProUGUI>();
        messageText.text = message;
        messageText.fontSize = 32;
        messageText.color = Color.yellow;
        messageText.alignment = TextAlignmentOptions.Center;
        
        RectTransform rect = tempMessage.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.8f);
        rect.anchorMax = new Vector2(0.5f, 0.8f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(500, 80);
        
        // Fade in
        CanvasGroup canvasGroup = tempMessage.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        
        float duration = 0.5f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            yield return null;
        }
        
        // Hiển thị trong 3 giây
        yield return new WaitForSeconds(3f);
        
        // Fade out
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            yield return null;
        }
        
        // Destroy
        Destroy(tempMessage);
    }

    // Event handlers cho UI components
}