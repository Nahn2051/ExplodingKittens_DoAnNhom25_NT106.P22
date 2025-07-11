using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System;

public class ExplodingKittenUI : MonoBehaviourPun
{
    [Header("Exploding Kitten UI")]
    [SerializeField] private GameObject explodingKittenPanel; 
    [SerializeField] private Image explodingKittenCardImage;
    [SerializeField] private GameObject defuseZone;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private GameObject positionInputPanel;
    [SerializeField] private TMP_InputField positionInputField;
    [SerializeField] private Button confirmPositionButton;
    
    private bool hasDefuseInZone = false;
    private Coroutine countdownCoroutine;
    private bool turnManagementInProgress = false; // Flag để tránh duplicate turn calls
    
    // Events để thông báo cho CardEffectManager
    public event Action<int> OnDefuseConfirmed;
    public event Action OnPlayerEliminated;
    public event Action<Card> OnDefuseCardDropped;
    
    private void Start()
    {
        if (explodingKittenPanel != null) explodingKittenPanel.SetActive(false);
        if (positionInputPanel != null) positionInputPanel.SetActive(false);
        
        if (confirmPositionButton != null)
            confirmPositionButton.onClick.AddListener(OnConfirmPositionClicked);
    }
    
    public void StartExplodingKittenSequence()
    {
        Debug.Log("Starting exploding kitten sequence!");
        
        // Đồng bộ hóa UI với tất cả người chơi qua RPC
        // Chỉ người chơi hiện tại (bị exploding) mới thấy defuse zone
        photonView.RPC("SyncStartExplodingSequence", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }
    
    [PunRPC]
    private void SyncStartExplodingSequence(int explodingPlayerActorNumber)
    {
        Debug.Log("Syncing exploding kitten sequence for all players!");
        
        hasDefuseInZone = false;
        turnManagementInProgress = false; // Reset flag khi bắt đầu sequence mới
        
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            Debug.Log("Stopped previous countdown before starting new sequence");
        }
        
        if (explodingKittenPanel != null)
        {
            explodingKittenPanel.SetActive(true);
            Debug.Log("Exploding kitten panel activated for all players");
            
            if (explodingKittenCardImage != null && CardManager.Instance != null)
            {
                for (int i = 0; i < CardManager.Instance.allCardSprites.Length; i++)
                {
                    if (CardManager.Instance.allCardSprites[i].name.Contains("Exploding"))
                    {
                        explodingKittenCardImage.sprite = CardManager.Instance.allCardSprites[i];
                        Debug.Log("Exploding card image set for all players");
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.LogError("explodingKittenPanel is null!");
        }
        
        // Chỉ hiển thị defuse zone cho người chơi bị exploding
        bool isLocalPlayerExploding = PhotonNetwork.LocalPlayer.ActorNumber == explodingPlayerActorNumber;
        if (defuseZone != null)
        {
            defuseZone.SetActive(isLocalPlayerExploding);
            
            if (isLocalPlayerExploding)
            {
                Debug.Log("Defuse zone activated for exploding player");
            }
            else
            {
                Debug.Log("Defuse zone hidden for non-exploding player");
            }
        }
        else
        {
            Debug.LogError("defuseZone is null!");
        }
        
        // Chỉ người bị exploding mới chạy countdown và update cho mọi người
        if (isLocalPlayerExploding)
        {
            countdownCoroutine = StartCoroutine(CountdownTimer(10f));
            Debug.Log("Started new 10-second countdown - only exploding player manages it");
        }
        else
        {
            Debug.Log("Non-exploding player - waiting for countdown updates from exploding player");
        }
    }
    
    private IEnumerator CountdownTimer(float duration)
    {
        float timeRemaining = duration;
        
        while (timeRemaining > 0)
        {
            // Kiểm tra xem có defuse được sử dụng không
            if (hasDefuseInZone)
            {
                Debug.Log("Defuse detected during countdown, stopping timer");
                yield break;
            }
            
            // Chỉ người bị exploding mới gửi countdown update cho tất cả
            photonView.RPC("SyncCountdownText", RpcTarget.All, timeRemaining);
                
            yield return new WaitForSeconds(0.1f);
            timeRemaining -= 0.1f;
        }
        
        if (hasDefuseInZone)
        {
            Debug.Log("[ExplodingKittenUI] Defuse was used during final check, player survives");
            yield break;
        }
        
        // Gửi countdown cuối cùng
        photonView.RPC("SyncCountdownText", RpcTarget.All, 0f);
            
        Debug.Log($"[ExplodingKittenUI] Countdown finished. hasDefuseInZone: {hasDefuseInZone}");
        
        if (!hasDefuseInZone)
        {
            Debug.Log("[ExplodingKittenUI] No defuse provided, eliminating player");
            
            // Ẩn panel và đồng bộ elimination với tất cả người chơi
            photonView.RPC("SyncHideExplodingPanel", RpcTarget.All);
            
            // Đồng bộ elimination với tất cả người chơi thay vì chỉ gọi local event
            photonView.RPC("SyncPlayerElimination", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
            
            // Backup elimination nếu cần
            StartCoroutine(BackupEliminationCheck());
        }
        else
        {
            Debug.Log("[ExplodingKittenUI] Defuse was provided, player survives");
        }
    }
    
    [PunRPC]
    private void SyncCountdownText(float timeRemaining)
    {
        if (countdownText != null)
        {
            if (timeRemaining > 0)
                countdownText.text = $"{Mathf.CeilToInt(timeRemaining)}s";
            else
                countdownText.text = "0s";
        }
    }
    
    [PunRPC]
    private void SyncPlayerElimination(int eliminatedPlayerActorNumber)
    {
        Debug.Log($"[ExplodingKittenUI] SyncPlayerElimination - Player {eliminatedPlayerActorNumber} eliminated");
        
        // Reset UI state trước khi elimination
        if (explodingKittenPanel != null)
            explodingKittenPanel.SetActive(false);
            
        if (defuseZone != null)
            defuseZone.SetActive(false);
            
        if (positionInputPanel != null)
            positionInputPanel.SetActive(false);
        
        // Reset countdown nếu đang chạy
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            Debug.Log("[ExplodingKittenUI] Stopped countdown due to player elimination");
        }
        
        // Chỉ gọi GameManager để xử lý elimination, không gọi local event nữa
        // vì GameManager sẽ handle tất cả logic elimination bao gồm cả việc thông báo CardEffectManager
        if (GameManager.Instance != null)
        {
            Debug.Log($"[ExplodingKittenUI] Calling GameManager.EliminatePlayer for player {eliminatedPlayerActorNumber}");
            GameManager.Instance.EliminatePlayer(eliminatedPlayerActorNumber);
        }
        else
        {
            Debug.LogError("[ExplodingKittenUI] GameManager.Instance is null during elimination sync!");
            // Fallback: chỉ gọi local event nếu GameManager không có
            OnPlayerEliminated?.Invoke();
        }
    }
    
    [PunRPC]
    private void SyncHideExplodingPanel()
    {
        if (explodingKittenPanel != null)
            explodingKittenPanel.SetActive(false);
            
        // Ẩn defuse zone khi panel bị ẩn
        if (defuseZone != null)
            defuseZone.SetActive(false);
    }
    
    private IEnumerator BackupEliminationCheck()
    {
        yield return new WaitForSeconds(2f);
        
        if (CardEffectManager.IsExplodingInProgress)
        {
            Debug.LogWarning("[ExplodingKittenUI] Backup elimination check: Still in exploding state, forcing elimination again");
            
            // Đồng bộ elimination với tất cả người chơi
            photonView.RPC("SyncPlayerElimination", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
    
    public void HandleDefuseCardDropped(Card defuseCard)
    {
        if (defuseCard.data.effect == "Defuse")
        {
            Debug.Log($"[ExplodingKittenUI] Defuse card {defuseCard.data.cardName} dropped in zone");
            
            // Đồng bộ việc sử dụng defuse với tất cả client
            photonView.RPC("SyncDefuseSuccess", RpcTarget.All);
            
            if (CardManager.Instance != null && CardManager.Instance.cardHolder != null)
            {
                if (CardManager.Instance.cardHolder.Cards.Contains(defuseCard))
                {
                    Debug.Log("[ExplodingKittenUI] Fallback: Removing defuse card from CardHolder");
                    CardManager.Instance.cardHolder.RemoveCard(defuseCard);
                    
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.UpdatePlayerCardCount();
                    }
                }
                else
                {
                    Debug.Log("[ExplodingKittenUI] Defuse card already removed from CardHolder");
                }
            }
            
            if (positionInputPanel != null)
            {
                positionInputPanel.SetActive(true);
                
                UpdatePlaceholderText();
            }
                
            OnDefuseCardDropped?.Invoke(defuseCard);
        }
    }
    
    [PunRPC]
    private void SyncDefuseSuccess()
    {
        hasDefuseInZone = true;
        
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            Debug.Log("[ExplodingKittenUI] Countdown stopped - defuse successful for all players");
        }
        
        if (explodingKittenPanel != null)
            explodingKittenPanel.SetActive(false);
            
        // Ẩn defuse zone cho tất cả người chơi khi defuse thành công
        if (defuseZone != null)
            defuseZone.SetActive(false);
            
        // QUAN TRỌNG: Không gọi ProcessExplodingKittenDefused ở đây nữa
        // Chỉ gọi sau khi hoàn thành position input trong OnConfirmPositionClicked
        Debug.Log("[ExplodingKittenUI] Defuse successful, waiting for position input to complete turn management");
    }
    
    private IEnumerator EnsureUIRestoration()
    {
        yield return new WaitForSeconds(1f); // Tăng delay để đảm bảo
        
        // Safety check: Đảm bảo UI interactions được khôi phục
        if (GameManager.Instance != null && !CardEffectManager.IsExplodingInProgress)
        {
            Debug.Log("[ExplodingKittenUI] Safety check - ensuring UI interactions are restored");
            GameManager.Instance.EnablePlayerInteractions();
        }
        else if (GameManager.Instance != null)
        {
            // Force reset exploding state nếu vẫn còn
            GameManager.Instance.SetExplodingInProgress(false);
            GameManager.Instance.EnablePlayerInteractions();
            Debug.Log("[ExplodingKittenUI] Force reset exploding state and enable UI");
        }
        
        // Backup: Ẩn tất cả UI panels có thể còn hiển thị
        if (positionInputPanel != null && positionInputPanel.activeInHierarchy)
        {
            positionInputPanel.SetActive(false);
            Debug.Log("[ExplodingKittenUI] Backup: Hidden position input panel");
        }
        
        if (explodingKittenPanel != null && explodingKittenPanel.activeInHierarchy)
        {
            explodingKittenPanel.SetActive(false);
            Debug.Log("[ExplodingKittenUI] Backup: Hidden exploding kitten panel");
        }
    }
    
    private void OnConfirmPositionClicked()
    {
        if (positionInputField != null)
        {
            string input = positionInputField.text;
            if (int.TryParse(input, out int position))
            {
                int deckCount = CardManager.Instance.GetDeckCount();
                if (position >= 1 && position <= deckCount + 1)
                {
                    positionInputPanel.SetActive(false);
                    
                    // Trigger defuse confirmed event
                    OnDefuseConfirmed?.Invoke(position - 1);
                    
                    // QUAN TRỌNG: Chỉ sau khi hoàn thành position input mới xử lý turn management
                    Debug.Log("[ExplodingKittenUI] Position input completed, now processing turn management");
                    
                    // Delay nhỏ để đảm bảo exploding card được add vào deck trước
                    StartCoroutine(ProcessTurnAfterPositionInput());
                }
                else
                {
                    Debug.LogWarning($"Invalid position! Please enter from 1 to {deckCount + 1}");
                }
            }
            else
            {
                Debug.LogWarning("Please enter a valid number!");
            }
        }
    }
    
    private IEnumerator ProcessTurnAfterPositionInput()
    {
        // Kiểm tra xem đã có turn management process nào đang chạy chưa
        if (turnManagementInProgress)
        {
            Debug.LogWarning("[ExplodingKittenUI] Turn management already in progress, skipping duplicate call");
            yield break;
        }
        
        turnManagementInProgress = true;
        Debug.Log("[ExplodingKittenUI] Starting turn management process");
        
        // Chờ một chút để đảm bảo exploding card được add vào deck
        yield return new WaitForSeconds(0.5f);
        
        // QUAN TRỌNG: Chỉ Master Client xử lý turn management để tránh duplicate
        if (PhotonNetwork.IsMasterClient && GameManager.Instance != null)
        {
            Debug.Log("[ExplodingKittenUI] Master client processing turn management after position input");
            
            // Lấy exploding player ID - sử dụng current player làm exploding player
            int explodingPlayerIdForTurnTransition = PhotonNetwork.LocalPlayer.ActorNumber;
            
            // Tìm index của người chơi đã rút exploding card trong playerList
            int explodingPlayerIndex = -1;
            for (int i = 0; i < GameManager.Instance.playerList.Count; i++)
            {
                if (GameManager.Instance.playerList[i].ActorNumber == explodingPlayerIdForTurnTransition)
                {
                    explodingPlayerIndex = i;
                    break;
                }
            }
            
            // Nếu không tìm thấy exploding player, sử dụng current turn index làm fallback
            int currentPlayerIndex = (explodingPlayerIndex >= 0) ? explodingPlayerIndex : GameManager.Instance.GetCurrentTurnIndex();
            int nextPlayerIndex = GameManager.Instance.GetNextAlivePlayerIndex(currentPlayerIndex);
            
            Debug.Log($"[ExplodingKittenUI] Defuse successful: explodingPlayerIndex={explodingPlayerIndex}, turn passing from player index {currentPlayerIndex} to player index {nextPlayerIndex}");
            
            // Đảm bảo rằng next player không phải là chính player đã defuse (trừ khi chỉ có 1 player còn lại)
            if (nextPlayerIndex == currentPlayerIndex && GameManager.Instance.playerList.Count > 1)
            {
                Debug.LogWarning("[ExplodingKittenUI] Next player is same as current player, this shouldn't happen unless only 1 player remains");
                // Thử tìm next player khác một lần nữa
                nextPlayerIndex = GameManager.Instance.GetNextAlivePlayerIndex(nextPlayerIndex);
            }
            
            // Chuyển lượt với 1 turn bình thường (không còn attack turns)
            // Sau khi exploding được defuse, turn hiện tại kết thúc và chuyển sang người tiếp theo
            GameManager.Instance.StartTurn(nextPlayerIndex, 1);
            
            // UI interactions sẽ được enable bởi RPC_StartTurn với delay
            Debug.Log("[ExplodingKittenUI] Turn changed, UI will be enabled by RPC_StartTurn");
        }
        else if (!PhotonNetwork.IsMasterClient && GameManager.Instance != null)
        {
            // Non-master clients yêu cầu Master Client xử lý turn management
            Debug.Log("[ExplodingKittenUI] Non-master client requesting turn management from Master Client");
            
            // Lấy current player index và tìm next player
            int currentPlayerIndex = GameManager.Instance.GetCurrentTurnIndex();
            int nextPlayerIndex = GameManager.Instance.GetNextAlivePlayerIndex(currentPlayerIndex);
            
            Debug.Log($"[ExplodingKittenUI] Non-master client requesting turn change from {currentPlayerIndex} to {nextPlayerIndex}");
            
            // Yêu cầu Master Client chuyển lượt
            photonView.RPC("RPC_RequestTurnChangeAfterDefuse", RpcTarget.MasterClient, nextPlayerIndex);
            
            Debug.Log($"[ExplodingKittenUI] Sent RPC_RequestTurnChangeAfterDefuse to Master Client for player {nextPlayerIndex}");
            
            // UI interactions sẽ được enable bởi RPC_StartTurn với delay  
            Debug.Log("[ExplodingKittenUI] UI will be enabled by RPC_StartTurn after turn change");
        }
        else
        {
            // Fallback: nếu GameManager không có, enable UI trực tiếp
            Debug.LogWarning("[ExplodingKittenUI] GameManager not found, enabling UI directly");
            StartCoroutine(EnsureUIRestoration());
        }
        
        // Reset flag sau khi hoàn thành
        turnManagementInProgress = false;
        Debug.Log("[ExplodingKittenUI] Turn management process completed");
    }
    
    public void HideExplodingPanel()
    {
        // Đồng bộ hóa việc ẩn panel cho tất cả người chơi
        photonView.RPC("SyncHideExplodingPanelWithCountdown", RpcTarget.All);
    }
    
    [PunRPC]
    private void SyncHideExplodingPanelWithCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            Debug.Log("[ExplodingKittenUI] Countdown stopped due to panel hide");
        }
        
        if (explodingKittenPanel != null)
            explodingKittenPanel.SetActive(false);
            
        // Ẩn defuse zone khi panel bị ẩn
        if (defuseZone != null)
            defuseZone.SetActive(false);
    }
    
    [PunRPC]
    private void SyncExplodingCardImage(string spriteName)
    {
        if (explodingKittenCardImage != null && CardManager.Instance != null)
        {
            for (int i = 0; i < CardManager.Instance.allCardSprites.Length; i++)
            {
                if (CardManager.Instance.allCardSprites[i].name == spriteName)
                {
                    explodingKittenCardImage.sprite = CardManager.Instance.allCardSprites[i];
                    Debug.Log($"Synced exploding card image: {spriteName} for all players");
                    break;
                }
            }
        }
    }
    
    public void HidePositionInputPanel()
    {
        if (positionInputPanel != null)
        {
            positionInputPanel.SetActive(false);
            
            if (positionInputField != null)
            {
                positionInputField.text = "";
            }
        }
    }
    
    public void ForcePlayerElimination()
    {
        Debug.Log("Force eliminating player due to exploding without defuse");
        
        // Đồng bộ elimination với tất cả người chơi
        photonView.RPC("SyncPlayerElimination", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }
    
    public bool IsCountdownActive()
    {
        return countdownCoroutine != null;
    }
    
    public void TestElimination()
    {
        Debug.Log("Test elimination triggered");
        OnPlayerEliminated?.Invoke();
    }
    
    private void UpdatePlaceholderText()
    {
        if (positionInputField != null && positionInputField.placeholder != null && CardManager.Instance != null)
        {
            try
            {
                var placeholderText = positionInputField.placeholder.GetComponent<TMP_Text>();
                if (placeholderText != null)
                {
                    int deckCount = CardManager.Instance.GetDeckCount();
                    placeholderText.text = $"Enter position (1-{deckCount + 1})";
                    Debug.Log($"Updated placeholder text: Enter position (1-{deckCount + 1})");
                }
                else
                {
                    Debug.LogWarning("Placeholder TMP_Text component not found!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating placeholder text: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Cannot update placeholder - missing components!");
        }
    }
    
    [PunRPC]
    private void RPC_RequestTurnChangeAfterDefuse(int nextPlayerIndex)
    {
        // Chỉ Master Client xử lý yêu cầu chuyển lượt
        if (PhotonNetwork.IsMasterClient && GameManager.Instance != null)
        {
            Debug.Log($"[ExplodingKittenUI] Master client received request to change turn to player {nextPlayerIndex} after defuse");
            GameManager.Instance.StartTurn(nextPlayerIndex, 1);
            Debug.Log($"[ExplodingKittenUI] Master client successfully processed turn change to player {nextPlayerIndex}");
        }
        else if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[ExplodingKittenUI] Non-master client received RPC_RequestTurnChangeAfterDefuse - this should not happen");
        }
        else
        {
            Debug.LogError("[ExplodingKittenUI] GameManager.Instance is null when processing turn change request");
        }
    }
}
