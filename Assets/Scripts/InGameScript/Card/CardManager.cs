using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Photon.Pun;

public class CardManager : MonoBehaviour
{
    public Sprite[] allCardSprites;
    public GameObject cardPrefab;  
    public CardHolder cardHolder;
    public Transform cardDeckVisual;
    public static CardManager Instance;
    [SerializeField] private TMP_Text deckCardCount;
    [SerializeField] private PlayCardZone playCardZone;

    private List<CardData> Deck = new List<CardData>();
    private PhotonView photonView;
    
    // Deck count synchronized across all clients
    private int synchronizedDeckCount = 0;
    
    // Card quantities based on player count [2,3,4,5 players]
    private int[,] cardQuantitiesByPlayers = {
        {1, 2, 3, 4},  // EXPLODING KITTEN
        {3, 4, 6, 7},  // DEFUSE
        {3, 4, 5, 6},  // ATTACK (2X)
        {1, 2, 4, 6},  // FAVOR
        {2, 3, 4, 5},  // NOPE
        {2, 3, 4, 5},  // SHUFFLE
        {3, 3, 4, 6},  // SKIP
        {3, 4, 4, 5},  // SEE THE FUTURE (3X)
        {4, 4, 4, 4},  // HAIRY POTATO CAT
        {0, 4, 4, 4},  // BEARD CAT
        {0, 0, 4, 4},  // CATTERMELON
        {4, 4, 4, 4},  // TACOCAT
        {4, 4, 4, 4}   // RAINBOW-RALPHING CAT
    };
    
    // Sprite indices for each card type - base indices
    private int[] cardSpriteIndices = {0, 4, 10, 14, 18, 23, 27, 31, 36, 37, 38, 39, 40};
    
    // Sprite ranges for each card type [start, end] - for cards that need different sprites
    private int[,] cardSpriteRanges = {
        {0, 3},   // EXPLODING KITTEN (0-3)
        {4, 9},   // DEFUSE (4-9)
        {10, 13}, // ATTACK (10-13)
        {14, 17}, // FAVOR (14-17)
        {18, 22}, // NOPE (18-22)
        {23, 26}, // SHUFFLE (23-26)
        {27, 30}, // SKIP (27-30)
        {31, 35}, // SEE THE FUTURE (31-35)
        {36, 36}, // HAIRY POTATO CAT (36-36)
        {37, 37}, // BEARD CAT (37-37)
        {38, 38}, // CATTERMELON (38-38)
        {39, 39}, // TACOCAT (39-39)
        {40, 40}  // RAINBOW-RALPHING CAT (40-40)
    };
    
    private List<CardData> allCardData = new List<CardData>();
    // Public getter cho photonView
    public PhotonView PhotonView => photonView;
    
    private void Awake()
    {
        // Đảm bảo chỉ có 1 instance của CardManager
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        photonView = GetComponent<PhotonView>();
        
        // Khởi tạo bộ bài ngay trong Awake để đảm bảo nó sẵn sàng trước khi phát
        if (PhotonNetwork.IsMasterClient)
        {
            CreateDeck(PhotonNetwork.PlayerList.Length);
            // Không xáo bài ngay lập tức - sẽ xáo sau khi phát bài ban đầu
        }
    }

    private void Start()
    {
        // Đồng bộ số lượng thẻ bài và cập nhật deck visual
        if (PhotonNetwork.IsMasterClient && Deck.Count > 0)
        {
            photonView.RPC("RPC_UpdateDeckCount", RpcTarget.All, Deck.Count);
        }
        
        // Khởi tạo deck visual cho tất cả client
        CheckDeckVisual(Deck.Count);
    }

