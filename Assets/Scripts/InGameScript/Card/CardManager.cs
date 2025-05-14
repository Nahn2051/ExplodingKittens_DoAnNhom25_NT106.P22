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
    private int[] cardQuantities = { 4, 6, 4, 4, 5, 4, 4, 5, 5 };
    private PhotonView photonView;
    
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
            CreateDeck();
            ShuffleDeck();
        }
    }

    private void Start()
    {
        // Đồng bộ số lượng thẻ bài
        if (PhotonNetwork.IsMasterClient && Deck.Count > 0)
        {
            photonView.RPC("RPC_UpdateDeckCount", RpcTarget.All, Deck.Count);
        }
    }

    private void CreateDeck()
    {
        int count = 0;
        int index = 0;
        AddCards("Exploding", ref count, cardQuantities[index++]);
        AddCards("Defuse", ref count, cardQuantities[index++]);
        AddCards("Attack", ref count, cardQuantities[index++]);
        AddCards("Favor", ref count, cardQuantities[index++]);
        AddCards("Nope", ref count, cardQuantities[index++]);
        AddCards("Shuffle", ref count, cardQuantities[index++]);
        AddCards("Skip", ref count, cardQuantities[index++]);
        AddCards("SeeTheFuture", ref count, cardQuantities[index++]);

        for (int i = 0; i < cardQuantities[index]; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                AddCard($"Normal{i+1}", count + i, j + 1);
            }
        }
    }

    private void AddCards(string name, ref int count, int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            AddCard(name, i + count, i + 1);
        }
        count += quantity;
    }

    private void AddCard(string name, int spriteIndex, int index)
    {
        if (spriteIndex >= allCardSprites.Length)
        {
            Debug.LogWarning("Sprite index out of range for card: " + name);
            return;
        }
        CardData data = new CardData
        {
            cardName = $"{name}_{index}",
            sprite = allCardSprites[spriteIndex],
            effect = name,
        };
        Deck.Add(data);
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
        if (deckCardCount != null)
        {
            deckCardCount.text = count.ToString();
        }
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
                Debug.LogWarning("Bộ bài đã hết!");
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
            
            cardHolder.DrawCard(cardPrefab, cardData);
            
            // Thông báo GameManager để cập nhật số lượng thẻ
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayerCardCount();
            }
        }
        
        // Kiểm tra xem deck có còn bài không
        CheckDeckVisual(remainingDeckCount);
    }
    
    // Lấy index của sprite trong mảng allCardSprites
    private int GetSpriteIndex(Sprite sprite)
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
        if (count == 0)
        {
            Debug.Log("Deck is empty");
            cardDeckVisual.gameObject.SetActive(false);
            return;
        }
    }
    
    // Phương thức này để player chơi thẻ bài từ tay vào khu vực chơi
    // Lưu ý: Đánh bài không tự động chuyển lượt - chỉ có rút bài mới chuyển lượt
    public void PlayCard(Card card, int playerActorNumber)
    {
        // Kiểm tra xem có phải lượt của người chơi không
        if (GameManager.Instance != null && GameManager.Instance.IsLocalPlayerTurn())
        {
            // Gửi RPC để tất cả người chơi đều thấy thẻ bài được chơi
            photonView.RPC("RPC_PlayCard", RpcTarget.All, 
                card.data.cardName, 
                GetSpriteIndex(card.data.sprite), 
                card.data.effect, 
                playerActorNumber);
            
            // Gọi CardEffectManager để xử lý hiệu ứng
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.ActivateCardEffect(card.data.effect, 0);
            }
            
            // Cập nhật số lượng thẻ bài
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayerCardCount();
            }
        }
        else
        {
            Debug.Log("Không thể chơi bài - chưa đến lượt của bạn!");
        }
    }
    
    [PunRPC]
    private void RPC_PlayCard(string cardName, int spriteIndex, string effect, int playerActorNumber)
    {
        Debug.Log($"Người chơi {playerActorNumber} đã chơi thẻ {cardName}");
        
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
        return Deck.Count;
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
                    Debug.LogWarning("Bộ bài đã hết trong quá trình phát!");
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
        // Cập nhật số lượng bài trong deck
        if (deckCardCount != null)
        {
            deckCardCount.text = remainingDeckCount.ToString();
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
        
        // Kiểm tra xem deck có còn bài không
        CheckDeckVisual(remainingDeckCount);
    }
}
