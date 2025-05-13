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
    
    [Header("Game Settings")]
    [SerializeField] private int initialCardCount = 5;
    
    [Header("UI References")]
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private Transform playerSlotsContainer;
    [SerializeField] private GameObject drawCardButton;
    [SerializeField] private Button drawCardButtonComponent;
    [SerializeField] private Color activePlayerColor = Color.green;
    [SerializeField] private Color inactivePlayerColor = Color.white;
    
    [Header("Game State")]
    [SerializeField] private int currentTurnIndex = 0;
    [SerializeField] private List<Player> playerList = new List<Player>();
    private int localPlayerIndex = -1;
    private List<PlayerSlot> playerSlots = new List<PlayerSlot>();
    
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
    }
    
    public int GetCurrentTurnIndex()
    {
        return currentTurnIndex;
    }
    
    public bool IsLocalPlayerTurn()
    {
        return currentTurnIndex == localPlayerIndex;
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
        
        // Nếu là master client, bắt đầu game và xáo bài
        if (PhotonNetwork.IsMasterClient)
        {
            // Xáo bộ bài
            photonView.RPC("RPC_ShuffleDeck", RpcTarget.AllBuffered);
            
            // Chia bài cho tất cả người chơi
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
        
        for (int i = 0; i < playerList.Count; i++)
        {
            if (i != localPlayerIndex) // Không tạo slot cho người chơi local
            {
                Player player = playerList[i];
                
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
    }
    
    [PunRPC]
    private void RPC_ShuffleDeck()
    {
        // Kiểm tra trường hợp đã có CardManager trong scene
        if (CardManager.Instance != null)
        {
            Debug.Log("Xáo bài ở đầu game");
        }
    }
    
    [PunRPC]
    private void RPC_DealInitialCards()
    {
        Debug.Log("Phát " + initialCardCount + " lá bài cho mỗi người chơi");
        
        // Chỉ có master client phát bài để đồng bộ
        if (PhotonNetwork.IsMasterClient && CardManager.Instance != null)
        {
            // Kiểm tra xem bộ bài có đủ bài để phát không
            int totalCardsNeeded = playerList.Count * initialCardCount;
            if (CardManager.Instance.GetDeckCount() < totalCardsNeeded)
            {
                Debug.LogError($"Không đủ bài để phát! Cần {totalCardsNeeded} lá nhưng chỉ có {CardManager.Instance.GetDeckCount()} lá.");
                return;
            }
            
            // Thực hiện phát bài ban đầu
            CardManager.Instance.DealInitialCards(playerList, initialCardCount);
        }
    }
    
    public void StartTurn(int playerIndex)
    {
        currentTurnIndex = playerIndex;
        photonView.RPC("RPC_StartTurn", RpcTarget.All, playerIndex);
    }
    
    [PunRPC]
    private void RPC_StartTurn(int playerIndex)
    {
        Debug.Log("Bắt đầu lượt chơi của người chơi index: " + playerIndex);
        
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
    
    public void OnDrawCardButtonClicked()
    {
        // Kiểm tra có phải lượt của người chơi hiện tại không
        if (currentTurnIndex == localPlayerIndex)
        {
            // Rút bài
            photonView.RPC("RPC_PlayerDrawCard", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
            
            // Chuyển lượt sang người tiếp theo
            int nextPlayerIndex = (currentTurnIndex + 1) % playerList.Count;
            StartTurn(nextPlayerIndex);
        }
    }
    
    [PunRPC]
    private void RPC_PlayerDrawCard(int playerActorNumber)
    {
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
} 