    private void CreateDeck(int playerCount)
    {
        // Convert player count to index (2 players = index 0, 3 players = index 1, etc.)
        int playerIndex = Mathf.Clamp(playerCount - 2, 0, 3);
        
        Debug.Log($"Creating deck for {playerCount} players (index: {playerIndex})");
        
        // Card names corresponding to each row in the table
        string[] cardNames = {
            "Exploding", "Defuse", "Attack", "Favor", "Nope",
            "Shuffle", "Skip", "SeeTheFuture", "HairyPotatoCat", 
            "BeardCat", "Cattermelon", "Tacocat", "RainbowRalphingCat"
        };
        
        // Create cards based on the quantities table
        for (int cardType = 0; cardType < cardNames.Length; cardType++)
        {
            int quantity = cardQuantitiesByPlayers[cardType, playerIndex];
            
            // Skip cards with 0 quantity
            if (quantity == 0) continue;
            
            // Get sprite range for this card type
            int startSpriteIndex = cardSpriteRanges[cardType, 0];
            int endSpriteIndex = cardSpriteRanges[cardType, 1];
            int availableSprites = endSpriteIndex - startSpriteIndex + 1;
            
            for (int i = 0; i < quantity; i++)
            {
                int spriteIndex;
                
                // For normal cards (last 5 types), they can repeat sprites
                if (cardType >= 8) // HairyPotatoCat and beyond
                {
                    // Use sprites from the available range, can repeat
                    spriteIndex = startSpriteIndex + (i % availableSprites);
                }
                else
                {
                    // For special cards, use different sprites when possible
                    if (i < availableSprites)
                    {
                        spriteIndex = startSpriteIndex + i;
                    }
                    else
                    {
                        // If we need more cards than available sprites, cycle through
                        spriteIndex = startSpriteIndex + (i % availableSprites);
                    }
                }
                
                AddCard(cardNames[cardType], spriteIndex, i + 1);
            }
        }
        
        Debug.Log($"Deck created with {Deck.Count} cards");
        
        // Update synchronized deck count for all clients
        synchronizedDeckCount = Deck.Count;
        
        LogSpriteMapping();
        LogDeckComposition();
        CheckDeckVisualSetup();
    }

    private void AddCard(string name, int spriteIndex, int index)
    {
        if (spriteIndex >= allCardSprites.Length)
        {
            Debug.LogWarning($"Sprite index {spriteIndex} out of range for card: {name}");
            return;
        }
        
        CardData data = new CardData
        {
            cardName = $"{name}_{index}",
            sprite = allCardSprites[spriteIndex],
            effect = name,
        };
        Deck.Add(data);
        allCardData.Add(data);
    }

