using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class NormalCardComboUI : MonoBehaviourPunCallbacks
{
    [Header("Normal Card Combo UI")]
    [SerializeField] private GameObject playerSelectionPanel;
    [SerializeField] private GameObject cardSelectionPanel;
    [SerializeField] private Transform playerButtonContainer;
    [SerializeField] private Transform cardButtonContainer;
    [SerializeField] private Button playerButtonPrefab;
    [SerializeField] private Button cardButtonPrefab;
    [SerializeField] private TMP_Text comboDescriptionText;
    
    // Biến cho Normal Card Combo
    private List<Card> pendingComboCards = new List<Card>();
    private int selectedTargetPlayer = -1;
    private string selectedCardType = "";
    
    // Events
    public System.Action<int, int> OnTwoCardComboExecuted;
    public System.Action<int, int, string> OnThreeCardComboExecuted;
    
    private void Start()
    {
        // Ẩn tất cả UI panels ban đầu
        if (playerSelectionPanel != null) 
        {
            playerSelectionPanel.SetActive(false);
            Debug.Log("NormalCardComboUI: playerSelectionPanel disabled");
        }
        if (cardSelectionPanel != null) 
        {
            cardSelectionPanel.SetActive(false);
            Debug.Log("NormalCardComboUI: cardSelectionPanel disabled");
        }
        
        // Đảm bảo text component không tự động resize
        if (comboDescriptionText != null)
        {
            comboDescriptionText.enableAutoSizing = false;
            Debug.Log("NormalCardComboUI: Auto-sizing disabled for combo description text");
        }
    }
    
    // Method to show helpful message when player tries to play normal card individually
    public void ShowComboHelpMessage()
    {
        if (comboDescriptionText != null)
        {
            comboDescriptionText.text = "Normal cards must be played in combos! Click 2-3 cards of the same type to select them for combo.";
            // Show message for a few seconds then hide
            StartCoroutine(HideHelpMessageAfterDelay(5f));
        }
    }
    
    // Method to show current combo selection status
    public void ShowComboSelectionStatus(int selectedCount, string cardType)
    {
        if (comboDescriptionText != null)
        {
            if (selectedCount == 0)
            {
                comboDescriptionText.text = "Click normal cards to select them for combo (2-3 cards of same type)";
            }
            else if (selectedCount == 1)
            {
                comboDescriptionText.text = $"Selected 1 {cardType} card. Select 1-2 more {cardType} cards to create combo.";
            }
            else if (selectedCount == 2)
            {
                comboDescriptionText.text = $"2-card combo ready! Auto-executing in 1 second or select 1 more {cardType} for 3-card combo.";
            }
            else if (selectedCount == 3)
            {
                comboDescriptionText.text = $"3-card combo ready! Auto-executing in 1 second.";
            }
        }
    }
    
    private IEnumerator HideHelpMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (comboDescriptionText != null && pendingComboCards.Count == 0)
        {
            comboDescriptionText.text = "";
        }
    }
    
    // Public method để xử lý combo normal cards
    public void HandleNormalCardCombo(List<Card> comboCards)
    {
        Debug.Log($"NormalCardComboUI.HandleNormalCardCombo: Received combo with {comboCards.Count} cards");
        
        if (comboCards.Count < 2 || comboCards.Count > 3)
        {
            Debug.LogWarning("Combo must have 2 or 3 cards!");
            return;
        }
        
        // Kiểm tra tất cả các lá bài có cùng loại không
        string cardType = comboCards[0].data.effect;
        Debug.Log($"NormalCardComboUI: Checking combo of type {cardType}");
        
        foreach (Card card in comboCards)
        {
            if (card.data.effect != cardType)
            {
                Debug.LogWarning($"All cards in combo must be of the same type! Found {card.data.effect} in {cardType} combo");
                return;
            }
        }
        
        // Kiểm tra có phải normal card không
        if (!IsNormalCard(cardType))
        {
            Debug.LogWarning($"Only Normal cards can create combos! {cardType} is not a normal card");
            return;
        }
        
        // Lưu combo để xử lý
        pendingComboCards = new List<Card>(comboCards);
        Debug.Log($"NormalCardComboUI: Stored {pendingComboCards.Count} cards for {cardType} combo");
        
        if (comboCards.Count == 2)
        {
            // Combo 2 lá: delay để cho phép Nope trước khi hiển thị UI
            Debug.Log($"NormalCardComboUI: Starting 2-card combo process for {cardType}");
            StartCoroutine(DelayedTwoCardCombo(cardType, 3f));
        }
        else if (comboCards.Count == 3)
        {
            // Combo 3 lá: delay để cho phép Nope trước khi hiển thị UI
            Debug.Log($"NormalCardComboUI: Starting 3-card combo process for {cardType}");
            StartCoroutine(DelayedThreeCardCombo(cardType, 3f));
        }
    }
    
    private IEnumerator DelayedTwoCardCombo(string cardType, float delay)
    {
        Debug.Log($"NormalCardComboUI: Starting 2-card combo delay ({delay}s) for {cardType}");
        yield return new WaitForSeconds(delay);
        
        // Kiểm tra xem combo có bị Nope không
        if (pendingComboCards.Count > 0) // Nếu combo chưa bị reset bởi Nope
        {
            Debug.Log($"NormalCardComboUI: Delay completed, showing 2-card combo UI for {cardType}");
            StartTwoCardCombo(cardType);
        }
        else
        {
            Debug.Log("NormalCardComboUI: 2-card combo was cancelled (likely by Nope)");
        }
    }
    
    private IEnumerator DelayedThreeCardCombo(string cardType, float delay)
    {
        Debug.Log($"NormalCardComboUI: Starting 3-card combo delay ({delay}s) for {cardType}");
        yield return new WaitForSeconds(delay);
        
        // Kiểm tra xem combo có bị Nope không
        if (pendingComboCards.Count > 0) // Nếu combo chưa bị reset bởi Nope
        {
            Debug.Log($"NormalCardComboUI: Delay completed, showing 3-card combo UI for {cardType}");
            StartThreeCardCombo(cardType);
        }
        else
        {
            Debug.Log("NormalCardComboUI: 3-card combo was cancelled (likely by Nope)");
        }
    }
    
    private bool IsNormalCard(string cardType)
    {
        return cardType == "HairyPotatoCat" || cardType == "BeardCat" || 
               cardType == "Cattermelon" || cardType == "Tacocat" || 
               cardType == "RainbowRalphingCat";
    }
    
    private void StartTwoCardCombo(string cardType)
    {
        if (comboDescriptionText != null)
        {
            comboDescriptionText.text = $"2-Card Combo ({cardType}): Select a player to steal 1 random card";
            // Đảm bảo text không bị tự động resize
            comboDescriptionText.enableAutoSizing = false;
        }
        
        ShowPlayerSelection();
    }
    
    private void StartThreeCardCombo(string cardType)
    {
        if (comboDescriptionText != null)
        {
            comboDescriptionText.text = $"3-Card Combo ({cardType}): Select a player to steal a specific card";
            // Đảm bảo text không bị tự động resize
            comboDescriptionText.enableAutoSizing = false;
        }
        
        ShowPlayerSelection();
    }
    
    private void ShowPlayerSelection()
    {
        Debug.Log("NormalCardComboUI.ShowPlayerSelection: About to show player selection panel");
        
        if (playerSelectionPanel != null)
        {
            playerSelectionPanel.SetActive(true);
            Debug.Log("NormalCardComboUI: playerSelectionPanel activated - this might block UI interaction!");
        }
        else
        {
            Debug.LogWarning("NormalCardComboUI: playerSelectionPanel is null!");
        }
        
        // Xóa các button cũ
        foreach (Transform child in playerButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Tạo button cho mỗi người chơi (trừ local player)
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                Button playerBtn = Instantiate(playerButtonPrefab, playerButtonContainer);
                playerBtn.GetComponentInChildren<TMP_Text>().text = player.NickName;
                int playerId = player.ActorNumber;
                playerBtn.onClick.AddListener(() => OnPlayerSelected(playerId));
            }
        }
    }
    
    private void OnPlayerSelected(int playerId)
    {
        selectedTargetPlayer = playerId;
        
        if (pendingComboCards.Count == 2)
        {
            // Combo 2 lá: thực hiện ngay lập tức
            ExecuteTwoCardCombo();
        }
        else if (pendingComboCards.Count == 3)
        {
            // Combo 3 lá: hiển thị menu chọn loại bài
            ShowCardTypeSelection();
        }
    }
    
    private void ShowCardTypeSelection()
    {
        if (playerSelectionPanel != null)
        {
            playerSelectionPanel.SetActive(false);
        }
        
        if (cardSelectionPanel != null)
        {
            cardSelectionPanel.SetActive(true);
        }
        
        // Xóa các button cũ
        foreach (Transform child in cardButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Danh sách các loại bài có thể chọn
        string[] cardTypes = {"Favor", "Nope", "Shuffle", "Skip", "SeeTheFuture", "Defuse",
                              "HairyPotatoCat", "BeardCat", "Cattermelon", "Tacocat", "RainbowRalphingCat"};
        
        foreach (string cardType in cardTypes)
        {
            Button cardBtn = Instantiate(cardButtonPrefab, cardButtonContainer);
            cardBtn.GetComponentInChildren<TMP_Text>().text = cardType;
            string type = cardType; // Capture for lambda
            cardBtn.onClick.AddListener(() => OnCardTypeSelected(type));
        }
    }
    
    private void OnCardTypeSelected(string cardType)
    {
        selectedCardType = cardType;
        
        // Automatically execute three card combo after selection
        ExecuteThreeCardCombo();
    }
    
    private void ResetComboUI()
    {
        // Reset và ẩn UI
        pendingComboCards.Clear();
        selectedTargetPlayer = -1;
        selectedCardType = "";
        
        if (playerSelectionPanel != null)
            playerSelectionPanel.SetActive(false);
        if (cardSelectionPanel != null)
            cardSelectionPanel.SetActive(false);
    }
    
    private void ExecuteTwoCardCombo()
    {
        Debug.Log($"NormalCardComboUI.ExecuteTwoCardCombo: Executing 2-card combo for player {PhotonNetwork.LocalPlayer.ActorNumber} targeting player {selectedTargetPlayer}");
        
        // Trigger event cho CardEffectManager
        OnTwoCardComboExecuted?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber, selectedTargetPlayer);
        
        // Reset UI
        ResetComboUI();
    }
    
    private void ExecuteThreeCardCombo()
    {
        Debug.Log($"NormalCardComboUI.ExecuteThreeCardCombo: Executing 3-card combo for player {PhotonNetwork.LocalPlayer.ActorNumber} targeting player {selectedTargetPlayer} for card type {selectedCardType}");
        
        // Trigger event cho CardEffectManager
        OnThreeCardComboExecuted?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber, selectedTargetPlayer, selectedCardType);
        
        // Reset UI
        ResetComboUI();
    }
    
    // Method to cancel pending combo (called when Nope is used)
    public void CancelPendingCombo()
    {
        Debug.Log("NormalCardComboUI: Pending combo cancelled by Nope");
        StopAllCoroutines(); // Stop any delayed combo coroutines
        ResetComboUI();
    }
    
    // Method to check if there's a pending combo
    public bool HasPendingCombo()
    {
        return pendingComboCards.Count > 0;
    }
    
    public void HideAllPanels()
    {
        ResetComboUI();
    }
}
