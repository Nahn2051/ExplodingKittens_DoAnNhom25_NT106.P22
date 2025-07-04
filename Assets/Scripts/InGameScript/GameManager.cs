using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System;
using UnityEngine.UI;
using UnityEngine.Audio;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public AudioMixer MainAudioMixer;
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
    private int lastPlayerDrawCardIndex = -1;

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
        float vol = PlayerPrefs.GetFloat("MusicVol", 0.75f); // Giá trị mặc định 0.75
        MainAudioMixer.SetFloat("MusicVol", vol);
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
    
    public void OnDrawCardButtonClicked()
    {
        if (IsLocalPlayerTurn())
        {
            drawCardButtonComponent.interactable = false;
            CardManager.Instance.PhotonView.RPC("RPC_RequestDrawCard", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);

            // Sau khi yêu cầu rút bài, đợi server phản hồi và không làm gì thêm ở đây.
            // Việc chuyển lượt sẽ được xử lý sau khi rút bài xong.
            StartCoroutine(ProcessTurnAfterDrawing());
        }
    }

    private IEnumerator ProcessTurnAfterDrawing()
    {
        // Chờ một chút để RPC rút bài có thời gian thực hiện
        yield return new WaitForSeconds(0.5f);

        attackTurns--; // Giảm số lượt tấn công còn lại

        if (attackTurns > 0)
        {
            // Nếu vẫn còn lượt tấn công, không chuyển người, chỉ reset lượt của chính mình
            photonView.RPC("RPC_StartTurn", RpcTarget.All, currentTurnIndex, attackTurns);
        }
        else // Hết lượt, chuyển cho người tiếp theo
        {
            // Yêu cầu Master Client chuyển lượt cho người chơi tiếp theo với 1 lượt bình thường
            int nextPlayerIndex = (currentTurnIndex + 1) % playerList.Count;
            photonView.RPC("RPC_RequestStartTurn", RpcTarget.MasterClient, nextPlayerIndex, 1);
        }
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

        // Yêu cầu bắt đầu lượt cho người chơi tiếp theo với số lượt bị dồn
        int nextPlayerIndex = (currentTurnIndex + 1) % playerList.Count;
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
            int nextPlayerIndex = (currentTurnIndex + 1) % playerList.Count;
            photonView.RPC("RPC_RequestStartTurn", RpcTarget.MasterClient, nextPlayerIndex, 1);
        }
    }
} 