    // Xáo bộ bài - chỉ host thực hiện
    public void ShuffleDeck()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Deck = Deck.OrderBy(a => Random.value).ToList();
            // Đồng bộ việc xáo bài
            photonView.RPC("RPC_DeckShuffled", RpcTarget.Others);
        }
    }
    
    [PunRPC]
    private void RPC_DeckShuffled()
    {
        Debug.Log("Bộ bài đã được xáo trộn bởi host");
    }
    
    [PunRPC]
    public void RPC_UpdateDeckCount(int count)
    {
        // Update synchronized deck count for all clients (including master client)
        synchronizedDeckCount = count;
        
        Debug.Log($"[RPC_UpdateDeckCount] Synchronized deck count updated to {count}");
        
        if (deckCardCount != null)
        {
            deckCardCount.text = count.ToString();
        }
        
        // Cập nhật deck visual khi có thay đổi số lượng
        CheckDeckVisual(count);
    }
    
    // Rút thẻ bài khi nhấn nút Draw
    public void OnDrawButtonClicked()
    {
        // Chỉ cho phép rút bài khi đến lượt
        if (GameManager.Instance != null && GameManager.Instance.IsLocalPlayerTurn())
        {
            // Gọi phương thức từ GameManager để rút bài và chuyển lượt
            GameManager.Instance.OnDrawCardButtonClicked();
        }
        else
        {
            Debug.Log("Chưa đến lượt của bạn!");
        }
    }
    
    [PunRPC]
    private void RPC_RequestDrawCard(int playerActorNumber)
    {
        // Chỉ host xử lý request
        if (PhotonNetwork.IsMasterClient)
        {
            if (Deck.Count == 0)
            {
                Debug.LogWarning("Deck is empty!");
                return;
            }
            
            // Lấy thẻ bài đầu tiên
            CardData data = Deck[0];
            Deck.RemoveAt(0);
            
            // Gửi thông tin thẻ bài đến người chơi đã request
            photonView.RPC("RPC_ReceiveDrawnCard", RpcTarget.All, 
                data.cardName, 
                GetSpriteIndex(data.sprite), 
                data.effect, 
                playerActorNumber,
                Deck.Count);
            
            // Đồng bộ deck count sau khi rút bài
            SyncDeckCount();
        }
    }
    
    [PunRPC]
    private void RPC_ReceiveDrawnCard(string cardName, int spriteIndex, string effect, int playerActorNumber, int remainingDeckCount)
    {
        // Cập nhật số lượng bài trong deck
        if (deckCardCount != null)
        {
            deckCardCount.text = remainingDeckCount.ToString();
        }
        
        CheckDeckVisual(remainingDeckCount);
        
        // Chỉ người chơi nhận thẻ mới hiển thị thẻ của mình
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerActorNumber)
        {
            CardData cardData = new CardData
            {
                cardName = cardName,
                sprite = allCardSprites[spriteIndex],
                effect = effect
            };
            
            // Kiểm tra nếu là exploding card
            if (effect == "Exploding")
            {
                Debug.Log("Rút được exploding card! Kích hoạt hiệu ứng...");
                
                // Kích hoạt hiệu ứng exploding ngay lập tức
                if (CardEffectManager.Instance != null)
                {
                    Debug.Log("CardEffectManager found, activating exploding effect");
                    CardEffectManager.Instance.ActivateCardEffect("Exploding", playerActorNumber);
                }
                else
                {
                    Debug.LogError("CardEffectManager.Instance is null! Cannot activate exploding effect!");
                }
                
                // KHÔNG thêm exploding card vào tay người chơi
                // Vì nó sẽ được xử lý bởi exploding effect
            }
            else
            {
                // Thêm card vào tay người chơi bình thường
                Debug.Log($"Adding normal card {cardName} to hand");
                cardHolder.DrawCard(cardPrefab, cardData);
            }
            
            // Thông báo GameManager để cập nhật số lượng thẻ
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayerCardCount();
            }
            
            // Thông báo CardEffectManager về việc rút bài
            if (CardEffectManager.Instance != null)
            {
                // Có thể thêm xử lý khi người chơi rút bài nếu cần
            }
        }
        
        // Kiểm tra xem deck có còn bài không
        CheckDeckVisual(remainingDeckCount);
    }
    
    // Lấy index của sprite trong mảng allCardSprites
    public int GetSpriteIndex(Sprite sprite)
    {
        for (int i = 0; i < allCardSprites.Length; i++)
        {
            if (allCardSprites[i] == sprite)
            {
                return i;
            }
        }
        return 0;
    }
    
    public void CheckDeckVisual(int count)
    {
        if (cardDeckVisual == null)
        {
            Debug.LogWarning("cardDeckVisual is null!");
            return;
        }
        
        if (count == 0)
        {
            Debug.Log("Deck is empty - hiding deck visual");
            cardDeckVisual.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log($"Deck has {count} cards - showing deck visual");
            cardDeckVisual.gameObject.SetActive(true);
        }
    }
    
    // Phương thức này để player chơi thẻ bài từ tay vào khu vực chơi
    // Lưu ý: Đánh bài không tự động chuyển lượt - chỉ có rút bài mới chuyển lượt
    public void PlayCard(Card card, int playerActorNumber)
    {
        Debug.Log($"Trying to play card {card.data.cardName} by player {playerActorNumber}");
        
        // Kiểm tra card đã được played chưa - CRITICAL SAFETY CHECK
        if (card.isPlayed)
        {
            Debug.LogWarning($"Card {card.data.cardName} already played, aborting");
            return;
        }
        
        // Kiểm tra null
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null when trying to play card!");
            return;
        }
        
        // Ghi log lượt chơi hiện tại để debug
        int currentTurnIndex = GameManager.Instance.GetCurrentTurnIndex();
        int localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log($"Current turn: {currentTurnIndex}, Local player: {localPlayerIndex}, IsLocalPlayerTurn: {GameManager.Instance.IsLocalPlayerTurn()}");
        
        // Kiểm tra xem có phải lượt của người chơi không
        bool isPlayerTurn = GameManager.Instance.IsLocalPlayerTurn();
        
        if (!isPlayerTurn)
        {
            Debug.LogWarning($"Cannot play card - not your turn! Current turn: {currentTurnIndex}, Local player: {localPlayerIndex}");
            return;
        }
        
        if (isPlayerTurn)
        {
            Debug.Log($"Player {playerActorNumber} is playing card {card.data.cardName}");
            
            // Mark card as played IMMEDIATELY để tránh duplicate processing
            card.isPlayed = true;
            
            // Gửi RPC để tất cả người chơi đều thấy thẻ bài được chơi
            photonView.RPC("RPC_PlayCard", RpcTarget.All, 
                card.data.cardName, 
                GetSpriteIndex(card.data.sprite), 
                card.data.effect, 
                playerActorNumber);
            
            // Gọi CardEffectManager để xử lý hiệu ứng
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.ActivateCardEffect(card.data.effect, playerActorNumber);
            }
            else
            {
                Debug.LogWarning("CardEffectManager.Instance is null when trying to activate card effect!");
            }
            
            // Xóa thẻ bài khỏi tay người chơi (only if local player)
            if (playerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber && cardHolder != null)
            {
                cardHolder.RemoveCard(card);
            }
            
            // Cập nhật số lượng thẻ bài
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayerCardCount();
            }
        }
        else
        {
            Debug.LogWarning($"Cannot play card {card.data.effect} - conditions not met!");
        }
    }
    
    [PunRPC]
    private void RPC_PlayCard(string cardName, int spriteIndex, string effect, int playerActorNumber)
    {
        Debug.Log($"Player {playerActorNumber} played card {cardName}");
        
        // Hiển thị thẻ bài trong khu vực chơi
        if (playCardZone != null)
        {
            CardData cardData = new CardData
            {
                cardName = cardName,
                sprite = allCardSprites[spriteIndex],
                effect = effect
            };
            
            // Tạo visual representation của thẻ bài trong PlayZone
            GameObject cardObj = Instantiate(cardPrefab, playCardZone.transform);
            Card cardComp = cardObj.GetComponentInChildren<Card>();
            if (cardComp != null)
            {
                cardComp.Setup(cardData);
                playCardZone.AddPlayedCard(cardComp, playerActorNumber);
            }
        }
    }

    // Phương thức trả về số lượng bài còn lại trong bộ bài
    public int GetDeckCount()
    {
        // If we're the master client, return the actual deck count and sync if needed
        if (PhotonNetwork.IsMasterClient)
        {
            // Sync if there's a mismatch
            if (synchronizedDeckCount != Deck.Count)
            {
                Debug.LogWarning($"Deck count mismatch detected! Actual: {Deck.Count}, Synchronized: {synchronizedDeckCount}. Syncing...");
                photonView.RPC("RPC_UpdateDeckCount", RpcTarget.All, Deck.Count);
            }
            return Deck.Count;
        }
        else
        {
            // If we're a client, return the synchronized count
            return synchronizedDeckCount;
        }
    }
    
    // Phương thức chèn card vào deck tại vị trí chỉ định
    public void InsertCardIntoDeck(CardData card, int position)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only master client can insert card into deck");
            return;
        }
        
        if (position < 0) position = 0;
        if (position > Deck.Count) position = Deck.Count;
        
        Deck.Insert(position, card);
        
        // Đồng bộ deck count với tất cả clients
        SyncDeckCount();
        
        Debug.Log($"Đã chèn {card.cardName} vào vị trí {position} trong deck. New count: {Deck.Count}");
    }
    
    // Phương thức phát bài ban đầu cho tất cả người chơi
    public void DealInitialCards(List<Photon.Realtime.Player> players, int cardsPerPlayer)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
            
        if (Deck.Count < players.Count * cardsPerPlayer)
        {
            Debug.LogError("Không đủ bài để phát!");
            return;
        }
        
        Debug.Log($"Đang phát bài cho {players.Count} người chơi, mỗi người {cardsPerPlayer} lá");
        
        foreach (Photon.Realtime.Player player in players)
        {
            for (int i = 0; i < cardsPerPlayer; i++)
            {
                // Kiểm tra lại số lượng bài trong bộ
                if (Deck.Count == 0)
                {
                    Debug.LogWarning("Deck ran out during dealing!");
                    return;
                }
                
                // Lấy thẻ bài đầu tiên
                CardData data = Deck[0];
                Deck.RemoveAt(0);
                
                // Gửi thông tin bài đến tất cả người chơi, nhưng chỉ người nhận mới thấy
                photonView.RPC("RPC_ReceiveInitialCard", RpcTarget.All, 
                    data.cardName, 
                    GetSpriteIndex(data.sprite), 
                    data.effect, 
                    player.ActorNumber,
                    Deck.Count);
                    
                // Tạm dừng một chút giữa mỗi lần phát bài để tránh quá tải network
                System.Threading.Thread.Sleep(50);
            }
        }
        
        // Cập nhật số lượng bộ bài sau khi rút
        photonView.RPC("RPC_UpdateDeckCount", RpcTarget.All, Deck.Count);
        
        Debug.Log($"Đã phát xong bài, bộ bài còn lại: {Deck.Count} lá");
    }
    
    [PunRPC]
    private void RPC_ReceiveInitialCard(string cardName, int spriteIndex, string effect, int playerActorNumber, int remainingDeckCount)
    {
        // Chỉ cập nhật deck count nếu không phải là -1 (special dealing)
        if (remainingDeckCount >= 0)
        {
            // Cập nhật số lượng bài trong deck
            if (deckCardCount != null)
            {
                deckCardCount.text = remainingDeckCount.ToString();
            }
        }
        
        // Chỉ người chơi nhận thẻ mới hiển thị thẻ của mình
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerActorNumber)
        {
            CardData cardData = new CardData
            {
                cardName = cardName,
                sprite = allCardSprites[spriteIndex],
                effect = effect
            };
            
            Debug.Log($"Nhận được lá bài ban đầu: {cardName}");
            
            // Đảm bảo cardHolder không null
            if (cardHolder != null)
            {
                cardHolder.DrawCard(cardPrefab, cardData);
            }
            else
            {
                Debug.LogError("cardHolder is null!");
            }
            
            // Thông báo GameManager để cập nhật số lượng thẻ
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayerCardCount();
            }
        }
        
        // Chỉ kiểm tra deck visual nếu không phải là special dealing
        if (remainingDeckCount >= 0)
        {
            CheckDeckVisual(remainingDeckCount);
        }
    }
    
    // Kiểm tra player có defuse card trong tay không
    public bool HasDefuseCardInHand()
    {
        if (cardHolder != null)
        {
            foreach (Card card in cardHolder.Cards)
            {
                if (card.data.effect == "Defuse")
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    // Lấy số lượng defuse card trong tay
    public int GetDefuseCardCount()
    {
        int count = 0;
        if (cardHolder != null)
        {
            foreach (Card card in cardHolder.Cards)
            {
                if (card.data.effect == "Defuse")
                {
                    count++;
                }
            }
        }
        return count;
    }
    
    // Public method to recreate deck for a specific number of players
    public void RecreateDeckForPlayers(int playerCount)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only master client can recreate deck");
            return;
        }
        
        // Clear existing deck
        Deck.Clear();
        
        // Create new deck
        CreateDeck(playerCount);
        ShuffleDeck();
        
        // Đồng bộ deck count với tất cả clients
        SyncDeckCount();
        
        Debug.Log($"Deck recreated for {playerCount} players with {Deck.Count} cards");
        LogSpriteMapping();
        LogDeckComposition();
    }

    // Debug method to show deck composition
    public void LogDeckComposition()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only master client can access deck composition");
            return;
        }
        
        Dictionary<string, int> cardCounts = new Dictionary<string, int>();
        Dictionary<string, List<int>> cardSpritesByType = new Dictionary<string, List<int>>();
        
        foreach (CardData card in Deck)
        {
            if (cardCounts.ContainsKey(card.effect))
            {
                cardCounts[card.effect]++;
            }
            else
            {
                cardCounts[card.effect] = 1;
                cardSpritesByType[card.effect] = new List<int>();
            }
            
            // Track sprite indices for each card type
            int spriteIndex = GetSpriteIndex(card.sprite);
            if (!cardSpritesByType[card.effect].Contains(spriteIndex))
            {
                cardSpritesByType[card.effect].Add(spriteIndex);
            }
        }
        
        Debug.Log("=== DECK COMPOSITION ===");
        Debug.Log($"Total cards in deck: {Deck.Count}");
        foreach (var kvp in cardCounts)
        {
            var spriteIndices = cardSpritesByType[kvp.Key];
            spriteIndices.Sort();
            string spriteInfo = string.Join(", ", spriteIndices);
            Debug.Log($"{kvp.Key}: {kvp.Value} cards (sprites: {spriteInfo})");
        }
        Debug.Log("========================");
    }
    
    // Debug method to show sprite mapping configuration
    public void LogSpriteMapping()
    {
        string[] cardNames = {
            "Exploding", "Defuse", "Attack", "Favor", "Nope",
            "Shuffle", "Skip", "SeeTheFuture", "HairyPotatoCat", 
            "BeardCat", "Cattermelon", "Tacocat", "RainbowRalphingCat"
        };
        
        Debug.Log("=== SPRITE MAPPING ===");
        for (int i = 0; i < cardNames.Length; i++)
        {
            int startSprite = cardSpriteRanges[i, 0];
            int endSprite = cardSpriteRanges[i, 1];
            int availableSprites = endSprite - startSprite + 1;
            
            Debug.Log($"{cardNames[i]}: sprites {startSprite}-{endSprite} ({availableSprites} available)");
        }
        Debug.Log("======================");
    }
    
    // Phương thức phát bài ban đầu đặc biệt: mỗi người 1 Defuse + 4 lá khác (không có Exploding)
    public void DealInitialCardsSpecial(List<Photon.Realtime.Player> players)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
            
        Debug.Log($"Đang phát bài đặc biệt cho {players.Count} người chơi (1 Defuse + 4 lá khác)");
        
        // Tạo danh sách các lá bài không phải Exploding và Defuse để phát
        List<CardData> nonExplodingNonDefuseCards = new List<CardData>();
        List<CardData> defuseCards = new List<CardData>();
        List<CardData> explodingCards = new List<CardData>();
        
        // Phân loại các lá bài
        foreach (CardData card in Deck)
        {
            if (card.effect == "Exploding")
            {
                explodingCards.Add(card);
            }
            else if (card.effect == "Defuse")
            {
                defuseCards.Add(card);
            }
            else
            {
                nonExplodingNonDefuseCards.Add(card);
            }
        }
        
        // Xáo trộn các lá bài không phải Exploding và Defuse
        nonExplodingNonDefuseCards = nonExplodingNonDefuseCards.OrderBy(a => Random.value).ToList();
        
        // Kiểm tra xem có đủ bài để phát không
        if (defuseCards.Count < players.Count)
        {
            Debug.LogError($"Không đủ lá Defuse! Cần {players.Count} lá nhưng chỉ có {defuseCards.Count} lá");
            return;
        }
        
        if (nonExplodingNonDefuseCards.Count < players.Count * 4)
        {
            Debug.LogError($"Không đủ lá bài khác! Cần {players.Count * 4} lá nhưng chỉ có {nonExplodingNonDefuseCards.Count} lá");
            return;
        }
        
        // Phát bài cho từng người chơi
        int nonExplodingIndex = 0;
        
        foreach (Photon.Realtime.Player player in players)
        {
            // Phát 1 lá Defuse
            if (defuseCards.Count > 0)
            {
                CardData defuseCard = defuseCards[0];
                defuseCards.RemoveAt(0);
                
                photonView.RPC("RPC_ReceiveInitialCard", RpcTarget.All, 
                    defuseCard.cardName, 
                    GetSpriteIndex(defuseCard.sprite), 
                    defuseCard.effect, 
                    player.ActorNumber,
                    -1); // -1 để không cập nhật deck count trong quá trình phát bài
                
                System.Threading.Thread.Sleep(50);
            }
            
            // Phát 4 lá bài khác
            for (int i = 0; i < 4; i++)
            {
                if (nonExplodingIndex < nonExplodingNonDefuseCards.Count)
                {
                    CardData card = nonExplodingNonDefuseCards[nonExplodingIndex];
                    nonExplodingIndex++;
                    
                    photonView.RPC("RPC_ReceiveInitialCard", RpcTarget.All, 
                        card.cardName, 
                        GetSpriteIndex(card.sprite), 
                        card.effect, 
                        player.ActorNumber,
                        -1); // -1 để không cập nhật deck count trong quá trình phát bài
                    
                    System.Threading.Thread.Sleep(50);
                }
            }
        }
        
        // Tạo lại deck với các lá bài còn lại
        Deck.Clear();
        
        // Thêm lại các lá Defuse còn lại
        foreach (CardData card in defuseCards)
        {
            Deck.Add(card);
        }
        
        // Thêm tất cả các lá Exploding
        foreach (CardData card in explodingCards)
        {
            Deck.Add(card);
        }
        
        // Thêm các lá bài khác còn lại
        for (int i = nonExplodingIndex; i < nonExplodingNonDefuseCards.Count; i++)
        {
            Deck.Add(nonExplodingNonDefuseCards[i]);
        }
        
        // Xáo trộn lại deck
        Deck = Deck.OrderBy(a => Random.value).ToList();
        
        // Đồng bộ deck count với tất cả clients
        SyncDeckCount();
        
        Debug.Log($"Đã phát xong bài đặc biệt. Deck còn lại: {Deck.Count} lá");
        LogDeckComposition();
    }
    
    // Debug method to check deck visual setup
    public void CheckDeckVisualSetup()
    {
        Debug.Log("=== DECK VISUAL SETUP ===");
        Debug.Log($"cardDeckVisual assigned: {cardDeckVisual != null}");
        Debug.Log($"deckCardCount assigned: {deckCardCount != null}");
        
        if (cardDeckVisual != null)
        {
            Debug.Log($"cardDeckVisual active: {cardDeckVisual.gameObject.activeSelf}");
            Debug.Log($"cardDeckVisual name: {cardDeckVisual.name}");
        }
        
        if (deckCardCount != null)
        {
            Debug.Log($"deckCardCount text: {deckCardCount.text}");
        }
        
        Debug.Log($"Current deck count: {Deck.Count}");
        Debug.Log("=========================");
    }
    
    // Public method to manually update deck visual (useful for testing)
    public void UpdateDeckVisual()
    {
        Debug.Log("Manually updating deck visual...");
        CheckDeckVisualSetup();
        CheckDeckVisual(Deck.Count);
        
        if (deckCardCount != null)
        {
            deckCardCount.text = Deck.Count.ToString();
        }
    }

    public List<int> GetTopCards(int count)
    {
        // Đảm bảo chỉ có Master Client mới có thể truy cập thông tin này
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Chỉ MasterClient mới có thể xem các lá bài trên cùng.");
            return new List<int>(); // Trả về danh sách rỗng
        }

        List<int> topCardIndexes = new List<int>();

        // Lấy 'count' lá bài đầu tiên, hoặc ít hơn nếu bộ bài không đủ
        for (int i = 0; i < count && i < Deck.Count; i++)
        {
            CardData cardData = Deck[i];
            int spriteIndex = GetSpriteIndex(cardData.sprite);
            topCardIndexes.Add(spriteIndex);
        }

        return topCardIndexes;
    }
    public CardData GetCardDataByName(string name)
    {
        return allCardData.FirstOrDefault(c => c.cardName == name);
    }

    [PunRPC]
    private void RPC_RequestShuffle()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ShuffleDeck();
        }
    }

    [PunRPC]
    private void RPC_RequestSeeTheFuture(int requestingPlayerId)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            List<int> topCardIndexes = GetTopCards(3);
            Photon.Realtime.Player requestingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(requestingPlayerId);
            if (requestingPlayer != null)
            {
                CardEffectManager.Instance.photonView.RPC("RPC_ReceiveFutureCards", requestingPlayer, (object)topCardIndexes.ToArray());
            }
        }
    }

    // Helper method để đảm bảo deck count được đồng bộ
    private void SyncDeckCount()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_UpdateDeckCount", RpcTarget.All, Deck.Count);
            Debug.Log($"[SyncDeckCount] Synced deck count: {Deck.Count}");
        }
    }
    
    // Method để manually force sync deck count (useful for debugging)
    [ContextMenu("Force Sync Deck Count")]
    public void ForceSyncDeckCount()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[ForceSyncDeckCount] Force syncing - Actual: {Deck.Count}, Synchronized: {synchronizedDeckCount}");
            SyncDeckCount();
        }
        else
        {
            Debug.Log($"[ForceSyncDeckCount] Client - Current synchronized count: {synchronizedDeckCount}");
        }
    }

    // Debug method để kiểm tra trạng thái đồng bộ deck count
    [ContextMenu("Debug Deck Count Sync")]
    public void DebugDeckCountSync()
    {
        Debug.Log("=== DECK COUNT SYNC DEBUG ===");
        Debug.Log($"Is Master Client: {PhotonNetwork.IsMasterClient}");
        
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"Actual Deck Count: {Deck.Count}");
            Debug.Log($"Synchronized Count: {synchronizedDeckCount}");
            Debug.Log($"Counts Match: {Deck.Count == synchronizedDeckCount}");
            
            if (Deck.Count != synchronizedDeckCount)
            {
                Debug.LogWarning("MISMATCH DETECTED! Consider calling ForceSyncDeckCount");
            }
        }
        else
        {
            Debug.Log($"Client - Synchronized Count: {synchronizedDeckCount}");
            Debug.Log("Note: Clients don't have access to actual deck data");
        }
        
        Debug.Log($"UI Display Count: {deckCardCount?.text ?? "NULL"}");
        Debug.Log("==============================");
    }
}
